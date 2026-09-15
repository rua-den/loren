[CmdletBinding()]
param(
    [switch] $NoBrowser,
    [switch] $DryRun,
    [switch] $NoPause,
    [switch] $SmokeTest
)

$ErrorActionPreference = 'Stop'
$port = 5091
$hostName = '127.0.0.1'
$url = "http://$hostName`:$port"
$healthUrl = "$url/health"
$scriptDirectory = Split-Path -Parent $MyInvocation.MyCommand.Definition
$repoRoot = Split-Path -Parent $scriptDirectory
$projectDirectory = Join-Path $repoRoot 'src\Loren.Web'
$projectFile = Join-Path $projectDirectory 'Loren.Web.csproj'
$compiledHost = Join-Path $projectDirectory 'bin\Release\net10.0\Loren.Web.dll'
$readyTimeoutSeconds = 60
$pollMilliseconds = 500

if ($SmokeTest) {
    $NoBrowser = $true
}

function Write-Failure([string] $Message) {
    Write-Host "[Loren] ERROR: $Message" -ForegroundColor Red
    Write-Host "[Loren] See docs/launcher.md for setup and troubleshooting." -ForegroundColor Yellow
}

function Test-LorenHealth {
    try {
        $healthResponse = Invoke-WebRequest -UseBasicParsing -Uri $healthUrl -TimeoutSec 2
        if ($healthResponse.StatusCode -ne 200) {
            return $false
        }

        $body = $healthResponse.Content | ConvertFrom-Json
        if ($body.status -ne 'ok') {
            return $false
        }

        $loginResponse = Invoke-WebRequest -UseBasicParsing -Uri "$url/login" -TimeoutSec 2
        return $loginResponse.StatusCode -eq 200 -and
            $loginResponse.Content.Contains('<title>Loren') -and
            $loginResponse.Content.Contains('Owner login')
    }
    catch {
        return $false
    }
}

function Test-PortInUse {
    $client = New-Object System.Net.Sockets.TcpClient
    try {
        $connect = $client.BeginConnect($hostName, $port, $null, $null)
        if (-not $connect.AsyncWaitHandle.WaitOne(500)) {
            return $false
        }
        $client.EndConnect($connect)
        return $true
    }
    catch {
        return $false
    }
    finally {
        $client.Dispose()
    }
}

function Stop-ChildProcess([System.Diagnostics.Process] $Process) {
    if ($null -ne $Process -and -not $Process.HasExited) {
        Write-Host "[Loren] Stopping host (PID $($Process.Id))..."
        try {
            Stop-Process -Id $Process.Id -ErrorAction SilentlyContinue
            $Process.WaitForExit(5000) | Out-Null
        }
        catch {
            Write-Host "[Loren] Could not stop host PID $($Process.Id): $($_.Exception.Message)" -ForegroundColor Yellow
        }
    }
}

