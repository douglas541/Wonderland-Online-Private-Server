$ErrorActionPreference = "Continue"

$projectRoot = $PSScriptRoot
$exePath = Join-Path $projectRoot "bin\Debug\PServer v2.exe"
$logPath = Join-Path $projectRoot "bin\Debug\LogOutput.txt"
$msbuildPath = "${env:ProgramFiles}\JetBrains\JetBrains Rider 2025.3.0.3\tools\MSBuild\Current\Bin\amd64\MSBuild.exe"

function Write-LogHeader {
    Clear-Host
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host "  PServer v2 - Debug Monitor" -ForegroundColor Cyan
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "Monitorando logs em tempo real..." -ForegroundColor Yellow
    Write-Host "Pressione Ctrl+C para parar" -ForegroundColor Gray
    Write-Host ""
    Write-Host "----------------------------------------" -ForegroundColor DarkGray
    Write-Host ""
}

function Find-MSBuild {
    $paths = @(
        "${env:ProgramFiles}\JetBrains\JetBrains Rider 2025.3.0.3\tools\MSBuild\Current\Bin\amd64\MSBuild.exe",
        "${env:ProgramFiles(x86)}\Microsoft Visual Studio\2022\*\MSBuild\Current\Bin\MSBuild.exe",
        "${env:ProgramFiles(x86)}\Microsoft Visual Studio\2019\*\MSBuild\Current\Bin\MSBuild.exe",
        "${env:ProgramFiles}\Microsoft Visual Studio\2022\*\MSBuild\Current\Bin\MSBuild.exe"
    )
    
    foreach ($path in $paths) {
        $resolved = Resolve-Path $path -ErrorAction SilentlyContinue
        if ($resolved) {
            return $resolved[0].Path
        }
    }
    return $null
}

function Build-Project {
    $csprojPath = Join-Path $projectRoot "PServer v2.csproj"
    
    if (-not $msbuildPath -or -not (Test-Path $msbuildPath)) {
        $msbuildPath = Find-MSBuild
    }
    
    if (-not $msbuildPath) {
        Write-Host "ERRO: MSBuild não encontrado. Instale o Visual Studio Build Tools ou ajuste o caminho no script." -ForegroundColor Red
        return $false
    }
    
    Write-Host "Compilando projeto..." -ForegroundColor Yellow
    Push-Location $projectRoot
    try {
        & $msbuildPath $csprojPath /t:Build /property:Configuration=Debug /property:Platform=x86 /nologo /verbosity:minimal
        if ($LASTEXITCODE -eq 0) {
            Write-Host "Compilação concluída!" -ForegroundColor Green
            return $true
        }
        else {
            Write-Host "ERRO: Falha na compilação!" -ForegroundColor Red
            return $false
        }
    }
    finally {
        Pop-Location
    }
}

function Start-Application {
    if (-not (Test-Path $exePath)) {
        Write-Host "ERRO: Executável não encontrado: $exePath" -ForegroundColor Red
        return $null
    }
    
    $exeDir = Split-Path $exePath
    $process = Start-Process -FilePath $exePath -WorkingDirectory $exeDir -PassThru -WindowStyle Normal
    return $process
}

function Monitor-LogFile {
    param(
        [string]$LogFile,
        [int]$LastPosition = 0
    )
    
    if (-not (Test-Path $LogFile)) {
        return $LastPosition
    }
    
    try {
        $fileInfo = Get-Item $LogFile -ErrorAction SilentlyContinue
        if ($fileInfo -and $fileInfo.Length -gt $LastPosition) {
            $stream = [System.IO.File]::Open($LogFile, [System.IO.FileMode]::Open, [System.IO.FileAccess]::Read, [System.IO.FileShare]::ReadWrite)
            $stream.Position = $LastPosition
            $reader = New-Object System.IO.StreamReader($stream)
            
            while (-not $reader.EndOfStream) {
                $line = $reader.ReadLine()
                if ($line) {
                    $timestamp = Get-Date -Format "HH:mm:ss"
                    Write-Host "[$timestamp] " -ForegroundColor DarkGray -NoNewline
                    
                    if ($line -match "ERRO|ERROR|FALHA|FAILED|Exception") {
                        Write-Host $line -ForegroundColor Red
                    }
                    elseif ($line -match "AVISO|WARNING|WARN") {
                        Write-Host $line -ForegroundColor Yellow
                    }
                    elseif ($line -match "SUCESSO|SUCCESS|OK|done") {
                        Write-Host $line -ForegroundColor Green
                    }
                    else {
                        Write-Host $line -ForegroundColor White
                    }
                }
            }
            
            $LastPosition = $stream.Position
            $reader.Close()
            $stream.Close()
        }
    }
    catch {
    }
    
    return $LastPosition
}

Write-LogHeader

if (-not (Build-Project)) {
    Write-Host ""
    Read-Host "Pressione Enter para sair"
    exit 1
}

Write-Host ""

$process = $null
$lastPosition = 0

if (Test-Path $logPath) {
    $fileInfo = Get-Item $logPath
    $lastPosition = $fileInfo.Length
}

Write-Host "Iniciando aplicação..." -ForegroundColor Cyan
$process = Start-Application

if (-not $process) {
    Write-Host "ERRO: Não foi possível iniciar a aplicação!" -ForegroundColor Red
    Read-Host "Pressione Enter para sair"
    exit 1
}

Write-Host "Aplicação iniciada (PID: $($process.Id))" -ForegroundColor Green
Write-Host ""
Write-Host "----------------------------------------" -ForegroundColor DarkGray
Write-Host ""

try {
    while ($true) {
        if (-not $process.HasExited) {
            $lastPosition = Monitor-LogFile -LogFile $logPath -LastPosition $lastPosition
        }
        else {
            Write-Host ""
            Write-Host "----------------------------------------" -ForegroundColor DarkGray
            Write-Host "Aplicação encerrada (Exit Code: $($process.ExitCode))" -ForegroundColor Yellow
            break
        }
        
        Start-Sleep -Milliseconds 500
    }
}
catch {
    Write-Host ""
    Write-Host "Monitoramento interrompido." -ForegroundColor Yellow
}
finally {
    if ($process -and -not $process.HasExited) {
        Write-Host ""
        Write-Host "Encerrando aplicação..." -ForegroundColor Yellow
        $process.Kill()
    }
}

Write-Host ""
Read-Host "Pressione Enter para sair"

