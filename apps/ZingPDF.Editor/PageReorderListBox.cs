using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace ZingPDF.Editor;

/// <summary>Single-page reordering in the wrapped thumbnail overview.</summary>
public sealed class PageReorderListBox : ListBox
{
    private readonly DispatcherTimer _scrollTimer = new() { Interval = TimeSpan.FromMilliseconds(40) };
    private ListBoxItem? _source;
    private IPointer? _pointer;
    private Point _start;
    private Point _lastPoint;
    private bool _dragging;
    private int? _insertion;
    private Rect? _marker;
    public event Action<int, int>? PageMoveRequested;
    protected override Type StyleKeyOverride => typeof(ListBox);

    public PageReorderListBox()
    {
        Focusable = true;
        ClipToBounds = true;
        // Intercept before ListBoxItem selects/renders a different page on mouse-down.
        AddHandler(PointerPressedEvent, Pressed, RoutingStrategies.Tunnel);
        AddHandler(PointerMovedEvent, Moved, RoutingStrategies.Tunnel);
        AddHandler(PointerReleasedEvent, Released, RoutingStrategies.Tunnel);
        _scrollTimer.Tick += (_, _) => ScrollAtEdge();
        DetachedFromVisualTree += (_, _) => CancelDrag();
    }

    private IEnumerable<(ListBoxItem Item, Rect Bounds)> Cards()
    {
        foreach (var item in this.GetVisualDescendants().OfType<ListBoxItem>())
            if (item.DataContext is PageItem && item.TranslatePoint(default, this) is { } point)
                yield return (item, new Rect(point, item.Bounds.Size));
    }

    private void Pressed(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is not MainViewModel { IsReady: true, IsPagesMode: true } || !e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) return;
        var point = e.GetPosition(this);
        var card = Cards().FirstOrDefault(c => c.Bounds.Contains(point));
        if (card.Item is null) return;
        Focus();
        _source = card.Item; _pointer = e.Pointer; _start = _lastPoint = point;
        e.Pointer.Capture(this);
        e.Handled = true;
    }

    private void Moved(object? sender, PointerEventArgs e)
    {
        if (_source is null)
        {
            Cursor = new Cursor(Cards().Any(c => c.Bounds.Contains(e.GetPosition(this))) ? StandardCursorType.Hand : StandardCursorType.Arrow);
            return;
        }
        _lastPoint = e.GetPosition(this);
        if (!_dragging && Math.Pow(_lastPoint.X - _start.X, 2) + Math.Pow(_lastPoint.Y - _start.Y, 2) >= 36)
        {
            _dragging = true;
            _source.Opacity = 0.5;
            Cursor = new Cursor(StandardCursorType.SizeAll);
            _scrollTimer.Start();
        }
        if (_dragging) UpdateDrop();
        e.Handled = true;
    }

    private void UpdateDrop()
    {
        _insertion = null; _marker = null;
        if (new Rect(Bounds.Size).Contains(_lastPoint))
        {
            // Distance to rectangles works across row breaks and the gaps between cards.
            var cards = Cards().OrderBy(c => ((PageItem)c.Item.DataContext!).Number).ToArray();
            var card = cards.OrderBy(c => DistanceSquared(c.Bounds, _lastPoint)).FirstOrDefault();
            if (card.Item?.DataContext is PageItem page)
            {
                var after = _lastPoint.X >= card.Bounds.Center.X;
                _insertion = page.Number - 1 + (after ? 1 : 0);
                // Resolve the insertion boundary once, rather than drawing against whichever
                // card the pointer is nearest. Both sides of a gap must yield the same line.
                var boundary = _insertion.Value;
                var next = cards.FirstOrDefault(c => ((PageItem)c.Item.DataContext!).Number == boundary + 1);
                var previous = cards.FirstOrDefault(c => ((PageItem)c.Item.DataContext!).Number == boundary);
                var anchor = next.Item is not null ? next : previous;
                double x;
                if (previous.Item is not null && next.Item is not null && Math.Abs(previous.Bounds.Top - next.Bounds.Top) < 1)
                    x = (previous.Bounds.Right + next.Bounds.Left) / 2;
                else
                    // At a row break use the start of the next row consistently.
                    x = next.Item is not null ? anchor.Bounds.Left - anchor.Item!.Margin.Left : anchor.Bounds.Right + anchor.Item!.Margin.Right;
                _marker = new Rect(x - 2, anchor.Bounds.Top + 8, 4, Math.Max(0, anchor.Bounds.Height - 16));
            }
        }
        InvalidateVisual();
    }

    private static double DistanceSquared(Rect rect, Point point)
    {
        var dx = Math.Max(0, Math.Max(rect.Left - point.X, point.X - rect.Right));
        var dy = Math.Max(0, Math.Max(rect.Top - point.Y, point.Y - rect.Bottom));
        return dx * dx + dy * dy;
    }

    private void Released(object? sender, PointerReleasedEventArgs e)
    {
        if (_source?.DataContext is not PageItem page) return;
        if (_dragging) { _lastPoint = e.GetPosition(this); UpdateDrop(); }
        var dragging = _dragging;
        var insertion = _insertion;
        CancelDrag();
        e.Handled = true;
        if (!dragging) { SelectedItem = page; return; }
        if (insertion is not { } boundary) return;
        var to = boundary > page.Number - 1 ? boundary : boundary + 1;
        if (to != page.Number) PageMoveRequested?.Invoke(page.Number, to);
    }

    private void ScrollAtEdge()
    {
        if (!_dragging || !new Rect(Bounds.Size).Contains(_lastPoint)) return;
        var viewer = this.GetVisualDescendants().OfType<ScrollViewer>().FirstOrDefault();
        if (viewer is null) return;
        var dy = _lastPoint.Y < 32 ? -18 : _lastPoint.Y > Bounds.Height - 32 ? 18 : 0;
        if (dy == 0) return;
        viewer.Offset = new Vector(viewer.Offset.X, Math.Clamp(viewer.Offset.Y + dy, 0, Math.Max(0, viewer.Extent.Height - viewer.Viewport.Height)));
        UpdateDrop();
    }

    public void CancelDrag()
    {
        _scrollTimer.Stop();
        if (_source is not null) _source.Opacity = 1;
        var pointer = _pointer;
        _source = null; _pointer = null; _dragging = false; _insertion = null; _marker = null;
        Cursor = null;
        pointer?.Capture(null);
        InvalidateVisual();
    }

    protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e) { CancelDrag(); base.OnPointerCaptureLost(e); }
    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.Escape && _source is not null) { CancelDrag(); e.Handled = true; return; }
        base.OnKeyDown(e);
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        if (_marker is { } marker) context.FillRectangle(new SolidColorBrush(Color.Parse("#2859B8")), marker, 2);
    }
}
