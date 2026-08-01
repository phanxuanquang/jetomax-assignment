<#
.SYNOPSIS
    Starts one or more ChatApp services (backend, frontend, mcp, n8n), each in its own window,
    by delegating to that project's own start-dev.ps1.

.PARAMETER Service
    Any combination of: backend, frontend, mcp, n8n, all. Omit to be prompted interactively.

.PARAMETER Lan
    Forwarded to backend/frontend's start-dev.ps1 — binds the backend to every network interface
    and allows this machine's LAN address through CORS, so another device on the network can reach it.

.EXAMPLE
    ./start-dev.ps1
.EXAMPLE
    ./start-dev.ps1 -Service backend,frontend -Lan
.EXAMPLE
    ./start-dev.ps1 -Service all
#>
param(
    [ValidateSet('backend', 'frontend', 'mcp', 'n8n', 'all')]
    [string[]]$Service,

    [switch]$Lan
)

$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$allServices = @('backend', 'frontend', 'mcp', 'n8n')

if (-not $Service) {
    Write-Host ""
    Write-Host "ChatApp — start dev services" -ForegroundColor Cyan
    Write-Host "  1) backend   — ASP.NET Core API"
    Write-Host "  2) frontend  — React PWA"
    Write-Host "  3) mcp       — MCP server (ChatGPT/Claude)"
    Write-Host "  4) n8n       — daily digest workflow"
    Write-Host "  5) all"
    Write-Host ""
    $choice = Read-Host "Pick one or more (comma-separated numbers or names, e.g. '1,2' or 'backend,frontend'), or 'all'"

    $map = @{ '1' = 'backend'; '2' = 'frontend'; '3' = 'mcp'; '4' = 'n8n'; 'all' = 'all' }
    $Service = $choice.Split(',') | ForEach-Object {
        $token = $_.Trim().ToLowerInvariant()
        if ($map.ContainsKey($token)) { $map[$token] } else { $token }
    }
}

if ($Service -contains 'all') {
    $Service = $allServices
}

$invalid = $Service | Where-Object { $allServices -notcontains $_ }
if ($invalid) {
    Write-Error "Unknown service(s): $($invalid -join ', '). Valid: $($allServices -join ', '), all."
    exit 1
}

$shell = if (Get-Command pwsh -ErrorAction SilentlyContinue) { 'pwsh' } else { 'powershell' }

foreach ($svc in $Service | Select-Object -Unique) {
    $scriptPath = Join-Path $root "$svc/start-dev.ps1"
    if (-not (Test-Path $scriptPath)) {
        Write-Warning "$scriptPath not found — skipping $svc."
        continue
    }

    $lanArg = if ($Lan -and ($svc -eq 'backend' -or $svc -eq 'frontend')) { ' -Lan' } else { '' }
    $command = "& '$scriptPath'$lanArg"

    Write-Host "Launching $svc in a new window..." -ForegroundColor Cyan
    Start-Process -FilePath $shell -ArgumentList @('-NoExit', '-Command', $command)
}

Write-Host ""
Write-Host "Done. Each service is starting in its own window." -ForegroundColor Green
