using System;
using System.Collections.Generic;
using VsMcpBridge.Shared.Interfaces;

namespace VsMcpBridge.Shared.Tests.Support;

public sealed class RecordingUnhandledExceptionSink : IUnhandledExceptionSink
{
    public List<(string Source, Exception Exception)> Entries { get; } = new List<(string Source, Exception Exception)>();

    public void Save(string source, Exception exception) => Entries.Add((source, exception));
}
