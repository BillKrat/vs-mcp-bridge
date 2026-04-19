using System;

namespace VsMcpBridge.Shared.Services;

public sealed class AmbiguousEditTargetException : InvalidOperationException
{
    public AmbiguousEditTargetException()
        : base("Target document contains multiple matches for the approved proposal.")
    {
    }
}
