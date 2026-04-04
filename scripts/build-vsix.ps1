param(
    [ValidateSet("Build", "Rebuild")]
    [string]$Target = "Build",
    [string]$Configuration = "Debug",
    [string]$Platform = "AnyCPU",
    [string]$Project = "VsMcpBridge.Vsix\VsMcpBridge.Vsix.csproj",
    [switch]$Restore
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$projectPath = Join-Path $repoRoot $Project

if (-not (Test-Path -LiteralPath $projectPath)) {
    throw "Project not found: $projectPath"
}

$candidateMsbuildPaths = @(
    "C:\Program Files\Microsoft Visual Studio\18\Insiders\MSBuild\Current\Bin\arm64\MSBuild.exe",
    "C:\Program Files\Microsoft Visual Studio\18\Insiders\MSBuild\Current\Bin\MSBuild.exe",
    "C:\Program Files\Microsoft Visual Studio\18\Insiders\MSBuild\Current\Bin\amd64\MSBuild.exe",
    "C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\arm64\MSBuild.exe",
    "C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe",
    "C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\amd64\MSBuild.exe"
)

$msbuildPath = $candidateMsbuildPaths | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
if (-not $msbuildPath) {
    throw "Could not locate Visual Studio MSBuild. Checked:`n$($candidateMsbuildPaths -join "`n")"
}

$arguments = @(
    $projectPath,
    "/t:$Target",
    "/p:Configuration=$Configuration",
    "/p:Platform=$Platform",
    "/nologo",
    "/v:m"
)

if ($Restore) {
    $arguments += "/restore"
}

Write-Host "Using MSBuild: $msbuildPath"
Write-Host "Building project: $projectPath"

& $msbuildPath @arguments
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}
