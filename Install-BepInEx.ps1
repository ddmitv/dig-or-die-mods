param(
    [string]$BepInExVersion = "5.4.23.3",
    [switch]$Console,
    [string]$GamePath,
    [Alias("InstallCfgMgr")]
    [switch]$InstallConfigurationManager
)

function Get-SteamInstallationPath {
    $installPath = (Get-ItemProperty -Path "HKLM:\SOFTWARE\Wow6432Node\Valve\Steam" -Name "InstallPath" -ErrorAction SilentlyContinue).InstallPath
    if (($null -ne $installPath) -and (Test-Path $installPath -PathType Container)) { return $installPath }

    $installPath = (Get-ItemProperty -Path "HKCU:\Software\Valve\Steam" -Name "SteamPath" -ErrorAction SilentlyContinue).SteamPath
    if (($null -ne $installPath) -and (Test-Path $installPath -PathType Container)) { return $installPath }

    $installPath = (Get-ItemProperty -Path "HKLM:\SOFTWARE\Valve\Steam" -Name "SteamPath" -ErrorAction SilentlyContinue).SteamPath
    if (($null -ne $installPath) -and (Test-Path $installPath -PathType Container)) { return $installPath }

    $installPath = "${env:ProgramFiles(x86)}\Steam"
    if ($installPath -and (Test-Path $installPath -PathType Container)) { return $installPath }

    $installPath = "${env:ProgramFiles}\Steam"
    if ($installPath -and (Test-Path $installPath -PathType Container)) { return $installPath }

    $installPath = "${env:HOME}\Steam"
    if ($installPath -and (Test-Path $installPath -PathType Container)) { return $installPath }

    return $null
}

