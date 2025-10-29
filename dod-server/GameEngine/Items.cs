
namespace GameEngine;

public static class GItems {
    public static readonly CItem_Device miniaturizorMK1 = new(CItem_Device.Group.Miniaturizor, CItem_Device.Type.None, 80f) {
        m_pickupDuration = -1,
        m_codeName = "miniaturizorMK1"
    };

    public static readonly CItem_Device miniaturizorMK2 = new(CItem_Device.Group.Miniaturizor, CItem_Device.Type.None, 160f) {
        m_pickupDuration = -1,
        m_codeName = "miniaturizorMK2"
    };

    public static readonly CItem_Device miniaturizorMK3 = new(CItem_Device.Group.Miniaturizor, CItem_Device.Type.None, 310f) {
        m_pickupDuration = -1,
        m_codeName = "miniaturizorMK3"
    };

    public static readonly CItem_Device miniaturizorMK4 = new(CItem_Device.Group.Miniaturizor, CItem_Device.Type.None, 510f) {
        m_pickupDuration = -1,
        m_codeName = "miniaturizorMK4"
    };

    public static readonly CItem_Device miniaturizorMK5 = new(CItem_Device.Group.Miniaturizor, CItem_Device.Type.None, 810f) {
        m_pickupDuration = -1,
        m_codeName = "miniaturizorMK5"
    };

    public static readonly CItem_Device miniaturizorUltimate = new(CItem_Device.Group.Miniaturizor, CItem_Device.Type.None, 1500f) {
        m_pickupDuration = -1,
        m_codeName = "miniaturizorUltimate"
    };

    public static readonly CItem_Device potionHp = new(CItem_Device.Group.PotionHP, CItem_Device.Type.Consumable, 0.3f) {
        m_cooldown = 15f,
        m_codeName = "potionHp"
    };

    public static readonly CItem_Device potionHpRegen = new(CItem_Device.Group.PotionHPRegen, CItem_Device.Type.Consumable, 1.5f) {
        m_cooldown = 120f,
        m_duration = 60f,
        m_codeName = "potionHpRegen"
    };

    public static readonly CItem_Device potionHpBig = new(CItem_Device.Group.PotionHP, CItem_Device.Type.Consumable, 0.5f) {
        m_cooldown = 15f,
        m_codeName = "potionHpBig"
    };

    public static readonly CItem_Device potionHpMega = new(CItem_Device.Group.PotionHP, CItem_Device.Type.Consumable, 1f) {
        m_cooldown = 15f,
        m_codeName = "potionHpMega"
    };

    public static readonly CItem_Device potionArmor = new(CItem_Device.Group.PotionArmor, CItem_Device.Type.Consumable, 0.7f) {
        m_cooldown = 120f,
        m_duration = 60f,
        m_codeName = "potionArmor"
    };

    public static readonly CItem_Device potionPheromones = new(CItem_Device.Group.PotionPheromones, CItem_Device.Type.Consumable, 0f) {
        m_cooldown = 120f,
        m_duration = 60f,
        m_codeName = "potionPheromones"
    };

    public static readonly CItem_Device potionCritics = new(CItem_Device.Group.PotionCritics, CItem_Device.Type.Consumable, 0.2f) {
        m_cooldown = 120f,
        m_duration = 60f,
        m_codeName = "potionCritics"
    };

    public static readonly CItem_Device potionInvisibility = new(CItem_Device.Group.PotionInvisibility, CItem_Device.Type.Consumable, 0.3f) {
        m_cooldown = 120f,
        m_duration = 60f,
        m_codeName = "potionInvisibility"
    };

    public static readonly CItem_Device potionSpeed = new(CItem_Device.Group.PotionSpeed, CItem_Device.Type.Consumable, 1.5f) {
        m_cooldown = 120f,
        m_duration = 60f,
        m_codeName = "potionSpeed"
    };

    public static readonly CItem_Device armorMk1 = new(CItem_Device.Group.Armor, CItem_Device.Type.Passive, 2f) {
        m_codeName = "armorMk1"
    };

    public static readonly CItem_Device armorMk2 = new(CItem_Device.Group.Armor, CItem_Device.Type.Passive, 4f) {
        m_codeName = "armorMk2"
    };

    public static readonly CItem_Device armorMk3 = new(CItem_Device.Group.Armor, CItem_Device.Type.Passive, 6f) {
        m_codeName = "armorMk3"
    };

    public static readonly CItem_Device armorUltimate = new(CItem_Device.Group.Armor, CItem_Device.Type.Passive, 100f) {
        m_codeName = "armorUltimate"
    };

    public static readonly CItem_Device defenseShield = new(CItem_Device.Group.Shield, CItem_Device.Type.Passive, 0.5f) {
        m_codeName = "defenseShield"
    };

    public static readonly CItem_Device drone = new(CItem_Device.Group.Drone, CItem_Device.Type.Activable, 0f) {
        m_cooldown = 0f,
        m_pickupDuration = -1,
        m_codeName = "drone"
    };

    public static readonly CItem_Device droneCombat = new(CItem_Device.Group.Drone, CItem_Device.Type.Activable, 1f) {
        m_cooldown = 0f,
        m_pickupDuration = -1,
        m_codeName = "droneCombat"
    };

    public static readonly CItem_Device droneWar = new(CItem_Device.Group.Drone, CItem_Device.Type.Activable, 2f) {
        m_cooldown = 0f,
        m_pickupDuration = -1,
        m_codeName = "droneWar"
    };

    public static readonly CItem_Device flashLight = new(CItem_Device.Group.FlashLight, CItem_Device.Type.Passive, 4f) {
        m_codeName = "flashLight"
    };

    public static readonly CItem_Device minimapper = new(CItem_Device.Group.Minimapper, CItem_Device.Type.Passive, 0f) {
        m_codeName = "minimapper"
    };

    public static readonly CItem_Device effeilGlasses = new(CItem_Device.Group.EffeilGlasses, CItem_Device.Type.Passive, 0f) {
        m_codeName = "effeilGlasses"
    };

    public static readonly CItem_Device metalDetector = new(CItem_Device.Group.MetalDetector, CItem_Device.Type.Activable, 100f) {
        m_cooldown = 2f,
        m_codeName = "metalDetector"
    };

    public static readonly CItem_Device waterDetector = new(CItem_Device.Group.WaterDetector, CItem_Device.Type.Passive, 0f) {
        m_codeName = "waterDetector"
    };

    public static readonly CItem_Device flashLightMK2 = new(CItem_Device.Group.FlashLight, CItem_Device.Type.Passive, 7f) {
        m_codeName = "flashLightMK2"
    };

    public static readonly CItem_Device waterBreather = new(CItem_Device.Group.WaterBreather, CItem_Device.Type.Passive, 3f) {
        m_codeName = "waterBreather"
    };

    public static readonly CItem_Device jetpack = new(CItem_Device.Group.Jetpack, CItem_Device.Type.Passive, 0f) {
        m_codeName = "jetpack"
    };

    public static readonly CItem_Device invisibilityDevice = new(CItem_Device.Group.Invisibility, CItem_Device.Type.Activable, 0f) {
        m_cooldown = 60f,
        m_duration = 30f,
        m_codeName = "invisibilityDevice"
    };

    public static readonly CItem_Device ultimateJetpack = new(CItem_Device.Group.Jetpack, CItem_Device.Type.Passive, 1f) {
        m_codeName = "ultimateJetpack"
    };

    public static readonly CItem_Device ultimateBrush = new(CItem_Device.Group.Brush, CItem_Device.Type.None, 0f) {
        m_codeName = "ultimateBrush"
    };

    public static readonly CItem_Device ultimateRebreather = new(CItem_Device.Group.WaterBreather, CItem_Device.Type.Passive, float.MaxValue) {
        m_codeName = "ultimateRebreather"
    };

    public static readonly CItem_Weapon gunRifle = new(0.15f, false, new CAttackDesc(15f, 4, 1, 0.1f, 2f, 5f, GBullets.plasma)) {
        m_codeName = "gunRifle"
    };

    public static readonly CItem_Weapon gunShotgun = new(0.3f, false, new CAttackDesc(9f, 4, 8, 0.13f, 10f, 5f, GBullets.shotgun)) {
        m_codeName = "gunShotgun"
    };

    public static readonly CItem_Weapon gunMachineGun = new(0.02f, true, new CAttackDesc(15f, 5, 1, 0.11f, 2f, 5f, GBullets.plasma)) {
        m_codeName = "gunMachineGun"
    };

