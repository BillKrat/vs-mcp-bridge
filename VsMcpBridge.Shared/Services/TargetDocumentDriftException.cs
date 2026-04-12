using System;

namespace VsMcpBridge.Shared.Services;

public sealed class TargetDocumentDriftException : InvalidOperationException
{
    public TargetDocumentDriftException()
        : base("Target document no longer matches the approved proposal.")
    {
    }
}
