using System.Collections.Generic;
using UnityEngine;

public sealed class ModCTile : CTile {
    public static readonly string texturePath = "mod-more-items";
    public static Texture2D texture = null;

    public ModCTile(int i, int j, int images = 1, int sizeX = 128, int sizeY = 128)
        : base(i, j, images, sizeX, sizeY) {
        base.m_textureName = texturePath;
    }
}

public sealed class ExtCItem_Collector : CItem_Defense {
    public ExtCItem_Collector(CTile tile, CTile tileIcon, ushort hpMax, uint mainColor, float rangeDetection, float angleMin, float angleMax, CAttackDesc attack, CTile tileUnit)
        : base(tile, tileIcon, hpMax, mainColor, rangeDetection, angleMin, angleMax, attack, tileUnit) {
        m_attack.m_damage = 0;
    }

    public ushort collectorDamage = 0;
    public bool isBasaltCollector = false;
}
public sealed class ExtCItem_Explosive : CItem_Defense {
    public ExtCItem_Explosive(CTile tile, CTile tileIcon, ushort hpMax, uint mainColor, float rangeDetection, float angleMin, float angleMax, CAttackDesc attack, CTile tileUnit)
        : base(tile, tileIcon, hpMax, mainColor, rangeDetection, angleMin, angleMax, attack, tileUnit) { }

    public const float deltaTime = 0.1f;

    public static float CalculateLavaQuantityStep(float totalQuantity, float time) {
        var t = Mathf.Pow(3, deltaTime);
        return totalQuantity * (1 - t) / (1 - Mathf.Pow(t, time / deltaTime + 1));
    }

    public float explosionTime = 5f;
    public float explosionSoundMultiplier = 1f;
    public bool alwaysStartEruption = false;
    public int destroyBackgroundRadius = 0;
    public int explosionBasaltBgRadius = 0;
    public float lavaQuantity = 0;
    public float lavaReleaseTime = -1f;
    public bool indestructible = false;
    public Color timerColor = Color.red;
    public float shockWaveRange = 0f;
    public float shockWaveKnockback = 0f;
    public float shockWaveDamage = 0f;

    public static Dictionary<ushort, float> lastTimeMap = new Dictionary<ushort, float>();
}
public sealed class ExtCBulletDesc : CBulletDesc {
    public ExtCBulletDesc(string spriteTextureName, string spriteName, float radius, float dispersionAngleRad, float speedStart, float speedEnd, uint light = 0)
        : base(spriteTextureName, spriteName, radius, dispersionAngleRad, speedStart, speedEnd, light) { }

    public int explosionBasaltBgRadius = 0;
    public bool emitLavaBurstParticles = true;
    public float shockWaveRange = 0f;
    public float shockWaveKnockback = 0f;
    public float shockWaveDamage = 0f;
}
public sealed class ExtCItem_IndestructibleMineral : CItem_Mineral {
    public ExtCItem_IndestructibleMineral(CTile tile, CTile tileIcon, ushort hpMax, uint mainColor, CSurface surface, bool isReplacable = false)
        : base(tile, tileIcon, hpMax, mainColor, surface, isReplacable) { }
}

