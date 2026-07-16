using DODModAPI;
using UnityEngine;

//public static readonly ModItem flashLightMK3 = new(codeName: "flashLightMK3",
//    name: "Flashlight MK3",
//    description: "The brightest handheld light ever engineered. Uses self-charging photon amplification to outshine even the void of deep caves.",
//    item: new CItem_Device(tile: new ModCTile(0, 0), tileIcon: new ModCTile(0, 0),
//        groupId: DeviceGroupIds.flashLight, type: CItem_Device.Type.Passive,
//        customValue: 10f // flashLightMK2.customValue = 7f
//    ),
//    recipe: new(groupId: "MK V", isUpgrade: true) {
//        in1 = GItems.flashLightMK2,
//        nb1 = 1,
//        in2 = GItems.titanium,
//        nb2 = 5,
//        in3 = GItems.masterGem,
//        nb3 = 1
//    }
//);

public static class CustomUnits {
    public static readonly ModUnit waterVaporizer = new("waterVaporizer", "Water Vaporizer",
        new ExtCUnitWaterVaporizer.CDesc(tier: -1, speed: 0, size: Vector2.zero, hpMax: 10, armor: 0)
    );
}

public static class CustomBullets {
    public static readonly ExtCBulletDesc meltdownSnipe = new(
        sprite: Textures.meltdownSnipe, radius: 0.7f, dispersionAngleRad: 0.1f,
        speedStart: 50f, speedEnd: 30f, light: 0xC0A57u
    ) {
        m_lavaQuantity = 40f,
        m_explosionRadius = 5f,
        m_hasSmoke = true,
        m_explosionSetFire = true,
        m_light = new Color24(240, 40, 40),
        explosionBasaltBgRadius = 4,
        emitLavaBurstParticles = false,
        shockWaveRange = 20f,
        shockWaveKnockback = 10f,
        shockWaveDamage = 15f,
    };

    public static readonly CBulletDesc zf0shotgunBullet = new(
        "particles/particles", "bullet",
        radius: 0.15f,
        dispersionAngleRad: 0.65f,
        speedStart: 35f, speedEnd: 25f,
        light: 13619151U
    ) {
        m_hasTrail = true,
        m_pierceArmor = true
    };

    public static readonly ExtCBulletDesc impactGrenadeBullet = new(
        sprite: Textures.particleImpactGrenade, radius: 0.5f, dispersionAngleRad: 0f,
        speedStart: 20f, speedEnd: 15f, light: 0x005E19
    ) {
        m_grenadeYSpeed = -40f,
        m_explosionRadius = 3f,
        m_explosionMaxBlockHp = 300,
    };

    public static readonly ExtCBulletDesc particleEnergyDiffuser = new(
        sprite: Textures.particleEnergyDiffuser, radius: 0.5f, dispersionAngleRad: 0f,
        speedStart: 15f, speedEnd: 10f, light: 0x05B7ED
    ) {
        explosionEnergyRadius = 6f,
        explosionEnergyDamage = 60f,
        m_explosionRadius = 1f,
    };
}

public static class CustomSurfaces {
    public static readonly ModSurface fertileDirt = new(
        surfaceTexture: Textures.fertileDirt_surfaceMaterial, surfaceSortingOrder: 30,
        surfaceTopTile: Textures.fertileDirt_surfaceTops, hasAltTop: true,
        surfaceGrass: GSurfaces.grass, surfaceGrassWet: GSurfaces.grassWet
    );
}

//public static class CustomRecipeGroups {
//    public static readonly ModRecipeGroup mk6 = new("MK VI", [
//        // GItems.autoBuilderMK1 //, GItems.autoBuilderMK2, GItems.autoBuilderMK3, GItems.autoBuilderMK4, GItems.autoBuilderMK5
//        (CItem_MachineAutoBuilder)CustomItems.autoBuilderMK6
//    ]);
//}

public static class CustomItems {
    public static readonly ModItem mixedSoil = new(codeName: "mixedSoil",
        name: "Mixed Soil",
        description: "TODO.",
        item: new CItem_Material(tile: Textures.mixedSoil, tileIcon: null),
        recipe: new(groupId: "MK IV") {
            in1 = new(GItems.dirt, 3),
            in2 = new(GItems.dirtRed, 3),
            in3 = new(GItems.silt, 3),
        }
    );

    public static readonly ModItem flashLightMK3 = new(
        codeName: "flashLightMK3",
        name: "Flashlight MK3",
        description: "The brightest handheld light ever engineered. Uses self-charging photon amplification to outshine even the void of deep caves.",
        item: new CItem_Device(tile: Textures.flashLightMK3_tile, tileIcon: null,
            groupId: DeviceGroupIds.flashLight, type: CItem_Device.Type.Passive,
            customValue: 10f // flashLightMK2.customValue = 7f
        ),
        recipe: new(groupId: "MK V", isUpgrade: true) {
            in1 = new(GItems.flashLightMK2, 1),
            in2 = new(GItems.titanium, 5),
            in3 = new(GItems.masterGem, 1),
        }
    );

    public static readonly ModItem waterVaporizer = new(codeName: "waterVaporizer",
        name: "Water Vaporizer",
        description: "Atmospheric dehydrator rapidly boils 5 water/sec into steam. Requires 5kW to maintain thermal induction coils.",
        item: new ExtCItem_WaterVaporizer(tile: Textures.waterVaporizer, tileIcon: null,
            hpMax: 10, mainColor: Textures.waterVaporizer.MainColor
        ) {
            evaporationRate = 5f,
            m_electricValue = -5
        },
        recipe: new(groupId: "MK III") {
            in1 = new(GItems.iron, 10),
            in2 = new(GItems.copper, 10),
            in3 = new(GItems.gold, 1),
        }
    );

