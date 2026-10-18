param([string]$Method='Unfin.Editor.PaperSpriteImporter.Apply')
$ErrorActionPreference='Stop'
$taskRoot=Split-Path $PSScriptRoot -Parent
$taskProject=Join-Path $taskRoot 'Builds/UnityWebGLProject'
foreach($folder in @('Assets','Packages','ProjectSettings')) {
 $source=[IO.Path]::GetFullPath((Join-Path $taskRoot "Book/$folder"))
 $destination=[IO.Path]::GetFullPath((Join-Path $taskProject $folder))
 if(!$destination.StartsWith(([IO.Path]::GetFullPath($taskProject)+'\'),[StringComparison]::OrdinalIgnoreCase)){throw 'Invalid build mirror target'}
 & robocopy $source $destination /MIR /R:2 /W:1 /NFL /NDL /NJH /NJS /NP | Out-Null
 if($LASTEXITCODE -ge 8){throw 'Mirror failed'}
}
$taskLog=Join-Path $taskRoot 'Builds/player-integration.log'
$taskArgs=@('-batchmode','-projectPath',('"'+$taskProject+'"'),'-executeMethod',$Method,'-quit','-logFile',('"'+$taskLog+'"'))
$taskProcess=Start-Process -FilePath 'C:/Program Files/Unity/Hub/Editor/6000.5.3f1/Editor/Unity.exe' -ArgumentList $taskArgs -WindowStyle Hidden -PassThru
Write-Output "Unity import PID $($taskProcess.Id); $taskLog"
$taskProcess.WaitForExit()
if($taskProcess.ExitCode -ne 0){throw "Unity failed: $($taskProcess.ExitCode); $taskLog"}
# Copy only the generated player assets, scene and metadata back to the real Book project.
foreach($folder in @('Assets/Unfin/Art/PlayerSprites','Assets/Unfin/Animations/PlayerSprites','Assets/Unfin/Art/RegionalPlatforms','Assets/Unfin/Platforms')) {
 & robocopy (Join-Path $taskProject $folder) (Join-Path $taskRoot "Book/$folder") /E /R:2 /W:1 /NFL /NDL /NJH /NJS /NP | Out-Null
 if($LASTEXITCODE -ge 8){throw 'Asset copy failed'}
 Copy-Item -LiteralPath (Join-Path $taskProject "$folder.meta") -Destination (Join-Path $taskRoot "Book/$folder.meta") -Force
}
foreach($file in @('Assets/Scenes/World12Prototype.unity','Assets/Unfin/Prefabs/PaperSpritePlayer.prefab','Assets/Unfin/Prefabs/PaperSpritePlayer.prefab.meta','Assets/Unfin/Runtime/PaperSpritePlayer.cs.meta','Assets/Unfin/Runtime/PaperSpriteLibrary.cs.meta','Assets/Unfin/Editor/PaperSpriteImporter.cs.meta','Assets/Unfin/Editor/PaperSpriteValidation.cs.meta','Validation/player-sprites-import.json','Validation/player-sprites-runtime.json','Validation/player-sprites-in-game.png')) {
 Copy-Item -LiteralPath (Join-Path $taskProject $file) -Destination (Join-Path $taskRoot "Book/$file") -Force
}
Write-Output 'Player sprite integration copied into Book.'
foreach($file in @('Assets/Unfin/Runtime/PaperSpriteCombat.cs.meta','Assets/Unfin/Editor/RegionalPlatformImporter.cs.meta','Assets/Unfin/Editor/PlayerCombatValidation.cs.meta','Validation/world12-validation.json','Validation/player-combat-runtime.json','Validation/regional-platforms.json','Validation/player-attack-in-game.png','Validation/player-air-attack-in-game.png','Validation/platform-world1-in-game.png')) {
 Copy-Item -LiteralPath (Join-Path $taskProject $file) -Destination (Join-Path $taskRoot "Book/$file") -Force
}
