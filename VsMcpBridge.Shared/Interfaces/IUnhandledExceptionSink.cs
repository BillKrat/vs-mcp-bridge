using System;

namespace VsMcpBridge.Shared.Interfaces;

public interface IUnhandledExceptionSink
{
    void Save(string source, Exception exception);
}