    public static readonly ModItem quantumCondenser = new(codeName: "quantumCondenser",
        name: "Quantum Condenser",
        description: "TODO.",
        item: new CItem_Material(tile: Textures.quantumCondenser, tileIcon: null),
        recipe: new(groupId: "MK V") {
            in1 = new(GItems.lootBalrog, 1),
            in2 = new(GItems.diamonds, 1),
            in3 = new(GItems.titanium, 30),
        }
    );
    public static readonly ModItem negamassAlloy = new(codeName: "negamassAlloy",
        name: "Negamass Alloy",
        description: "TODO.",
        item: new CItem_Material(tile: Textures.negamassAlloy, tileIcon: null),
        recipe: new(groupId: "MK V") {
            in1 = new(GItems.rockFlying, 10),
            in2 = new(GItems.rockGaz, 3),
            in3 = new(GItems.iron, 5),
        }
    );
    public static readonly ModItem plasmaAmplifier = new(codeName: "plasmaAmplifier",
        name: "Plasma amplifier",
        description: "TODO.",
        item: new CItem_Material(tile: Textures.plasmaAmplifier, tileIcon: null),
        recipe: new(groupId: "MK V") {
            in1 = new(GItems.woodGranit, 5),
            in2 = new(GItems.sapphire, 1),
        }
    );
    public static readonly ModItem harvestCore = new(codeName: "harvestCore",
        name: "Harvest Core",
        description: "TODO.",
        item: new CItem_Material(tile: Textures.harvestCore, tileIcon: null),
        recipe: new(groupId: "MK III") {
            in1 = new(GItems.gold, 1),
            in2 = new(GItems.iron, 5),
            in3 = new(GItems.energyGem, 3),
        }
    );
    public static readonly ModItem entropyCore = new(codeName: "entropyCore",
        name: "Entropy Core",
        description: "TODO.",
        item: new CItem_Material(tile: Textures.entropyCore, tileIcon: null),
        recipe: new(groupId: "MK V") {
            in1 = new(GItems.lightonium, 10),
            in2 = new(GItems.titanium, 10),
            in3 = new(GItems.thorium, 5),
        }
    );

    public static readonly ModItem miniaturizorMK6 = new(codeName: "miniaturizorMK6",
        name: "Miniaturizor MK VI",
        description: "The final word in portable matter compression. Matter compression device utilizing quantum-locked deatomization fields. Warning: Do not use on black holes.",
        item: new CItem_Device(tile: Textures.miniaturizorMK6_tile, tileIcon: Textures.miniaturizorMK6_icon,
            groupId: DeviceGroupIds.miniaturizor, type: CItem_Device.Type.None,
            customValue: 1500f // miniaturizorMK5.customValue = 810f
        ) { m_pickupDuration = -1 },
        recipe: new(groupId: "MK V", isUpgrade: true) {
            in1 = new(GItems.miniaturizorMK5, 1),
            in2 = new(GItems.reactor, 1),
            in3 = new(CustomItems.quantumCondenser.item, 2),
        }
    );

    public static readonly ModItem betterPotionHpRegen = new(codeName: "betterPotionHpRegen",
        name: "Better Health Regeneration Potion",
        description: "Advanced bio-stimulant compound (400% HP restoration over 60s). Rebuilds cells faster than they can die.",
        item: new CItem_Device(tile: Textures.betterPotionHpRegen, tileIcon: null,
            groupId: DeviceGroupIds.potionHPRegen, type: CItem_Device.Type.Consumable, customValue: 3f
        ) { m_cooldown = 120f, m_duration = 60f },
        recipe: new(groupId: "MK IV") {
            in1 = new(GItems.bloodyFlesh2, 7),
            in2 = new(GItems.flowerBlue, 5),
            in3 = new(GItems.flowerWhite, 1),
        }
    );

    public static readonly ModItem defenseShieldMK2 = new(codeName: "defenseShieldMK2",
        name: "Defense Shield MK2",
        description: "Projected quantum barrier capable of absorbing kinetic impacts equal to user's maximum HP. Recharges in 2s.",
        item: new CItem_Device(tile: Textures.defenseShieldMK2, tileIcon: null,
            groupId: DeviceGroupIds.shield, type: CItem_Device.Type.Passive, customValue: 1f
        ),
        recipe: new(groupId: "MK V", isUpgrade: true) {
            in1 = new(GItems.defenseShield, 1),
            in2 = new(GItems.diamonds, 1),
            in3 = new(GItems.organicRockHeart, 1),
        }
    );

    public static readonly ModItem waterBreatherMK2 = new(codeName: "waterBreatherMK2",
        name: "Rebreather MK2",
        description: "High-yield electrolytic filtration system extracts breathable gases from liquid environments.",
        item: new CItem_Device(tile: Textures.waterBreatherMK2, tileIcon: null,
            groupId: DeviceGroupIds.waterBreather, type: CItem_Device.Type.Passive, customValue: 7f
        ),
        recipe: new(groupId: "MK V", isUpgrade: true) {
            in1 = new(GItems.waterBreather, 2),
            in2 = new(GItems.coal, 100),
            in3 = new(GItems.reactor, 1),
        }
    );

    public static readonly ModItem jetpackMK2 = new(codeName: "jetpackMK2",
        name: "Jetpack MK2",
        description: "Dual-thrust VTOL propulsion system.",
        item: new ExtCItem_JetpackDevice(tile: Textures.jetpackMK2, tileIcon: null) {
            jetpackEnergyUsageMultiplier = 0.095f,
            jetpackFlyForce = 100f,
        },
        recipe: new(groupId: "MK V", isUpgrade: true) {
            in1 = new(GItems.jetpack, 2),
            in2 = new(GItems.reactor, 1),
            in3 = new(GItems.rockGaz, 20),
        }
    );

