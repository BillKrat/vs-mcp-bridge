using System;
using VsMcpBridge.Shared.Interfaces;

namespace VsMcpBridge.Shared.Loggers;

public sealed class BridgeLogSink : IBridgeLogSink
{
    private readonly object _sync = new();
    private readonly IBridgeLogForwarder _forwarder;
    private Action<BridgeLogEntry>? _entryLogged;

    public BridgeLogSink()
        : this(new NullBridgeLogForwarder())
    {
    }

    public BridgeLogSink(IBridgeLogForwarder forwarder)
    {
        _forwarder = forwarder ?? throw new ArgumentNullException(nameof(forwarder));
    }

    public event Action<BridgeLogEntry>? EntryLogged
    {
        add
        {
            lock (_sync)
            {
                _entryLogged += value;
            }
        }
        remove
        {
            lock (_sync)
            {
                _entryLogged -= value;
            }
        }
    }

    public void Publish(BridgeLogEntry entry)
    {
        if (entry == null)
            throw new ArgumentNullException(nameof(entry));

        _forwarder.Forward(entry);

        Action<BridgeLogEntry>? handlers;
        lock (_sync)
        {
            handlers = _entryLogged;
        }

        handlers?.Invoke(entry);
    }
}
