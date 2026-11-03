using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;

namespace ZingPDF.Editor;

public sealed class PageSurface : Control
{
    private Point? _pressed;
    private TextBoxBounds? _drag;
    private Vector _offset;
    private MainViewModel? _viewModel;
    private string? _hoverExisting;
    public Func<Task>? CommitDrafts { get; set; }
    public event Action? TextPlaced;
    public event Action? ExistingTextSelected;

    public PageSurface()
    {
        Focusable = true;
        ClipToBounds = true;
        DataContextChanged += (_, _) =>
        {
            if (_viewModel is not null) _viewModel.SurfaceChanged -= Update;
            _viewModel = DataContext as MainViewModel;
            if (_viewModel is not null) _viewModel.SurfaceChanged += Update;
            Update();
        };
    }

    private void Update()
    {
        Width = _viewModel?.PageWidth ?? 0;
        Height = _viewModel?.PageHeight ?? 0;
        Cursor = new Cursor(_viewModel?.AddingText == true ? StandardCursorType.Ibeam : StandardCursorType.Arrow);
        InvalidateVisual();
    }

    public override void Render(DrawingContext context)
    {
        context.FillRectangle(Brushes.White, new Rect(Bounds.Size));
        var vm = _viewModel;
        if (vm?.PageBitmap is { } bitmap) context.DrawImage(bitmap, new Rect(Bounds.Size));
        if (vm?.IsTextMode != true) return;
        foreach (var box in vm.GetExistingTextBounds())
        {
            var selected = box.Line.Id == vm.SelectedExistingLine?.Id;
            if (!selected && (box.Line.Id != _hoverExisting || vm.AddingText)) continue;
            var pen = new Pen(new SolidColorBrush(Color.Parse(selected ? "#2859B8" : "#9BB2DE")), selected ? 1.5 : 1, selected ? null : DashStyle.Dash);
            context.DrawRectangle(null, pen, box.Bounds.Inflate(3));
        }
        foreach (var box in vm.GetTextBounds())
        {
            var rect = box.Bounds.Inflate(3);
            var selected = box.Id == vm.SelectedTextId;
            if (_drag?.Id == box.Id) rect = rect.Translate(_offset);
            var pen = new Pen(new SolidColorBrush(Color.Parse(selected ? "#2859B8" : "#9BB2DE")), selected ? 1.5 : 1, selected ? null : DashStyle.Dash);
            context.DrawRectangle(null, pen, rect);
            if (selected) context.FillRectangle(new SolidColorBrush(Color.Parse("#2859B8")), new Rect(rect.BottomRight - new Vector(3, 3), new Size(6, 6)));
        }
    }

    protected override async void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        var vm = _viewModel;
        if (vm?.IsReady != true || !vm.IsTextMode || !e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) return;
        Focus();
        var point = e.GetPosition(this);
        if (vm.AddingText)
        {
            await vm.AddTextAtAsync(point.X / vm.ViewScale, point.Y / vm.ViewScale);
            TextPlaced?.Invoke();
            e.Handled = true;
            return;
        }
        if (CommitDrafts is not null) await CommitDrafts();
        if (vm.HasTextDraft || vm.HasFormDraft || vm.HasExistingDraft || vm.HasError) return;
        var hit = vm.GetTextBounds().Reverse().FirstOrDefault(t => t.Bounds.Inflate(6).Contains(point));
        if (hit is not null)
        {
            vm.SetTextSelection(vm.Document!.AddedText.Single(t => t.Id == hit.Id));
            _drag = hit; _pressed = e.GetPosition(TopLevel.GetTopLevel(this)); _offset = default;
            e.Pointer.Capture(this);
        }
        else
        {
            vm.SelectedTextId = null;
            vm.SelectedExistingLine = vm.GetExistingTextBounds().Reverse().FirstOrDefault(b => b.Bounds.Inflate(3).Contains(point))?.Line;
            if (vm.SelectedExistingLine is not null) ExistingTextSelected?.Invoke();
        }
        e.Handled = true;
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        if (_pressed is { } point) { _offset = e.GetPosition(TopLevel.GetTopLevel(this)) - point; InvalidateVisual(); }
        var vm = _viewModel;
        var hover = vm?.IsTextMode == true ? vm.GetExistingTextBounds().Reverse().FirstOrDefault(b => b.Bounds.Inflate(3).Contains(e.GetPosition(this))) : null;
        if (_hoverExisting != hover?.Line.Id) { _hoverExisting = hover?.Line.Id; InvalidateVisual(); }
        var overText = vm?.IsTextMode == true && vm.GetTextBounds().Any(t => t.Bounds.Inflate(6).Contains(e.GetPosition(this)));
        Cursor = new Cursor(_drag is not null || overText && vm?.AddingText != true ? StandardCursorType.SizeAll : vm?.AddingText == true || hover?.Line.CanEdit == true ? StandardCursorType.Ibeam : StandardCursorType.Arrow);
    }

    protected override async void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        var vm = _viewModel; var drag = _drag; var offset = _offset;
        _pressed = null; _drag = null; _offset = default; e.Pointer.Capture(null);
        if (drag is not null && vm?.Document is { } doc && offset.Length > 2)
            await vm.RunAsync(async () =>
            {
                await Task.Run(() => doc.MoveTextAsync(drag.Id, drag.DisplayAnchor.X + offset.X / vm.ViewScale, drag.DisplayAnchor.Y + offset.Y / vm.ViewScale));
                await vm.RefreshAsync();
            });
        InvalidateVisual();
    }

    protected override async void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (_viewModel is not { IsReady: true } vm || vm.Document is not { } doc) return;
        if (e.Key == Key.Escape) { vm.AddingText = false; vm.SelectedTextId = null; if (!vm.HasExistingDraft) vm.SelectedExistingLine = null; e.Handled = true; }
        if (e.Key == Key.Enter && vm.AddingText && vm.Geometry is { } geometry)
        {
            await vm.AddTextAtAsync(geometry.DisplayWidth / 3, geometry.DisplayHeight / 2);
            TextPlaced?.Invoke(); e.Handled = true;
        }
        if (vm.SelectedTextId is not { } id) return;
        if (e.Key == Key.Delete)
        {
            await vm.RunAsync(async () => { doc.RemoveText(id); vm.SelectedTextId = null; await vm.RefreshAsync(); });
            e.Handled = true;
        }
        if (e.Key is Key.Left or Key.Right or Key.Up or Key.Down)
        {
            var box = vm.GetTextBounds().Single(t => t.Id == id);
            var step = e.KeyModifiers.HasFlag(KeyModifiers.Shift) ? 10 : 1;
            var dx = e.Key == Key.Left ? -step : e.Key == Key.Right ? step : 0;
            var dy = e.Key == Key.Up ? -step : e.Key == Key.Down ? step : 0;
            await vm.RunAsync(async () => { await Task.Run(() => doc.MoveTextAsync(id, box.DisplayAnchor.X + dx, box.DisplayAnchor.Y + dy)); await vm.RefreshAsync(); });
            e.Handled = true;
        }
    }
}
