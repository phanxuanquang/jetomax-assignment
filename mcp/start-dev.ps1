<#
.SYNOPSIS
    Sets up secrets from .env.local and runs the ChatApp MCP server in Development.

.EXAMPLE
    ./start-dev.ps1
#>

$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$project = Join-Path $root 'ChatApp.Mcp.csproj'
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
    Write-Warning "mcp/.env.local created from .env.local.example — fill in the real values (see mcp/README.md#setup), then re-run this script."
    exit 1
}

Write-Host "Restoring MCP server..." -ForegroundColor Cyan
dotnet restore $project | Out-Host

Write-Host "Applying mcp/.env.local to dotnet user-secrets..." -ForegroundColor Cyan
$secretCount = 0
foreach ($line in Get-Content $envFile) {
    $trimmed = $line.Trim()
    if ($trimmed -eq '' -or $trimmed.StartsWith('#')) { continue }

    $separatorIndex = $trimmed.IndexOf('=')
    if ($separatorIndex -lt 0) { continue }

    $key = $trimmed.Substring(0, $separatorIndex).Trim()
    $value = $trimmed.Substring($separatorIndex + 1).Trim()
    if ($value -eq '') { continue }

    dotnet user-secrets set "$key" "$value" --project $project | Out-Null
    $secretCount++
}
Write-Host "  $secretCount secret(s) set." -ForegroundColor DarkGray

Stop-ProcessOnPort -Port 5001

Write-Host "Starting MCP server (dotnet run)..." -ForegroundColor Cyan
dotnet run --project $project