    public static readonly CItem_Weapon gunSnipe = new(0.4f, false, new CAttackDesc(20f, 35, 1, 0.25f, 10f, 30f, GBullets.snipe)) {
        m_codeName = "gunSnipe"
    };

    public static readonly CItem_Weapon gunLaser = new(0f, true, new CAttackDesc(15f, 10, 1, 0.22f, 0f, 0f, GBullets.laser)) {
        m_codeName = "gunLaser"
    };

    public static readonly CItem_Weapon gunRocket = new(0.5f, false, new CAttackDesc(25f, 50, 1, 0.3f, 5f, 30f, GBullets.rocket)) {
        m_codeName = "gunRocket"
    };

    public static readonly CItem_Weapon gunZF0 = new(0.01f, true, new CAttackDesc(25f, 7, 1, 0.08f, 0f, 2f, GBullets.zf0bullet)) {
        m_codeName = "gunZF0"
    };

    public static readonly CItem_Weapon gunMegaSnipe = new(0.8f, false, new CAttackDesc(20f, 200, 1, 0.75f, 30f, 70f, GBullets.megasnipe)) {
        m_codeName = "gunMegaSnipe"
    };

    public static readonly CItem_Weapon gunLaserGatling = new(0f, true, new CAttackDesc(15f, 10, 1, 0.1f, 0f, 0f, GBullets.laserGatling)) {
        m_codeName = "gunLaserGatling"
    };

    public static readonly CItem_Weapon gunStorm = new(0.2f, true, new CAttackDesc(25f, 65, 1, 0.8f, 0f, 30f, null)) {
        m_codeName = "gunStorm"
    };

    public static readonly CItem_Weapon gunGrenadeLaunch = new(0.2f, false, new CAttackDesc(20f, 50, 1, 0.2f, 0f, 30f, GBullets.grenade)) {
        m_codeName = "gunGrenadeLaunch"
    };

    public static readonly CItem_Weapon gunParticlesShotgun = new(0.3f, false, new CAttackDesc(7f, 40, 8, 0.1f, 12f, 7f, GBullets.particlesShotgun)) {
        m_codeName = "gunParticlesShotgun"
    };

    public static readonly CItem_Weapon gunParticlesSniper = new(0.3f, true, new CAttackDesc(20f, 100, 1, 0.15f, 8f, 20f, GBullets.particlesSnip)) {
        m_codeName = "gunParticlesSniper"
    };

    public static readonly CItem_Weapon gunFlamethrower = new(0f, true, new CAttackDesc(12f, 12, 1, 0.1f, 0f, 0f, GBullets.flamethrower)) {
        m_codeName = "gunFlamethrower"
    };

    public static readonly CItem_Weapon gunLightSword = new(0f, true, new CAttackDesc(1f, 100, 1, 0.3f, 0f, 0f, null)) {
        m_codeName = "gunLightSword"
    };

    public static readonly CItem_Weapon gunUltimateParticlesGatling = new(0f, true, new CAttackDesc(20f, 500, 1, 0.08f, 2f, 50f, GBullets.particlesSnip)) {
        m_codeName = "gunUltimateParticlesGatling"
    };

    public static readonly CItem_Weapon gunUltimateGrenadeLauncher = new(0f, false, new CAttackDesc(20f, 1010, 1, 0.2f, 0f, 50f, GBullets.grenadeUltimate)) {
        m_codeName = "gunUltimateGrenadeLauncher"
    };

    public static readonly CItem_Weapon ultimateWaterPistol = new(0f, true, new CAttackDesc(0f, 0, 1, 0.2f, 0f, 0f, null)) {
        m_codeName = "ultimateWaterPistol"
    };

    public static readonly CItem_Weapon ultimateLavaPistol = new(0f, true, new CAttackDesc(0f, 0, 1, 0.2f, 0f, 0f, null)) {
        m_codeName = "ultimateLavaPistol"
    };

    public static readonly CItem_Weapon ultimateSpongePistol = new(0f, true, new CAttackDesc(0f, 0, 1, 0.2f, 0f, 0f, null)) {
        m_codeName = "ultimateSpongePistol"
    };

    public static readonly CItem_Weapon ultimateTotoroGun = new(0f, true, new CAttackDesc(0f, 0, 1, 0.1f, 0f, 0f, null)) {
        m_codeName = "ultimateTotoroGun"
    };

    public static readonly CItem_Weapon ultimateMonstersGun = new(0f, false, new CAttackDesc(0f, 0, 1, 0.1f, 0f, 0f, null)) {
        m_codeName = "ultimateMonstersGun"
    };

    public static readonly CItem_Defense turret360 = new(25, 8947848U, 10f, -9999f, 9999f, new CAttackDesc(8f, 4, 1, 0.6f, 0f, 1f, GBullets.defenses)) {
        m_anchor = CItemCell.Anchor.Everyside_Small,
        m_codeName = "turret360"
    };

    public static readonly CItem_Defense turretGatling = new(75, 8947848U, 10f, -1f, 1f, new CAttackDesc(8f, 4, 1, 0.2f, 0f, 1f, GBullets.defenses)) {
        m_isReversable = true,
        m_codeName = "turretGatling"
    };

    public static readonly CItem_Defense turretReparator = new(75, 8947848U, 3.5f, -9999f, 9999f, new CAttackDesc(3.5f, -2, 0, 0.5f, 0f, 0f, null)) {
        m_anchor = CItemCell.Anchor.Everyside_Small,
        m_displayRangeOnCells = true,
        m_neverUnspawn = true,
        m_codeName = "turretReparator"
    };

    public static readonly CItem_Defense turretHeavy = new(150, 8947848U, 10f, -9999f, 9999f, new CAttackDesc(8f, 20, 1, 1f, 0f, 3f, GBullets.plasma)) {
        m_anchor = CItemCell.Anchor.Everyside_Small,
        m_codeName = "turretHeavy"
    };

    public static readonly CItem_Defense turretMine = new(150, 8947848U, 2f, 0f, 360f, new CAttackDesc(15f, 150, 0, -1f, 0f, 0f, null)) {
        m_light = Color24.FromNumber(9724047U),
        m_codeName = "turretMine"
    };

    public static readonly CItem_Defense turretSpikes = new(150, 8947848U, 0.9f, 0f, 180f, new CAttackDesc(0.9f, 20, 1, 0.5f, 0f, 0f, null)) {
        m_colRect = new(0.1f, 0f, 0.8f, 0.3f),
        m_electricValue = -1,
        m_light = Color24.FromNumber(9724047U),
        m_codeName = "turretSpikes"
    };

    public static readonly CItem_Defense turretReparatorMK2 = new(150, 8947848U, 6.5f, -9999f, 9999f, new CAttackDesc(5.5f, -6, 0, 0.5f, 0f, 0f, null)) {
        m_displayRangeOnCells = true,
        m_electricValue = -1,
        m_light = Color24.FromNumber(10329710U),
        m_neverUnspawn = true,
        m_codeName = "turretReparatorMK2"
    };

    public static readonly CItem_Defense turretCeiling = new(250, 8947848U, 2.9f, -105f, -75f, new CAttackDesc(3.1f, 30, 1, 1f, 0f, 0f, null)) {
        m_anchor = CItemCell.Anchor.Top_Small,
        m_colRect = new(0.1f, 0.6f, 0.8f, 0.4f),
        m_codeName = "turretCeiling"
    };

    public static readonly CItem_Defense turretLaser = new(250, 8947848U, 10f, -1f, 1f, new CAttackDesc(10f, 20, 1, 0.3f, 0f, 0f, GBullets.laser)) {
        m_isReversable = true,
        m_codeName = "turretLaser"
    };

    public static readonly CItem_Defense turretTesla = new(250, 8947848U, 8.9f, -9999f, 9999f, new CAttackDesc(8.1f, 100, 1, 2f, 0f, 10f, null)) {
        m_electricValue = -3,
        m_light = Color24.FromNumber(9724047U),
        m_codeName = "turretTesla"
    };

    public static readonly CItem_Defense turretFlame = new(400, 8947848U, 10f, -9999f, 9999f, new CAttackDesc(6f, 8, 1, 0.15f, 0f, 0f, GBullets.flamethrower)) {
        m_anchor = CItemCell.Anchor.Everyside_Small,
        m_codeName = "turretFlame"
    };

