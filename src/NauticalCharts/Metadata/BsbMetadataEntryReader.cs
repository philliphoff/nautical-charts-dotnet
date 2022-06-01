using System.Collections.Generic;

namespace NauticalCharts.Metadata;

public delegate T BsbMetadataEntryReader<T>(T model, IEnumerable<BsbTextEntry> entries);
