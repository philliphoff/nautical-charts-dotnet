using Xunit;

namespace NauticalCharts.Tests
{
    public class BsbMetadataReaderTests
    {
        [Fact]
        public void ReadSizeMetadata()
        {
            var metadata = BsbMetadataReader.ReadMetadata(new[] { new BsbTextEntry("BSB", new[] { "NA=CONTINUATION A,NU=344102,RA=1171,2098,DU=254" }) });

            Assert.NotNull(metadata);
            Assert.NotNull(metadata.Size);
            Assert.Equal(2098, metadata.Size.Height);
            Assert.Equal(1171, metadata.Size.Width);
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