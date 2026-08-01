<#
.SYNOPSIS
    Sets up secrets from .env.local and runs the ChatApp backend (ASP.NET Core) in Development.

.PARAMETER Lan
    Also bind Kestrel to every network interface (0.0.0.0) and register this machine's LAN IPv4
    as an allowed CORS origin, so a phone or another PC on the same network can reach the API.

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
$apiProject = Join-Path $root 'src/ChatApp.Api/ChatApp.Api.csproj'
$envFile = Join-Path $root '.env.local'
$envExample = Join-Path $root '.env.local.example'

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

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    Write-Error ".NET SDK not found on PATH. Install .NET SDK 10.x: https://dotnet.microsoft.com/download"
    exit 1
}

if (-not (Test-Path $envFile)) {
    Copy-Item $envExample $envFile
    Write-Warning "backend/.env.local created from .env.local.example — fill in the real values, then re-run this script."
    exit 1
}

Write-Host "Restoring backend..." -ForegroundColor Cyan
dotnet restore (Join-Path $root 'ChatApp.slnx') | Out-Host

Write-Host "Applying backend/.env.local to dotnet user-secrets..." -ForegroundColor Cyan
$secretCount = 0
foreach ($line in Get-Content $envFile) {
    $trimmed = $line.Trim()
    if ($trimmed -eq '' -or $trimmed.StartsWith('#')) { continue }

    $separatorIndex = $trimmed.IndexOf('=')
    if ($separatorIndex -lt 0) { continue }

    $key = $trimmed.Substring(0, $separatorIndex).Trim()
    $value = $trimmed.Substring($separatorIndex + 1).Trim()
    if ($value -eq '') { continue }

    dotnet user-secrets set "$key" "$value" --project $apiProject | Out-Null
    $secretCount++
}
Write-Host "  $secretCount secret(s) set." -ForegroundColor DarkGray

$runArgs = @('run', '--project', $apiProject)

if ($Lan) {
    $lanIp = (Get-NetIPAddress -AddressFamily IPv4 |
        Where-Object { $_.IPAddress -notlike '169.254.*' -and $_.IPAddress -ne '127.0.0.1' -and $_.PrefixOrigin -ne 'WellKnown' } |
        Select-Object -First 1 -ExpandProperty IPAddress)

    if (-not $lanIp) {
        Write-Warning "Could not detect a LAN IPv4 address — starting without -Lan behavior."
    }
    else {
        Write-Host "LAN mode: binding to 0.0.0.0, allowing origin http://$($lanIp):5173" -ForegroundColor Cyan
        dotnet user-secrets set "CORS:AllowedOrigins:2" "http://$($lanIp):5173" --project $apiProject | Out-Null
        $runArgs += @('--', '--urls', 'http://0.0.0.0:5000')
        Write-Host "Reachable at http://$($lanIp):5000 from other devices on this network." -ForegroundColor Green
    }
}

Stop-ProcessOnPort -Port 5000

Write-Host "Starting backend (dotnet run)..." -ForegroundColor Cyan
dotnet @runArgs
