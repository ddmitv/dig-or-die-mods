param(
    [string]$BepInExVersion = "5.4.23.3",
    [switch]$Console,
    [string]$GamePath,
    [Alias("InstallCfgMgr")]
    [switch]$InstallConfigurationManager,
    [switch]$InstallMonoDebug,
    [switch]$InstallDemystifyExceptions,
    [switch]$UseCecilHarmonyBackend,
    [switch]$NonInteractive
)

#region Functions
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
function Install-DebugMono {
    param([string]$GamePath)

    $monoDir = [IO.Path]::Combine($GamePath, "DigOrDie_Data", "Mono")
    $originalMono = Join-Path $monoDir "mono.dll"
    $backupMono = Join-Path $monoDir "mono.dll.old"
    
    if (-not (Test-Path $originalMono -PathType Leaf)) {
        Write-Host "[WARNING] mono.dll not found in game directory" -ForegroundColor Yellow
        return $false
    }

    $debugMonoUrl = "https://github.com/liesauer/Unity-debugging-dlls/releases/download/v2020.07.10/Unity-debugging-5.x.zip"
    $expectedChecksum = "058238350B0098A350760516B0557F6E366BC846C2F227322921A59238AF5875"
    $tempFile = Join-Path ([IO.Path]::GetTempPath()) "$(New-Guid).zip"

    try {
        Write-Host "`nDownloading debug Mono runtime..." -ForegroundColor Cyan
        Write-Host "  URL: $debugMonoUrl" -ForegroundColor White
        Invoke-WebRequest -Uri $debugMonoUrl -OutFile $tempFile -UseBasicParsing

        Write-Host "Download successful! ($([math]::Round((Get-Item $tempFile).Length / 1MB, 2)) MB)" -ForegroundColor Green
        
        if (-not (Test-Path $tempFile -PathType Leaf)) {
            Write-Host "[ERROR] Failed to save debug Mono package" -ForegroundColor Red
            return $false
        }
        $tempExtract = Join-Path ([IO.Path]::GetTempPath()) "$(New-Guid)"
        $null = New-Item -ItemType Directory -Path $tempExtract -Force
        
        Expand-Archive -Path $tempFile -DestinationPath $tempExtract -Force
        $sourceMono = [IO.Path]::Combine($tempExtract, "Unity-debugging", "unity-5.4.1", "win32", "mono.dll")
        
        if (-not (Test-Path $sourceMono -PathType Leaf)) {
            Write-Host "[ERROR] Debug Mono DLL not found in package" -ForegroundColor Red
            return $false
        }
        $actualChecksum = (Get-FileHash $sourceMono -Algorithm SHA256).Hash
        if ($actualChecksum -ne $expectedChecksum) {
            Write-Host "[ERROR] Checksum verification failed!" -ForegroundColor Red
            Write-Host "  Expected: $expectedChecksum" -ForegroundColor Yellow
            Write-Host "  Actual:   $actualChecksum" -ForegroundColor Yellow
            Write-Host "File may be corrupted or tampered with" -ForegroundColor Red
            return $false
        }
        if (-not (Test-Path $backupMono -PathType Leaf)) {
            Rename-Item -Path $originalMono -NewName "mono.dll.old" -Force
            Write-Host "  Created backup: mono.dll.old" -ForegroundColor Green
        }
        Copy-Item -Path $sourceMono -Destination $originalMono -Force

        Write-Host "Installed debug Mono runtime" -ForegroundColor Green
        Write-Host "  at $originalMono" -ForegroundColor DarkGray
    } catch {
        Write-Host "[ERROR] Debug Mono install failed: $_" -ForegroundColor Red
        return $false
    } finally {
        if (Test-Path $tempFile) { Remove-Item $tempFile -Force }
        if (Test-Path $tempExtract) { Remove-Item $tempExtract -Recurse -Force }
    }
    return $true
}