    public static readonly ModItem fertileDirt = new(codeName: "fertileDirt",
        name: "Fertile Dirt",
        description: "Bio-engineered soil infused with growth accelerants. Increases plant growth rate by 30% and supports all common dirt-based flora.",
        item: new ExtCItem_FertileMineralDirt(tile: null, tileIcon: Textures.fertileDirt_icon,
            hpMax: 30, mainColor: Textures.fertileDirt_icon.MainColor,
            surface: CustomSurfaces.fertileDirt,
            grassConditions: new CLifeConditions(
                altMin: 280, altMax: 1024, lightMin: 95, lightMax: 255, waterAboveMin: 0f, waterAboveMax: 0.2f, waterInMineralMin: 0.01f, waterInMineralMax: 9f
            )
        ) {
            plantGrowChange = 0.45f, // default: 0.15
            inheritedPlantsSupported = [GItems.dirt, GItems.dirtRed, GItems.silt, GItems.dirtBlack, GItems.dirtSky],
        },
        recipe: new(groupId: "MK IV") {
            in1 = new(CustomItems.mixedSoil.item, 1),
            in2 = new(GItems.dirtBlack, 3),
            in3 = new(GItems.dirtSky, 3),
        }
    );

    public static readonly ModItem gunMeltdown = new(codeName: "gunMeltdown",
        name: "Gun \"Meltdown\"",
        description: "Fires a condensed bolt of pure thermodynamic chaos, forcing targets into rapid atomic decay. The recoil has been known to send users sliding backwards through time (approx. 0.3 nanoseconds).",
        item: new CItem_Weapon(tile: Textures.gunPlasmaMegaSnipe_tile, tileIcon: Textures.gunPlasmaMegaSnipe_icon,
            heatingPerShot: 2f, isAuto: false,
            attackDesc: new CAttackDesc(
                range: 50f,
                damage: 1500,
                nbAttacks: 1,
                cooldown: 3f,
                knockbackOwn: 60f,
                knockbackTarget: 100f,
                projDesc: CustomBullets.meltdownSnipe,
                sound: GameAssets.SoundID.plasmaSnipe
            )
        ),
        recipe: new(groupId: "MK V") {
            in1 = new(CustomItems.entropyCore.item, 5),
            in2 = new(GItems.reactor, 3),
            in3 = new(GItems.lootParticleBirds, 50),
        }
    );

    public static readonly ModItem antiGravityWall = new(codeName: "antiGravityWall",
        name: "Anti-Gravity Wall",
        description: "Defies conventional physics by emitting a repulsive wave of synthesized negative mass. Installation requires chrono-stabilized anchoring.",
        item: new CItem_Wall(tile: Textures.antiGravityWall, tileIcon: null,
            hpMax: 100, mainColor: Textures.antiGravityWall.MainColor,
            forceResist: int.MaxValue - 10000, weight: 1000f, type: CItem_Wall.Type.WallBlock
        ),
        recipe: new(groupId: "MK V") {
            in1 = new(CustomItems.negamassAlloy.item, 2),
            in2 = new(GItems.wallConcrete, 1),
            in3 = new(GItems.aluminium, 3),
        }
    );

    public static readonly ModItem turretReparatorMK3 = new(codeName: "turretReparatorMK3",
        name: "Auto-Repair Turret MK3",
        description: "Deploys nano-assembler drones with 7.5m operational radius. Repair rate: 10 HP/s. Consumes 2kW.",
        item: new CItem_Defense(tile: Textures.turretReparatorMK3_tile, tileIcon: Textures.turretReparatorMK3_icon,
            hpMax: 200, mainColor: Textures.turretReparatorMK3_icon.MainColor,
            rangeDetection: 8.5f,
            angleMin: -9999f, angleMax: 9999f,
            attack: new CAttackDesc(
                range: 7.5f,
                damage: -10,
                nbAttacks: 0,
                cooldown: 0.5f,
                knockbackOwn: 0f, knockbackTarget: 0f,
                projDesc: null, sound: null
            ),
            tileUnit: Textures.turretReparatorMK3_unit
        ) {
            m_displayRangeOnCells = true,
            m_electricValue = -2,
            m_light = new Color24(10329710U),
            m_neverUnspawn = true
        },
        recipe: new(groupId: "MK IV") {
            in1 = new(GItems.aluminium, 8),
            in2 = new(GItems.uranium, 2),
            in3 = new(GItems.fish3Regen, 3),
        }
    );

    public static readonly ModItem megaExplosive = new(codeName: "megaExplosive",
        name: "Mega Explosive",
        description: "Thermonuclear demolition charge (yield: 3000 damage, 10m blast radius).",
        item: new ExtCItem_Explosive(tile: Textures.megaExplosive, tileIcon: null,
            hpMax: 250, mainColor: Textures.megaExplosive.MainColor,
            rangeDetection: 0f, angleMin: 0f, angleMax: 360f,
            attack: new CAttackDesc(
                range: 10f,
                damage: 3000,
                nbAttacks: 0,
                cooldown: -1f,
                knockbackOwn: 0f,
                knockbackTarget: 10f,
                projDesc: null,
                sound: GameAssets.SoundID.rocketExplosion
            ),
            tileUnit: null
        ) {
            m_isActivable = true,
            m_neverUnspawn = true,
            explosionTime = 6f,
            explosionSoundMultiplier = 5f,
            destroyBackgroundRadius = 2,
            explosionBasaltBgRadius = 5,
            explosionFlashIntensity = 1f,
            explosionFireAroundRadius = 35f,
            m_light = new Color24(10, 240, 71),
        },
        recipe: new(groupId: "MK V") {
            in1 = new(GItems.explosive, 3),
            in2 = new(GItems.uranium, 10),
            in3 = new(GItems.lootLavaSpider, 10),
        }
    );

