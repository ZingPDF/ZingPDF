using ZingPDF.Extensions;
using ZingPDF.Syntax;
using ZingPDF.Syntax.ContentStreamsAndResources;

namespace ZingPDF.Elements.Drawing;

internal sealed class PathContentStream : ContentStream
{
    public PathContentStream(Path path, ObjectContext context)
        : base(context)
    {
        ArgumentNullException.ThrowIfNull(path);

        var points = path.Points.ToList();

        this.SaveGraphicsState();

        if (path.StrokeOptions is not null)
        {
            this.SetStrokeColour(path.StrokeOptions.Colour)
                .SetLineWidth(path.StrokeOptions.Width);
        }

        if (path.FillOptions is not null)
        {
            this.SetColour(path.FillOptions.Colour);
        }

        this.MoveTo(points[0]);

        switch (path.Type)
        {
            case PathType.Linear:
                foreach (var point in points.Skip(1))
                {
                    this.LineTo(point);
                }
                break;
            case PathType.Bezier:
                for (var i = 1; i < points.Count; i += 3)
                {
                    this.CurveTo(points[i], points[i + 1], points[i + 2]);
                }
                break;
            default:
                throw new InvalidOperationException($"Unsupported path type '{path.Type}'.");
        }

        AddPaintOperation(path);

        this.RestoreGraphicsState();
    }

    private void AddPaintOperation(Path path)
    {
        var pathOperator = path switch
        {
            { StrokeOptions: not null, FillOptions: not null } => Operators.PathPainting.B,
            { StrokeOptions: not null } => Operators.PathPainting.S,
            { FillOptions: not null } => Operators.PathPainting.f,
            _ => throw new InvalidOperationException("A path requires stroke and/or fill options.")
        };

        Operations.Add(new ContentStreamOperation { Operator = pathOperator });
    }
}
