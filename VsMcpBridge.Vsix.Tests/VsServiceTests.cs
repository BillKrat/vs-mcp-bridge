using VsMcpBridge.Vsix.Services;
using VsMcpBridge.Vsix.Tests.Support;
using Xunit;

namespace VsMcpBridge.Vsix.Tests;

public sealed class VsServiceTests
{
    [Fact]
    public void Constructor_logs_bridge_service_startup()
    {
        var logger = new RecordingBridgeLogger();
        _ = new VsService(TestPackageFactory.CreatePackage(), logger);

        Assert.Contains("Bridge service startup complete.", logger.VerboseMessages);
    }

    [Fact]
    public async System.Threading.Tasks.Task ProposeTextEditAsync_returns_empty_diff_when_text_is_unchanged()
    {
        var logger = new RecordingBridgeLogger();
        var service = new VsService(TestPackageFactory.CreatePackage(), logger);

        var response = await service.ProposeTextEditAsync("sample.cs", "same", "same");

        Assert.True(response.Success);
        Assert.Equal(string.Empty, response.Diff);
        Assert.Contains(logger.InformationMessages, message => message.Contains("Generating proposed diff for 'sample.cs'"));
    }

    [Fact]
    public async System.Threading.Tasks.Task ProposeTextEditAsync_returns_unified_diff_when_text_changes()
    {
        var service = new VsService(TestPackageFactory.CreatePackage(), new RecordingBridgeLogger());

        var response = await service.ProposeTextEditAsync("sample.cs", "before", "after");

        Assert.True(response.Success);
        Assert.Contains("--- a/sample.cs", response.Diff);
        Assert.Contains("+++ b/sample.cs", response.Diff);
        Assert.Contains("-before", response.Diff);
        Assert.Contains("+after", response.Diff);
    }
}