    public static readonly ModItem turretParticlesMK2 = new(codeName: "turretParticlesMK2",
        name: "Particle Turret MK2",
        description: "Magnetized plasma accelerator turret which fires superheated particle bolts.",
        item: new CItem_Defense(tile: new ModTile(0, 0, "items_defenses"), tileIcon: Textures.turretParticlesMK2_icon,
            hpMax: 350, mainColor: Textures.turretParticlesMK2_icon.MainColor, rangeDetection: 10f,
            angleMin: -9999f, angleMax: 9999f,
            attack: new CAttackDesc(
                range: 12f,
                damage: 50,
                nbAttacks: 1,
                cooldown: 0.5f,
                knockbackOwn: 0f, knockbackTarget: 3f,
                projDesc: new ExtCBulletDesc(
                    sprite: Textures.particlesSnipTurretMK2,
                    radius: 0.45f, dispersionAngleRad: 0f,
                    speedStart: 40f, speedEnd: 30f, light: 0xE10AF5
                ),
                sound: GameAssets.SoundID.particleTurret
            ),
            tileUnit: Textures.turretParticlesMK2_unit
        ) {
            m_anchor = CItemCell.Anchor.Everyside_Small
        },
        recipe: new(groupId: "MK V") {
            in1 = new(GItems.titanium, 10),
            in2 = new(GItems.thorium, 10),
            in3 = new(CustomItems.plasmaAmplifier.item, 3),
        }
    );

    public static readonly ModItem turretTeslaMK2 = new(codeName: "turretTeslaMK2",
        name: "Tesla Turret MK2",
        description: "Summons artificial lightning from ionized atmosphere, chaining between targets with fractal precision. Consumes 5kW.",
        item: new CItem_Defense(tile: Textures.turretTeslaMK2, tileIcon: null,
            hpMax: 350, mainColor: Textures.turretTeslaMK2.MainColor,
            rangeDetection: 12.5f,
            angleMin: -9999f, angleMax: 9999f,
            attack: new CAttackDesc(
                range: 12f,
                damage: 200,
                nbAttacks: 1,
                cooldown: 2f,
                knockbackOwn: 0f, knockbackTarget: 10f,
                projDesc: null,
                sound: GameAssets.SoundID.storm
            ),
            tileUnit: null
        ) {
            m_electricValue = -5,
            m_light = new Color24(16, 133, 235)
        },
        recipe: new(groupId: "MK V") {
            in1 = new(GItems.titanium, 5),
            in2 = new(GItems.sapphire, 1),
            in3 = new(GItems.gold, 7),
        }
    );

    public static readonly ModItem collector = new(codeName: "collector",
        name: "Collector",
        description: "Automated botanical harvesting unit. Deploys precision cutting beams, compatible with all known flora.",
        item: new ExtCItem_Collector(tile: Textures.collector_tile, tileIcon: Textures.collector_icon,
            hpMax: 100, mainColor: Textures.collector_icon.MainColor,
            rangeDetection: 5f,
            angleMin: -9999f, angleMax: 9999f,
            attack: new CAttackDesc(
                range: 5.5f,
                damage: 0,
                nbAttacks: 0,
                cooldown: 0.5f,
                knockbackOwn: 0f, knockbackTarget: 0f,
                projDesc: null, sound: null
            ),
            tileUnit: Textures.collector_unit
        ) {
            m_anchor = CItemCell.Anchor.Everyside_Small,
            m_displayRangeOnCells = true,
            m_neverUnspawn = true,
            collectorDamage = 10,
            m_electricValue = -2
        },
        recipe: new(groupId: "MK III") {
            in1 = new(CustomItems.harvestCore.item, 1),
            in2 = new(GItems.turretReparator, 1),
            in3 = new(GItems.lightGem, 5),
        }
    );

    public static readonly ModItem blueLightSticky = new(codeName: "blueLightSticky",
        name: "Blue Wall Light",
        description: "You can attach this lamp to any surface and it will glow BLUE!",
        item: new CItem_Machine(tile: Textures.blueLightSticky, tileIcon: null,
            hpMax: 100, mainColor: Textures.blueLightSticky.MainColor,
            anchor: CItemCell.Anchor.Everywhere_Small
        ) {
            m_light = new Color24(20, 20, 220)
        },
        recipe: new(groupId: "MK III") {
            in1 = new(GItems.iron, 1),
            in2 = new(GItems.waterLight, 1),
            in3 = new(GItems.flowerBlue, 1),
        }
    );

    public static readonly ModItem redLightSticky = new(codeName: "redLightSticky",
        name: "Red Wall Light",
        description: "You can attach this lamp to any surface and it will glow RED!",
        item: new CItem_Machine(tile: Textures.redLightSticky, tileIcon: null,
            hpMax: 100, mainColor: Textures.redLightSticky.MainColor,
            anchor: CItemCell.Anchor.Everywhere_Small
        ) {
            m_light = new Color24(220, 20, 20)
        },
        recipe: new(groupId: "MK III") {
            in1 = new(GItems.iron, 1),
            in2 = new(GItems.waterLight, 1),
            in3 = new(GItems.fernRed, 1),
        }
    );

    public static readonly ModItem greenLightSticky = new(codeName: "greenLightSticky",
        name: "Green Wall Light",
        description: "You can attach this lamp to any surface and it will glow GREEN!",
        item: new CItem_Machine(tile: Textures.greenLightSticky, tileIcon: null,
            hpMax: 100, mainColor: Textures.greenLightSticky.MainColor,
            anchor: CItemCell.Anchor.Everywhere_Small
        ) {
            m_light = new Color24(20, 220, 20)
        },
        recipe: new(groupId: "MK III") {
            in1 = new(GItems.iron, 1),
            in2 = new(GItems.waterLight, 1),
            in3 = new(GItems.woodSky, 1),
        }
    );

    public static readonly ModItem basaltCollector = new(codeName: "basaltCollector",
        name: "Basalt Collector",
        description: "Industrial-grade mineral extraction unit optimized for volcanic rock.",
        item: new ExtCItem_Collector(tile: Textures.basaltCollector_tile, tileIcon: Textures.basaltCollector_icon,
            hpMax: 100, mainColor: Textures.basaltCollector_icon.MainColor,
            rangeDetection: 5f,
            angleMin: -9999f, angleMax: 9999f,
            attack: new CAttackDesc(
                range: 5.5f,
                damage: 0,
                nbAttacks: 0,
                cooldown: 0.5f,
                knockbackOwn: 0f, knockbackTarget: 0f,
                projDesc: null, sound: null
            ),
            tileUnit: Textures.basaltCollector_unit
        ) {
            m_anchor = CItemCell.Anchor.Everyside_Small,
            m_displayRangeOnCells = true,
            m_neverUnspawn = true,
            collectorDamage = 100,
            isBasaltCollector = true,
            m_electricValue = -5
        },
        recipe: new(groupId: "MK V") {
            in1 = new(CustomItems.harvestCore.item, 1),
            in2 = new(GItems.turretReparatorMK2, 1),
            in3 = new(GItems.darkGem, 10),
        }
    );

