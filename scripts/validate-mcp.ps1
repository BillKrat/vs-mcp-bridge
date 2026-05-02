[CmdletBinding()]
param(
    [string]$ServerDllPath,
    [int]$TimeoutSeconds = 20,
    [switch]$RequireVsToolSuccess
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Fail([string]$Message, [string[]]$Details = @())
{
    Write-Host "[FAIL] $Message" -ForegroundColor Red

    foreach ($detail in $Details)
    {
        if (-not [string]::IsNullOrWhiteSpace($detail))
        {
            Write-Host $detail
        }
    }

    exit 1
}

function Warn([string]$Message, [string[]]$Details = @())
{
    Write-Host "[WARN] $Message" -ForegroundColor Yellow

    foreach ($detail in $Details)
    {
        if (-not [string]::IsNullOrWhiteSpace($detail))
        {
            Write-Host $detail
        }
    }
}

function Resolve-RepoRoot
{
    return (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
}

function Resolve-ServerDll([string]$RepoRoot, [string]$ExplicitPath)
{
    if (-not [string]::IsNullOrWhiteSpace($ExplicitPath))
    {
        return (Resolve-Path $ExplicitPath).Path
    }

    $csprojPath = Join-Path $RepoRoot 'VsMcpBridge.McpServer\VsMcpBridge.McpServer.csproj'
    if (-not (Test-Path $csprojPath))
    {
        throw "Unable to locate MCP server project file at '$csprojPath'."
    }

    [xml]$csproj = Get-Content $csprojPath
    $targetFramework = $csproj.Project.PropertyGroup.TargetFramework | Select-Object -First 1
    if ([string]::IsNullOrWhiteSpace($targetFramework))
    {
        throw "Unable to determine TargetFramework from '$csprojPath'."
    }

    $candidate = Join-Path $RepoRoot "VsMcpBridge.McpServer\bin\Debug\$targetFramework\VsMcpBridge.McpServer.dll"
    if (Test-Path $candidate)
    {
        return (Resolve-Path $candidate).Path
    }

    throw "Built MCP server DLL was not found at '$candidate'. Build VsMcpBridge.McpServer first."
}

function New-ValidationHelperSource
{
    return @'
using System.Text.Json;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

if (args.Length != 3)
{
    Console.Error.WriteLine("Expected arguments: <repoRoot> <serverDllPath> <timeoutSeconds>");
    return 2;
}

string repoRoot = args[0];
string serverDllPath = args[1];
if (!int.TryParse(args[2], out int timeoutSeconds))
{
    Console.Error.WriteLine("TimeoutSeconds must be an integer.");
    return 2;
}

using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
CancellationToken cancellationToken = cancellationTokenSource.Token;
List<string> stderrLines = [];

var helperResult = new
{
    initializePassed = false,
    toolCount = 0,
    toolNames = Array.Empty<string>(),
    callPassed = false,
    callText = string.Empty,
    callIsError = false,
    serverInfo = string.Empty,
    exception = string.Empty,
    childStderr = Array.Empty<string>()
};

try
{
    var transportOptions = new StdioClientTransportOptions
    {
        Name = "vs-mcp-bridge-validate-mcp",
        Command = "dotnet",
        Arguments = [serverDllPath],
        WorkingDirectory = repoRoot,
        EnvironmentVariables = new Dictionary<string, string?>
        {
            ["DOTNET_NOLOGO"] = "1",
            ["DOTNET_CLI_TELEMETRY_OPTOUT"] = "1"
        },
        StandardErrorLines = line =>
        {
            if (!string.IsNullOrWhiteSpace(line))
            {
                stderrLines.Add(line);
            }
        }
    };

    await using var client = await McpClient.CreateAsync(
        new StdioClientTransport(transportOptions),
        cancellationToken: cancellationToken);

    IList<McpClientTool> tools = await client.ListToolsAsync(cancellationToken: cancellationToken);
    CallToolResult callResult = await client.CallToolAsync(
        "vs_get_active_document",
        arguments: new Dictionary<string, object?>(),
        cancellationToken: cancellationToken);

    string combinedText = string.Join(
        "\n",
        callResult.Content
            .OfType<TextContentBlock>()
            .Select(block => block.Text)
            .Where(text => !string.IsNullOrWhiteSpace(text)))
        .Trim();

    helperResult = new
    {
        initializePassed = true,
        toolCount = tools.Count,
        toolNames = tools.Select(tool => tool.Name).ToArray(),
        callPassed = callResult.IsError != true
            && !string.IsNullOrWhiteSpace(combinedText)
            && !combinedText.StartsWith("Error:", StringComparison.Ordinal),
        callText = combinedText,
        callIsError = callResult.IsError == true,
        serverInfo = client.ServerInfo is null ? string.Empty : $"{client.ServerInfo.Name} {client.ServerInfo.Version}".Trim(),
        exception = string.Empty,
        childStderr = stderrLines.ToArray()
    };
}
catch (Exception ex)
{
    helperResult = new
    {
        initializePassed = helperResult.initializePassed,
        toolCount = helperResult.toolCount,
        toolNames = helperResult.toolNames,
        callPassed = false,
        callText = helperResult.callText,
        callIsError = helperResult.callIsError,
        serverInfo = helperResult.serverInfo,
        exception = ex.ToString(),
        childStderr = stderrLines.ToArray()
    };
}

Console.Write(JsonSerializer.Serialize(helperResult));
return 0;
'@
}

function New-ValidationHelperProject
{
    return @'
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="ModelContextProtocol.Core" Version="1.1.0" />
  </ItemGroup>
</Project>
'@
}

function Invoke-DotnetCommand([string[]]$Arguments, [string]$WorkingDirectory)
{
    $stdoutPath = Join-Path $WorkingDirectory 'stdout.txt'
    $stderrPath = Join-Path $WorkingDirectory 'stderr.txt'

    $process = Start-Process `
        -FilePath 'dotnet' `
        -ArgumentList $Arguments `
        -WorkingDirectory $WorkingDirectory `
        -RedirectStandardOutput $stdoutPath `
        -RedirectStandardError $stderrPath `
        -NoNewWindow `
        -PassThru `
        -Wait

    $stdout = if (Test-Path $stdoutPath) { Get-Content $stdoutPath -Raw } else { '' }
    $stderr = if (Test-Path $stderrPath) { Get-Content $stderrPath -Raw } else { '' }

    return @{
        ExitCode = $process.ExitCode
        StdOut = $stdout
        StdErr = $stderr
    }
}

$repoRoot = Resolve-RepoRoot
$resolvedServerDllPath = Resolve-ServerDll -RepoRoot $repoRoot -ExplicitPath $ServerDllPath

Write-Host "[INFO] Repo root: $repoRoot"
Write-Host "[INFO] Using built MCP server DLL: $resolvedServerDllPath"

$tempRoot = Join-Path ([System.IO.Path]::GetTempPath()) ("vs-mcp-bridge-mcp-validate-" + [guid]::NewGuid().ToString('N'))
$helperProjectDir = Join-Path $tempRoot 'helper'
$helperProjectPath = Join-Path $helperProjectDir 'ValidateMcpHelper.csproj'
$helperProgramPath = Join-Path $helperProjectDir 'Program.cs'

New-Item -ItemType Directory -Path $helperProjectDir -Force | Out-Null

try
{
    Set-Content -Path $helperProjectPath -Value (New-ValidationHelperProject) -Encoding UTF8
    Set-Content -Path $helperProgramPath -Value (New-ValidationHelperSource) -Encoding UTF8

    Write-Host "[INFO] Building temporary MCP validation helper"
    $buildResult = Invoke-DotnetCommand `
        -Arguments @('build', $helperProjectPath, '--nologo', '--verbosity', 'quiet') `
        -WorkingDirectory $helperProjectDir

    if ($buildResult.ExitCode -ne 0)
    {
        Fail "Failed to build the temporary MCP validation helper." @($buildResult.StdOut, $buildResult.StdErr)
    }

    $helperDllPath = Join-Path $helperProjectDir 'bin\Debug\net10.0\ValidateMcpHelper.dll'
    if (-not (Test-Path $helperDllPath))
    {
        Fail "Temporary MCP validation helper did not produce '$helperDllPath'."
    }

    $runResult = Invoke-DotnetCommand `
        -Arguments @($helperDllPath, $repoRoot, $resolvedServerDllPath, $TimeoutSeconds.ToString()) `
        -WorkingDirectory $helperProjectDir

    if ($runResult.ExitCode -ne 0)
    {
        Fail "Temporary MCP validation helper exited with code $($runResult.ExitCode)." @($runResult.StdOut, $runResult.StdErr)
    }

    if ([string]::IsNullOrWhiteSpace($runResult.StdOut))
    {
        Fail "Temporary MCP validation helper did not return a result payload." @($runResult.StdErr)
    }

    $result = $runResult.StdOut | ConvertFrom-Json

    if (-not $result.initializePassed)
    {
        Fail "initialize failed." @($result.exception, ($result.childStderr -join [Environment]::NewLine), $runResult.StdErr)
    }

    Write-Host "[PASS] initialize"

    $toolNames = @($result.toolNames)
    if ($toolNames.Count -eq 0)
    {
        Fail "tools/list returned no tools." @($result.exception, ($result.childStderr -join [Environment]::NewLine), $runResult.StdErr)
    }

    if ($toolNames -notcontains 'vs_get_active_document')
    {
        Fail "tools/list did not expose 'vs_get_active_document'." @($result.exception, ($result.childStderr -join [Environment]::NewLine), $runResult.StdErr)
    }

    Write-Host "[PASS] tools/list ($($toolNames.Count) tools)"

    if (-not $result.callPassed)
    {
        $details = @()
        if (-not [string]::IsNullOrWhiteSpace([string]$result.callText))
        {
            $details += "Tool output: $($result.callText)"
        }

        if (-not [string]::IsNullOrWhiteSpace([string]$result.exception))
        {
            $details += $result.exception
        }

        if ($null -ne $result.childStderr)
        {
            $details += ($result.childStderr -join [Environment]::NewLine)
        }

        if (-not [string]::IsNullOrWhiteSpace($runResult.StdErr))
        {
            $details += $runResult.StdErr
        }

        if ($RequireVsToolSuccess)
        {
            Fail "vs_get_active_document failed." $details
        }

        Warn "vs_get_active_document is available in tools/list, but the downstream VS bridge or active document was unavailable." $details
        Write-Host "[PASS] MCP transport validation succeeded."
        exit 0
    }

    Write-Host "[PASS] tools/call vs_get_active_document"

    if (-not [string]::IsNullOrWhiteSpace([string]$result.serverInfo))
    {
        Write-Host "[INFO] Connected server: $($result.serverInfo)"
    }

    Write-Host "[PASS] Repo-owned MCP validation succeeded."
    exit 0
}
finally
{
    if (Test-Path $tempRoot)
    {
        Remove-Item -LiteralPath $tempRoot -Recurse -Force -ErrorAction SilentlyContinue
    }
}
