using System;

namespace VsMcpBridge.Vsix.Diagnostics;

public interface IUnhandledExceptionSink
{
    void Save(string source, Exception exception);
}