    public static readonly CItem_Defense turretParticles = new(250, 8947848U, 10f, -9999f, 9999f, new CAttackDesc(12f, 30, 1, 0.5f, 0f, 3f, GBullets.particlesSnipTurret)) {
        m_anchor = CItemCell.Anchor.Everyside_Small,
        m_codeName = "turretParticles"
    };

    public static readonly CItem_Defense explosive = new(250, 8947848U, 0f, 0f, 360f, new CAttackDesc(5f, 2000, 0, -1f, 0f, 10f, null)) {
        m_isActivable = true,
        m_neverUnspawn = true,
        m_codeName = "explosive"
    };

    public static readonly CItem_Wall wallWood = new(50, 12282173U, 2500, 350f, CItem_Wall.Type.WallBlock) {
        m_codeName = "wallWood"
    };

    public static readonly CItem_Wall platform = new(50, 12282173U, 0, 0f, CItem_Wall.Type.Platform) {
        m_codeName = "platform"
    };

    public static readonly CItem_Wall wallConcrete = new(150, 12697270U, 5000, 500f, CItem_Wall.Type.WallBlock) {
        m_codeName = "wallConcrete"
    };

    public static readonly CItem_Wall wallIronSupport = new(150, 11647938U, 4000, 360f, CItem_Wall.Type.WallPassable) {
        m_codeName = "wallIronSupport"
    };

    public static readonly CItem_Wall backwall = new(75, 8289397U, 0, 0f, CItem_Wall.Type.Backwall) {
        m_codeName = "backwall"
    };

    public static readonly CItem_Wall wallReinforced = new(300, 11315619U, 7000, 560f, CItem_Wall.Type.WallBlock) {
        m_codeName = "wallReinforced"
    };

    public static readonly CItem_Wall wallDoor = new(300, 12634571U, 7000, 650f, CItem_Wall.Type.WallBlock) {
        m_isActivable = true,
        m_isDoor = true,
        m_electricValue = -255,
        m_electricityOutletFlags = 2,
        m_codeName = "wallDoor"
    };

    public static readonly CItem_Wall platformSteel = new(50, 12634571U, 0, 0f, CItem_Wall.Type.Platform) {
        m_codeName = "platformSteel"
    };

    public static readonly CItem_Wall generatorWater = new(300, 13731096U, 7000, 750f, CItem_Wall.Type.WallBlock) {
        m_electricValue = 50,
        m_electricVariablePower = true,
        m_codeName = "generatorWater"
    };

    public static readonly CItem_Wall waterPump = new(300, 13731096U, 7000, 750f, CItem_Wall.Type.WallBlock) {
        m_electricValue = -4,
        m_isReversable = true,
        m_codeName = "waterPump"
    };

    public static readonly CItem_Wall wallComposite = new(500, 12039872U, 9000, 560f, CItem_Wall.Type.WallBlock) {
        m_codeName = "wallComposite"
    };

    public static readonly CItem_Wall wallCompositeSupport = new(400, 12634571U, 6000, 350f, CItem_Wall.Type.WallPassable) {
        m_codeName = "wallCompositeSupport"
    };

    public static readonly CItem_Wall wallCompositeLight = new(500, 12173251U, 4500, 280f, CItem_Wall.Type.WallBlock) {
        m_codeName = "wallCompositeLight"
    };

    public static readonly CItem_Wall wallCompositeDoor = new(500, 12634571U, 9000, 700f, CItem_Wall.Type.WallBlock) {
        m_isActivable = true,
        m_isDoor = true,
        m_electricValue = -255,
        m_electricityOutletFlags = 2,
        m_codeName = "wallCompositeDoor"
    };

    public static readonly CItem_Wall wallUltimate = new(1000, 12039872U, 12000, 500f, CItem_Wall.Type.WallBlock) {
        m_codeName = "wallUltimate"
    };

    public static readonly CItem_MachineAutoBuilder autoBuilderMK1 = new() {
        m_light = new(100, 100, 70),
        m_customValue = 1f,
        m_codeName = "autoBuilderMK1"
    };

    public static readonly CItem_MachineAutoBuilder autoBuilderMK2 = new() {
        m_light = new(100, 100, 70),
        m_customValue = 2f,
        m_codeName = "autoBuilderMK2"
    };

    public static readonly CItem_Machine light = new(30, 10066329U, CItemCell.Anchor.Bottom_Small) {
        m_light = new(150, 150, 95),
        m_codeName = "light"
    };

    public static readonly CItem_MachineAutoBuilder autoBuilderMK3 = new() {
        m_light = new(100, 100, 70),
        m_customValue = 3f,
        m_codeName = "autoBuilderMK3"
    };

    public static readonly CItem_Machine lightSticky = new(100, 10066329U, CItemCell.Anchor.Everywhere_Small) {
        m_light = new(150, 150, 95),
        m_codeName = "lightSticky"
    };

    public static readonly CItem_MachineWire electricWire = new() {
        m_codeName = "electricWire"
    };

    public static readonly CItem_Machine generatorSun = new(100, 10066329U, CItemCell.Anchor.Bottom_Small) {
        m_electricValue = 1,
        m_codeName = "generatorSun"
    };

    public static readonly CItem_Machine lightSun = new(100, 10066329U, CItemCell.Anchor.Top_Small) {
        m_light = new(140, 140, 140),
        m_electricValue = -1,
        m_electricityOutletFlags = 2,
        m_codeName = "lightSun"
    };

    public static readonly CItem_MachineTeleport teleport = new(100, 10066329U, CItemCell.Anchor.Bottom_Small) {
        m_isActivable = true,
        m_light = new(130, 172, 160),
        m_electricValue = -3,
        m_codeName = "teleport"
    };

    public static readonly CItem_Machine elecSwitch = new(100, 10066329U, CItemCell.Anchor.Back_Small) {
        m_isActivable = true,
        m_electricityOutletFlags = 3,
        m_codeName = "elecSwitch"
    };

    public static readonly CItem_MachineAutoBuilder autoBuilderMK4 = new() {
        m_light = new(100, 100, 70),
        m_customValue = 4f,
        m_codeName = "autoBuilderMK4"
    };

    public static readonly CItem_MachineAutoBuilder autoBuilderMK5 = new() {
        m_light = new(100, 100, 70),
        m_customValue = 5f,
        m_codeName = "autoBuilderMK5"
    };

    public static readonly CItem_Machine elecSwitchPush = new(100, 10066329U, CItemCell.Anchor.Back_Small) {
        m_isActivable = true,
        m_electricityOutletFlags = 3,
        m_codeName = "elecSwitchPush"
    };

    public static readonly CItem_Machine elecSwitchRelay = new(100, 10066329U, CItemCell.Anchor.Back_Small) {
        m_electricityOutletFlags = 11,
        m_codeName = "elecSwitchRelay"
    };

    public static readonly CItem_Machine elecCross = new(100, 10066329U, CItemCell.Anchor.Back_Small) {
        m_electricityOutletFlags = 15,
        m_fireProof = true,
        m_codeName = "elecCross"
    };

    public static readonly CItem_Machine elecSignal = new(100, 10066329U, CItemCell.Anchor.Back_Small) {
        m_electricValue = 255,
        m_electricityOutletFlags = 1,
        m_codeName = "elecSignal"
    };

    public static readonly CItem_Machine elecClock = new(100, 10066329U, CItemCell.Anchor.Back_Small) {
        m_electricValue = 255,
        m_electricityOutletFlags = 1,
        m_codeName = "elecClock"
    };

    public static readonly CItem_Machine elecToggle = new(100, 10066329U, CItemCell.Anchor.Back_Small) {
        m_electricValue = 255,
        m_electricityOutletFlags = 9,
        m_codeName = "elecToggle"
    };

    public static readonly CItem_Machine elecDelay = new(100, 10066329U, CItemCell.Anchor.Back_Small) {
        m_electricValue = 255,
        m_electricityOutletFlags = 9,
        m_codeName = "elecDelay"
    };

    public static readonly CItem_Machine elecWaterSensor = new(100, 10066329U, CItemCell.Anchor.Back_Small) {
        m_electricValue = 255,
        m_electricityOutletFlags = 1,
        m_fireProof = true,
        m_codeName = "elecWaterSensor"
    };

    public static readonly CItem_Machine elecProximitySensor = new(100, 10066329U, CItemCell.Anchor.Back_Small) {
        m_electricValue = 255,
        m_electricityOutletFlags = 1,
        m_fireProof = true,
        m_customValue = 0f,
        m_isActivable = true,
        m_codeName = "elecProximitySensor"
    };

