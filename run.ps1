$ErrorActionPreference = "Stop"

$projectRoot = $PSScriptRoot
$csprojPath = Join-Path $projectRoot "PServer v2.csproj"
$exePath = Join-Path $projectRoot "bin\Debug\PServer v2.exe"
$msbuildPath = "${env:ProgramFiles}\JetBrains\JetBrains Rider 2025.3.0.3\tools\MSBuild\Current\Bin\amd64\MSBuild.exe"

function Get-LatestSourceWriteTime {
    $includes = @("*.cs", "*.resx", "*.config", "*.json", "*.settings", "*.csproj")
    $files = Get-ChildItem -Path $projectRoot -Recurse -File -Include $includes -ErrorAction SilentlyContinue |
        Where-Object { $_.FullName -notmatch "\\(bin|obj|packages)\\" }

    if (-not $files) { return $null }
    return ($files | Sort-Object LastWriteTime -Descending | Select-Object -First 1).LastWriteTime
}

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  PServer v2 - Executor" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

if (-not (Test-Path $msbuildPath)) {
    Write-Host "ERRO: MSBuild não encontrado em:" -ForegroundColor Red
    Write-Host "  $msbuildPath" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Procurando MSBuild em outros locais..." -ForegroundColor Yellow
    
    $msbuildPaths = @(
        "${env:ProgramFiles(x86)}\Microsoft Visual Studio\2022\*\MSBuild\Current\Bin\MSBuild.exe",
        "${env:ProgramFiles(x86)}\Microsoft Visual Studio\2019\*\MSBuild\Current\Bin\MSBuild.exe",
        "${env:ProgramFiles}\Microsoft Visual Studio\2022\*\MSBuild\Current\Bin\MSBuild.exe"
    )
    
    $found = $false
    foreach ($path in $msbuildPaths) {
        $resolved = Resolve-Path $path -ErrorAction SilentlyContinue
        if ($resolved) {
            $msbuildPath = $resolved[0].Path
            $found = $true
            Write-Host "MSBuild encontrado: $msbuildPath" -ForegroundColor Green
            break
        }
    }
    
    if (-not $found) {
        Write-Host "ERRO: MSBuild não encontrado. Instale o Visual Studio Build Tools ou ajuste o caminho no script." -ForegroundColor Red
        Read-Host "Pressione Enter para sair"
        exit 1
    }
}

$needsBuild = -not (Test-Path $exePath)
if (-not $needsBuild) {
    $exeTime = (Get-Item $exePath).LastWriteTime
    $latestSourceTime = Get-LatestSourceWriteTime
    if ($latestSourceTime -and $latestSourceTime -gt $exeTime) {
        $needsBuild = $true
    }
}

if ($needsBuild) {
    Write-Host "Compilando o projeto..." -ForegroundColor Yellow
    Write-Host ""
    
    Push-Location $projectRoot
    try {
        & $msbuildPath $csprojPath /t:Build /property:Configuration=Debug /property:Platform=x86 /nologo /verbosity:minimal
        
        if ($LASTEXITCODE -ne 0) {
            Write-Host ""
            Write-Host "ERRO: Falha na compilação!" -ForegroundColor Red
            Read-Host "Pressione Enter para sair"
            exit 1
        }
        
        Write-Host ""
        Write-Host "Compilação concluída com sucesso!" -ForegroundColor Green
    }
    finally {
        Pop-Location
    }
}
else {
    Write-Host "Executável já está atualizado. Pulando compilação." -ForegroundColor Green
}

Write-Host ""
Write-Host "Iniciando aplicação..." -ForegroundColor Cyan
Write-Host ""

$exeDir = Split-Path $exePath
Set-Location $exeDir

if (Test-Path $exePath) {
    Start-Process -FilePath $exePath -WorkingDirectory $exeDir
    Write-Host "Aplicação iniciada!" -ForegroundColor Green
}
else {
    Write-Host "ERRO: Executável não encontrado em:" -ForegroundColor Red
    Write-Host "  $exePath" -ForegroundColor Yellow
    Read-Host "Pressione Enter para sair"
    exit 1
}

