$ErrorActionPreference = 'Stop'
dotnet build (Join-Path $PSScriptRoot 'Tests\Stub\Stub.csproj') -c Release --nologo
if ($LASTEXITCODE -ne 0) { throw 'Falha ao compilar simulador.' }
dotnet build (Join-Path $PSScriptRoot 'Tests\Tests.csproj') -c Release --nologo
if ($LASTEXITCODE -ne 0) { throw 'Falha ao compilar testes.' }
& (Join-Path $PSScriptRoot 'Tests\bin\Release\net48\Tests.exe') (Join-Path $PSScriptRoot 'Tests\Stub\bin\Release\net48\steam-idle.exe')
if ($LASTEXITCODE -ne 0) { throw 'Falha nos testes.' }
