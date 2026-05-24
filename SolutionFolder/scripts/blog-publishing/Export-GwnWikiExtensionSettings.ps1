[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string]$SqlConnectionString,

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
    $OutputRoot = Join-Path $script:ScriptRoot '..\..\docs\blogs\source-of-truth\gwn-wiki-extension-export-20260516'
}

function ConvertTo-IsoString {
    param([object]$Value)

    if ($null -eq $Value -or $Value -eq [DBNull]::Value) {
        return $null
    }

    return ([datetime]$Value).ToString('yyyy-MM-ddTHH:mm:ss.fff', [System.Globalization.CultureInfo]::InvariantCulture)
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

function ConvertTo-SafePathSegment {
    param(
        [string]$Value,
        [string]$Fallback
    )

    $candidate = if ([string]::IsNullOrWhiteSpace($Value)) { $Fallback } else { $Value.Trim().ToLowerInvariant() }
    $candidate = [regex]::Replace($candidate, '[^a-z0-9._-]+', '-')
    $candidate = $candidate.Trim('-')

    if ([string]::IsNullOrWhiteSpace($candidate)) {
        return $Fallback
    }

    return $candidate
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

function New-TokenLinkMapping {
    param(
        [string]$Token,
        [string]$Url,
        [string]$Source
    )

    [pscustomobject]@{
        token = $Token
        url = $Url
        source = $Source
    }
}

function Get-RegexTokenLinkMappings {
    param([string]$Settings)

    $mappings = New-Object 'System.Collections.Generic.List[object]'
    $seen = New-Object 'System.Collections.Generic.HashSet[string]' ([StringComparer]::OrdinalIgnoreCase)

    $patterns = @(
        '(?is)(?<token>[A-Za-z][A-Za-z0-9 _.-]{1,120}).{0,500}?(?<url>https?://[^<>"''\s]+)',
        '(?is)(?<url>https?://[^<>"''\s]+).{0,500}?(?<token>[A-Za-z][A-Za-z0-9 _.-]{1,120})'
    )

    foreach ($pattern in $patterns) {
        foreach ($match in [regex]::Matches($Settings, $pattern)) {
            $token = [System.Net.WebUtility]::HtmlDecode($match.Groups['token'].Value).Trim()
            $url = [System.Net.WebUtility]::HtmlDecode($match.Groups['url'].Value).Trim()

            if ([string]::IsNullOrWhiteSpace($token) -or [string]::IsNullOrWhiteSpace($url)) {
                continue
            }

            $key = "$token|$url"
            if ($seen.Add($key)) {
                $mappings.Add((New-TokenLinkMapping -Token $token -Url $url -Source 'regex-nearby-token-url'))
            }
        }
    }

    return ,@($mappings.ToArray())
}

function Get-XmlTokenLinkMappings {
    param([xml]$Xml)

    $mappings = New-Object 'System.Collections.Generic.List[object]'
    $seen = New-Object 'System.Collections.Generic.HashSet[string]' ([StringComparer]::OrdinalIgnoreCase)
    $settingsGroups = @($Xml.ManagedExtension.Settings)

    foreach ($settingsGroup in $settingsGroups) {
        $parameters = @($settingsGroup.Parameters)
        if ($parameters.Count -eq 0) {
            continue
        }

        $parameterValues = @{}
        $maxValueCount = 0

        foreach ($parameter in $parameters) {
            $parameterName = [System.Net.WebUtility]::HtmlDecode([string]$parameter.Name).Trim()
            if ([string]::IsNullOrWhiteSpace($parameterName)) {
                continue
            }

            $values = @($parameter.Values | ForEach-Object { [System.Net.WebUtility]::HtmlDecode([string]$_).Trim() })
            $parameterValues[$parameterName] = $values
            if ($values.Count -gt $maxValueCount) {
                $maxValueCount = $values.Count
            }
        }

        for ($index = 0; $index -lt $maxValueCount; $index++) {
            $token = if ($parameterValues.ContainsKey('CommandParameter') -and $index -lt $parameterValues['CommandParameter'].Count) { $parameterValues['CommandParameter'][$index] } else { $null }
            $url = if ($parameterValues.ContainsKey('PermaLink') -and $index -lt $parameterValues['PermaLink'].Count) { $parameterValues['PermaLink'][$index] } else { $null }
            $command = if ($parameterValues.ContainsKey('Command') -and $index -lt $parameterValues['Command'].Count) { $parameterValues['Command'][$index] } else { $null }

            if ([string]::IsNullOrWhiteSpace($token) -or [string]::IsNullOrWhiteSpace($url)) {
                continue
            }

            if ($url -notmatch '^https?://') {
                continue
            }

            $key = "$token|$url"
            if ($seen.Add($key)) {
                $mapping = New-TokenLinkMapping -Token $token -Url $url -Source 'xml-settings-parameter-row'
                $mapping | Add-Member -NotePropertyName command -NotePropertyValue $command
                $mapping | Add-Member -NotePropertyName rowIndex -NotePropertyValue $index
                $mappings.Add($mapping)
            }
        }
    }

    return ,@($mappings.ToArray())
}

function ConvertTo-ParsedSettings {
    param([string]$Settings)

    $parseAttempts = New-Object 'System.Collections.Generic.List[object]'
    $mappings = @()
    $format = 'raw'

    if (-not [string]::IsNullOrWhiteSpace($Settings)) {
        try {
            $json = $Settings | ConvertFrom-Json -ErrorAction Stop
            $parseAttempts.Add([pscustomobject]@{ format = 'json'; success = $true; error = $null })
            $format = 'json'
            $jsonText = $json | ConvertTo-Json -Depth 20
            $mappings = Get-RegexTokenLinkMappings -Settings $jsonText
        }
        catch {
            $parseAttempts.Add([pscustomobject]@{ format = 'json'; success = $false; error = $_.Exception.Message })
        }

        try {
            [xml]$xml = $Settings
            $parseAttempts.Add([pscustomobject]@{ format = 'xml'; success = $true; error = $null })
            $format = 'xml'
            $mappings = Get-XmlTokenLinkMappings -Xml $xml
            if ($mappings.Count -eq 0) {
                $mappings = Get-RegexTokenLinkMappings -Settings $Settings
            }
        }
        catch {
            $parseAttempts.Add([pscustomobject]@{ format = 'xml'; success = $false; error = $_.Exception.Message })
        }

        if ($mappings.Count -eq 0) {
            $mappings = Get-RegexTokenLinkMappings -Settings $Settings
            if ($mappings.Count -gt 0 -and $format -eq 'raw') {
                $format = 'regex'
            }
        }
    }

    [pscustomobject]@{
        parseable = ($format -ne 'raw')
        format = $format
        parseAttempts = @($parseAttempts.ToArray())
        mappingCount = @($mappings).Count
        mappings = @($mappings)
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
$rows = New-Object 'System.Collections.Generic.List[object]'
$allMappings = New-Object 'System.Collections.Generic.List[object]'

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
WHERE ExtensionId = @ExtensionId
ORDER BY DataStoreSettingRowId;
'@
    [void]$command.Parameters.Add((New-SqlParameter -Name '@ExtensionId' -Type NVarChar -Value 'GwnWikiExtension'))

    $reader = $command.ExecuteReader()

    while ($reader.Read()) {
        $rowId = [int]$reader['DataStoreSettingRowId']
        $blogId = if ($reader['BlogId'] -eq [DBNull]::Value) { $null } else { [string]$reader['BlogId'] }
        $extensionType = if ($reader['ExtensionType'] -eq [DBNull]::Value) { '' } else { [string]$reader['ExtensionType'] }
        $extensionId = if ($reader['ExtensionId'] -eq [DBNull]::Value) { '' } else { [string]$reader['ExtensionId'] }
        $settings = if ($reader['Settings'] -eq [DBNull]::Value) { '' } else { [string]$reader['Settings'] }
        $rowFolder = ConvertTo-SafePathSegment -Value "row-$rowId-$blogId" -Fallback "row-$rowId"
        $rowDirectory = Join-Path $resolvedOutputRoot $rowFolder
        New-Item -ItemType Directory -Path $rowDirectory -Force | Out-Null

        $rawPath = Join-Path $rowDirectory 'settings.raw.txt'
        [System.IO.File]::WriteAllText($rawPath, $settings, $utf8NoBom)

        $parsed = ConvertTo-ParsedSettings -Settings $settings
        $parsedPath = Join-Path $rowDirectory 'settings.parsed.json'
        [System.IO.File]::WriteAllText($parsedPath, ($parsed | ConvertTo-Json -Depth 20), $utf8NoBom)

        foreach ($mapping in @($parsed.mappings)) {
            $allMappings.Add([pscustomobject]@{
                dataStoreSettingRowId = $rowId
                blogId = $blogId
                token = $mapping.token
                url = $mapping.url
                source = $mapping.source
            })
        }

        $metadata = [ordered]@{
            source = [ordered]@{
                databaseTable = 'dbo.be_DataStoreSettings'
                dataStoreSettingRowId = $rowId
                blogId = $blogId
                extensionType = $extensionType
                extensionId = $extensionId
            }
            settings = [ordered]@{
                rawFile = 'settings.raw.txt'
                rawLength = $settings.Length
                rawSha256 = Get-StringHash -Value $settings
                parsedFile = 'settings.parsed.json'
                parseable = [bool]$parsed.parseable
                format = $parsed.format
                mappingCount = [int]$parsed.mappingCount
            }
        }

        [System.IO.File]::WriteAllText((Join-Path $rowDirectory 'setting.database.json'), ($metadata | ConvertTo-Json -Depth 12), $utf8NoBom)

        $rows.Add([pscustomobject]@{
            dataStoreSettingRowId = $rowId
            blogId = $blogId
            extensionType = $extensionType
            extensionId = $extensionId
            folder = $rowFolder
            rawSettingsLength = $settings.Length
            rawSettingsSha256 = $metadata.settings.rawSha256
            parseable = [bool]$parsed.parseable
            parsedFormat = $parsed.format
            mappingCount = [int]$parsed.mappingCount
        })
    }

    $reader.Close()
}
finally {
    if ($null -ne $connection) {
        $connection.Dispose()
    }
}

$exportName = [System.IO.Path]::GetFileName($resolvedOutputRoot)
$exportedAt = (Get-Date).ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ss.fffZ', [System.Globalization.CultureInfo]::InvariantCulture)
$rowArray = @($rows.ToArray())
$mappingArray = @($allMappings.ToArray())

$manifest = [ordered]@{
    exportName = $exportName
    exportedAt = $exportedAt
    source = [ordered]@{
        databaseTable = 'dbo.be_DataStoreSettings'
        extensionId = 'GwnWikiExtension'
        query = "SELECT DataStoreSettingRowId, BlogId, ExtensionType, ExtensionId, Settings FROM dbo.be_DataStoreSettings WHERE ExtensionId = 'GwnWikiExtension'"
    }
    counts = [ordered]@{
        rows = $rowArray.Count
        parseableRows = @($rowArray | Where-Object { $_.parseable }).Count
        mappings = $mappingArray.Count
    }
    rows = $rowArray
    mappings = $mappingArray
}

[System.IO.File]::WriteAllText((Join-Path $resolvedOutputRoot 'manifest.json'), ($manifest | ConvertTo-Json -Depth 20), $utf8NoBom)

$readme = @'
# GwnWikiExtension Settings Export

This folder is a read-only preservation export from BlogEngine database table `dbo.be_DataStoreSettings` for `ExtensionId = 'GwnWikiExtension'`.

Export contents:

- `manifest.json`: index of exported settings rows and best-effort parsed token/link mappings.
- `<row>/setting.database.json`: metadata for one settings row.
- `<row>/settings.raw.txt`: exact exported `Settings` field for one settings row.
- `<row>/settings.parsed.json`: best-effort parser output. Parsing is inspection-only; `settings.raw.txt` is the source of truth.

Re-run from the repo root:

```powershell
.\scripts\blog-publishing\Export-GwnWikiExtensionSettings.ps1 `
  -SqlConnectionString $env:AdventuresOnTheEdgeCS
```

Use `-OutputRoot` to write to a different dated folder.

Do not edit database records from this export path. It exists to preserve the hyperlink-token plugin settings before canonical blog cleanup.
'@

[System.IO.File]::WriteAllText((Join-Path $resolvedOutputRoot 'README.md'), $readme, $utf8NoBom)

Write-Host "Exported $($rowArray.Count) GwnWikiExtension settings row(s) to $resolvedOutputRoot"
Write-Host "Parseable rows: $($manifest.counts.parseableRows); token/link mappings: $($manifest.counts.mappings)"
