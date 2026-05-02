using System;
using VsMcpBridge.Shared.Loggers;

namespace VsMcpBridge.Shared.Interfaces;

public interface IBridgeLogSink
{
    event Action<BridgeLogEntry>? EntryLogged;

    void Publish(BridgeLogEntry entry);
}