function Show-Menu {
    param([array]$Options)

    $currentIndex = 0
    $menuStartLine = [Console]::CursorTop
    try {
        [Console]::CursorVisible = $false
        :loop while ($true) {
            for ($i = 0; $i -lt $Options.Count + 1; $i++) {
                [Console]::SetCursorPosition(0, $menuStartLine + $i)
                $selectedPrefix = if ($i -eq $currentIndex) { ">" } else { " " }
                if ($i -eq 0) {
                    $line = "$selectedPrefix Continue..."
                } else {
                    $enabledPrefix = if ($Options[$i - 1].Selected) { "[x]" } else { "[ ]" }
                    $line = "$selectedPrefix $enabledPrefix $($Options[$i - 1].Name)"
                }
                $line = $line.PadRight([Console]::WindowWidth, ' ')
                
                if ($i -eq $currentIndex) {
                    Write-Host $line -BackgroundColor Cyan -ForegroundColor Black -NoNewline
                } else {
                    Write-Host $line -NoNewline
                }
            }
            $key = [Console]::ReadKey($true)
            switch ($key.Key) {
                UpArrow {
                    $currentIndex = [Math]::Max(0, $currentIndex - 1)
                }
                DownArrow {
                    $currentIndex = [Math]::Min($Options.Count, $currentIndex + 1)
                }
                Enter {
                    if ($currentIndex -eq 0) { break loop }
                    $Options[$currentIndex - 1].Selected = -not $Options[$currentIndex - 1].Selected
                }
            }
        }
    }
    finally {
        [Console]::CursorVisible = $true
        [Console]::SetCursorPosition(0, $menuStartLine + $Options.Count + 1)
    }
    return $Options
}
function Install-Plugin {
    param([string]$Name, [string]$DownloadURL)

    $pluginTempFile = Join-Path ([IO.Path]::GetTempPath()) "$Name-$(New-Guid).zip"
    try {
        Write-Host "`nDownloading $Name..." -ForegroundColor Cyan
        Write-Host "  URL: $DownloadURL" -ForegroundColor White
        Invoke-WebRequest -Uri $DownloadURL -OutFile $pluginTempFile -UseBasicParsing
        
        if (-not (Test-Path $pluginTempFile -PathType Leaf)) {
            Write-Host "[WARNING] Failed to save $Name" -ForegroundColor Yellow
            return $false
        }
        Write-Host "Download successful! ($([math]::Round((Get-Item $pluginTempFile).Length / 1MB, 2)) MB)" -ForegroundColor Green
        Write-Host "  Checksum SHA256: $((Get-FileHash $pluginTempFile -Algorithm SHA256).Hash)" -ForegroundColor White

        Write-Host "Extracting to game directory..." -ForegroundColor Cyan
        Expand-Archive -Path $pluginTempFile -DestinationPath $GamePath -Force
        Write-Host "$Name installed!" -ForegroundColor Green
    } catch {
        Write-Host "[WARNING] Failed to install $($Name): $_" -ForegroundColor Yellow
        return $false
    } finally {
        if (Test-Path $pluginTempFile -PathType Leaf) {
            Remove-Item $pluginTempFile -Force -ErrorAction SilentlyContinue
        }
    }
    return $true
}
#endregion Functions

