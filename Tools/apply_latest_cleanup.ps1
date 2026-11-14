$ErrorActionPreference = 'Stop'
$taskRoot = [IO.Path]::GetFullPath((Split-Path $PSScriptRoot -Parent))
$taskPrefix = $taskRoot.TrimEnd('\') + '\'
$taskDirectory = Join-Path $taskRoot 'Builds\cleanup-20261023'
$taskManifest = Get-Content -LiteralPath (Join-Path $taskDirectory 'manifest.json') -Raw -Encoding utf8 | ConvertFrom-Json
if ($taskManifest.root -ne $taskRoot) { throw 'Manifest workspace does not match.' }
$taskArchive = Join-Path $taskDirectory 'removed-files.zip'
if (Test-Path -LiteralPath $taskArchive) { throw 'Recovery archive already exists; refusing to overwrite.' }
$taskFiles = @()
foreach ($taskEntry in $taskManifest.delete) {
    $taskRelative = [string]$taskEntry.path
    if ($taskRelative -notmatch '^(Art/(samples|animations|out)/|Book/Validation/)') { throw "Unexpected cleanup scope: $taskRelative" }
    $taskAbsolute = [IO.Path]::GetFullPath((Join-Path $taskRoot $taskRelative))
    if (!$taskAbsolute.StartsWith($taskPrefix, [StringComparison]::OrdinalIgnoreCase)) { throw 'Cleanup target leaves workspace.' }
    if (!(Test-Path -LiteralPath $taskAbsolute -PathType Leaf)) { throw "Missing cleanup target: $taskRelative" }
    if ((Get-Item -LiteralPath $taskAbsolute).LinkType) { throw "Linked cleanup target: $taskRelative" }
    $taskHash = (Get-FileHash -LiteralPath $taskAbsolute -Algorithm SHA256).Hash.ToLowerInvariant()
    if ($taskHash -ne $taskEntry.sha256) { throw "Cleanup target changed: $taskRelative" }
    $taskFiles += [pscustomobject]@{ Absolute=$taskAbsolute; Relative=$taskRelative; Hash=$taskHash }
}
# All absolute targets are checked before archiving or deleting anything.
Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem
$taskZip = [IO.Compression.ZipFile]::Open($taskArchive, [IO.Compression.ZipArchiveMode]::Create)
try {
    foreach ($taskFile in $taskFiles) {
        [IO.Compression.ZipFileExtensions]::CreateEntryFromFile($taskZip, $taskFile.Absolute, $taskFile.Relative, [IO.Compression.CompressionLevel]::NoCompression) | Out-Null
    }
} finally { $taskZip.Dispose() }
$taskZip = [IO.Compression.ZipFile]::OpenRead($taskArchive)
try {
    if ($taskZip.Entries.Count -ne $taskFiles.Count) { throw 'Recovery archive file count mismatch.' }
    foreach ($taskFile in $taskFiles) {
        $taskStream = $taskZip.GetEntry($taskFile.Relative).Open()
        try { $taskHash = (Get-FileHash -InputStream $taskStream -Algorithm SHA256).Hash.ToLowerInvariant() }
        finally { $taskStream.Dispose() }
        if ($taskHash -ne $taskFile.Hash) { throw "Recovery archive hash mismatch: $($taskFile.Relative)" }
    }
} finally { $taskZip.Dispose() }
Write-Output "Recovery archive verified for $($taskFiles.Count) files."
foreach ($taskFile in $taskFiles) { Remove-Item -LiteralPath $taskFile.Absolute -Force }
# Leave historical preview entry points usable without keeping superseded images.
foreach ($taskRedirect in $taskManifest.redirects.PSObject.Properties) {
    $taskPreview = [IO.Path]::GetFullPath((Join-Path $taskRoot $taskRedirect.Name))
    if (!$taskPreview.StartsWith($taskPrefix, [StringComparison]::OrdinalIgnoreCase)) { throw 'Preview target leaves workspace.' }
    $taskDestination = [System.Net.WebUtility]::HtmlEncode([string]$taskRedirect.Value)
    $taskHtml = '<!doctype html><html lang="ko"><meta charset="utf-8"><meta http-equiv="refresh" content="0;url=' + $taskDestination + '"><title>최신 시안</title><p>이전 시안은 정리되었습니다. <a href="' + $taskDestination + '">최신 시안 열기</a></p></html>'
    [IO.File]::WriteAllText($taskPreview, $taskHtml, [Text.UTF8Encoding]::new($false))
}
foreach ($taskProperty in $taskManifest.preserve.PSObject.Properties) {
    $taskAbsolute = Join-Path $taskRoot $taskProperty.Name
    $taskHash = (Get-FileHash -LiteralPath $taskAbsolute -Algorithm SHA256).Hash.ToLowerInvariant()
    if ($taskHash -ne $taskProperty.Value) { throw "Protected asset changed: $($taskProperty.Name)" }
}
$taskRemaining = @($taskFiles | Where-Object { Test-Path -LiteralPath $_.Absolute })
if ($taskRemaining.Count) { throw 'Some obsolete files remain.' }
$taskReport = [ordered]@{ passed=$true; removedFiles=$taskFiles.Count; removedBytes=$taskManifest.bytes; protectedFiles=@($taskManifest.preserve.PSObject.Properties).Count; canonicalDocs=$taskManifest.canonicalDocs.Count; recoveryArchive='Builds/cleanup-20261023/removed-files.zip'; verifiedArchive=$true; verifiedProtectedHashes=$true; redirects=@($taskManifest.redirects.PSObject.Properties).Count }
$taskReport | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $taskDirectory 'result.json') -Encoding utf8
$taskReport | ConvertTo-Json -Compress
