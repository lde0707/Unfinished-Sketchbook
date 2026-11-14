$ErrorActionPreference = 'Stop'
$taskRoot = [IO.Path]::GetFullPath((Split-Path $PSScriptRoot -Parent))
$taskPrefix = $taskRoot.TrimEnd('\') + '\'
$taskDirectory = Join-Path $taskRoot 'Builds\runtime-cleanup-20261025'
$taskManifest = Get-Content -LiteralPath (Join-Path $taskDirectory 'manifest.json') -Raw -Encoding utf8 | ConvertFrom-Json
if ($taskManifest.root -ne $taskRoot) { throw 'Manifest workspace mismatch.' }
$taskArchive = Join-Path $taskDirectory 'removed-files.zip'
if (Test-Path -LiteralPath $taskArchive) { throw 'Recovery archive already exists.' }
$taskFiles = @()
foreach ($taskEntry in $taskManifest.delete) {
    $taskRelative = [string]$taskEntry.path
    if ($taskRelative -notmatch '^(Book/Assets/Unfin/|Art/samples/|Tools/)') { throw 'Unexpected cleanup scope.' }
    $taskAbsolute = [IO.Path]::GetFullPath((Join-Path $taskRoot $taskRelative))
    if (!$taskAbsolute.StartsWith($taskPrefix, [StringComparison]::OrdinalIgnoreCase)) { throw 'Target leaves workspace.' }
    if (!(Test-Path -LiteralPath $taskAbsolute -PathType Leaf)) { throw "Missing target: $taskRelative" }
    if ((Get-Item -LiteralPath $taskAbsolute).LinkType) { throw 'Linked cleanup target.' }
    if ((Get-FileHash -LiteralPath $taskAbsolute -Algorithm SHA256).Hash.ToLowerInvariant() -ne $taskEntry.sha256) { throw "Changed target: $taskRelative" }
    $taskFiles += [pscustomobject]@{ Absolute=$taskAbsolute; Relative=$taskRelative; Hash=$taskEntry.sha256 }
}
# Resolve and validate every absolute path before any file is removed.
Add-Type -AssemblyName System.IO.Compression.FileSystem
$taskZip = [IO.Compression.ZipFile]::Open($taskArchive, [IO.Compression.ZipArchiveMode]::Create)
try {
    foreach ($taskFile in $taskFiles) { [IO.Compression.ZipFileExtensions]::CreateEntryFromFile($taskZip, $taskFile.Absolute, $taskFile.Relative, [IO.Compression.CompressionLevel]::Optimal) | Out-Null }
} finally { $taskZip.Dispose() }
$taskZip = [IO.Compression.ZipFile]::OpenRead($taskArchive)
try {
    if ($taskZip.Entries.Count -ne $taskFiles.Count) { throw 'Archive count mismatch.' }
    foreach ($taskFile in $taskFiles) {
        $taskStream = $taskZip.GetEntry($taskFile.Relative).Open()
        try { $taskHash = (Get-FileHash -InputStream $taskStream -Algorithm SHA256).Hash.ToLowerInvariant() }
        finally { $taskStream.Dispose() }
        if ($taskHash -ne $taskFile.Hash) { throw 'Archive hash mismatch.' }
    }
} finally { $taskZip.Dispose() }
foreach ($taskFile in $taskFiles) { Remove-Item -LiteralPath $taskFile.Absolute -Force }
$taskReport = [ordered]@{ passed=$true; removedFiles=$taskFiles.Count; removedBytes=$taskManifest.bytes; checkedUnityGuids=$taskManifest.checkedUnityGuids; recoveryArchive='Builds/runtime-cleanup-20261025/removed-files.zip'; verifiedArchive=$true }
$taskReport | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $taskDirectory 'result.json') -Encoding utf8
$taskReport | ConvertTo-Json -Compress