    public static readonly CItem_Machine elecDistanceSensor = new(100, 10066329U, CItemCell.Anchor.Back_Small) {
        m_electricValue = 255,
        m_electricityOutletFlags = 1,
        m_fireProof = true,
        m_customValue = 6f,
        m_isActivable = true,
        m_codeName = "elecDistanceSensor"
    };

    public static readonly CItem_Machine elecAND = new(100, 10066329U, CItemCell.Anchor.Back_Small) {
        m_electricValue = 255,
        m_electricityOutletFlags = 13,
        m_codeName = "elecAND"
    };

    public static readonly CItem_Machine elecOR = new(100, 10066329U, CItemCell.Anchor.Back_Small) {
        m_electricValue = 255,
        m_electricityOutletFlags = 13,
        m_codeName = "elecOR"
    };

    public static readonly CItem_Machine elecXOR = new(100, 10066329U, CItemCell.Anchor.Back_Small) {
        m_electricValue = 255,
        m_electricityOutletFlags = 13,
        m_codeName = "elecXOR"
    };

    public static readonly CItem_Machine elecNOT = new(100, 10066329U, CItemCell.Anchor.Back_Small) {
        m_electricValue = 255,
        m_electricityOutletFlags = 9,
        m_codeName = "elecNOT"
    };

    public static readonly CItem_Machine elecLight = new(100, 10066329U, CItemCell.Anchor.Back_Small) {
        m_electricValue = -255,
        m_electricityOutletFlags = 2,
        m_light = new(byte.MaxValue, 0, 0),
        m_codeName = "elecLight"
    };

    public static readonly CItem_Machine elecAlarm = new(100, 10066329U, CItemCell.Anchor.Back_Small) {
        m_electricValue = -255,
        m_electricityOutletFlags = 2,
        m_light = new(byte.MaxValue, 0, 0),
        m_codeName = "elecAlarm"
    };

    public static readonly CItem_Machine reactor = new(100, 10066329U, CItemCell.Anchor.Bottom_Small) {
        m_light = Color24.FromNumber(8683943U),
        m_electricValue = 5,
        m_codeName = "reactor"
    };

    public static readonly CItem_Machine rocketTop = new(100, 10066329U, CItemCell.Anchor.Special) {
        m_isActivable = true,
        m_codeName = "rocketTop"
    };

    public static readonly CItem_Machine rocketTank = new(100, 10066329U, CItemCell.Anchor.Special) {
        m_codeName = "rocketTank"
    };

    public static readonly CItem_Machine rocketEngine = new(100, 10066329U, CItemCell.Anchor.Bottom_Small) {
        m_codeName = "rocketEngine"
    };

    public static readonly CItem_MachineAutoBuilder autoBuilderUltimate = new() {
        m_allFree = true,
        m_light = new(100, 100, 70),
        m_customValue = 6f,
        m_codeName = "autoBuilderUltimate"
    };

    public static readonly CItem_MineralDirt dirt = new(30, 9465936U, new CLifeConditions(280, 1024, 95, 255, 0f, 0.2f, 0.01f, 9f, true, true, false)) {
        m_codeName = "dirt"
    };

    public static readonly CItem_MineralDirt dirtRed = new(30, 12017473U, new CLifeConditions(280, 1024, 95, 255, 0f, 0.2f, 0.01f, 9f, true, true, false)) {
        m_codeName = "dirtRed"
    };

    public static readonly CItem_MineralDirt silt = new(30, 11900747U, new CLifeConditions(280, 1024, 64, 255, 0.1f, 9f, 0.01f, 9f, true, true, false)) {
        m_codeName = "silt"
    };

    public static readonly CItem_MineralDirt dirtBlack = new(30, 3552048U, null) {
        m_codeName = "dirtBlack"
    };

    public static readonly CItem_MineralDirt dirtSky = new(30, 5343355U, null) {
        m_codeName = "dirtSky"
    };

    public static readonly CItem_Mineral rock = new(120, 7303805U, false) {
        m_codeName = "rock"
    };

    public static readonly CItem_Mineral iron = new(120, 13479064U, false) {
        m_codeName = "iron"
    };

    public static readonly CItem_Mineral coal = new(140, 3226438U, false) {
        m_codeName = "coal"
    };

    public static readonly CItem_Mineral copper = new(140, 15371089U, false) {
        m_codeName = "copper"
    };

    public static readonly CItem_Mineral gold = new(140, 16244886U, false) {
        m_canBeMetalDetected = true,
        m_codeName = "gold"
    };

    public static readonly CItem_Mineral aluminium = new(250, 14606046U, false) {
        m_codeName = "aluminium"
    };

    public static readonly CItem_Mineral rockFlying = new(250, 10271685U, false) {
        m_codeName = "rockFlying"
    };

    public static readonly CItem_Mineral rockGaz = new(250, 13694451U, false) {
        m_codeName = "rockGaz"
    };

    public static readonly CItem_Mineral crystal = new(400, 9560307U, false) {
        m_isCrystalGrowingOnWater = true,
        m_codeName = "crystal"
    };

    public static readonly CItem_Mineral crystalLight = new(400, 13498107U, false) {
        m_light = Color24.FromNumber(8172468U),
        m_isCrystalGrowingOnWater = true,
        m_codeName = "crystalLight"
    };

    public static readonly CItem_Mineral crystalBlack = new(400, 5789784U, false) {
        m_isCrystalGrowingOnWater = true,
        m_codeName = "crystalBlack"
    };

    public static readonly CItem_Mineral granit = new(400, 6579054U, false) {
        m_codeName = "granit"
    };

    public static readonly CItem_Mineral uranium = new(400, 11924194U, false) {
        m_canBeMetalDetected = true,
        m_codeName = "uranium"
    };

    public static readonly CItem_Mineral titanium = new(600, 15132390U, false) {
        m_codeName = "titanium"
    };

    public static readonly CItem_Mineral lightonium = new(600, 14606046U, false) {
        m_light = Color24.FromNumber(14540253U),
        m_codeName = "lightonium"
    };

    public static readonly CItem_Mineral thorium = new(600, 14961403U, false) {
        m_canBeMetalDetected = true,
        m_codeName = "thorium"
    };

    public static readonly CItem_Mineral sulfur = new(600, 16775349U, false) {
        m_codeName = "sulfur"
    };

    public static readonly CItem_Mineral sapphire = new(600, 4341707U, false) {
        m_light = Color24.FromNumber(4534368U),
        m_pickupDuration = -1,
        m_codeName = "sapphire"
    };

    public static readonly CItem_Mineral organicRock = new(600, 7414047U, false) {
        m_pickupDuration = 5,
        m_codeName = "organicRock"
    };

    public static readonly CItem_Mineral organicRockHeart = new(600, 10758460U, false) {
        m_light = Color24.FromNumber(6684672U),
        m_pickupDuration = -1,
        m_codeName = "organicRockHeart"
    };

    public static readonly CItem_Mineral lava = new(600, 5131597U, false) {
        m_codeName = "lava"
    };

    public static readonly CItem_Mineral lavaOld = new(1000, 6118492U, false) {
        m_stopMonsters = true,
        m_codeName = "lavaOld"
    };

    public static readonly CItem_Mineral diamonds = new(1000, 15175053U, false) {
        m_pickupDuration = -1,
        m_stopMonsters = true,
        m_codeName = "diamonds"
    };

    public static readonly CItem_Mineral organicRockDefenseLead = new(1000, 16777215U, false) {
        m_light = Color24.FromNumber(5592405U),
        m_pickupDuration = 5,
        m_stopMonsters = true,
        m_codeName = "organicRockDefenseLead"
    };

    public static readonly CItem_Mineral organicRockDefense = new(1000, 16777215U, false) {
        m_light = Color24.FromNumber(5592405U),
        m_pickupDuration = 5,
        m_stopMonsters = true,
        m_codeName = "organicRockDefense"
    };

    public static readonly CItem_Material wood = new(50, 12282168U, CItemCell.Anchor.NotPlacable) {
        m_codeName = "wood"
    };

    public static readonly CItem_Material woodwater = new(50, 12282168U, CItemCell.Anchor.NotPlacable) {
        m_codeName = "woodwater"
    };

    public static readonly CItem_Material woodSky = new(50, 6740927U, CItemCell.Anchor.NotPlacable) {
        m_codeName = "woodSky"
    };

