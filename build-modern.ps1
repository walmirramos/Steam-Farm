param([string]$RuntimeDirectory)
$ErrorActionPreference = 'Stop'
$project = Join-Path $PSScriptRoot 'Source\SteamIdleManager\SteamIdleManager.csproj'
dotnet build $project -c Release --nologo
if ($LASTEXITCODE -ne 0) { throw 'Falha ao compilar a interface.' }
$destination = Join-Path $PSScriptRoot 'Source\SteamIdleManager\bin\Release\net48'
if ($RuntimeDirectory) {
    foreach ($name in @('steam-idle.exe', 'steam-idle.exe.config', 'steam_api64.dll', 'Steamworks.NET.dll')) {
        Copy-Item -LiteralPath (Join-Path $RuntimeDirectory $name) -Destination $destination -Force
    }
}
Write-Host "Executável: $destination\SteamHoraFarmada.exe"
