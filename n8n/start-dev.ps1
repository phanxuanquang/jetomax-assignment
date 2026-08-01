<#
.SYNOPSIS
    Installs n8n (if needed) and runs it locally, with settings from .env.local.

.EXAMPLE
    ./start-dev.ps1
#>

$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$envFile = Join-Path $root '.env.local'
$envExample = Join-Path $root '.env.example'

function Stop-ProcessOnPort {
    param([int]$Port)
    $owningPids = Get-NetTCPConnection -LocalPort $Port -State Listen -ErrorAction SilentlyContinue |
        Select-Object -ExpandProperty OwningProcess -Unique
    foreach ($processId in $owningPids) {
        $proc = Get-Process -Id $processId -ErrorAction SilentlyContinue
        if ($proc) {
            Write-Host "Stopping existing process on port $Port (PID $processId, $($proc.ProcessName))..." -ForegroundColor Yellow
            Stop-Process -Id $processId -Force -ErrorAction SilentlyContinue
        }
    }
    if ($owningPids) { Start-Sleep -Milliseconds 500 }
}

if (-not (Get-Command npm -ErrorAction SilentlyContinue)) {
    Write-Error "Node.js/npm not found on PATH. Install Node.js 20+: https://nodejs.org"
    exit 1
}

if (-not (Test-Path $envFile)) {
    Copy-Item $envExample $envFile
    Write-Warning "n8n/.env.local created from .env.example — review it, then re-run this script."
    exit 1
}

if (-not (Test-Path (Join-Path $root 'node_modules'))) {
    Write-Host "Installing n8n..." -ForegroundColor Cyan
    Push-Location $root
    npm install
    Pop-Location
}

Write-Host "Loading n8n/.env.local..." -ForegroundColor Cyan
foreach ($line in Get-Content $envFile) {
    $trimmed = $line.Trim()
    if ($trimmed -eq '' -or $trimmed.StartsWith('#')) { continue }

    $separatorIndex = $trimmed.IndexOf('=')
    if ($separatorIndex -lt 0) { continue }

    $key = $trimmed.Substring(0, $separatorIndex).Trim()
    $value = $trimmed.Substring($separatorIndex + 1).Trim()

    Set-Item -Path "Env:$key" -Value $value
}

$n8nPort = if ($env:N8N_PORT) { [int]$env:N8N_PORT } else { 5678 }
Stop-ProcessOnPort -Port $n8nPort

Write-Host "Starting n8n (npm run dev)..." -ForegroundColor Cyan
Write-Host "  First run: open the printed URL, create a local account, then Import from File -> workflow.json." -ForegroundColor DarkGray
Push-Location $root
npm run dev
Pop-Location