    public static readonly ModItem turretLaser360 = new(codeName: "turretLaser360",
        name: "Rotating Laser Turret",
        description: "360-degree photon emitter. Penetrates organic matter completely.",
        item: new CItem_Defense(tile: new ModTile(0, 0, "items_defenses"), tileIcon: Textures.turretLaser360_icon,
            hpMax: 250, mainColor: Textures.turretLaser360_icon.MainColor, rangeDetection: 10f,
            angleMin: -9999f, angleMax: 9999f,
            attack: new CAttackDesc(
                range: 10f,
                damage: 20,
                nbAttacks: 1,
                cooldown: 0.3f,
                knockbackOwn: 0f, knockbackTarget: 0f,
                projDesc: GBullets.laser, sound: GameAssets.SoundID.laser
            ),
            tileUnit: new ModTile(2, 2, "items_defenses")
        ),
        recipe: new(groupId: "MK V") {
            in1 = new(GItems.titanium, 10),
            in2 = new(GItems.crystalBlack, 5),
            in3 = new(GItems.darkGem, 5),
        }
    );

    public static readonly ModItem volcanicExplosive = new(codeName: "volcanicExplosive",
        name: "Volcanic Explosive",
        description: "Tectonic induction device. Upon detonation, generates a localized subduction zone and summons an artificial magma plume. Could potentially trigger an eruption of nearby volcanoes.",
        item: new ExtCItem_Explosive(tile: Textures.volcanicExplosive, tileIcon: null,
            hpMax: 500, mainColor: Textures.volcanicExplosive.MainColor,
            rangeDetection: 0f, angleMin: 0f, angleMax: 360f,
            attack: new CAttackDesc(
                range: 25f,
                damage: 2000,
                nbAttacks: 0,
                cooldown: -1f,
                knockbackOwn: 0f,
                knockbackTarget: 500f,
                projDesc: null,
                sound: GameAssets.SoundID.rocketExplosion
            ),
            tileUnit: null
        ) {
            m_isActivable = true,
            m_neverUnspawn = true,
            explosionTime = 10f,
            explosionSoundMultiplier = 25f,
            alwaysStartEruption = true,
            destroyBackgroundRadius = 3,
            explosionBasaltBgRadius = 18,
            lavaQuantity = ExtCItem_Explosive.CalculateLavaQuantityStep(totalQuantity: 1500f, time: 5f),
            lavaReleaseTime = 5f,
            indestructible = true,
            timerColor = Color.red * 0.3f,
            m_light = new Color24(240, 38, 38),
            m_fireProof = true,
            shockWaveDamage = 30f,
            shockWaveKnockback = 30f,
            shockWaveRange = 50f,
            explosionFlashIntensity = 1.6f,
        },
        recipe: new(groupId: "MK V") {
            in1 = new(CustomItems.entropyCore.item, 2),
            in2 = new(CustomItems.megaExplosive.item, 1),
            in3 = new(GItems.lavaOld, 100),
        }
    );

    public static readonly ModItem titanferrumAlloy = new(codeName: "titanferrumAlloy",
        name: "Titanferrum Alloy",
        description: "TODO.",
        item: new CItem_Material(tile: Textures.titanferrumAlloy, tileIcon: null),
        recipe: new(groupId: "MK V") {
            in1 = new(GItems.iron, 10),
            in2 = new(GItems.aluminium, 7),
            in3 = new(GItems.titanium, 5),
        }
    );

    public static readonly ModItem wallCompositeReinforced = new(codeName: "wallCompositeReinforced",
        name: "Composite Reinforced Wall",
        description: "Ultra-dense construction material. Layered graphene-ceramic alloy with shock dispersion matrix.",
        item: new CItem_Wall(tile: Textures.wallCompositeReinforced, tileIcon: null,
            hpMax: 700, mainColor: Textures.wallCompositeReinforced.MainColor,
            forceResist: 11000, weight: 560f,
            type: CItem_Wall.Type.WallBlock
        ),
        recipe: new(groupId: "MK V") {
            in1 = new(CustomItems.titanferrumAlloy.item, 1),
            in2 = new(GItems.coal, 2),
            in3 = new(GItems.lavaOld, 1),
        }
    );

    public static readonly ModItem gunNukeLauncher = new(codeName: "gunNukeLauncher",
        name: "Mini-Nuke Launcher",
        description: "Compact nuclear delivery system (1000 damage, 15m radius). Fires stabilized micro-fusion warheads. Backblast not included.",
        item: new CItem_Weapon(tile: Textures.gunNukeLauncher_tile, tileIcon: null,
            heatingPerShot: 0f, isAuto: false,
            attackDesc: new CAttackDesc(
                range: 100f,
                damage: 1000,
                nbAttacks: 1,
                cooldown: 0f,
                knockbackOwn: 100f,
                knockbackTarget: 200f,
                projDesc: new ExtCBulletDesc(
                    sprite: ModSprite.Vanilla("particles/particles", "grenade"),
                    radius: 0.5f,
                    dispersionAngleRad: 0f,
                    speedStart: 20f,
                    speedEnd: 15f,
                    light: 0x005E19
                ) {
                    m_grenadeYSpeed = -15f,
                    m_explosionRadius = 15f,
                    m_lavaQuantity = 1f,
                    emitLavaBurstParticles = false,
                },
                sound: GameAssets.SoundID.rocketFire
            )
        ),
        recipe: new(groupId: "ULTIMATE")
    );

