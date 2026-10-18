$ErrorActionPreference='Stop'
$taskRoot=Split-Path $PSScriptRoot -Parent
$taskProject=Join-Path $taskRoot 'Builds/UnityWebGLProject'
& (Join-Path $PSScriptRoot 'apply_mezzanines_unity.ps1') -Method 'Unfin.Editor.RoomBackdropImporter.ApplyAndRender'
if($LASTEXITCODE -ne 0){throw 'Room backdrop integration failed'}
foreach($taskFolder in @('Assets/Unfin/Art/RoomBackdropsV1','Validation/room-backdrops')){
 & robocopy (Join-Path $taskProject $taskFolder) (Join-Path $taskRoot "Book/$taskFolder") /E /R:2 /W:1 /NFL /NDL /NJH /NJS /NP | Out-Null
 if($LASTEXITCODE -ge 8){throw "Copy failed: $taskFolder"}
}
foreach($taskFile in @('Assets/Scenes/World12Prototype.unity','Assets/Unfin/Art/Region1BackgroundV3/Background.mat','Assets/Unfin/Art/RoomBackdropsV1.meta','Assets/Unfin/Runtime/RoomBackdropLayer.cs.meta','Assets/Unfin/Runtime/RoomBackdrop.shader.meta','Assets/Unfin/Editor/RoomBackdropImporter.cs.meta','Validation/room-backdrops.json')){
 $taskSource=Join-Path $taskProject $taskFile;$taskDest=Join-Path $taskRoot "Book/$taskFile"
 if((Test-Path -LiteralPath $taskDest) -and (Get-FileHash -LiteralPath $taskSource).Hash -eq (Get-FileHash -LiteralPath $taskDest).Hash){continue}
 Copy-Item -LiteralPath $taskSource -Destination $taskDest -Force
}
Write-Output 'Room backdrops and verified scene copied to Book.'
