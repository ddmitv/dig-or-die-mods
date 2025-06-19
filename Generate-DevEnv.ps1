$ErrorActionPreference = 'Stop'

$steamPaths = @()

Write-Host "`nChecking registry for Steam installations..." -ForegroundColor Yellow
$regPaths = @(
    'HKLM:\SOFTWARE\Wow6432Node\Valve\Steam',
    'HKLM:\SOFTWARE\Valve\Steam'
)

foreach ($regPath in $regPaths) {
    if (-not (Test-Path $regPath)) { continue }
    
    Write-Host "  Found registry entry: $regPath" -ForegroundColor DarkGray
    $installPath = (Get-ItemProperty -Path $regPath -Name 'InstallPath' -ErrorAction SilentlyContinue).InstallPath
    if (-not ($installPath -and (Test-Path $installPath))) { continue }

    $normalizedPath = [IO.Path]::GetFullPath($installPath)
    if (-not $steamPaths.Contains($normalizedPath)) {
        $steamPaths += $normalizedPath
        Write-Host "    [ADDED] Registry installation: $normalizedPath" -ForegroundColor Green
    } else {
        Write-Host "    [SKIPPED] Duplicate registry path: $normalizedPath" -ForegroundColor DarkYellow
    }
}

Write-Host "Checking default Steam locations..." -ForegroundColor Yellow
$defaultPaths = @(
    "${env:ProgramFiles(x86)}\Steam",
    "${env:ProgramFiles}\Steam",
    "${env:HOME}\Steam"
)

foreach ($path in $defaultPaths) {
    try {
        $normalizedPath = [IO.Path]::GetFullPath($path)
        if (-not (Test-Path $normalizedPath)) { continue }

        if (-not $steamPaths.Contains($normalizedPath)) {
            $steamPaths += $normalizedPath
            Write-Host "    [ADDED] Default location: $normalizedPath" -ForegroundColor Green
        } else {
            Write-Host "    [SKIPPED] Duplicate default path: $normalizedPath" -ForegroundColor DarkYellow
        }
    } catch {
        Write-Host "    [WARNING] Invalid path '$path': $_" -ForegroundColor Red
    }
}

Write-Host "Searching for Steam libraries..." -ForegroundColor Yellow
$libraryRoots = @()  # Stores Steam library roots (e.g. "D:\SteamLibrary")
foreach ($steamPath in $steamPaths) {
    $vdfPath = Join-Path $steamPath 'steamapps\libraryfolders.vdf'
    if (-not (Test-Path $vdfPath)) { 
        Write-Host "  No libraryfolders.vdf found at: $vdfPath" -ForegroundColor DarkGray
        continue 
    }
    
    Write-Host "  Found libraryfolders.vdf at: $vdfPath" -ForegroundColor DarkGray
    try {
        $vdfContent = Get-Content $vdfPath -Raw
        $vdfMatches = [regex]::Matches($vdfContent, '"path"\s+"([^"]+)"')
        
        if ($vdfMatches.Count -gt 0) {
            Write-Host "    Found $($vdfMatches.Count) libraries in VDF file" -ForegroundColor DarkGray
        } else {
            Write-Host "    No libraries found in VDF file" -ForegroundColor DarkYellow
            continue
        }
        
        foreach ($match in $vdfMatches) {
            $rawPath = $match.Groups[1].Value
            try {
                $normalPath = [IO.Path]::GetFullPath($rawPath.Replace('\\', '\'))
                
                if (-not (Test-Path $normalPath)) {
                    Write-Host "      [SKIPPED] Library path not found: $normalPath" -ForegroundColor Red
                    continue
                }
                
                if (-not $libraryRoots.Contains($normalPath)) {
                    $libraryRoots += $normalPath
                    Write-Host "      [ADDED] Library root: $normalPath" -ForegroundColor Green
                } else {
                    Write-Host "      [SKIPPED] Duplicate library root: $normalPath" -ForegroundColor DarkYellow
                }
            } catch {
                Write-Host "      [ERROR] Invalid library path '$rawPath': $_" -ForegroundColor Red
            }
        }
    } catch {
        Write-Host "    [WARNING] Error parsing $vdfPath : $_" -ForegroundColor Red
    }
}

foreach ($steamPath in $steamPaths) {
    if (-not $libraryRoots.Contains($steamPath)) {
        $libraryRoots += $steamPath
        Write-Host "  [ADDED] Main Steam installation as library root: $steamPath" -ForegroundColor Green
    }
}

$gameName = 'Dig or Die'
$gamePath = $null
$searchPaths = @()
Write-Host "Searching for $gameName installation..." -ForegroundColor Yellow

foreach ($root in $libraryRoots) {
    $commonPath = Join-Path $root 'steamapps\common'
    $gamePathCandidate = Join-Path $commonPath $gameName
    $searchPaths += $gamePathCandidate
    
    $altGamePath = Join-Path $root $gameName
    if ($altGamePath -ne $gamePathCandidate) {
        $searchPaths += $altGamePath
    }
}

foreach ($testPath in $searchPaths) {
    $normalizedTestPath = [IO.Path]::GetFullPath($testPath)
    Write-Host "  Checking: $normalizedTestPath" -ForegroundColor DarkGray
    if (Test-Path $normalizedTestPath) {
        $gamePath = $normalizedTestPath
        Write-Host "[SUCCESS] Found game installation at: $gamePath" -ForegroundColor Green
        break
    }
}

if (-not $gamePath) {
    Write-Host "`n[ERROR] Game installation not found. Searched locations:" -ForegroundColor Red
    $searchPaths | ForEach-Object { Write-Host "  - $_" -ForegroundColor Yellow }
    exit 1
}

$xmlContent = @"
<?xml version="1.0" encoding="utf-8"?>
<Project xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
  <PropertyGroup>
    <!-- Path automatically generated by PowerShell script -->
    <GameDir>$gamePath</GameDir>
  </PropertyGroup>
</Project>
"@

$outputFile = Join-Path $PWD 'DevEnv.targets'
$xmlContent | Out-File -FilePath $outputFile -Encoding utf8

Write-Host "`nSuccessfully created configuration file: $outputFile" -ForegroundColor Green
Write-Host "Game directory set to: $gamePath" -ForegroundColor Cyan