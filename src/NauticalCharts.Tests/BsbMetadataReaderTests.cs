using Xunit;

namespace NauticalCharts.Tests
{
    public class BsbMetadataReaderTests
    {
        [Fact]
        public void ReadSize()
        {
            var metadata = BsbMetadataReader.ReadMetadata(new[] { new BsbTextEntry("BSB", new[] { "NA=CONTINUATION A,NU=344102,RA=1171,2098,DU=254" }) });

            Assert.NotNull(metadata);
            Assert.NotNull(metadata.Size);
            Assert.Equal(2098, metadata.Size.Height);
            Assert.Equal(1171, metadata.Size.Width);
        }
    }
}