using UnityEngine;

public static class CustomBullets {
    public static CustomCBulletDesc meltdownSnipe = new(
        CustomCTile.texturePath, "meltdownSnipe",
        radius: 0.7f, dispersionAngleRad: 0.1f,
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

    public static CBulletDesc zf0shotgunBullet = new(
        "particles/particles", "bullet",
        radius: 0.15f,
        dispersionAngleRad: 0.65f,
        speedStart: 35f, speedEnd: 25f,
        light: 13619151U
    ) {
        m_hasTrail = true,
        m_pierceArmor = true
    };
}

public static class CItemDeviceGroupIds {
    public static readonly string miniaturizor = "Miniaturizor";
    public static readonly string potionHP = "PotionHP";
    public static readonly string potionHPRegen = "PotionHPRegen";
    public static readonly string potionArmor = "PotionArmor";
    public static readonly string potionPheromones = "PotionPheromones";
    public static readonly string potionCritics = "PotionCritics";
    public static readonly string potionInvisibility = "PotionInvisibility";
    public static readonly string potionSpeed = "PotionSpeed";
    public static readonly string armor = "Armor";
    public static readonly string shield = "Shield";
    public static readonly string drone = "Drone";
    public static readonly string flashLight = "FlashLight";
    public static readonly string minimapper = "Minimapper";
    public static readonly string effeilGlasses = "EffeilGlasses";
    public static readonly string metalDetector = "MetalDetector";
    public static readonly string waterDetector = "WaterDetector";
    public static readonly string waterBreather = "WaterBreather";
    public static readonly string jetpack = "Jetpack";
    public static readonly string invisibility = "Invisibility";
    public static readonly string brush = "Brush";
}

public static class CustomItems {

