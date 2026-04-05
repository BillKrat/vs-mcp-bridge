using System.Runtime.Serialization;
using VsMcpBridge.Shared.Interfaces;

namespace VsMcpBridge.Vsix.Tests.Support;

internal static class TestPackageFactory
{
    internal static IAsyncPackage CreatePackage()
    {
        return (FakeAsyncPackage)FormatterServices.GetUninitializedObject(typeof(FakeAsyncPackage));
    }
}
