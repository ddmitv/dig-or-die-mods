$textures = @(
    "flashLightMK3.png",
    "miniaturizorMK6_icon.png",
    "miniaturizorMK6.png",
    "betterPotionHpRegen.png",
    "defenseShieldMK2.png",
    "waterBreatherMK2.png",
    "jetpackMK2.png",
    "antiGravityWall.png",
    "turretReparatorMK3_unit.png",
    "turretReparatorMK3_icon.png",
    "turretReparatorMK3.png",
    "megaExplosive.png",
    "turretParticlesMK2_icon.png",
    "turretParticlesMK2_unit.png",
    "turretTeslaMK2.png",
    "harvester.png",
    "harvester_icon.png",
    "harvester_unit.png"
)

magick $($textures.foreach({"textures/$_"})) +append "Resources/textures"
