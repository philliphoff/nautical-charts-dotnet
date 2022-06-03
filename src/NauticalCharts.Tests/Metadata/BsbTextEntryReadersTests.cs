﻿using NauticalCharts.Metadata;
using Xunit;

namespace NauticalCharts.Tests.Metadata;

public class BsbTextEntryReadersTests
{
    [Fact]
    public void ReadBorderMetadata()
    {
        var metadata = BsbTextEntryReaders.Border.Reader(
            new[]
            {
                    new BsbTextEntry("PLY", new[] { "1,48.483188901744,-123.559587398261" }),
                    new BsbTextEntry("PLY", new[] { "2,48.483188901744,-123.509681638709" }),
                    new BsbTextEntry("PLY", new[] { "3,48.549788623084,-123.509681638709" }),
                    new BsbTextEntry("PLY", new[] { "4,48.549788623084,-123.559587398261" }),
                    new BsbTextEntry("PLY", new[] { "5,48.483188901744,-123.559587398261" }),
            });

        Assert.NotNull(metadata);
        Assert.Equal(
            new[]
            {
                    new BsbCoordinate(48.483188901744, -123.559587398261),
                    new BsbCoordinate(48.483188901744, -123.509681638709),
                    new BsbCoordinate(48.549788623084, -123.509681638709),
                    new BsbCoordinate(48.549788623084, -123.559587398261),
                    new BsbCoordinate(48.483188901744, -123.559587398261),
            },
            metadata);
    }

    [Fact]
    public void ReadNameMetadata()
    {
        var metadata = BsbTextEntryReaders.NameAndSize.Reader(new[] { new BsbTextEntry("BSB", new[] { "NA=CONTINUATION A,NU=344102,RA=1171,2098,DU=254" }) });

        Assert.NotNull(metadata.Name);
        Assert.Equal("CONTINUATION A", metadata.Name);

        Assert.NotNull(metadata.Size);
        Assert.Equal(2098, metadata.Size.Height);
        Assert.Equal(1171, metadata.Size.Width);
    }

    [Fact]
    public void ReadPrimaryPalette()
    {
        var metadata = BsbTextEntryReaders.PrimaryPalette.Reader(
            new[] {
                    new BsbTextEntry("RGB", new[] { "1,0,0,0" }),
                    new BsbTextEntry("RGB", new[] { "2,255,255,255" }),
                    new BsbTextEntry("RGB", new[] { "3,201,208,184" }),
            });

        Assert.NotNull(metadata);
        Assert.Equal(3, metadata.Count);
        Assert.True(metadata.ContainsKey(1));
        Assert.Equal(0, metadata[1].R);
        Assert.Equal(0, metadata[1].G);
        Assert.Equal(0, metadata[1].B);
        Assert.True(metadata.ContainsKey(2));
        Assert.Equal(255, metadata[2].R);
        Assert.Equal(255, metadata[2].G);
        Assert.Equal(255, metadata[2].B);
        Assert.True(metadata.ContainsKey(3));
        Assert.Equal(201, metadata[3].R);
        Assert.Equal(208, metadata[3].G);
        Assert.Equal(184, metadata[3].B);
    }

    [Fact]
    public void ReadPanelRecords()
    {
        var textEntries = new[]
        {
            new BsbTextEntry("K01", new[] { "NA=VICTORIA HARBOUR,NU=341201,TY=Base,FN=341201.KAP" }),
            new BsbTextEntry("K02", new[] { "NA=CONTINUATION A,NU=341202,TY=Inset,FN=341202.KAP" }),
        };

        var records = BsbTextEntryReaders.Panels.Reader(textEntries);

        Assert.NotNull(records);
        Assert.Equal(2, records.Count);

        Assert.Equal("VICTORIA HARBOUR", records[0].Name);
        Assert.Equal("341201", records[0].Number);
        Assert.Equal("Base", records[0].Type);
        Assert.Equal("341201.KAP", records[0].FileName);

        Assert.Equal("CONTINUATION A", records[1].Name);
        Assert.Equal("341202", records[1].Number);
        Assert.Equal("Inset", records[1].Type);
        Assert.Equal("341202.KAP", records[1].FileName);
    }
}

