<#
.SYNOPSIS
    Installs dependencies (if needed) and runs the ChatApp frontend (Vite dev server).

.PARAMETER Lan
    Just prints this machine's LAN URL — the dev server already binds every network interface
    (see vite.config.ts's server.host: true), so no extra flag is needed to make it reachable.

.EXAMPLE
    ./start-dev.ps1
.EXAMPLE
    ./start-dev.ps1 -Lan
#>
param(
    [switch]$Lan
)

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
    Write-Warning "frontend/.env.local created from .env.example — fill in the real values, then re-run this script."
    exit 1
}

if (-not (Test-Path (Join-Path $root 'node_modules'))) {
    Write-Host "Installing frontend dependencies..." -ForegroundColor Cyan
    Push-Location $root
    npm install
    Pop-Location
}

if ($Lan) {
    $lanIp = (Get-NetIPAddress -AddressFamily IPv4 |
        Where-Object { $_.IPAddress -notlike '169.254.*' -and $_.IPAddress -ne '127.0.0.1' -and $_.PrefixOrigin -ne 'WellKnown' } |
        Select-Object -First 1 -ExpandProperty IPAddress)

    if ($lanIp) {
        Write-Host "Reachable at http://$($lanIp):5173 from other devices on this network." -ForegroundColor Green
        Write-Host "Note: the installable PWA/service worker only registers on https:// or localhost — a LAN device gets a working page, not the installable shell." -ForegroundColor DarkYellow
    }
}

Stop-ProcessOnPort -Port 5173

Write-Host "Starting frontend (npm run dev)..." -ForegroundColor Cyan
Push-Location $root
npm run dev
Pop-Location
