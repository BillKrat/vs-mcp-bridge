using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using VsMcpBridge.Shared.Interfaces;
using VsMcpBridge.Shared.Loggers;
using Xunit;

namespace VsMcpBridge.Shared.Tests;

public sealed class BridgeLogSinkTests
{
    [Fact]
    public void Publish_forwards_entry_and_raises_event()
    {
        var forwarder = new RecordingBridgeLogForwarder();
        var sink = new BridgeLogSink(forwarder);
        BridgeLogEntry? publishedEntry = null;
        sink.EntryLogged += entry => publishedEntry = entry;

        var entry = new BridgeLogEntry
        {
            TimestampUtc = new DateTime(2026, 4, 12, 12, 0, 0, DateTimeKind.Utc),
            Level = LogLevel.Trace,
            CategoryName = "Tests",
            Message = "message"
        };

        sink.Publish(entry);

        Assert.Same(entry, publishedEntry);
        Assert.Single(forwarder.Entries);
        Assert.Same(entry, forwarder.Entries[0]);
    }

    [Fact]
    public void FileBridgeLogForwarder_writes_formatted_entry_to_disk()
    {
        var directory = Path.Combine(Path.GetTempPath(), "VsMcpBridge.Tests", Guid.NewGuid().ToString("N"));
        var path = Path.Combine(directory, "forwarder.log");
        var forwarder = new FileBridgeLogForwarder(path);
        var entry = new BridgeLogEntry
        {
            TimestampUtc = new DateTime(2026, 4, 12, 12, 0, 0, DateTimeKind.Utc),
            Level = LogLevel.Information,
            CategoryName = "Tests",
            Message = "forwarded message"
        };

        try
        {
            forwarder.Forward(entry);

            Assert.True(File.Exists(path));
            var contents = File.ReadAllText(path);
            Assert.Contains("[Information] [Tests] forwarded message", contents, StringComparison.Ordinal);
        }
        finally
        {
            if (Directory.Exists(directory))
                Directory.Delete(directory, recursive: true);
        }
    }

    private sealed class RecordingBridgeLogForwarder : IBridgeLogForwarder
    {
        public List<BridgeLogEntry> Entries { get; } = new();

        public void Forward(BridgeLogEntry entry)
        {
            Entries.Add(entry);
        }
    }
}