    public static readonly ModItem generatorSunMK2 = new(codeName: "generatorSunMK2",
        name: "Solar Panel MK2",
        description: "High-efficiency photovoltaic array (3kW output). Self-cleaning surface maintains 98% light absorption in all conditions.",
        item: new CItem_Machine(tile: Textures.generatorSunMK2, tileIcon: null,
            hpMax: 200, mainColor: Textures.generatorSunMK2.MainColor,
            anchor: CItemCell.Anchor.Bottom_Small
        ) {
            m_electricValue = 3
        },
        recipe: new(groupId: "MK IV") {
            in1 = new(GItems.aluminium, 3),
            in2 = new(GItems.copper, 5),
            in3 = new(GItems.gold, 1),
            nbOut = 2,
        }
    );

    public static readonly ModItem RTG = new(codeName: "RTG",
        name: "Radioisotope Thermoelectric Generator",
        description: "Radioactive decay-powered generator (15kW output). Utilizes plutonium-238 core with 87-year half-life. Shielded housing prevents contamination.",
        item: new CItem_Machine(tile: Textures.RTG, tileIcon: null,
            hpMax: 200, mainColor: Textures.RTG.MainColor,
            anchor: CItemCell.Anchor.Bottom_Small
        ) {
            m_light = new Color24(0xED0CE9),
            m_electricValue = 15
        },
        recipe: new(groupId: "MK V") {
            in1 = new(CustomItems.entropyCore.item, 3),
            in2 = new(GItems.lootLargeParticleBirds, 15),
            in3 = new(GItems.masterGem, 1),
        }
    );

    public static readonly ModItem indestructibleLavaOld = new(codeName: "indestructibleLavaOld",
        name: "Indestructible Ancient Basalt",
        description: "Metastable mineral formation. No known force can compromise structural integrity.",
        item: new ExtCItem_IndestructibleMineral(tile: null, tileIcon: new ModTile(3, 5, "items_minerals"),
            hpMax: 1000, mainColor: 6118492U, surface: GSurfaces.lavaOld, isReplacable: false
        )
    );

    public static readonly ModItem gunRocketGatling = new(codeName: "gunRocketGatling",
        name: "Rocket Launcher Gatling",
        description: "Rotary micro-missile array. Gatling version of standard rocket launcher fires 40-damage projectiles.",
        item: new CItem_Weapon(tile: Textures.gunRocketGatling_tile, tileIcon: Textures.gunRocketGatling_icon,
            heatingPerShot: 0.1f, /* gunRocket: 0.5f */ isAuto: true,
            attackDesc: new CAttackDesc(
                range: 20f /*25f*/, damage: 40 /*50*/, nbAttacks: 1, cooldown: 0.15f /*0.3*/,
                knockbackOwn: 3f, /*5f*/
                knockbackTarget: 25f, /*30f*/
                projDesc: GBullets.rocket,
                sound: GameAssets.SoundID.rocketFire
            )
        ),
        recipe: new(groupId: "MK IV", isUpgrade: true) {
            in1 = new(GItems.gunRocket, 3),
            in2 = new(GItems.gold, 5),
            in3 = new(GItems.darkGem, 10),
        }
    );

    public static readonly ModItem gunRailgun = new(codeName: "gunRailgun",
        name: "Railgun",
        description: "Electromagnetic projectile accelerator. Requires capacitor cooling between discharges.",
        item: new CItem_Weapon(tile: new ModTile(3, 0, "items_weapons"), tileIcon: new ModTile(3, 3, "items_icons"),
            heatingPerShot: 1f, isAuto: false,
            attackDesc: new CAttackDesc(
                range: 200f, damage: 100, nbAttacks: 1, cooldown: 1f,
                knockbackOwn: 60f,
                knockbackTarget: 2000f,
                projDesc: new CBulletDesc(
                    "particles/particles", "plasmaBig",
                    radius: 0.5f, dispersionAngleRad: 0f,
                    speedStart: 5000f, speedEnd: 4000f,
                    light: 11358926U
                ),
                sound: GameAssets.SoundID.plasmaSnipe
            )
        ),
        recipe: new(groupId: "ULTIMATE")
    );

    public static readonly ModItem gunBeamLaser = new(codeName: "gunBeamLaser",
        name: "Laser Beam Gun",
        description: "Continuous-wave photon emitter. Improved over standard laser guns with infinite penetration capability.",
        item: new CItem_Weapon(tile: new ModTile(0, 1, "items_weapons"), tileIcon: new ModTile(4, 3, "items_icons"),
            heatingPerShot: 0f, isAuto: true,
            attackDesc: new CAttackDesc(
                range: 10f, damage: 1, nbAttacks: 1, cooldown: 0f,
                knockbackOwn: 0f, knockbackTarget: 0f,
                projDesc: new CBulletDesc(
                    "particles/particles", "laser",
                    radius: 0.36f, dispersionAngleRad: 0f,
                    speedStart: 90f, speedEnd: 225f,
                    light: 16733782U
                ) {
                    m_goThroughEnnemies = true,
                    m_criticsRate = 0f
                }
            )
        ),
        recipe: new(groupId: "ULTIMATE")
    );

    public static readonly ModItem gunZF0Shotgun = new(codeName: "gunZF0Shotgun",
        name: "ZF-0 Shotgun",
        description: "Multi-barrel flechette disperser. Upgraded ZF-0 model fires 10 armor-piercing rounds per trigger pull.",
        item: new CItem_Weapon(tile: Textures.gunZF0Shotgun_tile, tileIcon: Textures.gunZF0Shotgun_icon,
            heatingPerShot: 0.4f /*0.01f*/, isAuto: false /*true*/,
            attackDesc: new CAttackDesc(
                range: 20f /*25f*/, damage: 8 /*7*/, nbAttacks: 10 /*1*/, cooldown: 0.25f /*0.08f*/,
                knockbackOwn: 11f /*0f*/, knockbackTarget: 2f /*2f*/,
                projDesc: CustomBullets.zf0shotgunBullet,
                sound: GameAssets.SoundID.shotgun
            )
        ),
        recipe: new(groupId: "MK V") {
            in1 = new(GItems.gunZF0, 5),
            in2 = new(GItems.titanium, 10),
            in3 = new(GItems.lootParticleGround, 5),
        }
    );

