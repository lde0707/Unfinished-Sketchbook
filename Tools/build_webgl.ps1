param(
    [string]$Unity = 'C:\Program Files\Unity\Hub\Editor\6000.5.3f1\Editor\Unity.exe',
    [switch]$ValidateCamera,
    [switch]$ValidateCombat,
    [switch]$ValidateWall,
    [switch]$ValidateTempo,
    [switch]$ValidateSpecialTiles,
    [switch]$ValidateAudio,
    [switch]$ValidateHazards,
    [switch]$ValidateContact,
    [switch]$FastLocal,
    [switch]$ValidateVfx,
    [switch]$ValidateEnemies,
    [switch]$ValidateProjectiles
)
$ErrorActionPreference = 'Stop'
$taskRoot = Split-Path $PSScriptRoot -Parent
$taskBuilds = Join-Path $taskRoot 'Builds'
$taskProject = Join-Path $taskBuilds 'UnityWebGLProject'
$taskOutput = Join-Path $taskBuilds 'WebGLSite\dist'
$taskLog = Join-Path $taskBuilds 'webgl-build.log'
if (!(Test-Path -LiteralPath $Unity)) { throw "Unity not found: $Unity" }
$taskModule = Join-Path (Split-Path $Unity -Parent) 'Data\PlaybackEngines\WebGLSupport'
if (!(Test-Path -LiteralPath $taskModule)) { throw 'Install Web Build Support in Unity Hub first.' }
New-Item -ItemType Directory -Path $taskProject,$taskOutput -Force | Out-Null
# Only mirror the three source folders into our dedicated, ignored build project.
# The user's open Book editor and its Library are never touched.
foreach ($taskFolder in @('Assets','Packages','ProjectSettings')) {
    $taskSource = [IO.Path]::GetFullPath((Join-Path $taskRoot "Book\$taskFolder"))
    $taskDestination = [IO.Path]::GetFullPath((Join-Path $taskProject $taskFolder))
    if (!$taskDestination.StartsWith(([IO.Path]::GetFullPath($taskProject) + '\'), [StringComparison]::OrdinalIgnoreCase)) {
        throw 'Build copy must stay inside the dedicated build project.'
    }
    & robocopy $taskSource $taskDestination /MIR /R:2 /W:1 /NFL /NDL /NJH /NJS /NP
    if ($LASTEXITCODE -ge 8) { throw "Source copy failed: $taskFolder" }
}
$env:UNFIN_WEBGL_OUTPUT = $taskOutput
$env:UNFIN_WEBGL_FAST_LOCAL = if ($FastLocal) { '1' } else { '0' }
$taskMethod = if ($ValidateHazards) { 'Unfin.Editor.InkHazardValidation.RunAndBuild' } elseif ($ValidateSpecialTiles) { 'Unfin.Editor.SpecialTileImporter.ApplyValidateAndBuild' } elseif ($ValidateAudio) { 'Unfin.Editor.GameAudioValidation.RunAndBuild' } elseif ($ValidateVfx) { 'Unfin.Editor.PaperVfxValidation.RunAndBuild' } elseif ($ValidateEnemies) { 'Unfin.Editor.EnemyAnimationValidation.RunAndBuild' } elseif ($ValidateWall) { 'Unfin.Editor.PlayerWallValidation.RunAndBuild' } elseif ($ValidateTempo) { 'Unfin.Editor.PlayerTempoValidation.RunAndBuild' } elseif ($ValidateCombat) { 'Unfin.Editor.PlayerCombatValidation.RunAndBuild' } elseif ($ValidateCamera) { 'Unfin.Editor.CameraFollowValidation.RunAndBuild' } else { 'Unfin.Editor.WebGLBuilder.Build' }
if ($ValidateContact) { $taskMethod = 'Unfin.Editor.PlayerContactValidation.RunAndBuild' }
if ($ValidateProjectiles) { $taskMethod = 'Unfin.Editor.EnemyProjectileValidation.RunAndBuild' }
$taskArguments = @('-batchmode','-projectPath',('"'+$taskProject+'"'),'-buildTarget','WebGL',
    '-executeMethod',$taskMethod,'-quit','-logFile',('"'+$taskLog+'"'))
if (!$ValidateCombat -and !$ValidateTempo -and !$ValidateWall -and !$ValidateContact) { $taskArguments += '-nographics' }
$taskProcess = Start-Process -FilePath $Unity -ArgumentList $taskArguments -WindowStyle Hidden -PassThru
Write-Output "Building Unity WebGL (PID $($taskProcess.Id)). Log: $taskLog"
$taskProcess.WaitForExit()
if ($taskProcess.ExitCode -ne 0) { throw "Unity WebGL build failed ($($taskProcess.ExitCode)). See $taskLog" }
if (!(Test-Path (Join-Path $taskOutput 'index.html'))) { throw 'Build did not produce index.html.' }
& node (Join-Path $taskRoot 'Tools/prepare_webgl_site.cjs')
if ($LASTEXITCODE -ne 0) { throw 'Unity hosting packaging failed.' }
Write-Output "Ready: $taskOutput"
