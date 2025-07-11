
function NeedsUpdate {
    if (-not (Test-Path "$PSScriptRoot\textures\combined_textures.png")) { return $true }
    if (-not (Test-Path "$PSScriptRoot\textures\combined_particles.png")) { return $true }

    $outputLastWrite = ((
        (Get-Item "$PSScriptRoot\textures\combined_textures.png").LastWriteTime,
        (Get-Item "$PSScriptRoot\textures\combined_particles.png").LastWriteTime
    ) | Measure-Object -Maximum).Maximum

    foreach ($file in (Get-ChildItem "$PSScriptRoot\textures\*.png") + (Get-Item $PSCommandPath)) {
        if ($file.LastWriteTime -gt $outputLastWrite) { return $true }
    }
    return $false
}
if (-not (NeedsUpdate)) {
    exit 0
}
Write-Host "Recreating spritesheet..."

function CreateTextureSpriteSheetCmd {
    param($placements, $outputFile)

    $canvasWidth = (($placements | ForEach-Object { $_[0] } | Measure-Object -Maximum).Maximum + 1) * 128
    $canvasHeight = (($placements | ForEach-Object { $_[1] } | Measure-Object -Maximum).Maximum + 1) * 128

    $magickCmd = "magick -size ${canvasWidth}x${canvasHeight} xc:none "

    foreach ($entry in $placements) {
        $x, $y, $file = $entry
        $magickCmd += "`"$PSScriptRoot/textures/$file`" -geometry +$($x*128)+$($y*128) -composite "
    }
    $magickCmd += "`"$PSScriptRoot/textures/$outputFile`""

    return $magickCmd
}
function CreateParticlesSpriteSheetCmd {
    param($placements, $outputFile)

    $particlesCanvasWidth = ($placements | ForEach-Object { $_[0] + $_[2] } | Measure-Object -Maximum).Maximum
    $particlesCanvasHeight = ($placements | ForEach-Object { $_[1] + $_[3] } | Measure-Object -Maximum).Maximum

    $particlesMagickCmd = "magick -size ${particlesCanvasWidth}x${particlesCanvasHeight} xc:none "
    foreach ($entry in $placements) {
        $x, $y, $width, $height, $file = $entry
        $particlesMagickCmd += "`"$PSScriptRoot/textures/$file`" -geometry +${x}+${y} -composite "
    }
    $particlesMagickCmd += "`"$PSScriptRoot/textures/$outputFile`""

    return $particlesMagickCmd
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
    @(7, 4, "autoBuilderMK6.png"),
    @(0, 5, "impactShieldMk1.png"),
    @(1, 5, "impactShieldMk2.png"),
    @(2, 5, "impactGrenade.png"),
    @(3, 5, "impactGrenade_icon.png"),
    @(4, 5, "waterVaporizer.png"),
    @(5, 5, "turretCeilingMK2.png"),
    @(6, 5, "turretSpikesMK2.png"),
    @(7, 5, "gunEnergyDiffuser_icon.png"),
    @(0, 6, "gunEnergyDiffuser.png"),
    @(1, 6, "waterVaporizerMK2.png"),
    @(2, 6, "advancedMetalDetector.png"),
    @(3, 6, "quantumCondenser.png"),
    @(4, 6, "negamassAlloy.png"),
    @(5, 6, "plasmaAmplifier.png"),
    @(6, 6, "harvestCore.png"),
    @(6, 7, "entropyCore.png"),
    @(7, 7, "titanferrumAlloy.png"),
    @(0, 8, "mixedSoil.png"),
    @(1, 8, "gunRocketGatling_icon.png"),
    @(2, 8, "gunRocketGatling.png"),
    @(3, 8, "gunZF0Shotgun.png"),
    @(4, 8, "gunZF0Shotgun_icon.png"),
    @(5, 8, "turretMegaSnipe.png"),
    @(6, 8, "turretMegaSnipe_icon.png"),
    @(7, 8, "turretMegaSnipe_unit.png"),
    @(0, 9, "turretDuo360_icon.png"),
    @(1, 9, "turretDuo360_unit.png")
)
$particlesTexturePlacements = @(
    @(0, 0, 255, 119, "meltdownSnipe.png"),
    @(255, 0, 209, 98, "particlesSnipTurretMK2.png"),
    @(464, 0, 40, 40, "particleImpactGrenade.png"),
    @(504, 0, 100, 100, "particleEnergyDiffuser.png")
)

Invoke-Expression (CreateTextureSpriteSheetCmd -placements $texturePlacements -outputFile "combined_textures.png")
Invoke-Expression (CreateParticlesSpriteSheetCmd -placements $particlesTexturePlacements -outputFile "combined_particles.png")

