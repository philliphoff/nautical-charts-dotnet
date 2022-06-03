using Xunit;

namespace NauticalCharts.Tests
{
    public class BsbMetadataReaderTests
    {
        [Fact]
        public void ReadBorderMetadata()
        {
            var metadata = BsbMetadataReader.ReadMetadata(
                new[]
                {
                    new BsbTextEntry("PLY", new[] { "1,48.483188901744,-123.559587398261" }),
                    new BsbTextEntry("PLY", new[] { "2,48.483188901744,-123.509681638709" }),
                    new BsbTextEntry("PLY", new[] { "3,48.549788623084,-123.509681638709" }),
                    new BsbTextEntry("PLY", new[] { "4,48.549788623084,-123.559587398261" }),
                    new BsbTextEntry("PLY", new[] { "5,48.483188901744,-123.559587398261" }),
                });

            Assert.NotNull(metadata);
            Assert.NotNull(metadata.Border);
            Assert.Equal(
                new[]
                {
                    new BsbCoordinate(48.483188901744, -123.559587398261),
                    new BsbCoordinate(48.483188901744, -123.509681638709),
                    new BsbCoordinate(48.549788623084, -123.509681638709),
                    new BsbCoordinate(48.549788623084, -123.559587398261),
                    new BsbCoordinate(48.483188901744, -123.559587398261),
                },
                metadata.Border);
        }

        [Fact]
        public void ReadNameMetadata()
        {
            var metadata = BsbMetadataReader.ReadMetadata(new[] { new BsbTextEntry("BSB", new[] { "NA=CONTINUATION A,NU=344102,RA=1171,2098,DU=254" }) });

            Assert.NotNull(metadata);
            Assert.NotNull(metadata.Name);
            Assert.Equal("CONTINUATION A", metadata.Name);
        }

        [Fact]
        public void ReadSizeMetadata()
        {
            var metadata = BsbMetadataReader.ReadMetadata(new[] { new BsbTextEntry("BSB", new[] { "NA=CONTINUATION A,NU=344102,RA=1171,2098,DU=254" }) });

            Assert.NotNull(metadata);
            Assert.NotNull(metadata.Size);
            Assert.Equal(2098U, metadata.Size.Height);
            Assert.Equal(1171U, metadata.Size.Width);
        }

        [Fact]
        public void ReadPrimaryPalette()
        {
            var metadata = BsbMetadataReader.ReadMetadata(
                new[] {
                    new BsbTextEntry("RGB", new[] { "1,0,0,0" }),
                    new BsbTextEntry("RGB", new[] { "2,255,255,255" }),
                    new BsbTextEntry("RGB", new[] { "3,201,208,184" }),
                });

            Assert.NotNull(metadata);
            Assert.NotNull(metadata.Palette);
            Assert.Equal(3, metadata.Palette.Count);
            Assert.True(metadata.Palette.ContainsKey(1));
            Assert.Equal(0, metadata.Palette[1].R);
            Assert.Equal(0, metadata.Palette[1].G);
            Assert.Equal(0, metadata.Palette[1].B);
            Assert.True(metadata.Palette.ContainsKey(2));
            Assert.Equal(255, metadata.Palette[2].R);
            Assert.Equal(255, metadata.Palette[2].G);
            Assert.Equal(255, metadata.Palette[2].B);
            Assert.True(metadata.Palette.ContainsKey(3));
            Assert.Equal(201, metadata.Palette[3].R);
            Assert.Equal(208, metadata.Palette[3].G);
            Assert.Equal(184, metadata.Palette[3].B);
        }
    }
}