    public static readonly CItem_Material woodGranit = new(50, 6740927U, CItemCell.Anchor.NotPlacable) {
        m_codeName = "woodGranit"
    };

    public static readonly CItem_Material deadPlant = new(50, 8224125U, CItemCell.Anchor.NotPlacable) {
        m_pickupDuration = 30,
        m_codeName = "deadPlant"
    };

    public static readonly CItem_Tree tree = new(new CLifeConditions(280, 1024, 95, 255, 0f, 0.8f, 0.01f, 1f, true, true, false), 50, 10509611U, GItems.wood, 7, 0) {
        m_codeName = "tree"
    };

    public static readonly CItem_Tree treePine = new(new CLifeConditions(280, 1024, 95, 255, 0f, 0.8f, 0.01f, 1f, true, true, false), 50, 10509611U, GItems.wood, 5, 0) {
        m_codeName = "treePine"
    };

    public static readonly CItem_Tree treeWater = new(new CLifeConditions(280, 1024, 64, 255, 1.1f, 90f, 0.01f, 1f, true, true, false), 50, 15094318U, GItems.woodwater, 3, 0) {
        m_codeName = "treeWater"
    };

    public static readonly CItem_Tree treeSky = new(new CLifeConditions(0, 1024, 95, 255, 0f, 0.8f, 0f, 1f, true, true, false), 50, 5951920U, GItems.woodSky, 6, 2) {
        m_codeName = "treeSky"
    };

    public static readonly CItem_Tree treeGranit = new(new CLifeConditions(0, 280, 95, 255, 0f, 1f, 0f, 1f, true, true, false), 50, 2716319U, GItems.woodGranit, 4, 2) {
        m_codeName = "treeGranit"
    };

    public static readonly CItem_Plant bush = new(new CLifeConditions(280, 1024, 95, 255, 0f, 0.2f, 0.01f, 9f, true, true, false), 30, 15910423U, 1) {
        m_codeName = "bush"
    };

    public static readonly CItem_Plant flowerBlue = new(new CLifeConditions(280, 1024, 95, 255, 0f, 0.2f, 0.01f, 9f, true, true, false), 30, 6137855U, 1) {
        m_codeName = "flowerBlue"
    };

    public static readonly CItem_Plant flowerWhite = new(new CLifeConditions(0, 1024, 95, 255, 0f, 0.2f, 0.01f, 9f, true, true, false), 30, 14211288U, 1) {
        m_codeName = "flowerWhite"
    };

    public static readonly CItem_Plant fernRed = new(new CLifeConditions(280, 1024, 95, 255, 0f, 1.4f, 0.01f, 9f, true, true, false), 30, 16401476U, 1) {
        m_codeName = "fernRed"
    };

    public static readonly CItem_Plant waterBush = new(new CLifeConditions(280, 1024, 64, 255, 0.15f, 1.7f, 0.01f, 9f, true, true, false), 30, 14887956U, 1) {
        m_codeName = "waterBush"
    };

    public static readonly CItem_Plant waterLight = new(new CLifeConditions(280, 1024, 0, 255, 0.15f, 90f, 0.01f, 9f, true, true, false), 30, 16761958U, 0) {
        m_light = Color24.FromNumber(10523748U),
        m_codeName = "waterLight"
    };

    public static readonly CItem_Plant waterCoral = new(new CLifeConditions(280, 1024, 64, 255, 1.7f, 90f, 0.01f, 9f, true, true, false), 30, 15886409U, 1) {
        m_spontaneousGrowAdditionnalChance = 0.1f,
        m_codeName = "waterCoral"
    };

    public static readonly CItem_Plant blackGrass = new(new CLifeConditions(0, 1024, 0, 25, 0f, 0.2f, 0f, 1f, true, true, false), 7, 4603192U, 1) {
        m_codeName = "blackGrass"
    };

    public static readonly CItem_Plant blackMushroom = new(new CLifeConditions(0, 1024, 0, 25, 0f, 0.2f, 0f, 1f, true, true, false), 7, 3484196U, 1) {
        m_codeName = "blackMushroom"
    };

    public static readonly CItem_Plant skyBush = new(new CLifeConditions(0, 1024, 95, 255, 0f, 0.2f, 0f, 1f, true, true, false), 30, 2056333U, 1) {
        m_codeName = "skyBush"
    };

    public static readonly CItem_Plant lavaFlower = new(new CLifeConditions(0, 1024, 0, 255, 1f, 15f, 0f, 1f, true, true, true), 30, 15879709U, 1) {
        m_fireProof = true,
        m_codeName = "lavaFlower"
    };

    public static readonly CItem_Plant lavaPlant = new(new CLifeConditions(0, 1024, 0, 255, 15f, 99f, 0f, 1f, true, true, true), 30, 13386244U, 1) {
        m_fireProof = true,
        m_codeName = "lavaPlant"
    };

    public static readonly CItem_Plant bushGranit = new(new CLifeConditions(0, 280, 95, 255, 0f, 1f, 0f, 1f, true, true, false), 30, 1726340U, 1) {
        m_codeName = "bushGranit"
    };

    public static readonly CItem_Plant organicHair = new(new CLifeConditions(0, 1024, 95, 255, 0f, 1f, 0f, 1f, true, true, false), 30, 8742224U, 12) {
        m_codeName = "organicHair"
    };

    public static readonly CItem_Material metalScrap = new(0, 16777215U, CItemCell.Anchor.NotPlacable) {
        m_pickupDuration = 1200,
        m_codeName = "metalScrap"
    };

    public static readonly CItem_Material lightGem = new(0, 16777215U, CItemCell.Anchor.NotPlacable) {
        m_codeName = "lightGem"
    };

    public static readonly CItem_Material energyGem = new(0, 16777215U, CItemCell.Anchor.NotPlacable) {
        m_codeName = "energyGem"
    };

    public static readonly CItem_Material darkGem = new(0, 16777215U, CItemCell.Anchor.NotPlacable) {
        m_codeName = "darkGem"
    };

    public static readonly CItem_Material dogHorn = new(0, 16777215U, CItemCell.Anchor.NotPlacable) {
        m_codeName = "dogHorn"
    };

    public static readonly CItem_Material dogHorn3 = new(0, 16777215U, CItemCell.Anchor.NotPlacable) {
        m_codeName = "dogHorn3"
    };

    public static readonly CItem_Material moleShell = new(0, 16777215U, CItemCell.Anchor.NotPlacable) {
        m_codeName = "moleShell"
    };

    public static readonly CItem_Material moleShellBlack = new(0, 16777215U, CItemCell.Anchor.NotPlacable) {
        m_codeName = "moleShellBlack"
    };

    public static readonly CItem_Material fish2Regen = new(0, 16777215U, CItemCell.Anchor.NotPlacable) {
        m_codeName = "fish2Regen"
    };

    public static readonly CItem_Material fish3Regen = new(0, 16777215U, CItemCell.Anchor.NotPlacable) {
        m_codeName = "fish3Regen"
    };

    public static readonly CItem_Material bat2Sonar = new(0, 16777215U, CItemCell.Anchor.NotPlacable) {
        m_codeName = "bat2Sonar"
    };

    public static readonly CItem_Material bat3Sonar = new(0, 16777215U, CItemCell.Anchor.NotPlacable) {
        m_codeName = "bat3Sonar"
    };

    public static readonly CItem_Material antShell = new(0, 16777215U, CItemCell.Anchor.NotPlacable) {
        m_codeName = "antShell"
    };

    public static readonly CItem_Material sharkSkin = new(0, 16777215U, CItemCell.Anchor.NotPlacable) {
        m_codeName = "sharkSkin"
    };

    public static readonly CItem_Material unstableGemResidue = new(0, 16777215U, CItemCell.Anchor.NotPlacable) {
        m_codeName = "unstableGemResidue"
    };

    public static readonly CItem_Material lootDwellerLord = new(0, 16777215U, CItemCell.Anchor.NotPlacable) {
        m_pickupDuration = -1,
        m_pickupAutoPicked = true,
        m_codeName = "lootDwellerLord"
    };

    public static readonly CItem_Material lootParticleGround = new(0, 16777215U, CItemCell.Anchor.NotPlacable) {
        m_codeName = "lootParticleGround"
    };

    public static readonly CItem_Material lootParticleBirds = new(0, 16777215U, CItemCell.Anchor.NotPlacable) {
        m_codeName = "lootParticleBirds"
    };

