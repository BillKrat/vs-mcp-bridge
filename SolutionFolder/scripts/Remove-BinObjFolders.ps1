[CmdletBinding(SupportsShouldProcess = $true, ConfirmImpact = 'Medium')]
param(
    [Parameter(Position = 0)]
    [string]$RootPath = $(if ($PSScriptRoot) { $PSScriptRoot } else { (Get-Location).Path }),

    [switch]$IncludeHidden,

    [switch]$KeepRootBinObj
)

$ErrorActionPreference = 'Stop'

if (-not (Test-Path -LiteralPath $RootPath -PathType Container)) {
    throw "Root path does not exist or is not a directory: $RootPath"
}

$resolvedRoot = (Resolve-Path -LiteralPath $RootPath).Path
Write-Host "Scanning for bin/obj folders under: $resolvedRoot"

$directories = Get-ChildItem -LiteralPath $resolvedRoot -Directory -Recurse -Force:$IncludeHidden |
    Where-Object {
        ($_.Name -eq 'bin' -or $_.Name -eq 'obj') -and
        $_.FullName -notmatch '[\\/]\.git([\\/]|$)'
    }

if ($KeepRootBinObj) {
    $rootBin = Join-Path $resolvedRoot 'bin'
    $rootObj = Join-Path $resolvedRoot 'obj'

    $directories = $directories | Where-Object {
        $_.FullName -ne $rootBin -and $_.FullName -ne $rootObj
    }
}

if (-not $directories) {
    Write-Host 'No bin/obj folders found.'
    return
}

$directories = $directories | Sort-Object FullName -Unique

Write-Host "Found $($directories.Count) folder(s)."

foreach ($dir in $directories) {
    if ($PSCmdlet.ShouldProcess($dir.FullName, 'Remove folder recursively')) {
        Remove-Item -LiteralPath $dir.FullName -Recurse -Force
        Write-Host "Removed: $($dir.FullName)"
    }
}

Write-Host 'Done.'