function Get-SteamGamePath {
    param([string]$GameName)

    $steamPath = [IO.Path]::GetFullPath((Get-SteamInstallationPath))
    if ($null -eq $steamPath) {
        Write-Host "Failed to find Steam installation" -ForegroundColor Red
        exit 1
    }
    Write-Host "  [FOUND] Steam installation: $steamPath" -ForegroundColor Green

    $libraryRoots = @()

    $vdfPath = [IO.Path]::Combine($steamPath, "steamapps", "libraryfolders.vdf")
    if (-not (Test-Path $vdfPath -PathType Leaf)) { 
        Write-Host "  No libraryfolders.vdf at: $vdfPath" -ForegroundColor Red
        exit 1 
    }
    
    try {
        Write-Host "  Parsing libraryfolders.vdf at: $vdfPath" -ForegroundColor DarkGray
        $vdfContent = Get-Content $vdfPath -Raw
        $vdfMatches = [regex]::Matches($vdfContent, '"path"\s+"([^"]+)"')
        
        if ($vdfMatches.Count -eq 0) {
            Write-Host "    No libraries found in VDF" -ForegroundColor Red
            exit 1
        }
        
        foreach ($match in $vdfMatches) {
            $normalPath = [IO.Path]::GetFullPath($match.Groups[1].Value.Replace('\\', '\'))
            
            if (-not (Test-Path $normalPath -PathType Container)) {
                Write-Host "    [SKIPPED] Library path not found: $normalPath" -ForegroundColor Red
                continue
            }
            
            if (-not $libraryRoots.Contains($normalPath)) {
                $libraryRoots += $normalPath
                Write-Host "    [ADDED] Steam library: $normalPath" -ForegroundColor Green
            }
        }
    }
    catch {
        Write-Host "  [ERROR] Parsing VDF: $_" -ForegroundColor Red
        exit 1
    }

    $searchPaths = @()
    Write-Host "Searching for '$GameName' in Steam libraries..." -ForegroundColor Cyan
    
    foreach ($root in $libraryRoots) {
        $gamePathCandidate = [IO.Path]::Combine($root, "steamapps", "common", $GameName)
        if (-not $searchPaths.Contains($gamePathCandidate)) {
            $searchPaths += $gamePathCandidate
        }
        $altPath = Join-Path $root $GameName
        if (-not $searchPaths.Contains($altPath)) {
            $searchPaths += $altPath
        }
    }

    foreach ($testPath in $searchPaths) {
        $normalizedTestPath = [IO.Path]::GetFullPath($testPath)
        Write-Host "  Checking: $normalizedTestPath" -ForegroundColor DarkGray
        if (Test-Path $normalizedTestPath -PathType Container) {
            Write-Host "  [FOUND] Game at: $normalizedTestPath" -ForegroundColor Green
            return $normalizedTestPath
        }
    }
    Write-Host "  [ERROR] Game not found in any Steam library" -ForegroundColor Red
    exit 1
}
function Update-IniSetting {
    param(
        [string[]]$Content,
        [string]$Section,
        [string]$Key,
        [string]$Value
    )
    
    $sectionPattern = "^\s*\[$([regex]::Escape($Section))\]\s*$"
    $keyPattern = "^\s*$([regex]::Escape($Key))\s*="
    
    $inSection = $false
    $keyFound = $false
    $output = @()
    $lineNumber = 0
    
    foreach ($line in $Content) {
        $lineNumber++

        if ($line -match '^\s*\[') {
            if ($inSection -and -not $keyFound) {
                $output += "$Key = $Value"
                $keyFound = $true
                Write-Host "  (line $lineNumber) Adding new key at the end of section [$Section]: '$Key = $Value'" -ForegroundColor DarkGray
            }
            $inSection = $line -match $sectionPattern
            if ($inSection) {
                Write-Host "  (line $lineNumber) Entering section: $line" -ForegroundColor DarkGray
            }
            $output += $line
            continue
        }
        if ($inSection -and -not $keyFound -and ($line -match $keyPattern)) {
            $output += "$Key = $Value"
            $keyFound = $true
            Write-Host "  (line $lineNumber) Updating entry: '$line' -> '$Key = $Value'" -ForegroundColor DarkGray
            continue
        }
        
        $output += $line
    }
    if (-not $keyFound) {
        if ($inSection) {
            $output += "$Key = $Value"
            Write-Host "  Adding new key to existing section [$Section]: '$Key = $Value'" -ForegroundColor DarkGray
        } else {
            $output += "[$Section]"
            $output += "$Key = $Value"
            Write-Host "  Creating new section [$Section] with key: '$Key = $Value'" -ForegroundColor DarkGray
        }
    }
    
    return $output
}

$gameName = "Dig or Die"
Write-Host "`n=== Dig or Die BepInEx Installer ===" -ForegroundColor Magenta

if (-not $GamePath) {
    Write-Host "Auto-detecting game path..." -ForegroundColor Cyan
    $GamePath = Get-SteamGamePath -GameName $gameName
}

if (-not $GamePath -or -not (Test-Path $GamePath -PathType Container)) {
    Write-Host "`[ERROR] Invalid game path: folder not found" -ForegroundColor Red
    Write-Host "Please specify a valid game path using -GamePath parameter`n" -ForegroundColor Yellow
    exit 1
}

if (-not (Test-Path (Join-Path $GamePath "DigOrDie.exe") -PathType Leaf) -or
    -not (Test-Path (Join-Path $GamePath "steam_api.dll") -PathType Leaf) -or
    -not (Test-Path (Join-Path $GamePath "DigOrDie_Data") -PathType Container)
) {
    Write-Host "`[ERROR] Invalid game path: the directory should contain: 'DigOrDie.exe', steam_api.dll' and 'DigOrDie_Data'" -ForegroundColor Red
    Write-Host "Please specify a valid game path using -GamePath parameter`n" -ForegroundColor Yellow
    exit 1
}

$parseBepInExVersion = [version]::new()
if ((-not ([version]::TryParse($BepInExVersion, [ref]$parseBepInExVersion)) -or ($parseBepInExVersion.Major -ne 5))) {
    Write-Host "[ERROR] Only BepInEx 5 is supported" -ForegroundColor Red
    exit 1
}

$versionNumber = "win_x86_$BepInExVersion" -replace '.*?(\d+\.\d+\.\d+\.\d+)$', '$1'
$downloadUrl = "https://github.com/BepInEx/BepInEx/releases/download/v$versionNumber/BepInEx_win_x86_$BepInExVersion.zip"
$tempFile = Join-Path ([System.IO.Path]::GetTempPath()) "$(New-Guid).zip"

try {
    Write-Host "`nDownloading BepInEx..." -ForegroundColor Cyan
    Write-Host "  Version: $BepInExVersion" -ForegroundColor White
    Write-Host "  Download URL: $downloadUrl" -ForegroundColor White

    Invoke-WebRequest -Uri $downloadUrl -OutFile $tempFile -UseBasicParsing
    
    if (-not (Test-Path $tempFile -PathType Leaf)) {
        Write-Host "[ERROR] Failed to save downloaded file"
        exit 1
    }
    Write-Host "Download successful! ($([math]::Round((Get-Item $tempFile).Length / 1MB, 2)) MB)" -ForegroundColor Green
    Write-Host "  Checksum SHA256: $((Get-FileHash $tempFile -Algorithm SHA256).Hash)" -ForegroundColor White
}
catch {
    Write-Host "[ERROR] Failed to download BepInEx: $_" -ForegroundColor Red
    Write-Host "Possible solutions:" -ForegroundColor Yellow
    Write-Host "1. Check available versions at https://github.com/BepInEx/BepInEx/releases"
    Write-Host "2. Verify your internet connection"
    Write-Host "3. Try different BepInEx version using -BepInExVersion parameter`n"
    exit 1
}

try {
    Write-Host "`nExtracting to game directory..." -ForegroundColor Cyan

    Expand-Archive -Path $tempFile -DestinationPath $GamePath -Force
    Remove-Item $tempFile -Force -ErrorAction SilentlyContinue
    Write-Host "Extraction complete!" -ForegroundColor Green
}
catch {
    Write-Host "`n[ERROR] Extraction failed: $_" -ForegroundColor Red
    if (Test-Path $tempFile -PathType Leaf) {
        Remove-Item $tempFile -Force -ErrorAction SilentlyContinue
    }
    exit 1
}

$isConfigurationManagerSuccessfullyInstalled = $false
if ($InstallConfigurationManager) {
    $configManagerUrl = "https://github.com/BepInEx/BepInEx.ConfigurationManager/releases/download/v18.4.1/BepInEx.ConfigurationManager_BepInEx5_v18.4.1.zip"
    $configManagerTempFile = Join-Path ([System.IO.Path]::GetTempPath()) "$(New-Guid).zip"

    try {
        Write-Host "`nDownloading BepInEx.ConfigurationManager..." -ForegroundColor Cyan
        Write-Host "  URL: $configManagerUrl" -ForegroundColor White
        Invoke-WebRequest -Uri $configManagerUrl -OutFile $configManagerTempFile -UseBasicParsing
        
        if (-not (Test-Path $configManagerTempFile -PathType Leaf)) {
            Write-Host "[WARNING] Failed to save BepInEx.ConfigurationManager" -ForegroundColor Yellow
        }
        else {
            Write-Host "Download successful! ($([math]::Round((Get-Item $configManagerTempFile).Length / 1MB, 2)) MB)" -ForegroundColor Green
            Write-Host "  Checksum SHA256: $((Get-FileHash $configManagerTempFile -Algorithm SHA256).Hash)" -ForegroundColor White

            Write-Host "Extracting to game directory..." -ForegroundColor Cyan
            Expand-Archive -Path $configManagerTempFile -DestinationPath $GamePath -Force
            Write-Host "BepInEx.ConfigurationManager installed!" -ForegroundColor Green
            $isConfigurationManagerSuccessfullyInstalled = $true
        }
    }
    catch {
        Write-Host "[WARNING] Failed to install BepInEx.ConfigurationManager: $_" -ForegroundColor Yellow
    }
    finally {
        if (Test-Path $configManagerTempFile -PathType Leaf) {
            Remove-Item $configManagerTempFile -Force -ErrorAction SilentlyContinue
        }
    }
}

$isConsoleSuccessfullyEnabled = $false
try {
    $bepInExPath = Join-Path $GamePath "BepInEx"
    $configDir = Join-Path $bepInExPath "config"
    $configPath = Join-Path $configDir "BepInEx.cfg"
    $null = New-Item -ItemType Directory -Path $configDir -Force -ErrorAction Stop
    
    Write-Host "`nConfiguring BepInEx..." -ForegroundColor Cyan
    
    $configLines = if (Test-Path $configPath -PathType Leaf) {
        Get-Content $configPath
    } else {
        @()
    }
    
    $configLines = Update-IniSetting -Content $configLines -Section "Preloader.Entrypoint" -Key "Type" -Value "MonoBehaviour"
    
    if ($Console) {
        $configLines = Update-IniSetting -Content $configLines -Section "Logging.Console" -Key "Enabled" -Value "true"
        $isConsoleSuccessfullyEnabled = $true
    }
    
    $configLines | Set-Content $configPath -Force
    Write-Host "Configuration updated successfully!" -ForegroundColor Green
} catch {
    Write-Host "`n[ERROR] Configuration failed: $_" -ForegroundColor Yellow
    Write-Host "You may need to manually edit BepInEx.cfg" -ForegroundColor Yellow
}

$null = New-Item -ItemType Directory -Path (Join-Path $bepInExPath "plugins") -Force -ErrorAction SilentlyContinue
$null = New-Item -ItemType Directory -Path (Join-Path $bepInExPath "patchers") -Force -ErrorAction SilentlyContinue

Write-Host "`n=== Installation Summary ===" -ForegroundColor Magenta
Write-Host "Game Path:       $GamePath" -ForegroundColor Cyan
Write-Host "BepInEx Version: $BepInExVersion" -ForegroundColor Cyan
Write-Host "Console Enabled: $isConsoleSuccessfullyEnabled" -ForegroundColor Cyan
Write-Host "BepInEx.ConfigurationManager Installed: $isConfigurationManagerSuccessfullyInstalled" -ForegroundColor Cyan
Write-Host "`nSuccessfully installed BepInEx!`n" -ForegroundColor Green