[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string]$SqlConnectionString,

    [Parameter(Mandatory = $true)]
    [int]$DataStoreSettingRowId,

    [string]$OutputRoot
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

if ([string]::IsNullOrWhiteSpace($PSScriptRoot)) {
    $script:ScriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Definition
}
else {
    $script:ScriptRoot = $PSScriptRoot
}

if ([string]::IsNullOrWhiteSpace($OutputRoot)) {
    $OutputRoot = Join-Path $script:ScriptRoot '..\..\docs\blogs\source-of-truth\widget-settings\datastore-row-26512-20260516'
}

function New-SqlParameter {
    param(
        [string]$Name,
        [System.Data.SqlDbType]$Type,
        [object]$Value
    )

    $parameter = New-Object System.Data.SqlClient.SqlParameter
    $parameter.ParameterName = $Name
    $parameter.SqlDbType = $Type
    $parameter.Value = if ($null -eq $Value) { [DBNull]::Value } else { $Value }
    return $parameter
}

function Get-StringHash {
    param([string]$Value)

    $sha256 = [System.Security.Cryptography.SHA256]::Create()
    try {
        $bytes = [System.Text.Encoding]::UTF8.GetBytes($Value)
        $hashBytes = $sha256.ComputeHash($bytes)
        return [System.BitConverter]::ToString($hashBytes).Replace('-', '').ToLowerInvariant()
    }
    finally {
        $sha256.Dispose()
    }
}

function ConvertTo-ParsedSettings {
    param([string]$Settings)

    $parseAttempts = New-Object 'System.Collections.Generic.List[object]'
    $links = New-Object 'System.Collections.Generic.List[object]'
    $format = 'raw'

    if (-not [string]::IsNullOrWhiteSpace($Settings)) {
        try {
            $json = $Settings | ConvertFrom-Json -ErrorAction Stop
            $parseAttempts.Add([pscustomobject]@{ format = 'json'; success = $true; error = $null })
            $format = 'json'
            $textForLinkInspection = $json | ConvertTo-Json -Depth 50
        }
        catch {
            $parseAttempts.Add([pscustomobject]@{ format = 'json'; success = $false; error = $_.Exception.Message })
            $textForLinkInspection = $Settings
        }

        try {
            [xml]$xml = $Settings
            $parseAttempts.Add([pscustomobject]@{ format = 'xml'; success = $true; error = $null })
            $format = 'xml'
            $textForLinkInspection = $xml.OuterXml
        }
        catch {
            $parseAttempts.Add([pscustomobject]@{ format = 'xml'; success = $false; error = $_.Exception.Message })
        }

        $seen = New-Object 'System.Collections.Generic.HashSet[string]' ([StringComparer]::OrdinalIgnoreCase)
        foreach ($match in [regex]::Matches($textForLinkInspection, 'https?://[^<>"''\s\)]+')) {
            $url = [System.Net.WebUtility]::HtmlDecode($match.Value.Trim())
            if ($seen.Add($url)) {
                $links.Add([pscustomobject]@{
                    url = $url
                    source = 'regex-url-scan'
                })
            }
        }
    }

    [pscustomobject]@{
        parseable = ($format -ne 'raw')
        format = $format
        parseAttempts = @($parseAttempts.ToArray())
        linkCount = $links.Count
        links = @($links.ToArray())
    }
}

if ([System.IO.Path]::IsPathRooted($OutputRoot)) {
    $resolvedOutputRoot = [System.IO.Path]::GetFullPath($OutputRoot)
}
else {
    $resolvedOutputRoot = [System.IO.Path]::GetFullPath((Join-Path (Get-Location) $OutputRoot))
}

New-Item -ItemType Directory -Path $resolvedOutputRoot -Force | Out-Null

$connection = New-Object System.Data.SqlClient.SqlConnection $SqlConnectionString
$utf8NoBom = [System.Text.UTF8Encoding]::new($false)