    public static readonly CItem_Material lootLargeParticleBirds = new(0, 16777215U, CItemCell.Anchor.NotPlacable) {
        m_codeName = "lootLargeParticleBirds"
    };

    public static readonly CItem_Material lootLavaSpider = new(0, 16777215U, CItemCell.Anchor.NotPlacable) {
        m_codeName = "lootLavaSpider"
    };

    public static readonly CItem_Material lootLavaBat = new(0, 16777215U, CItemCell.Anchor.NotPlacable) {
        m_codeName = "lootLavaBat"
    };

    public static readonly CItem_Material lootMiniBalrog = new(0, 16777215U, CItemCell.Anchor.NotPlacable) {
        m_codeName = "lootMiniBalrog"
    };

    public static readonly CItem_Material bloodyFlesh1 = new(0, 16777215U, CItemCell.Anchor.NotPlacable) {
        m_codeName = "bloodyFlesh1"
    };

    public static readonly CItem_Material bloodyFlesh2 = new(0, 16777215U, CItemCell.Anchor.NotPlacable) {
        m_codeName = "bloodyFlesh2"
    };

    public static readonly CItem_Material bloodyFlesh3 = new(0, 16777215U, CItemCell.Anchor.NotPlacable) {
        m_codeName = "bloodyFlesh3"
    };

    public static readonly CItem_Material bossMadCrabSonar = new(0, 16777215U, CItemCell.Anchor.NotPlacable) {
        m_pickupDuration = -1,
        m_pickupAutoPicked = true,
        m_codeName = "bossMadCrabSonar"
    };

    public static readonly CItem_Material bossMadCrabMaterial = new(0, 16777215U, CItemCell.Anchor.NotPlacable) {
        m_pickupDuration = -1,
        m_pickupAutoPicked = true,
        m_codeName = "bossMadCrabMaterial"
    };

    public static readonly CItem_Material masterGem = new(0, 16777215U, CItemCell.Anchor.NotPlacable) {
        m_pickupDuration = -1,
        m_pickupAutoPicked = true,
        m_codeName = "masterGem"
    };

    public static readonly CItem_Material lootBalrog = new(0, 16777215U, CItemCell.Anchor.NotPlacable) {
        m_pickupDuration = -1,
        m_pickupAutoPicked = true,
        m_codeName = "lootBalrog"
    };

    public static readonly CItem?[] items = [
        null,
        miniaturizorMK1, miniaturizorMK2, miniaturizorMK3, miniaturizorMK4, miniaturizorMK5, miniaturizorUltimate,
        potionHp, potionHpRegen, potionHpBig, potionHpMega, potionArmor, potionPheromones, potionCritics,
        potionInvisibility, potionSpeed, armorMk1, armorMk2, armorMk3, armorUltimate, defenseShield,
        drone, droneCombat, droneWar, flashLight, minimapper, effeilGlasses, metalDetector, waterDetector,
        flashLightMK2, waterBreather, jetpack, invisibilityDevice, ultimateJetpack, ultimateBrush, ultimateRebreather,
        
        gunRifle, gunShotgun, gunMachineGun, gunSnipe, gunLaser, gunRocket, gunZF0, gunMegaSnipe,
        gunLaserGatling, gunStorm, gunGrenadeLaunch, gunParticlesShotgun, gunParticlesSniper, gunFlamethrower,
        gunLightSword, gunUltimateParticlesGatling, gunUltimateGrenadeLauncher, ultimateWaterPistol,
        ultimateLavaPistol, ultimateSpongePistol, ultimateTotoroGun, ultimateMonstersGun,
        
        turret360, turretGatling, turretReparator, turretHeavy, turretMine, turretSpikes, turretReparatorMK2,
        turretCeiling, turretLaser, turretTesla, turretFlame, turretParticles, explosive,
        
        wallWood, platform, wallConcrete, wallIronSupport, backwall, wallReinforced, wallDoor, platformSteel,
        generatorWater, waterPump, wallComposite, wallCompositeSupport, wallCompositeLight, wallCompositeDoor,
        wallUltimate,
        
        autoBuilderMK1, autoBuilderMK2, light, autoBuilderMK3, lightSticky, electricWire, generatorSun, lightSun,
        teleport, elecSwitch, autoBuilderMK4, autoBuilderMK5, elecSwitchPush, elecSwitchRelay, elecCross,
        elecSignal, elecClock, elecToggle, elecDelay, elecWaterSensor, elecProximitySensor, elecDistanceSensor,
        elecAND, elecOR, elecXOR, elecNOT, elecLight, elecAlarm, reactor, rocketTop, rocketTank, rocketEngine,
        autoBuilderUltimate,
        
        dirt, dirtRed, silt, dirtBlack, dirtSky, rock, iron, coal, copper, gold, aluminium, rockFlying, rockGaz,
        crystal, crystalLight, crystalBlack, granit, uranium, titanium, lightonium, thorium, sulfur, sapphire,
        organicRock, organicRockHeart, lava, lavaOld, diamonds, organicRockDefenseLead, organicRockDefense,
        
        wood, woodwater, woodSky, woodGranit, deadPlant,
        
        tree, treePine, treeWater, treeSky, treeGranit,
        
        bush, flowerBlue, flowerWhite, fernRed, waterBush, waterLight, waterCoral, blackGrass, blackMushroom,
        skyBush, lavaFlower, lavaPlant, bushGranit, organicHair,
        
        metalScrap, lightGem, energyGem, darkGem, dogHorn, dogHorn3, moleShell, moleShellBlack, fish2Regen,
        fish3Regen, bat2Sonar, bat3Sonar, antShell, sharkSkin, unstableGemResidue, lootDwellerLord,
        lootParticleGround, lootParticleBirds, lootLargeParticleBirds, lootLavaSpider, lootLavaBat,
        lootMiniBalrog, bloodyFlesh1, bloodyFlesh2, bloodyFlesh3, bossMadCrabSonar, bossMadCrabMaterial,
        masterGem, lootBalrog
    ];

    public static readonly CItem_PluginData[] itemsPluginData = new CItem_PluginData[items.Length];

    public static void Init() {
        autoBuilderMK1.recipes = ItemRecipes.MK_I;
        autoBuilderMK2.recipes = ItemRecipes.MK_II;
        autoBuilderMK3.recipes = ItemRecipes.MK_III;
        autoBuilderMK4.recipes = ItemRecipes.MK_IV;
        autoBuilderMK5.recipes = ItemRecipes.MK_V;
        autoBuilderUltimate.recipes = ItemRecipes.ULTIMATE;

        for (int i = 1; i < items.Length; ++i) {
            items[i]!.m_id = (ushort)i;
        }

        for (int i = 0; i < items.Length; ++i) {
            itemsPluginData[i] = default;
            if (items[i] is not CItemCell item) { continue; }
            ref var itemData = ref itemsPluginData[i];

            itemData.m_weight = (item as CItem_Wall)?.m_weight ?? 0f;
            itemData.m_electricValue = item.m_electricValue;
            itemData.m_electricOutletFlags = item.m_electricityOutletFlags;
            itemData.m_elecSwitchType = item == GItems.elecCross ? 1 : (item == GItems.elecSwitchRelay ? 2 : (item == GItems.elecSwitch ? 3 : (item == GItems.elecSwitchPush ? 4 : 0)));
            itemData.m_elecVariablePower = item.m_electricVariablePower ? 1 : 0;
            itemData.m_anchor = (int)item.m_anchor;
            itemData.m_light = item.m_light;
            itemData.m_isBlock = item.IsBlock() ? 1 : 0;
            itemData.m_isBlockDoor = item.IsBlockDoor() ? 1 : 0;
            itemData.m_isReceivingForces = item.IsReceivingForces() ? 1 : 0;
            itemData.m_isMineral = item is CItem_Mineral ? 1 : 0;
            itemData.m_isDirt = item is CItem_MineralDirt ? 1 : 0;
            itemData.m_isPlant = item is CItem_Plant ? 1 : 0;
            itemData.m_isFireProof = item.m_fireProof || ((item as CItem_Plant)?.m_conditions.m_isFireProof ?? false) ? 1 : 0;
            itemData.m_isWaterGenerator = item == GItems.generatorWater ? 1 : 0;
            itemData.m_isWaterPump = item == GItems.waterPump ? 1 : 0;
            itemData.m_isLightGenerator = item == GItems.generatorSun ? 1 : 0;
            itemData.m_isBasalt = item == GItems.lava ? 1 : 0;
            itemData.m_isLightonium = item == GItems.lightonium ? 1 : 0;
            itemData.m_isOrganicHeart = item == GItems.organicRockHeart ? 1 : 0;
            itemData.m_isSunLamp = item == GItems.lightSun ? 1 : 0;
            itemData.m_isAutobuilder = item is CItem_MachineAutoBuilder ? 1 : 0;
            itemData.m_customValue = (item as CItem_Machine)?.m_customValue ?? 0f;
        }
    }
    public static bool IsValidItem(int itemId) {
        return itemId >= 0 || itemId < items.Length;
    }
    public static bool TryGetItemOrNull(int itemId, out CItem? item) {
        if (itemId < 0 || itemId >= items.Length) {
            item = default;
            return false;
        }
        item = items[itemId];
        return true;
    }
    public static bool TryGetItem(int itemId, out CItem item) {
        if (itemId <= 0 || itemId >= items.Length) {
            item = default!;
            return false;
        }
        item = items[itemId]!;
        return true;
    }
    public static bool TryGetItemOrNull<T>(int itemId, out T? item) where T : CItem {
        item = null;
        if (itemId < 0 || itemId >= items.Length) {
            return false;
        }
        if (itemId == 0) {
            return true;
        }
        if (items[itemId] is not T resItem) {
            return false;
        }
        item = resItem;
        return true;
    }
    public static bool TryGetItem<T>(int itemId, out T item) where T : CItem {
        item = default!;
        if (itemId < 0 || itemId >= items.Length) {
            return false;
        }
        if (items[itemId] is not T resItem) {
            return false;
        }
        item = resItem;
        return true;
    }
}