    public static readonly ModItem portableTeleport = new(codeName: "portableTeleport",
        name: "Portable Teleporter",
        description: "Personal quantum translocation device utilizing folded-space technology, which is a compact version of a standard teleporter. Safety protocols prevent matter reintegration errors",
        item: new CItem_Device(tile: Textures.portableTeleport, tileIcon: null,
            groupId: null, type: CItem_Device.Type.Activable
        ),
        recipe: new(groupId: "MK V") {
            in1 = new(GItems.teleport, 1),
            in2 = new(GItems.reactor, 1),
            in3 = new(GItems.diamonds, 1),
        }
    );

    public static readonly ModItem autoBuilderMK6 = new(codeName: "autoBuilderMK6",
        name: "Auto-Builder MK VI",
        description: "TODO.",
        item: new ExtCItem_ConditionalMachineAutoBuilder(tile: Textures.autoBuilderMK6, tileIcon: null) {
            m_light = new Color24(220, 20, 220),
            m_customValue = 6f,
            m_electricValue = -10,
            checkCondition = (int x, int y) => {
                return SWorld.Grid[x, y].GetBgSurface() == GSurfaces.bgOrganic;
            }
        },
        recipe: new(groupId: "MK V")
    );

    public static readonly ModItem gunImpactGrenade = new(codeName: "gunImpactGrenade",
        name: "Impact granade",
        description: "High-velocity demolition charge detonating on contact (45 damage, 25m throw range)",
        item: new ExtCItem_ConsumableWeapon(tile: Textures.gunImpactGrenade_tile, tileIcon: Textures.gunImpactGrenade_icon,
            heatingPerShot: 0f, isAuto: false,
            attackDesc: new CAttackDesc(
                range: 25f,
                damage: 45,
                nbAttacks: 1,
                cooldown: 0.5f,
                knockbackOwn: 10f,
                knockbackTarget: 45f,
                projDesc: CustomBullets.impactGrenadeBullet
            )
        ),
        recipe: new(groupId: "MK II") {
            in1 = new(GItems.iron, 2),
            in2 = new(GItems.coal, 3),
            in3 = new(GItems.light, 1),
        }
    );

    public static readonly ModItem impactShieldMk1 = new(codeName: "impactShieldMk1",
        name: "Impact Shield MK1",
        description: "Kinetic dampener reduces fall and collision damage by 25%. Automatically activates upon high-velocity impacts with terrain or structures.",
        item: new ExtCItem_ImpactShield(tile: Textures.impactShieldMk1,
            customValue: 0.25f
        ),
        recipe: new(groupId: "MK III") {
            in1 = new(GItems.energyGem, 5),
            in2 = new(GItems.aluminium, 10),
            in3 = new(GItems.gold, 5),
        }
    );
    public static readonly ModItem impactShieldMk2 = new(codeName: "impactShieldMk2",
        name: "Impact Shield MK2",
        description: "Enhanced stabilizer absorbs 50% of fall/collision damage. Reinforced field emitter prevents overload from repeated hard landings.",
        item: new ExtCItem_ImpactShield(tile: Textures.impactShieldMk2,
            customValue: 0.5f
        ),
        recipe: new(groupId: "MK IV", isUpgrade: true) {
            in1 = new(CustomItems.impactShieldMk1.item, 1),
            in2 = new(GItems.bossMadCrabMaterial, 1),
            in3 = new(GItems.uranium, 3),
        }
    );

