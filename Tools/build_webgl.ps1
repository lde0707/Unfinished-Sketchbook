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
    [switch]$ValidateProjectiles,
    [switch]$ValidatePlayerResolution,
    [switch]$ValidateHitFeedback,
    [switch]$ValidatePlayerLife,
    [switch]$ValidateExploration,
    [switch]$ValidatePickups,
    [switch]$ValidateSaveShrines,
    [switch]$ValidatePropOrder,
    [switch]$ValidatePropPlacement,
    [switch]$ValidateDeviceArt,
    [switch]$ValidateFriendAnimations,
    [switch]$ValidatePlayerSurface,
    [switch]$ValidateBossRegion1,
    [switch]$ValidateRegion2,
    [switch]$ValidateCameraGrade,
    [switch]$ValidateAnimationCompletion,
    [switch]$ValidateEnemyCombatVfx,
    [switch]$ValidateReviveHold,
    [switch]$ValidateTitle,
    [switch]$ValidateAmbience,
    [switch]$ValidateInteractionProximity,
    [switch]$ValidateRuntimeOptimization,
    [switch]$ValidateJournalUI,
    [switch]$ValidateSpecialBlockFeedback,
    [switch]$ValidateStructureAnimation,
    [switch]$ValidateEconomy,
    [switch]$ValidateEnemyPressure,
    [switch]$ValidateMapConnections,
    [switch]$ValidateAbilityTrials,
    [switch]$ValidateTradingUI,
    [switch]$ValidateEnemySpecialties,
    [switch]$ValidateMapColors,
    [switch]$ValidateResolutionPreview
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
if ($ValidatePlayerResolution) { $taskMethod = 'Unfin.Editor.PlayerResolutionValidation.RunAndBuild' }
if ($ValidateHitFeedback) { $taskMethod = 'Unfin.Editor.HitFeedbackValidation.RunAndBuild' }
if ($ValidatePlayerLife) { $taskMethod = 'Unfin.Editor.PlayerLifeValidation.RunAndBuild' }
if ($ValidateExploration) { $taskMethod = 'Unfin.Editor.ExplorationIntegration.RunAndBuild' }
if ($ValidatePickups) { $taskMethod = 'Unfin.Editor.PickupArtIntegration.RunAndBuild' }
if ($ValidateSaveShrines) { $taskMethod = 'Unfin.Editor.SaveShrineValidation.RunAndBuild' }
if ($ValidatePropOrder) { $taskMethod = 'Unfin.Editor.PropRenderOrderValidation.RunAndBuild' }
if ($ValidatePropPlacement) { $taskMethod = 'Unfin.Editor.WorldPropPlacementIntegration.RunAndBuild' }
if ($ValidateDeviceArt) { $taskMethod = 'Unfin.Editor.DeviceArtIntegration.RunAndBuild' }
if ($ValidateFriendAnimations) { $taskMethod = 'Unfin.Editor.FriendAnimationIntegration.RunAndBuild' }
if ($ValidatePlayerSurface) { $taskMethod = 'Unfin.Editor.PlayerSurfaceIntegration.RunAndBuild' }
if ($ValidateBossRegion1) { $taskMethod = 'Unfin.Editor.BossRegion1Integration.RunAndBuild' }
if ($ValidateRegion2) { $taskMethod = 'Unfin.Editor.Region2Integration.RunAndBuild' }
if ($ValidateCameraGrade) { $taskMethod = 'Unfin.Editor.WorldCameraGradeIntegration.RunAndBuild' }
if ($ValidateAnimationCompletion) { $taskMethod = 'Unfin.Editor.AnimationCompletionIntegration.RunAndBuild' }
if ($ValidateEnemyCombatVfx) { $taskMethod = 'Unfin.Editor.EnemyCombatVfxIntegration.RunAndBuild' }
if ($ValidateReviveHold) { $taskMethod = 'Unfin.Editor.ReviveHoldIntegration.RunAndBuild' }
if ($ValidateTitle) { $taskMethod = 'Unfin.Editor.StilldrawnTitleIntegration.RunAndBuild' }
if ($ValidateAmbience) { $taskMethod = 'Unfin.Editor.AmbienceIntegration.RunAndBuild' }
if ($ValidateInteractionProximity) { $taskMethod = 'Unfin.Editor.InteractionProximityValidation.RunAndBuild' }
if ($ValidateRuntimeOptimization) { $taskMethod = 'Unfin.Editor.RuntimeOptimizationValidation.RunAndBuild' }
if ($ValidateJournalUI) { $taskMethod = 'Unfin.Editor.JournalUIIntegration.RunAndBuild' }
if ($ValidateSpecialBlockFeedback) { $taskMethod = 'Unfin.Editor.SpecialBlockFeedbackValidation.RunAndBuild' }
if ($ValidateStructureAnimation) { $taskMethod = 'Unfin.Editor.StructureAnimationValidation.RunAndBuild' }
if ($ValidateEconomy) { $taskMethod = 'Unfin.Editor.EconomyValidation.RunAndBuild' }
if ($ValidateEnemyPressure) { $taskMethod = 'Unfin.Editor.EnemyPressureValidation.RunAndBuild' }
if ($ValidateMapConnections) { $taskMethod = 'Unfin.Editor.RoomConnectionValidation.RunAndBuild' }
if ($ValidateAbilityTrials) { $taskMethod = 'Unfin.Editor.AbilityTrialValidation.RunAndBuild' }
if ($ValidateTradingUI) { $taskMethod = 'Unfin.Editor.TradingUIIntegration.RunAndBuild' }
if ($ValidateEnemySpecialties) { $taskMethod = 'Unfin.Editor.EnemySpecialtyValidation.RunAndBuild' }
if ($ValidateMapColors) { $taskMethod = 'Unfin.Editor.MapZoneColorValidation.RunAndBuild' }
if ($ValidateResolutionPreview) { $taskMethod = 'Unfin.Editor.ResolutionPreviewValidation.RunAndBuild' }
$taskArguments = @('-batchmode','-projectPath',('"'+$taskProject+'"'),'-buildTarget','WebGL',
    '-executeMethod',$taskMethod,'-quit','-logFile',('"'+$taskLog+'"'))
if ($ValidateTitle) { $taskArguments += '-force-d3d11' }
if (!$ValidateEnemySpecialties -and !$ValidateTradingUI -and !$ValidateAbilityTrials -and !$ValidateStructureAnimation -and !$ValidateJournalUI -and !$ValidateRuntimeOptimization -and !$ValidateCombat -and !$ValidateTempo -and !$ValidateWall -and !$ValidateContact -and !$ValidatePlayerSurface -and !$ValidateBossRegion1 -and !$ValidateRegion2 -and !$ValidateCameraGrade -and !$ValidateAnimationCompletion -and !$ValidateEnemyCombatVfx -and !$ValidateReviveHold -and !$ValidateTitle) { $taskArguments += '-nographics' }
$taskProcess = Start-Process -FilePath $Unity -ArgumentList $taskArguments -WindowStyle Hidden -PassThru
Write-Output "Building Unity WebGL (PID $($taskProcess.Id)). Log: $taskLog"
$taskProcess.WaitForExit()
if ($taskProcess.ExitCode -ne 0) { throw "Unity WebGL build failed ($($taskProcess.ExitCode)). See $taskLog" }
if (!(Test-Path (Join-Path $taskOutput 'index.html'))) { throw 'Build did not produce index.html.' }
& node (Join-Path $taskRoot 'Tools/prepare_webgl_site.cjs')
if ($LASTEXITCODE -ne 0) { throw 'Unity hosting packaging failed.' }
Write-Output "Ready: $taskOutput"