public static class ItemRecipes {
    public static readonly CRecipe[] MK_I = [
        new(GItems.potionHp, 1, GItems.flowerBlue, 3, GItems.dogHorn, 1),
        new(GItems.gunRifle, 1, GItems.iron, 2, GItems.waterLight, 2),
        new(GItems.gunShotgun, 1, GItems.iron, 5, GItems.waterLight, 5),
        new(GItems.platform, 3, GItems.wood, 1),
        new(GItems.wallWood, 2, GItems.wood, 1),
        new(GItems.turret360, 1, GItems.iron, 5, GItems.lightGem, 1),
        new(GItems.autoBuilderMK1, 1, GItems.iron, 10, GItems.lightGem, 3),
        new(GItems.autoBuilderMK2, 1, GItems.autoBuilderMK1, 1, GItems.lightGem, 3, isUpgrade: true),
        new(GItems.iron, 1, GItems.metalScrap, 2)
    ];

    public static readonly CRecipe[] MK_II = [
        ..MK_I,
        new(GItems.miniaturizorMK2, 1, GItems.miniaturizorMK1, 1, GItems.lightGem, 2, isUpgrade: true),
        new(GItems.potionHpRegen, 1, GItems.flowerBlue, 3, GItems.bloodyFlesh1, 7),
        new(GItems.potionPheromones, 1, GItems.tree, 3, GItems.bloodyFlesh1, 7),
        new(GItems.armorMk1, 1, GItems.iron, 5, GItems.moleShell, 3),
        new(GItems.flashLight, 1, GItems.iron, 3, GItems.waterLight, 4),
        new(GItems.minimapper, 1, GItems.iron, 3, GItems.bat2Sonar, 2, GItems.lightGem, 1),
        new(GItems.effeilGlasses, 1, GItems.iron, 3, GItems.bat2Sonar, 2, GItems.bloodyFlesh1, 10),
        new(GItems.gunMachineGun, 1, GItems.iron, 15, GItems.coal, 10, GItems.lightGem, 2),
        new(GItems.wallConcrete, 2, GItems.dirt, 1, GItems.rock, 1),
        new(GItems.wallIronSupport, 2, GItems.iron, 1),
        new(GItems.backwall, 3, GItems.dirt, 1, GItems.rock, 1),
        new(GItems.turretGatling, 1, GItems.iron, 8, GItems.lightGem, 1, GItems.bloodyFlesh1, 3),
        new(GItems.turretReparator, 1, GItems.iron, 8, GItems.waterCoral, 3, GItems.fish2Regen, 2),
        new(GItems.light, 2, GItems.wood, 1, GItems.waterLight, 1),
        new(GItems.autoBuilderMK3, 1, GItems.autoBuilderMK2, 1, GItems.fernRed, 5, GItems.bloodyFlesh1, 15, true)
    ];

    public static readonly CRecipe[] MK_III = [
        ..MK_II,
        new(GItems.miniaturizorMK3, 1, GItems.miniaturizorMK2, 1, GItems.energyGem, 3, GItems.waterCoral, 5, true),
        new(GItems.potionHpBig, 1, GItems.fernRed, 3, GItems.dogHorn3, 1),
        new(GItems.potionCritics, 1, GItems.treePine, 3, GItems.bloodyFlesh2, 7),
        new(GItems.armorMk2, 1, GItems.armorMk1, 1, GItems.iron, 10, GItems.moleShellBlack, 3, true),
        new(GItems.waterDetector, 1, GItems.aluminium, 3, GItems.waterBush, 10, GItems.bloodyFlesh2, 10),
        new(GItems.metalDetector, 1, GItems.iron, 5, GItems.gold, 2, GItems.bossMadCrabSonar, 1),
        new(GItems.drone, 1, GItems.aluminium, 5, GItems.gold, 2),
        new(GItems.gunLaser, 1, GItems.aluminium, 10, GItems.flowerWhite, 7, GItems.energyGem, 5),
        new(GItems.gunSnipe, 1, GItems.aluminium, 10, GItems.coal, 20, GItems.bloodyFlesh1, 10),
        new(GItems.gunRocket, 1, GItems.aluminium, 10, GItems.blackGrass, 10, GItems.bloodyFlesh1, 15),
        new(GItems.wallReinforced, 2, GItems.iron, 1, GItems.dirt, 1, GItems.rock, 1),
        new(GItems.wallDoor, 1, GItems.iron, 1, GItems.dirt, 1, GItems.rock, 1),
        new(GItems.platformSteel, 4, GItems.iron, 1, GItems.coal, 1),
        new(GItems.generatorWater, 1, GItems.iron, 5, GItems.copper, 1, GItems.gold, 1),
        new(GItems.waterPump, 1, GItems.iron, 5, GItems.copper, 1, GItems.gold, 1),
        new(GItems.turretHeavy, 1, GItems.aluminium, 8, GItems.coal, 7),
        new(GItems.turretReparatorMK2, 1, GItems.aluminium, 8, GItems.flowerWhite, 3, GItems.fish3Regen, 2),
        new(GItems.turretMine, 1, GItems.aluminium, 2, GItems.coal, 5, GItems.bloodyFlesh2, 10),
        new(GItems.turretSpikes, 1, GItems.aluminium, 3, GItems.bloodyFlesh2, 10),
        new(GItems.lightSticky, 3, GItems.iron, 1, GItems.waterLight, 1),
        new(GItems.electricWire, 10, GItems.copper, 1),
        new(GItems.generatorSun, 1, GItems.iron, 2, GItems.copper, 1, GItems.coal, 5),
        new(GItems.lightSun, 1, GItems.iron, 2, GItems.copper, 1, GItems.flowerWhite, 2),
        new(GItems.teleport, 1, GItems.iron, 15, GItems.copper, 1, GItems.gold, 3),
        new(GItems.elecSwitch, 2, GItems.iron, 1, GItems.copper, 1),
        new(GItems.autoBuilderMK4, 1, GItems.autoBuilderMK3, 1, GItems.rockGaz, 2, GItems.fish3Regen, 5, true),
        new(GItems.coal, 1, GItems.wood, 10)
    ];

