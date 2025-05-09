
function NeedsUpdate {
    if (-not (Test-Path "$PSScriptRoot\textures\combined_textures.png")) { return $true }
    $outputLastWrite = (Get-Item "$PSScriptRoot\textures\combined_textures.png").LastWriteTime
    foreach ($file in (Get-ChildItem "$PSScriptRoot\textures\*.png") + (Get-Item $PSCommandPath)) {
        if ($file.LastWriteTime -gt $outputLastWrite) { return $true }
    }
    return $false
}
if (-not (NeedsUpdate)) {
    exit 0
}

$texturePlacements = @(
    @(0, 0, "flashLightMK3.png"),
    @(1, 0, "miniaturizorMK6_icon.png"),
    @(2, 0, "miniaturizorMK6.png"),
    @(3, 0, "betterPotionHpRegen.png"),
    @(4, 0, "defenseShieldMK2.png"),
    @(5, 0, "waterBreatherMK2.png"),
    @(6, 0, "jetpackMK2.png"),
    @(7, 0, "antiGravityWall.png"),
    @(0, 1, "turretReparatorMK3_unit.png"),
    @(1, 1, "turretReparatorMK3_icon.png"),
    @(2, 1, "turretReparatorMK3.png"),
    @(3, 1, "megaExplosive.png"),
    @(4, 1, "turretParticlesMK2_icon.png"),
    @(5, 1, "turretParticlesMK2_unit.png"),
    @(6, 1, "turretTeslaMK2.png"),
    @(7, 1, "collector.png"),
    @(0, 2, "collector_icon.png"),
    @(1, 2, "collector_unit.png"),
    @(2, 2, "blueLightSticky.png"),
    @(3, 2, "blueLightSticky_midair.png"),
    @(4, 2, "redLightSticky.png"),
    @(5, 2, "redLightSticky_midair.png"),
    @(6, 2, "greenLightSticky.png"),
    @(7, 2, "greenLightSticky_midair.png"),
    @(0, 3, "basaltCollector_icon.png"),
    @(1, 3, "basaltCollector_unit.png"),
    @(2, 3, "turretLaser360_icon.png"),
    @(3, 3, "gunPlasmaMegaSnipe.png"),
    @(4, 3, "gunPlasmaMegaSnipe_icon.png"),
    @(5, 3, "volcanicExplosive.png"),
    @(6, 3, "wallCompositeReinforced.png"),
    @(7, 3, "gunNukeLauncher.png"),
    @(0, 4, "gunNukeLauncher_icon.png"),
    @(1, 4, "generatorSunMK2.png"),
    @(2, 4, "RTG.png"),
    @(3, 4, "gunPlasmaThrower.png"),
    @(4, 4, "gunPlasmaThrower_icon.png"),
    @(5, 4, "portableTeleport.png"),
    @(6, 4, "fertileDirt.png"),
    @(7, 4, "autoBuilderMK6.png")
)
$canvasWidth = (($texturePlacements | Measure-Object -Property {$_[0]} -Maximum).Maximum + 1) * 128
$canvasHeight = (($texturePlacements | Measure-Object -Property {$_[1]} -Maximum).Maximum + 1) * 128

$magickCmd = "magick -size ${canvasWidth}x${canvasHeight} xc:none "

foreach ($entry in $texturePlacements) {
    $magickCmd += "`"$PSScriptRoot/textures/$($entry[2])`" -geometry +$($entry[0]*128)+$($entry[1]*128) -composite "
}
$magickCmd += "`"$PSScriptRoot/textures/combined_textures.png`""

# Write-Host $magickCmd
Invoke-Expression $magickCmd