try {
    if (-not (Test-Path -LiteralPath $projectFile -PathType Leaf)) {
        throw "The Loren web project was not found at '$projectFile'. Run this launcher from a complete repository checkout."
    }

    $dotnet = Get-Command dotnet -CommandType Application -ErrorAction SilentlyContinue
    if ($null -eq $dotnet) {
        throw "The .NET SDK was not found on PATH. Install the SDK version pinned in global.json, then start Loren again."
    }

    $sdkFile = Join-Path $repoRoot 'global.json'
    Push-Location $repoRoot
    try {
        $requiredSdk = ([string](Get-Content -Raw -LiteralPath $sdkFile | ConvertFrom-Json).sdk.version)
        $installedSdk = (& $dotnet.Source --version 2>&1 | Out-String).Trim()
    }
    finally {
        Pop-Location
    }
    if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($installedSdk)) {
        throw "The .NET SDK could not report its version. Install the SDK pinned by global.json ($requiredSdk), then start Loren again."
    }
    $requiredSdkMatch = [regex]::Match($requiredSdk, '^(\d+\.\d+\.)(\d)\d\d$')
    $installedSdkMatch = [regex]::Match($installedSdk, '^(\d+\.\d+\.)(\d)\d\d$')
    if (-not $requiredSdkMatch.Success -or -not $installedSdkMatch.Success -or
        $requiredSdkMatch.Groups[1].Value -ne $installedSdkMatch.Groups[1].Value -or
        $requiredSdkMatch.Groups[2].Value -ne $installedSdkMatch.Groups[2].Value) {
        throw "The .NET SDK version is $installedSdk, but this checkout requires the $requiredSdk feature band from global.json (latest patch is allowed). Install or select a compatible SDK, then start Loren again."
    }

    $baseConfiguration = Join-Path $projectDirectory 'appsettings.json'
    $localConfiguration = Join-Path $projectDirectory 'appsettings.Local.json'
    if (-not (Test-Path -LiteralPath $baseConfiguration -PathType Leaf) -and
        -not (Test-Path -LiteralPath $localConfiguration -PathType Leaf) -and
        [string]::IsNullOrWhiteSpace($env:LOREN_OWNER_PASSWORD)) {
        Write-Host "[Loren] Warning: no appsettings.json/appsettings.Local.json or LOREN_OWNER_PASSWORD was found. Owner login will not be configured until you add JSON configuration or an environment value." -ForegroundColor Yellow
    }

    if (Test-LorenHealth) {
        if ($SmokeTest) {
            throw "Smoke test requires port $port to be free; an existing Loren host is already ready at $url."
        }
        Write-Host "[Loren] An existing Loren host is already ready at $url."
        if (-not $NoBrowser -and -not $DryRun) {
            Start-Process $url
        }
        exit 0
    }

    if (Test-PortInUse) {
        throw "Port $port is already in use by another process, and $healthUrl did not identify a ready Loren host. Stop that process or free the port before starting Loren."
    }

    $buildArguments = @('build', $projectFile, '--configuration', 'Release')
    $hostArguments = @('"' + $compiledHost + '"', '--urls', $url)

    if ($DryRun) {
        Write-Host "[Loren] Dry run: working directory '$projectDirectory'"
        Write-Host "[Loren] Dry run: dotnet $($buildArguments -join ' ')"
        Write-Host "[Loren] Dry run: dotnet $($hostArguments -join ' ')"
        Write-Host "[Loren] Dry run: browser launch $(if ($NoBrowser) { 'disabled' } else { 'enabled' })"
        exit 0
    }

    $smokeDataDirectory = $null
    $previousOwnerPassword = $env:LOREN_OWNER_PASSWORD
    $previousDataDirectory = $env:LOREN_DATA_DIRECTORY
    if ($SmokeTest) {
        $smokeDataDirectory = Join-Path ([System.IO.Path]::GetTempPath()) ("loren-launcher-smoke-" + [guid]::NewGuid().ToString('N'))
        New-Item -ItemType Directory -Path $smokeDataDirectory -Force | Out-Null
        $env:LOREN_OWNER_PASSWORD = 'launcher-smoke-test-only'
        $env:LOREN_DATA_DIRECTORY = $smokeDataDirectory
        Write-Host "[Loren] Smoke test: using a temporary data directory and no browser."
    }

    Push-Location $projectDirectory
    $hostProcess = $null
    try {
        Write-Host "[Loren] Starting host at $url"
        Write-Host "[Loren] Keep this window open for host logs. Press Ctrl+C to stop Loren."
        & $dotnet.Source @buildArguments
        if ($LASTEXITCODE -ne 0 -or -not (Test-Path -LiteralPath $compiledHost -PathType Leaf)) {
            throw "The Loren Release build failed. Review the build output above and check SDK/project configuration; secret values are intentionally not displayed."
        }

        $hostProcess = Start-Process -FilePath $dotnet.Source -ArgumentList $hostArguments -WorkingDirectory $projectDirectory -NoNewWindow -PassThru

        $ready = $false
        $deadline = (Get-Date).AddSeconds($readyTimeoutSeconds)
        while ((Get-Date) -lt $deadline) {
            if ($hostProcess.HasExited) {
                $exitCode = $hostProcess.ExitCode
                throw "The Loren host stopped during startup with exit code $exitCode. Review the host output above for missing JSON/environment configuration or another startup error. Secret values are intentionally not displayed."
            }

            if (Test-LorenHealth) {
                $ready = $true
                break
            }

            Start-Sleep -Milliseconds $pollMilliseconds
        }

        if (-not $ready) {
            throw "Loren did not become ready within $readyTimeoutSeconds seconds. Review the host output above; check appsettings.Local.json or environment configuration without printing secret values."
        }

        Write-Host "[Loren] Host is ready at $url"
        if ($SmokeTest) {
            Write-Host "[Loren] Smoke test: health and Loren login marker passed; shutting down."
            exit 0
        }
        if (-not $NoBrowser) {
            Start-Process $url
        }

        Wait-Process -Id $hostProcess.Id
        if ($hostProcess.ExitCode -ne 0) {
            throw "The Loren host exited with code $($hostProcess.ExitCode)."
        }
    }
    finally {
        Stop-ChildProcess $hostProcess
        if ($SmokeTest) {
            $env:LOREN_OWNER_PASSWORD = $previousOwnerPassword
            $env:LOREN_DATA_DIRECTORY = $previousDataDirectory
            if ($null -ne $smokeDataDirectory -and (Test-Path -LiteralPath $smokeDataDirectory)) {
                $smokeItem = Get-Item -LiteralPath $smokeDataDirectory -ErrorAction SilentlyContinue
                $tempRoot = (Get-Item -LiteralPath ([System.IO.Path]::GetTempPath())).FullName
                $safeSmokeName = [regex]::IsMatch(
                    [string]$smokeItem.Name,
                    '^loren-launcher-smoke-[0-9a-f]{32}$')
                if ($null -ne $smokeItem -and $smokeItem.PSIsContainer -and
                    $smokeItem.Parent.FullName -eq $tempRoot -and
                    $safeSmokeName) {
                    Remove-Item -LiteralPath $smokeItem.FullName -Recurse -Force -ErrorAction SilentlyContinue
                }
                else {
                    Write-Host "[Loren] Smoke test cleanup skipped because its temporary path was not in the expected location." -ForegroundColor Yellow
                }
            }
        }
        Pop-Location
    }
}
catch {
    Write-Failure $_.Exception.Message
    exit 1
}
