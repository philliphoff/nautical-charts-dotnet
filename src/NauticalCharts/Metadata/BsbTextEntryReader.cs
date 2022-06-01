using System;
using System.Collections.Generic;

namespace NauticalCharts.Metadata;

public sealed record BsbTextEntryReader<T>(string EntryTypePattern, Func<IEnumerable<BsbTextEntry>, T> Reader);