try {
    $connection.Open()

    $command = $connection.CreateCommand()
    $command.CommandType = [System.Data.CommandType]::Text
    $command.CommandTimeout = 120
    $command.CommandText = @'
SELECT
    DataStoreSettingRowId,
    BlogId,
    ExtensionType,
    ExtensionId,
    Settings
FROM dbo.be_DataStoreSettings
WHERE DataStoreSettingRowId = @DataStoreSettingRowId;
'@
    [void]$command.Parameters.Add((New-SqlParameter -Name '@DataStoreSettingRowId' -Type Int -Value $DataStoreSettingRowId))

    $reader = $command.ExecuteReader()
    if (-not $reader.Read()) {
        throw "No dbo.be_DataStoreSettings row found for DataStoreSettingRowId=$DataStoreSettingRowId."
    }

    $rowId = [int]$reader['DataStoreSettingRowId']
    $blogId = if ($reader['BlogId'] -eq [DBNull]::Value) { $null } else { [string]$reader['BlogId'] }
    $extensionType = if ($reader['ExtensionType'] -eq [DBNull]::Value) { '' } else { [string]$reader['ExtensionType'] }
    $extensionId = if ($reader['ExtensionId'] -eq [DBNull]::Value) { '' } else { [string]$reader['ExtensionId'] }
    $settings = if ($reader['Settings'] -eq [DBNull]::Value) { '' } else { [string]$reader['Settings'] }
    $reader.Close()

    [System.IO.File]::WriteAllText((Join-Path $resolvedOutputRoot 'settings.raw.txt'), $settings, $utf8NoBom)

    $parsed = ConvertTo-ParsedSettings -Settings $settings
    [System.IO.File]::WriteAllText((Join-Path $resolvedOutputRoot 'settings.parsed.json'), ($parsed | ConvertTo-Json -Depth 50), $utf8NoBom)

    $exportedAt = (Get-Date).ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ss.fffZ', [System.Globalization.CultureInfo]::InvariantCulture)
    $metadata = [ordered]@{
        exportName = [System.IO.Path]::GetFileName($resolvedOutputRoot)
        exportedAt = $exportedAt
        source = [ordered]@{
            databaseTable = 'dbo.be_DataStoreSettings'
            dataStoreSettingRowId = $rowId
            blogId = $blogId
            extensionType = $extensionType
            extensionId = $extensionId
            query = 'SELECT DataStoreSettingRowId, BlogId, ExtensionType, ExtensionId, Settings FROM dbo.be_DataStoreSettings WHERE DataStoreSettingRowId = @DataStoreSettingRowId'
        }
        settings = [ordered]@{
            rawFile = 'settings.raw.txt'
            rawLength = $settings.Length
            rawSha256 = Get-StringHash -Value $settings
            parsedFile = 'settings.parsed.json'
            parseable = [bool]$parsed.parseable
            format = $parsed.format
            linkCount = [int]$parsed.linkCount
        }
    }

    [System.IO.File]::WriteAllText((Join-Path $resolvedOutputRoot 'setting.database.json'), ($metadata | ConvertTo-Json -Depth 20), $utf8NoBom)

    $readme = @"
# BlogEngine DataStore Settings Row Export

This folder is a read-only preservation export from BlogEngine database table ``dbo.be_DataStoreSettings``.

Row exported:

- ``DataStoreSettingRowId``: ``$rowId``
- ``BlogId``: ``$blogId``
- ``ExtensionType``: ``$extensionType``
- ``ExtensionId``: ``$extensionId``
- exported at: ``$exportedAt``

Export contents:

- ``setting.database.json``: row metadata and raw settings hash.
- ``settings.raw.txt``: exact exported ``Settings`` field and the source of truth for this preservation slice.
- ``settings.parsed.json``: best-effort JSON/XML/link inspection output. Parsing is inspection-only.

Re-run from the repo root:

````powershell
.\scripts\blog-publishing\Export-DataStoreSettingRow.ps1 ````
  -SqlConnectionString `$env:AdventuresOnTheEdgeCS ````
  -DataStoreSettingRowId $rowId
````

Use ``-OutputRoot`` to write to a different dated folder.
"@

    [System.IO.File]::WriteAllText((Join-Path $resolvedOutputRoot 'README.md'), $readme, $utf8NoBom)

    Write-Host "Exported dbo.be_DataStoreSettings row $rowId to $resolvedOutputRoot"
    Write-Host "ExtensionId: $extensionId; parseable: $($parsed.parseable); format: $($parsed.format); links: $($parsed.linkCount)"
}
finally {
    if ($null -ne $connection) {
        $connection.Dispose()
    }
}
