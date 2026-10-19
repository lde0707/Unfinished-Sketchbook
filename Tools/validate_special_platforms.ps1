$ErrorActionPreference='Stop'
$taskRoot=Split-Path $PSScriptRoot -Parent
$taskProject=Join-Path $taskRoot 'Builds/UnityWebGLProject'
$taskLog=Join-Path $taskRoot 'Builds/special-platform-validation.log'
Copy-Item (Join-Path $taskRoot 'Book/Assets/Unfin/Editor/SpecialTileImporter.cs') (Join-Path $taskProject 'Assets/Unfin/Editor/SpecialTileImporter.cs') -Force
$taskArgs=@('-batchmode','-nographics','-projectPath',('"'+$taskProject+'"'),'-executeMethod','Unfin.Editor.SpecialTileImporter.ValidateBehaviour','-quit','-logFile',('"'+$taskLog+'"'))
$taskProcess=Start-Process 'C:/Program Files/Unity/Hub/Editor/6000.5.3f1/Editor/Unity.exe' -ArgumentList $taskArgs -WindowStyle Hidden -PassThru
$taskProcess.WaitForExit()
if($taskProcess.ExitCode -ne 0){throw "Special platform validation failed. See $taskLog"}
Copy-Item (Join-Path $taskProject 'Validation/special-platform-behaviour.json') (Join-Path $taskRoot 'Book/Validation/special-platform-behaviour.json') -Force
Copy-Item (Join-Path $taskProject 'Validation/special-liquid-geometry.json') (Join-Path $taskRoot 'Book/Validation/special-liquid-geometry.json') -Force
Get-Content (Join-Path $taskRoot 'Book/Validation/special-platform-behaviour.json')