if (-not $NonInteractive) {
    $menuItems = @(
        @{Name = "Enable Console (enable logging console on game startup)"; Selected = $Console.IsPresent},
        @{Name = "Install plugin BepInEx.ConfigurationManager"; Selected = $InstallConfigurationManager.IsPresent},
        @{Name = "Install Mono Debug (required when debugging with dnSpy)"; Selected = $InstallMonoDebug.IsPresent},
        @{Name = "Install preloader patcher BepInEx.Debug.DemystifyExceptions"; Selected = $InstallDemystifyExceptions.IsPresent},
        @{Name = "Use ""cecil"" Harmony backend"; Selected = $UseCecilHarmonyBackend.IsPresent}
    )
    Write-Host "Use UP/DOWN to select options, ENTER to toggle.`nSelect 'Continue...' and press ENTER to start installation." -ForegroundColor DarkGray
    $selection = Show-Menu -Options $menuItems

    $Console = $selection[0].Selected
    $InstallConfigurationManager = $selection[1].Selected
    $InstallMonoDebug = $selection[2].Selected
    $InstallDemystifyExceptions = $selection[3].Selected
    $UseCecilHarmonyBackend = $selection[4].Selected
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
$tempFile = Join-Path ([IO.Path]::GetTempPath()) "$(New-Guid).zip"

try {
    Write-Host "`nDownloading BepInEx..." -ForegroundColor Cyan
    Write-Host "  Version: $BepInExVersion" -ForegroundColor White
    Write-Host "  URL: $downloadUrl" -ForegroundColor White

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
} catch {
    Write-Host "`n[ERROR] Extraction failed: $_" -ForegroundColor Red
    if (Test-Path $tempFile -PathType Leaf) {
        Remove-Item $tempFile -Force -ErrorAction SilentlyContinue
    }
    exit 1
}

$isConfigurationManagerSuccessfullyInstalled = $false
if ($InstallConfigurationManager) {
    $isConfigurationManagerSuccessfullyInstalled = Install-Plugin -Name "BepInEx.ConfigurationManager" -DownloadURL "https://github.com/BepInEx/BepInEx.ConfigurationManager/releases/download/v18.4.1/BepInEx.ConfigurationManager_BepInEx5_v18.4.1.zip"
}
$isDemystifyExceptionsSuccessfullyInstalled = $false
if ($InstallDemystifyExceptions) {
    $isDemystifyExceptionsSuccessfullyInstalled = Install-Plugin -Name "BepInEx.Debug.DemystifyExceptions" -DownloadURL "https://github.com/BepInEx/BepInEx.Debug/releases/download/r11/DemystifyExceptions_r11.zip"
}

$isConsoleSuccessfullyEnabled = $false
$isCecilHarmonyBackendSuccessfullyEnabled = $false
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
    if ($UseCecilHarmonyBackend) {
        $configLines = Update-IniSetting -Content $configLines -Section "Preloader" -Key "HarmonyBackend" -Value "cecil"
        $isCecilHarmonyBackendSuccessfullyEnabled = $true
    }
    
    $configLines | Set-Content $configPath -Force
    Write-Host "Configuration updated successfully!" -ForegroundColor Green
} catch {
    Write-Host "`n[ERROR] Configuration failed: $_" -ForegroundColor Yellow
    Write-Host "You may need to manually edit BepInEx.cfg" -ForegroundColor Yellow
}
$isDebugMonoInstalled = $false
if ($InstallMonoDebug) {
    $isDebugMonoInstalled = Install-DebugMono -GamePath $GamePath
}

$null = New-Item -ItemType Directory -Path (Join-Path $bepInExPath "plugins") -Force -ErrorAction SilentlyContinue
$null = New-Item -ItemType Directory -Path (Join-Path $bepInExPath "patchers") -Force -ErrorAction SilentlyContinue

Write-Host "`n=== Installation Summary ===" -ForegroundColor Magenta
Write-Host "Game Path:       $GamePath" -ForegroundColor Cyan
Write-Host "BepInEx Version: $BepInExVersion" -ForegroundColor Cyan
Write-Host "Console Enabled: $isConsoleSuccessfullyEnabled" -ForegroundColor Cyan
Write-Host "Use cecil Harmony backend: $isCecilHarmonyBackendSuccessfullyEnabled" -ForegroundColor Cyan
Write-Host "BepInEx.ConfigurationManager Installed: $isConfigurationManagerSuccessfullyInstalled" -ForegroundColor Cyan
Write-Host "BepInEx.Debug.DemystifyExceptions Installed: $isDemystifyExceptionsSuccessfullyInstalled" -ForegroundColor Cyan
Write-Host "Debug Mono Installed: $isDebugMonoInstalled" -ForegroundColor Cyan
Write-Host "`nSuccessfully installed BepInEx!`n" -ForegroundColor Green