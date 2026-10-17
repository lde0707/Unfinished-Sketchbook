$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$runtimeRoot = Join-Path $projectRoot 'Art/inbox/open-sprite-runtime'
$pythonPath = Join-Path $runtimeRoot 'venv/Scripts/python.exe'
$comfyRoot = Join-Path $runtimeRoot 'ComfyUI'
if (!(Test-Path -LiteralPath $pythonPath)) {
    throw 'The isolated ComfyUI runtime has not been installed.'
}
try {
    $response = Invoke-RestMethod 'http://127.0.0.1:8189/system_stats' -TimeoutSec 2
    Write-Output 'ComfyUI is already running at http://127.0.0.1:8189'
    return
} catch {
    # A new local instance is needed.
}
$taskProcess = Start-Process -FilePath $pythonPath -ArgumentList @(
    'main.py', '--listen', '127.0.0.1', '--port', '8189',
    '--disable-all-custom-nodes', '--whitelist-custom-nodes', 'ComfyUI_IPAdapter_plus',
    '--offline', '--disable-auto-launch',
    '--use-pytorch-cross-attention'
) -WorkingDirectory $comfyRoot -WindowStyle Hidden -PassThru `
  -RedirectStandardOutput (Join-Path $runtimeRoot 'server.stdout.log') `
  -RedirectStandardError (Join-Path $runtimeRoot 'server.stderr.log')
Write-Output "ComfyUI starting on http://127.0.0.1:8189 (PID $($taskProcess.Id))"
