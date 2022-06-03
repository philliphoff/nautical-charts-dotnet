﻿using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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

    public static readonly BsbTextEntryReader<IReadOnlyList<BsbCoordinate>?> Border = new("^PLY$", BorderReader);

    private static Regex NameRegex = new Regex("NA=(?<name>[^,]+)");
    private static Regex NumberRegex = new Regex("NU=(?<number>[^,]+)");
    private static Regex SizeRegex = new Regex("RA=(?<width>\\d+),(?<height>\\d+)");
    private static Regex DrawingUnitsRegex = new Regex("DU=(?<number>\\d+)");

    private static BsbPanelGeneralParameters? PanelGeneralParametersReader(IEnumerable<BsbTextEntry> textEntries)
    {
        bool TryGetMatch(Regex regex, [NotNullWhen(true)] out Match? match)
        {
            match =
                textEntries
                    .SelectMany(entry => entry.Lines)
                    .Select(line => regex.Match(line))
                    .FirstOrDefault(match => match.Success);

            return match != null;
        }

        var parameters = new BsbPanelGeneralParameters();

        if (TryGetMatch(NameRegex, out Match? nameMatch))
        {
            parameters = parameters with { Name = nameMatch.Groups["name"].Value };
        }

        if (TryGetMatch(NumberRegex, out Match? numberMatch))
        {
            parameters = parameters with { Number = numberMatch.Groups["number"].Value };
        }

        if (TryGetMatch(SizeRegex, out Match? sizeMatch))
        {
            parameters = parameters with { Size = new BsbSize(UInt32.Parse(sizeMatch.Groups["height"].Value), UInt32.Parse(sizeMatch.Groups["width"].Value)) };
        }

        if (TryGetMatch(DrawingUnitsRegex, out Match? duRegex))
        {
            parameters = parameters with { DrawingUnits = UInt16.Parse(duRegex.Groups["number"].Value) };
        }

        if (parameters != default)
        {
            return parameters;
        }

        return null;
    }

    public static readonly BsbTextEntryReader<BsbPanelGeneralParameters?> PanelGeneralParameters = new("^BSB$", PanelGeneralParametersReader);

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

    public static readonly BsbTextEntryReader<IReadOnlyDictionary<byte, BsbColor>?> PrimaryPalette = new("^RGB$", PrimaryPaletteReader);

    private static Regex PanelNameRegex = new Regex("NA=(?<name>[^,]+)");
    private static Regex PanelNumberRegex = new Regex("NU=(?<number>[^,]+)");
    private static Regex PanelTypeRegex = new Regex("TY=(?<type>[^,]+)");
    private static Regex PanelFileNameRegex = new Regex("FN=(?<filename>[^,]+)");

    private static IReadOnlyList<BsbPanelRecord> PanelsReader(IEnumerable<BsbTextEntry> textEntries)
    {
        var records = new List<BsbPanelRecord>();

        foreach (var textEntry in textEntries.OrderBy(e => e.EntryType))
        {
            bool TryGetMatch(Regex regex, [NotNullWhen(true)] out Match? match)
            {
                match =
                    textEntry
                        .Lines
                        .Select(line => regex.Match(line))
                        .FirstOrDefault(match => match.Success);

                return match != null;
            }

            var record = new BsbPanelRecord();

            if (TryGetMatch(PanelNameRegex, out Match? nameMatch))
            {
                record = record with { Name = nameMatch.Groups["name"].Value };
            }

            if (TryGetMatch(PanelNumberRegex, out Match? numberMatch))
            {
                record = record with { Number = numberMatch.Groups["number"].Value };
            }

            if (TryGetMatch(PanelTypeRegex, out Match? typeMatch))
            {
                record = record with { Type = typeMatch.Groups["type"].Value };
            }

            if (TryGetMatch(PanelFileNameRegex, out Match? fileNameMatch))
            {
                record = record with { FileName = fileNameMatch.Groups["filename"].Value };
            }

            if (record != default)
            {
                records.Add(record);
            }
        }

        return records;
    }

    public static readonly BsbTextEntryReader<IReadOnlyList<BsbPanelRecord>?> Panels = new(@"^K\d{2}$", PanelsReader);

    private static Regex ChartNameRegex = new Regex("NA=(?<name>[^,]+)");
    private static Regex ChartNumberRegex = new Regex("NU=(?<number>[^,]+)");

    private static BsbChartGeneralParameters? ChartGeneralParametersReader(IEnumerable<BsbTextEntry> textEntries)
    {
        foreach (var textEntry in textEntries.OrderBy(e => e.EntryType))
        {
            bool TryGetMatch(Regex regex, [NotNullWhen(true)] out Match? match)
            {
                match =
                    textEntry
                        .Lines
                        .Select(line => regex.Match(line))
                        .FirstOrDefault(match => match.Success);

                return match != null;
            }

            var record = new BsbChartGeneralParameters();

            if (TryGetMatch(ChartNameRegex, out Match? nameMatch))
            {
                record = record with { Name = nameMatch.Groups["name"].Value };
            }

            if (TryGetMatch(ChartNumberRegex, out Match? numberMatch))
            {
                record = record with { Number = numberMatch.Groups["number"].Value };
            }

            if (record != default)
            {
                return record;
            }
        }

        return null;
    }

    public static readonly BsbTextEntryReader<BsbChartGeneralParameters?> ChartGeneralParameters = new(@"^CHT$", ChartGeneralParametersReader);
}