    public static readonly ModItem turretCeilingMK2 = new(codeName: "turretCeilingMK2",
        name: "Death Pulse Turret MK2",
        description: "Overhead-mounted sonic emitter fires twin destabilization waves (60 damage x2, 4m range). Effective against clustered enemies with its 120° firing arc.",
        item: new ExtCItem_CeilingTurret(tile: Textures.turretCeilingMK2,
            hpMax: 300, mainColor: Textures.turretCeilingMK2.MainColor,
            rangeDetection: 3.8f, angleMin: -120f, angleMax: -60f,
            attack: new CAttackDesc(
                range: 4f, damage: 60, nbAttacks: 2, cooldown: 1f, knockbackOwn: 0f, knockbackTarget: 0f,
                projDesc: null, sound: GameAssets.SoundID.ceilingTurret
            )
        ) {
            m_colRect = new Rect(0.1f, 0.6f, 0.8f, 0.4f)
        },
        recipe: new(groupId: "MK V") {
            in1 = new(GItems.titanium, 5),
            in2 = new(GItems.lootParticleBirds, 4),
            in3 = new(GItems.thorium, 1),
        }
    );
    public static readonly ModItem turretSpikesMK2 = new(codeName: "turretSpikesMK2",
        name: "Electrified Spikes MK2",
        description: "Supercharged deterrent grid delivers 30 damage per spike with enhanced conductivity. Consumes 3kW to maintain lethal charge.",
        item: new ExtCItem_SpikesTurret(tile: Textures.turretSpikesMK2,
            hpMax: 400, mainColor: Textures.turretSpikesMK2.MainColor,
            rangeDetection: 1.5f, angleMin: 0f, angleMax: 180f,
            attack: new CAttackDesc(
                range: 1.5f, damage: 30, nbAttacks: 2, cooldown: 0.5f, knockbackOwn: 0f, knockbackTarget: 0f,
                projDesc: null, sound: GameAssets.SoundID.stormLight
            )
        ) {
            m_colRect = new Rect(0.1f, 0f, 0.8f, 0.35f),
            m_electricValue = -3,
            m_light = new Color24(9724047U)
        },
        recipe: new(groupId: "MK IV") {
            in1 = new(GItems.uranium, 1),
            in2 = new(GItems.aluminium, 5),
            in3 = new(GItems.gold, 2),
        }
    );
    public static readonly ModItem gunEnergyDiffuser = new(codeName: "gunEnergyDiffuser",
        name: "MB-X Plasma Diffuser",
        description: "Scatters superheated plasma bolts in a widening arc (20 damage, 15m range). Each discharge briefly ionizes the air, creating residual static fields.",
        item: new CItem_Weapon(tile: Textures.gunEnergyDiffuser_tile, tileIcon: Textures.gunEnergyDiffuser_icon,
            heatingPerShot: 0.4f, isAuto: true,
            attackDesc: new CAttackDesc(
                range: 15f, damage: 20, nbAttacks: 1,
                cooldown: 1f, knockbackOwn: 5f, knockbackTarget: 10f,
                projDesc: CustomBullets.particleEnergyDiffuser, sound: GameAssets.SoundID.storm
            )
        ),
        recipe: new(groupId: "MK V") {
            in1 = new(CustomItems.entropyCore.item, 1),
            in2 = new(GItems.gold, 20),
            in3 = new(GItems.lootLargeParticleBirds, 15),
        }
    );
    public static readonly ModItem waterVaporizerMK2 = new(codeName: "waterVaporizerMK2",
        name: "Water Vaporizer MK2",
        description: "Industrial-grade dehydrator processes 8 water/sec with improved heat recycling. Drains 10kW from power grids during operation.",
        item: new ExtCItem_WaterVaporizer(tile: Textures.waterVaporizerMK2, tileIcon: null,
            hpMax: 20, mainColor: Textures.waterVaporizerMK2.MainColor
        ) {
            evaporationRate = 8f,
            m_electricValue = -10
        },
        recipe: new(groupId: "MK IV") {
            in1 = new(GItems.aluminium, 15),
            in2 = new(GItems.copper, 30),
            in3 = new(GItems.crystalBlack, 10),
        }
    );
    public static readonly ModItem advancedMetalDetector = new(codeName: "advancedMetalDetector",
        name: "Advanced Metal Detector",
        description: "Multi-spectral scanner identifies rare ores, also detects common metals (40-120m). Hold {Input_Shift} to reduce detection range.",
        item: new ExtCItem_MetalDetector(tile: Textures.advancedMetalDetector, range: 120f) {
            m_cooldown = 3f,
            detectableItems = [GItems.iron, GItems.copper, GItems.gold, GItems.aluminium, GItems.uranium, GItems.titanium, GItems.thorium, GItems.sulfur, GItems.sapphire, GItems.diamonds]
        },
        recipe: new(groupId: "MK IV", isUpgrade: true) {
            in1 = new(GItems.metalDetector, 1),
            in2 = new(GItems.bossMadCrabSonar, 1),
            in3 = new(GItems.bat3Sonar, 10),
        }
    );
    public static readonly ModItem turretMegaSnipe = new(codeName: "turretMegaSnipe",
        name: "Overcharged Plasma Turret",
        description: "TODO.",
        item: new CItem_Defense(tile: Textures.turretMegaSnipe_tile, tileIcon: Textures.turretMegaSnipe_icon,
            hpMax: 400, mainColor: Textures.turretMegaSnipe_icon.MainColor, rangeDetection: 16f,
            angleMin: -1f, angleMax: 1f,
            attack: new CAttackDesc(
                range: 15f, damage: 200,
                nbAttacks: 1, cooldown: 2f, knockbackOwn: 100f, knockbackTarget: 50f,
                projDesc: GBullets.megasnipe, sound: GameAssets.SoundID.plasmaSnipe
            ),
            tileUnit: Textures.turretMegaSnipe_unit
        ) {
            m_isReversable = true
        },
        recipe: new(groupId: "MK V") {
            in1 = new(GItems.titanium, 20),
            in2 = new(GItems.bossMadCrabMaterial, 1),
            in3 = new(GItems.lootBalrog, 1),
        }
    );
    public static readonly ModItem turretDuo360 = new(codeName: "turretDuo360",
        name: "Duo Rotating Turret",
        description: "TODO.",
        item: new CItem_Defense(tile: new ModTile(0, 0, "items_defenses"), tileIcon: Textures.turretDuo360_icon,
            hpMax: 75, mainColor: Textures.turretDuo360_icon.MainColor,
            rangeDetection: 10f, angleMin: -9999f, angleMax: 9999f,
            attack: new ExtDuoCAttackDesc(
                range: 8f, damage: 8, nbAttacks: 1, cooldown: 0.3f, knockbackOwn: 0f, knockbackTarget: 1f,
                projDesc: GBullets.defenses, sound: GameAssets.SoundID.defensePlasma
            ),
            tileUnit: Textures.turretDuo360_unit
        ) {
            m_anchor = CItemCell.Anchor.Everywhere_Small
        },
        recipe: new(groupId: "MK II") {
            in1 = new(GItems.iron, 9),
            in2 = new(GItems.lightGem, 2),
            in3 = new(GItems.coal, 5),
        }
    );

    // public static CustomItem gunPlasmaThrower = new CustomItem(name: "gunPlasmaThrower",
    //     item: new CItem_Weapon(tile: new CustomCTile(35, 0), tileIcon: new CustomCTile(36, 0),
    //         heatingPerShot: 0f, isAuto: true,
    //         attackDesc: new CAttackDesc(
    //             range: 16f,
    //             damage: 20,
    //             nbAttacks: 1,
    //             cooldown: 0.1f,
    //             knockbackOwn: 0f,
    //             knockbackTarget: 1f,
    //             projDesc: new CustomCBulletDesc(
    //                 CustomCTile.texturePath, "particlePlasmaCloud",
    //                 radius: 0.5f, dispersionAngleRad: 0.1f,
    //                 speedStart: 25f, speedEnd: 15f, light: 0x770BDB
    //             ) {
    //                 m_goThroughEnnemies = true,
    //                 m_pierceArmor = true,
    //                 m_inflame = true,
    //             },
    //             sound: null
    //         )
    //     )
    // );
}