    public static CustomItem flashLightMK3 = new(name: "flashLightMK3",
        item: new CItem_Device(tile: new CustomCTile(0, 0), tileIcon: new CustomCTile(0, 0),
            groupId: CItemDeviceGroupIds.flashLight, type: CItem_Device.Type.Passive, customValue: 10f
        )
    );
    public static CustomItem miniaturizorMK6 = new(name: "miniaturizorMK6",
        item: new CItem_Device(tile: new CustomCTile(2, 0), tileIcon: new CustomCTile(1, 0),
            groupId: CItemDeviceGroupIds.miniaturizor, type: CItem_Device.Type.None, customValue: 999f
        // Above 999 the miniaturizor would break Ancient Basalt (oldLava)
        ) { m_pickupDuration = -1 }
    );
    public static CustomItem betterPotionHpRegen = new(name: "betterPotionHpRegen",
        item: new CItem_Device(tile: new CustomCTile(3, 0), tileIcon: new CustomCTile(3, 0),
            // "potionHpRegen" has 1.5f customValue
            groupId: CItemDeviceGroupIds.potionHPRegen, type: CItem_Device.Type.Consumable, customValue: 3f
        ) { m_cooldown = 120f, m_duration = 60f }
    );
    public static CustomItem defenseShieldMK2 = new(name: "defenseShieldMK2",
        item: new CItem_Device(tile: new CustomCTile(4, 0), tileIcon: new CustomCTile(4, 0),
            // "defenseShield" has 0.5f customValue
            groupId: CItemDeviceGroupIds.shield, type: CItem_Device.Type.Passive, customValue: 1f
        )
    );
    public static CustomItem waterBreatherMK2 = new(name: "waterBreatherMK2",
        item: new CItem_Device(tile: new CustomCTile(5, 0), tileIcon: new CustomCTile(5, 0),
            // "waterBreather" has 3f customValue
            groupId: CItemDeviceGroupIds.waterBreather, type: CItem_Device.Type.Passive, customValue: 7f
        )
    );
    public static CustomItem jetpackMK2 = new(name: "jetpackMK2",
        item: new CItem_Device(tile: new CustomCTile(6, 0), tileIcon: new CustomCTile(6, 0),
            groupId: CItemDeviceGroupIds.jetpack, type: CItem_Device.Type.Passive, customValue: 1f
        )
    );
    public static CustomItem antiGravityWall = new(name: "antiGravityWall",
        item: new CItem_Wall(tile: new CustomCTile(7, 0), tileIcon: new CustomCTile(7, 0),
            hpMax: 100, mainColor: 12173251U, forceResist: int.MaxValue - 10000, weight: 1000f, type: CItem_Wall.Type.WallBlock
        )
    );
    public static CustomItem turretReparatorMK3 = new(name: "turretReparatorMK3",
        item: new CItem_Defense(tile: new CustomCTile(10, 0), tileIcon: new CustomCTile(9, 0),
            hpMax: 200, mainColor: 8947848U, rangeDetection: 8.5f,
            angleMin: -9999f, angleMax: 9999f,
            attack: new CAttackDesc(
                range: 7.5f,
                damage: -10,
                nbAttacks: 0,
                cooldown: 0.5f,
                knockbackOwn: 0f, knockbackTarget: 0f,
                projDesc: null, sound: null
            ),
            tileUnit: new CustomCTile(8, 0)
        ) {
            m_displayRangeOnCells = true,
            m_electricValue = -2,
            m_light = new Color24(10329710U),
            m_neverUnspawn = true
        }
    );
    public static CustomItem megaExplosive = new(name: "megaExplosive",
        item: new CItem_Explosive(tile: new CustomCTile(11, 0), tileIcon: new CustomCTile(11, 0),
            hpMax: 250, mainColor: 8947848U, rangeDetection: 0f, angleMin: 0f, angleMax: 360f,
            attack: new CAttackDesc(
                range: 10f,
                damage: 3000,
                nbAttacks: 0,
                cooldown: -1f,
                knockbackOwn: 0f,
                knockbackTarget: 10f,
                projDesc: null,
                sound: "rocketExplosion"
            ),
            tileUnit: null
        ) {
            m_isActivable = true,
            m_neverUnspawn = true,
            explosionTime = 6f,
            explosionSoundMultiplier = 5f,
            destroyBackgroundRadius = 2,
            explosionBasaltBgRadius = 5,
            m_light = new Color24(10, 240, 71)
        }
    );
    public static CustomItem turretParticlesMK2 = new(name: "turretParticlesMK2",
        item: new CItem_Defense(tile: new CTile(0, 0) { m_textureName = "items_defenses" }, tileIcon: new CustomCTile(12, 0),
            hpMax: 350, mainColor: 8947848U, rangeDetection: 10f,
            angleMin: -9999f, angleMax: 9999f,
            attack: new CAttackDesc(
                range: 12f,
                damage: 50,
                nbAttacks: 1,
                cooldown: 0.5f,
                knockbackOwn: 0f, knockbackTarget: 3f,
                projDesc: new CBulletDesc(
                    CustomCTile.texturePath, "particlesSnipTurretMK2",
                    radius: 0.45f, dispersionAngleRad: 0f,
                    speedStart: 40f, speedEnd: 30f, light: 0xE10AF5
                ),
                sound: "particleTurret"
            ),
            tileUnit: new CustomCTile(13, 0)
        ) {
            m_anchor = CItemCell.Anchor.Everyside_Small
        }
    );
    public static CustomItem turretTeslaMK2 = new(name: "turretTeslaMK2",
        item: new CItem_Defense(tile: new CustomCTile(14, 0), tileIcon: new CustomCTile(14, 0),
            hpMax: 350, mainColor: 8947848U, rangeDetection: 12.5f,
            angleMin: -9999f, angleMax: 9999f,
            attack: new CAttackDesc(
                range: 12f,
                damage: 200,
                nbAttacks: 1,
                cooldown: 2f,
                knockbackOwn: 0f, knockbackTarget: 10f,
                projDesc: null,
                sound: "storm"
            ),
            tileUnit: null
        ) {
            m_electricValue = -5,
            m_light = new Color24(16, 133, 235)
        }
    );
    public static CustomItem collector = new(name: "collector",
        item: new CItem_Collector(tile: new CustomCTile(15, 0), tileIcon: new CustomCTile(16, 0),
            hpMax: 100, mainColor: 8947848U, rangeDetection: 5f,
            angleMin: -9999f, angleMax: 9999f,
            attack: new CAttackDesc(
                range: 5.5f,
                damage: 0,
                nbAttacks: 0,
                cooldown: 0.5f,
                knockbackOwn: 0f, knockbackTarget: 0f,
                projDesc: null, sound: null
            ),
            tileUnit: new CustomCTile(17, 0)
        ) {
            m_anchor = CItemCell.Anchor.Everyside_Small,
            m_displayRangeOnCells = true,
            m_neverUnspawn = true,
            collectorDamage = 10,
            m_electricValue = -2
        }
    );
    public static CustomItem blueLightSticky = new(name: "blueLightSticky",
        item: new CItem_Machine(tile: new CustomCTile(18, 0), tileIcon: new CustomCTile(18, 0),
            hpMax: 100, mainColor: 10066329U, anchor: CItemCell.Anchor.Everywhere_Small
        ) {
            m_light = new Color24(20, 20, 220)
        }
    );
    public static CustomItem redLightSticky = new(name: "redLightSticky",
        item: new CItem_Machine(tile: new CustomCTile(20, 0), tileIcon: new CustomCTile(20, 0),
            hpMax: 100, mainColor: 10066329U, anchor: CItemCell.Anchor.Everywhere_Small
        ) {
            m_light = new Color24(220, 20, 20)
        }
    );
    public static CustomItem greenLightSticky = new(name: "greenLightSticky",
        item: new CItem_Machine(tile: new CustomCTile(22, 0), tileIcon: new CustomCTile(22, 0),
            hpMax: 100, mainColor: 10066329U, anchor: CItemCell.Anchor.Everywhere_Small
        ) {
            m_light = new Color24(20, 220, 20)
        }
    );
    public static CustomItem basaltCollector = new CustomItem(name: "basaltCollector",
        item: new CItem_Collector(tile: new CustomCTile(15, 0), tileIcon: new CustomCTile(24, 0),
            hpMax: 100, mainColor: 8947848U, rangeDetection: 5f,
            angleMin: -9999f, angleMax: 9999f,
            attack: new CAttackDesc(
                range: 5.5f,
                damage: 0,
                nbAttacks: 0,
                cooldown: 0.5f,
                knockbackOwn: 0f, knockbackTarget: 0f,
                projDesc: null, sound: null
            ),
            tileUnit: new CustomCTile(25, 0)
        ) {
            m_anchor = CItemCell.Anchor.Everyside_Small,
            m_displayRangeOnCells = true,
            m_neverUnspawn = true,
            collectorDamage = 100,
            isBasaltCollector = true,
            m_electricValue = -5
        }
    );
    public static CustomItem turretLaser360 = new(name: "turretLaser360",
        item: new CItem_Defense(tile: new CTile(0, 0) { m_textureName = "items_defenses" }, tileIcon: new CustomCTile(26, 0),
            hpMax: 250, mainColor: 8947848U, rangeDetection: 10f,
            angleMin: -9999f, angleMax: 9999f,
            attack: new CAttackDesc(
                range: 10f,
                damage: 20,
                nbAttacks: 1,
                cooldown: 0.3f,
                knockbackOwn: 0f, knockbackTarget: 0f,
                projDesc: GBullets.laser, sound: "laser"
            ),
            tileUnit: new CTile(2, 2) { m_textureName = "items_defenses" }
        )
    );
    public static CustomItem gunMeltdown = new(name: "gunMeltdown",
        item: new CItem_Weapon(tile: new CustomCTile(27, 0), tileIcon: new CustomCTile(28, 0),
            heatingPerShot: 2f, isAuto: false,
            attackDesc: new CAttackDesc(
                range: 50f,
                damage: 1500,
                nbAttacks: 1,
                cooldown: 3f,
                knockbackOwn: 60f,
                knockbackTarget: 100f,
                projDesc: CustomBullets.meltdownSnipe,
                sound: "plasmaSnipe"
            )
        )
    );
    public static CustomItem volcanicExplosive = new(name: "volcanicExplosive",
        item: new CItem_Explosive(tile: new CustomCTile(29, 0), tileIcon: new CustomCTile(29, 0),
            hpMax: 500, mainColor: 8947848U, rangeDetection: 0f, angleMin: 0f, angleMax: 360f,
            attack: new CAttackDesc(
                range: 25f,
                damage: 5000,
                nbAttacks: 0,
                cooldown: -1f,
                knockbackOwn: 0f,
                knockbackTarget: 500f,
                projDesc: null,
                sound: "rocketExplosion"
            ),
            tileUnit: null
        ) {
            m_isActivable = true,
            m_neverUnspawn = true,
            explosionTime = 10f,
            explosionSoundMultiplier = 30f,
            alwaysStartEruption = true,
            destroyBackgroundRadius = 3,
            explosionBasaltBgRadius = 18,
            lavaQuantity = CItem_Explosive.CalculateLavaQuantityStep(totalQuantity: 1500f, time: 5f),
            lavaReleaseTime = 5f,
            indestructible = true,
            timerColor = Color.red * 0.3f,
            m_light = new Color24(240, 38, 38),
            m_fireProof = true,
            shockWaveDamage = 30f,
            shockWaveKnockback = 30f,
            shockWaveRange = 50f,
        }
    );
    public static CustomItem wallCompositeReinforced = new(name: "wallCompositeReinforced",
        item: new CItem_Wall(tile: new CustomCTile(30, 0), tileIcon: new CustomCTile(30, 0),
            hpMax: 700, mainColor: 12039872U, forceResist: 11000, weight: 560f,
            type: CItem_Wall.Type.WallBlock
        )
    );
    public static CustomItem gunNukeLauncher = new(name: "gunNukeLauncher",
        item: new CItem_Weapon(tile: new CustomCTile(31, 0), tileIcon: new CustomCTile(32, 0),
            heatingPerShot: 0f, isAuto: false,
            attackDesc: new CAttackDesc(
                range: 100f,
                damage: 1000,
                nbAttacks: 1,
                cooldown: 0f,
                knockbackOwn: 100f,
                knockbackTarget: 200f,
                projDesc: new CustomCBulletDesc(
                    "particles/particles", "grenade",
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
                sound: "rocketFire"
            )
        )
    );
    public static CustomItem generatorSunMK2 = new(name: "generatorSunMK2",
        item: new CItem_Machine(tile: new CustomCTile(33, 0), tileIcon: new CustomCTile(33, 0),
            hpMax: 200, mainColor: 10066329U,
            anchor: CItemCell.Anchor.Bottom_Small
        ) {
            m_electricValue = 3
        }
    );
    public static CustomItem RTG = new(name: "RTG",
        item: new CItem_Machine(tile: new CustomCTile(34, 0), tileIcon: new CustomCTile(34, 0),
            hpMax: 200, mainColor: 10066329U,
            anchor: CItemCell.Anchor.Bottom_Small
        ) {
            m_light = new Color24(0xED0CE9),
            m_electricValue = 15
        }
    );
    public static CustomItem indestructibleLavaOld = new(name: "indestructibleLavaOld",
        item: new CItem_IndestructibleMineral(tile: null, tileIcon: new CTile(3, 5),
            hpMax: 1000, mainColor: 6118492U, surface: GSurfaces.lavaOld, isReplacable: false
        )
    );
    public static CustomItem gunRocketGatling = new(name: "gunRocketGatling",
        item: new CItem_Weapon(tile: new CTile(2, 1) { m_textureName = "items_weapons" }, tileIcon: new CTile(6, 3) { m_textureName = "items_icons" },
            heatingPerShot: 0.1f, isAuto: true,
            attackDesc: new CAttackDesc(
                range: 20f, damage: 40, nbAttacks: 1, cooldown: 0.15f,
                knockbackOwn: 3f,
                knockbackTarget: 25f,
                projDesc: GBullets.rocket,
                sound: "rocketFire"
            )
        )
    );
    public static CustomItem gunRailgun = new(name: "gunRailgun",
        item: new CItem_Weapon(tile: new CTile(3, 0) { m_textureName = "items_weapons" }, tileIcon: new CTile(3, 3) { m_textureName = "items_icons" },
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
                sound: "plasmaSnipe"
            )
        )
    );
    public static CustomItem gunBeamLaser = new(name: "gunBeamLaser",
        item: new CItem_Weapon(tile: new CTile(0, 1) { m_textureName = "items_weapons" }, tileIcon: new CTile(4, 3) { m_textureName = "items_icons" },
            heatingPerShot: 0f, isAuto: true,
            attackDesc: new CAttackDesc(
                range: 10f, damage: 1, nbAttacks: 1, cooldown: 0f,
                knockbackOwn: 0f, knockbackTarget: 0f,
                projDesc: new CBulletDesc(
                    "particles/particles", "laser",
                    radius: 0.15f, dispersionAngleRad: 0f,
                    speedStart: 250f, speedEnd: 250f,
                    light: 16733782U
                ) {
                    m_goThroughEnnemies = true,
                    m_criticsRate = 0f,
                }
            )
        )
    );
    public static CustomItem gunZF0Shotgun = new(name: "gunZF0Shotgun",
        item: new CItem_Weapon(tile: new CTile(3, 1) { m_textureName = "items_weapons" }, tileIcon: new CTile(7, 3) { m_textureName = "items_icons" },
            heatingPerShot: 0.4f, isAuto: false,
            attackDesc: new CAttackDesc(
                range: 20f, damage: 8, nbAttacks: 10, cooldown: 0.25f,
                knockbackOwn: 11f, knockbackTarget: 2f,
                projDesc: CustomBullets.zf0shotgunBullet,
                sound: "shotgun"
            )
        )
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

