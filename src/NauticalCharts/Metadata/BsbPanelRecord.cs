namespace NauticalCharts.Metadata;

public sealed record BsbPanelRecord
{
    public string? Name { get; init; }

    public string? Number { get; init; }

    public string? Type { get; init; }

    public string? FileName { get; init; }
}
