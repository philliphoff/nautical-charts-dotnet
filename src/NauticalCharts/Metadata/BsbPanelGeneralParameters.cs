namespace NauticalCharts.Metadata;

public sealed record BsbPanelGeneralParameters
{
    public string? Name { get; init; }

    public string? Number { get; init; }

    public BsbSize? Size { get; init; }

    public uint? DrawingUnits { get; init; }
}