    public static readonly CRecipe[] MK_IV = [
        ..MK_III,
        new(GItems.miniaturizorMK4, 1, GItems.miniaturizorMK3, 1, GItems.blackMushroom, 5, GItems.bloodyFlesh2, 10, true),
        new(GItems.flashLightMK2, 1, GItems.flashLight, 1, GItems.aluminium, 1, GItems.crystalLight, 5, true),
        new(GItems.potionArmor, 1, GItems.treeWater, 3, GItems.bloodyFlesh2, 7),
        new(GItems.potionInvisibility, 1, GItems.treeSky, 3, GItems.bloodyFlesh3, 7),
        new(GItems.waterBreather, 1, GItems.aluminium, 2, GItems.sharkSkin, 2, GItems.bloodyFlesh2, 10),
        new(GItems.jetpack, 1, GItems.aluminium, 5, GItems.masterGem, 1),
        new(GItems.armorMk3, 1, GItems.armorMk2, 1, GItems.iron, 10, GItems.antShell, 3, true),
        new(GItems.invisibilityDevice, 1, GItems.aluminium, 5, GItems.uranium, 1, GItems.bloodyFlesh2, 20),
        new(GItems.droneCombat, 1, GItems.drone, 1, GItems.aluminium, 5, GItems.uranium, 3),
        new(GItems.gunMegaSnipe, 1, GItems.aluminium, 10, GItems.coal, 30, GItems.bossMadCrabMaterial, 1),
        new(GItems.gunZF0, 1, GItems.aluminium, 10, GItems.bat3Sonar, 4, GItems.crystalBlack, 2),
        new(GItems.gunLaserGatling, 1, GItems.aluminium, 10, GItems.uranium, 1, GItems.darkGem, 5),
        new(GItems.gunStorm, 1, GItems.aluminium, 10, GItems.skyBush, 5, GItems.bloodyFlesh2, 10),
        new(GItems.gunGrenadeLaunch, 1, GItems.aluminium, 10, GItems.unstableGemResidue, 4),
        new(GItems.wallComposite, 3, GItems.granit, 3, GItems.aluminium, 1, GItems.coal, 1),
        new(GItems.wallCompositeDoor, 1, GItems.granit, 3, GItems.aluminium, 1, GItems.coal, 1),
        new(GItems.wallCompositeSupport, 4, GItems.crystal, 2, GItems.aluminium, 1, GItems.coal, 1),
        new(GItems.wallCompositeLight, 3, GItems.crystal, 1, GItems.rockFlying, 2, GItems.coal, 1),
        new(GItems.turretCeiling, 1, GItems.aluminium, 5, GItems.skyBush, 5, GItems.bat3Sonar, 2),
        new(GItems.turretLaser, 1, GItems.aluminium, 5, GItems.crystalLight, 6, GItems.darkGem, 2),
        new(GItems.turretTesla, 1, GItems.aluminium, 3, GItems.gold, 5),
        new(GItems.elecSwitchPush, 2, GItems.iron, 1, GItems.copper, 1),
        new(GItems.elecSwitchRelay, 2, GItems.iron, 1, GItems.copper, 1),
        new(GItems.elecCross, 2, GItems.iron, 1, GItems.copper, 1),
        new(GItems.elecSignal, 2, GItems.iron, 1, GItems.copper, 1),
        new(GItems.elecClock, 2, GItems.iron, 1, GItems.copper, 1),
        new(GItems.elecToggle, 2, GItems.iron, 1, GItems.copper, 1),
        new(GItems.elecDelay, 2, GItems.iron, 1, GItems.copper, 1),
        new(GItems.elecWaterSensor, 2, GItems.iron, 1, GItems.copper, 1),
        new(GItems.elecProximitySensor, 2, GItems.iron, 1, GItems.copper, 1),
        new(GItems.elecDistanceSensor, 2, GItems.iron, 1, GItems.copper, 1),
        new(GItems.elecAND, 2, GItems.iron, 1, GItems.copper, 1),
        new(GItems.elecOR, 2, GItems.iron, 1, GItems.copper, 1),
        new(GItems.elecXOR, 2, GItems.iron, 1, GItems.copper, 1),
        new(GItems.elecNOT, 2, GItems.iron, 1, GItems.copper, 1),
        new(GItems.elecLight, 2, GItems.iron, 1, GItems.copper, 1, GItems.waterLight, 1),
        new(GItems.elecAlarm, 2, GItems.iron, 1, GItems.copper, 1, GItems.waterLight, 1),
        new(GItems.autoBuilderMK5, 1, GItems.autoBuilderMK4, 1, GItems.crystalBlack, 4, GItems.rockGaz, 30, true)
    ];

    public static readonly CRecipe[] MK_V = [
        ..MK_IV,
        new(GItems.miniaturizorMK5, 1, GItems.miniaturizorMK4, 1, GItems.crystalLight, 10, GItems.uranium, 5, true),
        new(GItems.potionHpMega, 1, GItems.lavaPlant, 1, GItems.lootMiniBalrog, 1),
        new(GItems.potionSpeed, 1, GItems.treeGranit, 3, GItems.bloodyFlesh3, 7),
        new(GItems.defenseShield, 1, GItems.titanium, 10, GItems.lootDwellerLord, 1, GItems.sapphire, 1),
        new(GItems.droneWar, 1, GItems.droneCombat, 1, GItems.titanium, 5, GItems.thorium, 5),
        new(GItems.gunParticlesShotgun, 1, GItems.titanium, 10, GItems.lootParticleGround, 5, GItems.bushGranit, 10),
        new(GItems.gunParticlesSniper, 1, GItems.titanium, 10, GItems.lightonium, 15, GItems.lootParticleBirds, 5),
        new(GItems.gunFlamethrower, 1, GItems.titanium, 10, GItems.sulfur, 20, GItems.lootLavaSpider, 5),
        new(GItems.turretFlame, 1, GItems.titanium, 5, GItems.lavaFlower, 5, GItems.lootLavaBat, 2),
        new(GItems.turretParticles, 1, GItems.titanium, 5, GItems.woodGranit, 10, GItems.sapphire, 1),
        new(GItems.explosive, 1, GItems.titanium, 5, GItems.sulfur, 30, GItems.unstableGemResidue, 6),
        new(GItems.reactor, 1, GItems.titanium, 10, GItems.thorium, 10, GItems.lootLargeParticleBirds, 7),
        new(GItems.rocketTop, 1, GItems.titanium, 30, GItems.diamonds, 1, GItems.lootBalrog, 1),
        new(GItems.rocketTank, 1, GItems.titanium, 10, GItems.rockGaz, 10, GItems.bloodyFlesh3, 10),
        new(GItems.rocketEngine, 1, GItems.titanium, 30, GItems.organicRockHeart, 3, GItems.sapphire, 5),
        new(GItems.iron, 1, GItems.lava, 3, GItems.tree, 3, GItems.waterBush, 3),
        new(GItems.copper, 1, GItems.lava, 3, GItems.bush, 3, GItems.waterCoral, 3),
        new(GItems.gold, 1, GItems.lava, 5, GItems.bushGranit, 5, GItems.waterLight, 10),
        new(GItems.aluminium, 1, GItems.lava, 3, GItems.flowerBlue, 5, GItems.treeWater, 3),
        new(GItems.rockGaz, 1, GItems.lava, 5, GItems.treeSky, 5, GItems.flowerWhite, 5),
        new(GItems.uranium, 1, GItems.lava, 5, GItems.flowerWhite, 5, GItems.tree, 5),
        new(GItems.titanium, 1, GItems.lava, 3, GItems.blackGrass, 3, GItems.treePine, 3),
        new(GItems.thorium, 1, GItems.lava, 3, GItems.fernRed, 3, GItems.treeGranit, 3),
        new(GItems.sulfur, 1, GItems.lava, 2, GItems.blackMushroom, 2, GItems.lavaFlower, 2),
        new(GItems.sapphire, 1, GItems.lava, 20, GItems.lavaPlant, 10, GItems.treeGranit, 10),
        new(GItems.rock, 1, GItems.lava, 1, GItems.woodwater, 1),
        new(GItems.granit, 1, GItems.lava, 1, GItems.woodGranit, 1),
        new(GItems.rockFlying, 1, GItems.lava, 1, GItems.woodSky, 1),
        new(GItems.dirt, 1, GItems.deadPlant, 10)
    ];

    public static readonly CRecipe[] ULTIMATE = [
        ..MK_V,
        new(GItems.miniaturizorUltimate, 1),
        new(GItems.ultimateJetpack, 1),
        new(GItems.ultimateBrush, 1),
        new(GItems.armorUltimate, 1),
        new(GItems.ultimateRebreather, 1),
        new(GItems.gunUltimateGrenadeLauncher, 1),
        new(GItems.gunUltimateParticlesGatling, 1),
        new(GItems.wallUltimate, 1),
        new(GItems.autoBuilderUltimate, 1),
        new(GItems.ultimateWaterPistol, 1),
        new(GItems.ultimateLavaPistol, 1),
        new(GItems.ultimateSpongePistol, 1),
        new(GItems.ultimateTotoroGun, 1),
        new(GItems.ultimateMonstersGun, 1),
        new(GItems.metalScrap, 1)
    ];
}
