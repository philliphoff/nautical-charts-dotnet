using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace NauticalCharts.Metadata;

public static class BsbTextEntryReaders
{
    private static Regex BorderRegex = new Regex("^(?<order>\\d+),(?<latitude>[-+]?(\\d*\\.?\\d+|\\d+)),(?<longitude>[-+]?(\\d*\\.?\\d+|\\d+))$");

    private static IReadOnlyList<BsbCoordinate>? BorderReader(IEnumerable<BsbTextEntry> textEntries)
    {
        var border =
            textEntries
                .SelectMany(textEntry => textEntry.Lines)
                .Select(line => BorderRegex.Match(line))
                .Where(match => match.Success)
                .Select(
                    match =>
                    {
                        return new
                        {
                            Order = Int32.Parse(match.Groups["order"].Value),
                            Coordinate = new BsbCoordinate(
                                Double.Parse(match.Groups["latitude"].Value),
                                Double.Parse(match.Groups["longitude"].Value))
                        };
                    })
                .OrderBy(item => item.Order)
                .Select(item => item.Coordinate)
                .ToList();

        return border.Count > 0
            ? border
            : null;
    }

    public static readonly BsbTextEntryReader<IReadOnlyList<BsbCoordinate>?> Border = new("PLY", BorderReader);

    private static Regex NameRegex = new Regex("NA=(?<name>[^,]+)");
    private static Regex SizeRegex = new Regex("RA=(?<width>\\d+),(?<height>\\d+)");

    private static (string? Name, BsbSize? Size) NameAndSizeReader(IEnumerable<BsbTextEntry> textEntries)
    {
        string? name = null;
        BsbSize? size = null;

        var nameMatch =
            textEntries
                .SelectMany(textEntry => textEntry.Lines)
                .Select(line => NameRegex.Match(line))
                .FirstOrDefault(match => match.Success);

        if (nameMatch != null)
        {
            name = nameMatch.Groups["name"].Value;
        }

        var sizeMatch =
            textEntries
                .SelectMany(textEntry => textEntry.Lines)
                .Select(line => SizeRegex.Match(line))
                .FirstOrDefault(match => match.Success);

        if (sizeMatch != null)
        {
            int height = Int32.Parse(sizeMatch.Groups["height"].Value);
            int width = Int32.Parse(sizeMatch.Groups["width"].Value);

            size = new BsbSize(height, width);
        }

        return (name, size);
    }

    public static readonly BsbTextEntryReader<(string? Name, BsbSize? Size)> NameAndSize = new("BSB", NameAndSizeReader);

    private static Regex PaletteRegex = new Regex("^(?<index>\\d+),(?<r>\\d+),(?<g>\\d+),(?<b>\\d+)$");

    private static IReadOnlyDictionary<byte, BsbColor>? PrimaryPaletteReader(IEnumerable<BsbTextEntry> textEntries)
    {
        var palette =
            textEntries
                .SelectMany(textEntry => textEntry.Lines)
                .Select(line => PaletteRegex.Match(line))
                .Where(match => match.Success)
                .ToDictionary(
                    match =>
                    {
                        return Byte.Parse(match.Groups["index"].Value);
                    },
                    match =>
                    {
                        byte r = Byte.Parse(match.Groups["r"].Value);
                        byte g = Byte.Parse(match.Groups["g"].Value);
                        byte b = Byte.Parse(match.Groups["b"].Value);

                        return new BsbColor(r, g, b);
                    });

        return palette.Count > 0
            ? palette
            : null;
    }

    public static readonly BsbTextEntryReader<IReadOnlyDictionary<byte, BsbColor>?> PrimaryPalette = new("RGB", PrimaryPaletteReader);
}
