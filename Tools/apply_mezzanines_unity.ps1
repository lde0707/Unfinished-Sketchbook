param([string]$Method='Unfin.Editor.MezzanineValidation.BuildAndValidate')
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
$taskLog=Join-Path $taskRoot 'Builds/mezzanine-integration.log'
$taskArgs=@('-batchmode','-projectPath',('"'+$taskProject+'"'),'-executeMethod',$Method,'-quit','-logFile',('"'+$taskLog+'"'))
$taskProcess=Start-Process -FilePath 'C:/Program Files/Unity/Hub/Editor/6000.5.3f1/Editor/Unity.exe' -ArgumentList $taskArgs -WindowStyle Hidden -PassThru
Write-Output "Unity map integration PID $($taskProcess.Id); $taskLog"
$taskProcess.WaitForExit()
if($taskProcess.ExitCode -ne 0){throw "Unity failed: $($taskProcess.ExitCode); $taskLog"}
if($Method -notin @('Unfin.Editor.MezzanineValidation.BuildAndValidate','Unfin.Editor.RegionalPlatformImporter.ApplyZonesAndRender')){exit 0}
foreach($folder in @('Assets/Unfin/Art/Region1V8','Assets/Unfin/Generated','Assets/Unfin/Platforms','Validation')) {
 & robocopy (Join-Path $taskProject $folder) (Join-Path $taskRoot "Book/$folder") /E /R:2 /W:1 /NFL /NDL /NJH /NJS /NP | Out-Null
 if($LASTEXITCODE -ge 8){throw "Copy failed: $folder"}
}
foreach($file in @('Assets/Scenes/World12Prototype.unity','Assets/Unfin/Data/room-mezzanines.json.meta','Assets/Unfin/Editor/MezzanineValidation.cs.meta')) {
 if((Test-Path -LiteralPath (Join-Path $taskRoot "Book/$file")) -and (Get-FileHash -LiteralPath (Join-Path $taskProject $file)).Hash -eq (Get-FileHash -LiteralPath (Join-Path $taskRoot "Book/$file")).Hash){continue}
 Copy-Item -LiteralPath (Join-Path $taskProject $file) -Destination (Join-Path $taskRoot "Book/$file") -Force
}
Write-Output 'Verified mezzanine scene and terrain copied into Book.'
foreach($file in @('Assets/Unfin/Runtime/RegionalTilePalette.cs.meta','Assets/Unfin/Runtime/RegionalTiles.shader.meta')) {
 if((Test-Path -LiteralPath (Join-Path $taskRoot "Book/$file")) -and (Get-FileHash -LiteralPath (Join-Path $taskProject $file)).Hash -eq (Get-FileHash -LiteralPath (Join-Path $taskRoot "Book/$file")).Hash){continue}
 if(Test-Path -LiteralPath (Join-Path $taskProject $file)){Copy-Item -LiteralPath (Join-Path $taskProject $file) -Destination (Join-Path $taskRoot "Book/$file") -Force}
}
