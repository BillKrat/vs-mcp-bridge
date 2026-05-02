using System;
using VsMcpBridge.Shared.Interfaces;

namespace VsMcpBridge.Shared.Loggers;

public sealed class BridgeLogSink : IBridgeLogSink
{
    private readonly object _sync = new();
    private Action<BridgeLogEntry>? _entryLogged;

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

        Action<BridgeLogEntry>? handlers;
        lock (_sync)
        {
            handlers = _entryLogged;
        }

        handlers?.Invoke(entry);
    }
}
