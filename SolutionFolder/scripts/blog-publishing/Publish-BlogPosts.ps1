[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string]$SqlConnectionString,

    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string]$ReloadBaseUrl,

    [string]$ReloadKey,

    [string]$PostsRoot,

    [string[]]$Slugs
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

if ([string]::IsNullOrWhiteSpace($PSScriptRoot)) {
    $script:ScriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Definition
}
else {
    $script:ScriptRoot = $PSScriptRoot
}

if ([string]::IsNullOrWhiteSpace($PostsRoot)) {
    $PostsRoot = Join-Path $script:ScriptRoot '..\..\docs\blogs\posts'
}

$resolvedPostsRoot = (Resolve-Path -LiteralPath $PostsRoot).Path
$publishScriptPath = Join-Path $script:ScriptRoot 'Publish-BlogPostDraft.ps1'

if (-not (Test-Path -LiteralPath $publishScriptPath -PathType Leaf)) {
    throw "Publish-BlogPostDraft.ps1 was not found at '$publishScriptPath'."
}

$postDirectories = Get-ChildItem -LiteralPath $resolvedPostsRoot -Directory | Sort-Object Name

if ($PSBoundParameters.ContainsKey('Slugs') -and $null -ne $Slugs -and $Slugs.Count -gt 0) {
    $slugSet = New-Object 'System.Collections.Generic.HashSet[string]' ([System.StringComparer]::OrdinalIgnoreCase)
    foreach ($slug in $Slugs) {
        if (-not [string]::IsNullOrWhiteSpace($slug)) {
            [void]$slugSet.Add($slug.Trim())
        }
    }

    $postDirectories = @($postDirectories | Where-Object { $slugSet.Contains($_.Name) })
}

foreach ($postDirectory in $postDirectories) {
    $slug = $postDirectory.Name
    Write-Host "Publishing: $slug"

    try {
        $invokeParameters = @{
            Slug = $slug
            SqlConnectionString = $SqlConnectionString
            ReloadBaseUrl = $ReloadBaseUrl
            PostsRoot = $resolvedPostsRoot
        }

        if (-not [string]::IsNullOrWhiteSpace($ReloadKey)) {
            $invokeParameters['ReloadKey'] = $ReloadKey
        }

        & $publishScriptPath @invokeParameters

        Write-Host "SUCCESS: $slug" -ForegroundColor Green
    }
    catch {
        Write-Host "FAILED: $slug" -ForegroundColor Red
        Write-Host $_.Exception.Message
    }
}