public static class CustomBullets {
    public static ExtCBulletDesc meltdownSnipe = new(
        ModCTile.texturePath, "meltdownSnipe",
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
    public static readonly ModItem flashLightMK3 = new(codeName: "flashLightMK3",
        name: "Flashlight MK3", description: "Even more light!",
        item: new CItem_Device(tile: new ModCTile(0, 0), tileIcon: new ModCTile(0, 0),
            groupId: CItemDeviceGroupIds.flashLight, type: CItem_Device.Type.Passive, customValue: 10f
        ),
        recipe: new(groupId: "MK V", isUpgrade: true) {
            in1 = GItems.flashLightMK2, nb1 = 1,
            in2 = GItems.titanium, nb2 = 5,
            in3 = GItems.masterGem, nb3 = 1
        }
    );
    
    public static readonly ModItem miniaturizorMK6 = new(codeName: "miniaturizorMK6",
        name: "Miniaturizor MK VI", description: "Absolutely the best Miniaturizor ever invented.",
        item: new CItem_Device(tile: new ModCTile(2, 0), tileIcon: new ModCTile(1, 0),
            groupId: CItemDeviceGroupIds.miniaturizor, type: CItem_Device.Type.None, customValue: 1500f
        ) { m_pickupDuration = -1 },
        recipe: new(groupId: "MK V", isUpgrade: true) {
            in1 = GItems.miniaturizorMK5, nb1 = 1,
            in2 = GItems.reactor, nb2 = 1,
            in3 = GItems.lootBalrog, nb3 = 1
        }
    );
    
    public static readonly ModItem betterPotionHpRegen = new(codeName: "betterPotionHpRegen",
        name: "Better Health Regeneration Potion", 
        description: "The high radioactivity mixed with a multiple specials chemical ingredients helps the tissues to regenerate. Heals 400% of your HP over 60s.",
        item: new CItem_Device(tile: new ModCTile(3, 0), tileIcon: new ModCTile(3, 0),
            groupId: CItemDeviceGroupIds.potionHPRegen, type: CItem_Device.Type.Consumable, customValue: 3f
        ) { m_cooldown = 120f, m_duration = 60f },
        recipe: new(groupId: "MK III") {
            in1 = GItems.bloodyFlesh2, nb1 = 5
        }
    );
    
    public static readonly ModItem defenseShieldMK2 = new(codeName: "defenseShieldMK2",
        name: "Defense Shield MK2", 
        description: "Creates a strong magnetic field around you that can absorb your maximum health points from projectiles. Refreshes in 2s.",
        item: new CItem_Device(tile: new ModCTile(4, 0), tileIcon: new ModCTile(4, 0),
            groupId: CItemDeviceGroupIds.shield, type: CItem_Device.Type.Passive, customValue: 1f
        ),
        recipe: new(groupId: "MK V", isUpgrade: true) {
            in1 = GItems.defenseShield, nb1 = 1,
            in2 = GItems.diamonds, nb2 = 1
        }
    );
    
    public static readonly ModItem waterBreatherMK2 = new(codeName: "waterBreatherMK2",
        name: "Rebreather MK2", 
        description: "This small device can get a large amount of oxygen from the water.",
        item: new CItem_Device(tile: new ModCTile(5, 0), tileIcon: new ModCTile(5, 0),
            groupId: CItemDeviceGroupIds.waterBreather, type: CItem_Device.Type.Passive, customValue: 7f
        ),
        recipe: new(groupId: "MK V", isUpgrade: true) {
            in1 = GItems.waterBreather, nb1 = 2,
            in2 = GItems.coal, nb2 = 100,
            in3 = GItems.reactor, nb3 = 1
        }
    );
    
    public static readonly ModItem jetpackMK2 = new(codeName: "jetpackMK2",
        name: "Jetpack MK2", 
        description: "Portable double mini-rockets attached to a pack. Very heavy when walking or swimming. Doesn't work very well near volcanic Basalt (radiations and heat causing interferences).",
        item: new CItem_Device(tile: new ModCTile(6, 0), tileIcon: new ModCTile(6, 0),
            groupId: CItemDeviceGroupIds.jetpack, type: CItem_Device.Type.Passive, customValue: 1f
        ),
        recipe: new(groupId: "MK V") {
            in1 = GItems.aluminium, nb1 = 5,
            in2 = GItems.masterGem, nb2 = 1
        }
    );
    
    public static readonly ModItem antiGravityWall = new(codeName: "antiGravityWall",
        name: "Anti-Gravity Wall", description: "How???",
        item: new CItem_Wall(tile: new ModCTile(7, 0), tileIcon: new ModCTile(7, 0),
            hpMax: 100, mainColor: 12173251U, forceResist: int.MaxValue - 10000, weight: 1000f, type: CItem_Wall.Type.WallBlock
        ),
        recipe: new(groupId: "ULTIMATE")
    );
    
    public static readonly ModItem turretReparatorMK3 = new(codeName: "turretReparatorMK3",
        name: "Auto-Repair Turret MK3",
        description: "Quickly repairs nearby damaged walls, turrets and machines. Consumes 2kW.",
        item: new CItem_Defense(tile: new ModCTile(10, 0), tileIcon: new ModCTile(9, 0),
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
            tileUnit: new ModCTile(8, 0)
        ) {
            m_displayRangeOnCells = true,
            m_electricValue = -2,
            m_light = new Color24(10329710U),
            m_neverUnspawn = true
        },
        recipe: new(groupId: "ULTIMATE")
    );
    
    public static readonly ModItem megaExplosive = new(codeName: "megaExplosive",
        name: "Mega Explosive", description: "Nuke.",
        item: new ExtCItem_Explosive(tile: new ModCTile(11, 0), tileIcon: new ModCTile(11, 0),
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
        },
        recipe: new(groupId: "ULTIMATE")
    );
    
    public static readonly ModItem turretParticlesMK2 = new(codeName: "turretParticlesMK2",
        name: "Particle Turret MK2",
        description: "Long range and very incredibly powerful turret.",
        item: new CItem_Defense(tile: new CTile(0, 0) { m_textureName = "items_defenses" }, tileIcon: new ModCTile(12, 0),
            hpMax: 350, mainColor: 8947848U, rangeDetection: 10f,
            angleMin: -9999f, angleMax: 9999f,
            attack: new CAttackDesc(
                range: 12f,
                damage: 50,
                nbAttacks: 1,
                cooldown: 0.5f,
                knockbackOwn: 0f, knockbackTarget: 3f,
                projDesc: new CBulletDesc(
                    ModCTile.texturePath, "particlesSnipTurretMK2",
                    radius: 0.45f, dispersionAngleRad: 0f,
                    speedStart: 40f, speedEnd: 30f, light: 0xE10AF5
                ),
                sound: "particleTurret"
            ),
            tileUnit: new ModCTile(13, 0)
        ) {
            m_anchor = CItemCell.Anchor.Everyside_Small
        },
        recipe: new(groupId: "ULTIMATE")
    );
    
    public static readonly ModItem turretTeslaMK2 = new(codeName: "turretTeslaMK2",
        name: "Tesla Turret MK2",
        description: "Creates a very powerful lightning strike on nearby monsters, with a area damage effect. Consumes 5kW.",
        item: new CItem_Defense(tile: new ModCTile(14, 0), tileIcon: new ModCTile(14, 0),
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
        },
        recipe: new(groupId: "ULTIMATE")
    );
    
    public static readonly ModItem collector = new(codeName: "collector",
        name: "Collector", description: "-Plants.",
        item: new ExtCItem_Collector(tile: new ModCTile(15, 0), tileIcon: new ModCTile(16, 0),
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
            tileUnit: new ModCTile(17, 0)
        ) {
            m_anchor = CItemCell.Anchor.Everyside_Small,
            m_displayRangeOnCells = true,
            m_neverUnspawn = true,
            collectorDamage = 10,
            m_electricValue = -2
        },
        recipe: new(groupId: "ULTIMATE")
    );
    
    public static readonly ModItem blueLightSticky = new(codeName: "blueLightSticky",
        name: "Blue Wall Light",
        description: "You can attach this lamp to any surface and it will glow BLUE!",
        item: new CItem_Machine(tile: new ModCTile(18, 0), tileIcon: new ModCTile(18, 0),
            hpMax: 100, mainColor: 10066329U, anchor: CItemCell.Anchor.Everywhere_Small
        ) {
            m_light = new Color24(20, 20, 220)
        },
        recipe: new(groupId: "ULTIMATE")
    );
    
    public static readonly ModItem redLightSticky = new(codeName: "redLightSticky",
        name: "Red Wall Light",
        description: "You can attach this lamp to any surface and it will glow RED!",
        item: new CItem_Machine(tile: new ModCTile(20, 0), tileIcon: new ModCTile(20, 0),
            hpMax: 100, mainColor: 10066329U, anchor: CItemCell.Anchor.Everywhere_Small
        ) {
            m_light = new Color24(220, 20, 20)
        },
        recipe: new(groupId: "ULTIMATE")
    );
    
    public static readonly ModItem greenLightSticky = new(codeName: "greenLightSticky",
        name: "Green Wall Light",
        description: "You can attach this lamp to any surface and it will glow GREEN!",
        item: new CItem_Machine(tile: new ModCTile(22, 0), tileIcon: new ModCTile(22, 0),
            hpMax: 100, mainColor: 10066329U, anchor: CItemCell.Anchor.Everywhere_Small
        ) {
            m_light = new Color24(20, 220, 20)
        },
        recipe: new(groupId: "ULTIMATE")
    );
    
    public static readonly ModItem basaltCollector = new(codeName: "basaltCollector",
        name: "Basalt Collector", description: "-Basalt.",
        item: new ExtCItem_Collector(tile: new ModCTile(15, 0), tileIcon: new ModCTile(24, 0),
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
            tileUnit: new ModCTile(25, 0)
        ) {
            m_anchor = CItemCell.Anchor.Everyside_Small,
            m_displayRangeOnCells = true,
            m_neverUnspawn = true,
            collectorDamage = 100,
            isBasaltCollector = true,
            m_electricValue = -5
        },
        recipe: new(groupId: "ULTIMATE")
    );
    
    public static readonly ModItem turretLaser360 = new(codeName: "turretLaser360",
        name: "Rotating Laser Turret",
        description: "Much more efficient than plasma technology with rotation! Burn through all organic material.",
        item: new CItem_Defense(tile: new CTile(0, 0) { m_textureName = "items_defenses" }, tileIcon: new ModCTile(26, 0),
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
        ),
        recipe: new(groupId: "ULTIMATE")
    );
    
    public static readonly ModItem gunMeltdown = new(codeName: "gunMeltdown",
        name: "Gun \"Meltdown\"", description: "The true power...",
        item: new CItem_Weapon(tile: new ModCTile(27, 0), tileIcon: new ModCTile(28, 0),
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
        ),
        recipe: new(groupId: "ULTIMATE")
    );
    
    public static readonly ModItem volcanicExplosive = new(codeName: "volcanicExplosive",
        name: "Volcanic Explosive", description: "A explosive that powerful, that it creates a mini volcan and can awake an actual volcan at any location.",
        item: new ExtCItem_Explosive(tile: new ModCTile(29, 0), tileIcon: new ModCTile(29, 0),
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
            lavaQuantity = ExtCItem_Explosive.CalculateLavaQuantityStep(totalQuantity: 1500f, time: 5f),
            lavaReleaseTime = 5f,
            indestructible = true,
            timerColor = Color.red * 0.3f,
            m_light = new Color24(240, 38, 38),
            m_fireProof = true,
            shockWaveDamage = 30f,
            shockWaveKnockback = 30f,
            shockWaveRange = 50f,
        },
        recipe: new(groupId: "ULTIMATE")
    );
    
    public static readonly ModItem wallCompositeReinforced = new(codeName: "wallCompositeReinforced",
        name: "Composite Reinforced Wall", description: "Better than Composite Wall!",
        item: new CItem_Wall(tile: new ModCTile(30, 0), tileIcon: new ModCTile(30, 0),
            hpMax: 700, mainColor: 12039872U, forceResist: 11000, weight: 560f,
            type: CItem_Wall.Type.WallBlock
        ),
        recipe: new(groupId: "ULTIMATE")
    );
    
    public static readonly ModItem gunNukeLauncher = new(codeName: "gunNukeLauncher",
        name: "Mini-Nuke Launcher", description: "Laundes nukes!",
        item: new CItem_Weapon(tile: new ModCTile(31, 0), tileIcon: new ModCTile(32, 0),
            heatingPerShot: 0f, isAuto: false,
            attackDesc: new CAttackDesc(
                range: 100f,
                damage: 1000,
                nbAttacks: 1,
                cooldown: 0f,
                knockbackOwn: 100f,
                knockbackTarget: 200f,
                projDesc: new ExtCBulletDesc(
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
        ),
        recipe: new(groupId: "ULTIMATE")
    );
    
    public static readonly ModItem generatorSunMK2 = new(codeName: "generatorSunMK2",
        name: "Solar Panel MK2", description: "Produces even more electricity from sun light than regular Solar Panel (3kW).",
        item: new CItem_Machine(tile: new ModCTile(33, 0), tileIcon: new ModCTile(33, 0),
            hpMax: 200, mainColor: 10066329U,
            anchor: CItemCell.Anchor.Bottom_Small
        ) {
            m_electricValue = 3
        },
        recipe: new(groupId: "ULTIMATE")
    );
    
    public static readonly ModItem RTG = new(codeName: "RTG",
        name: "Radioisotope Thermoelectric Generator", 
        description: "A type of nuclear battery that uses an array of thermocouples to convert the heat released by the decay of a suitable radioactive material into electricity by the Seebeck effect (15kW).",
        item: new CItem_Machine(tile: new ModCTile(34, 0), tileIcon: new ModCTile(34, 0),
            hpMax: 200, mainColor: 10066329U,
            anchor: CItemCell.Anchor.Bottom_Small
        ) {
            m_light = new Color24(0xED0CE9),
            m_electricValue = 15
        },
        recipe: new(groupId: "ULTIMATE")
    );
    
    public static readonly ModItem indestructibleLavaOld = new(codeName: "indestructibleLavaOld",
        name: "Indestructible Ancient Basalt", description: "Impossible to destory.",
        item: new ExtCItem_IndestructibleMineral(tile: null, tileIcon: new CTile(3, 5),
            hpMax: 1000, mainColor: 6118492U, surface: GSurfaces.lavaOld, isReplacable: false
        )
    );
    
    public static readonly ModItem gunRocketGatling = new(codeName: "gunRocketGatling",
        name: "Rocket Launcher Gatling", description: "Very powerful explosive impact.",
        item: new CItem_Weapon(tile: new CTile(2, 1) { m_textureName = "items_weapons" }, tileIcon: new CTile(6, 3) { m_textureName = "items_icons" },
            heatingPerShot: 0.1f, isAuto: true,
            attackDesc: new CAttackDesc(
                range: 20f, damage: 40, nbAttacks: 1, cooldown: 0.15f,
                knockbackOwn: 3f,
                knockbackTarget: 25f,
                projDesc: GBullets.rocket,
                sound: "rocketFire"
            )
        ),
        recipe: new(groupId: "ULTIMATE")
    );
    
    public static readonly ModItem gunRailgun = new(codeName: "gunRailgun",
        name: "Railgun", description: "TODO.",
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
        ),
        recipe: new(groupId: "ULTIMATE")
    );
    
    public static readonly ModItem gunBeamLaser = new(codeName: "gunBeamLaser",
        name: "Laser Beam Gun", description: "TODO.",
        item: new CItem_Weapon(tile: new CTile(0, 1) { m_textureName = "items_weapons" }, tileIcon: new CTile(4, 3) { m_textureName = "items_icons" },
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
        name: "ZF-0 Shotgun", description: "TODO.",
        item: new CItem_Weapon(tile: new CTile(3, 1) { m_textureName = "items_weapons" }, tileIcon: new CTile(7, 3) { m_textureName = "items_icons" },
            heatingPerShot: 0.4f, isAuto: false,
            attackDesc: new CAttackDesc(
                range: 20f, damage: 8, nbAttacks: 10, cooldown: 0.25f,
                knockbackOwn: 11f, knockbackTarget: 2f,
                projDesc: CustomBullets.zf0shotgunBullet,
                sound: "shotgun"
            )
        ),
        recipe: new(groupId: "ULTIMATE")
    );

    public static readonly ModItem portableTeleport = new(codeName: "portableTeleport",
        name: "Portable Teleporter", description: "TODO.",
        item: new CItem_Device(tile: new ModCTile(37, 0), tileIcon: new ModCTile(37, 0),
            groupId: null, type: CItem_Device.Type.Activable
        ),
        recipe: new(groupId: "ULTIMATE")
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

