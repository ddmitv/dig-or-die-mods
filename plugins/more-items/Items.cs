using UnityEngine;

// REFLECTION: Method Patches.SUnits_OnInit iterates all public static fields and expects they has type CUnit.CDesc
public static class CustomUnits {
    public static readonly ExtCUnitWaterVaporizer.CDesc unitDesc = new(tier: -1, speed: 0, size: Vector2.zero, hpMax: 10, armor: 0) {
        m_codeName = "more-items_waterVaporizer"
    };
}

public static class CustomBullets {
    public static readonly string particlesPath = "more-items_particles";
    public static Texture2D particlesTexture = null;

    public static readonly ExtCBulletDesc meltdownSnipe = new(
        particlesPath, "meltdownSnipe",
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

    public static readonly CBulletDesc impactGrenadeBullet = new(
        particlesPath, "impactGrenade",
        radius: 0.5f,
        dispersionAngleRad: 0f,
        speedStart: 20f,
        speedEnd: 15f,
        light: 0x005E19
    ) {
        m_grenadeYSpeed = -40f,
        m_explosionRadius = 3f,
        m_explosionMaxBlockHp = 300,
    };

    public static readonly ExtCBulletDesc particleEnergyDiffuser = new(
        particlesPath, "particleEnergyDiffuser",
        radius: 0.5f,
        dispersionAngleRad: 0f,
        speedStart: 15f,
        speedEnd: 10f,
        light: 0x05B7ED
    ) {
        explosionEnergyRadius = 6f,
        explosionEnergyDamage = 60f,
        m_explosionRadius = 1f,
    };
}

public static class CustomSurfaces {
    public static readonly ModCSurface fertileDirt = new(
        surfaceTexture: ModCSurface.fertileDirtTexturePath, surfaceSortingOrder: 30,
        topTileI: 0, topTileJ: 0, hasAltTop: true
    );
}

public static class CustomRecipeGroups {
    public static readonly ModRecipeGroup mk6 = new("MK VI", [
        // GItems.autoBuilderMK1 //, GItems.autoBuilderMK2, GItems.autoBuilderMK3, GItems.autoBuilderMK4, GItems.autoBuilderMK5
        (CItem_MachineAutoBuilder)CustomItems.autoBuilderMK6
    ]);
}

// REFLECTION: Methods Patches.SItems_OnInit and Patches.SDataLua_OnInit iterates all public static fields
// and expects they has type ModItem
public static class CustomItems {
    public static readonly ModItem flashLightMK3 = new(codeName: "flashLightMK3",
        name: "Flashlight MK3",
        description: "The brightest handheld light ever engineered. Uses self-charging photon amplification to outshine even the void of deep caves.",
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
        name: "Miniaturizor MK VI",
        description: "The final word in portable matter compression. Matter compression device utilizing quantum-locked deatomization fields. Warning: Do not use on black holes.",
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
        description: "Advanced bio-stimulant compound (400% HP restoration over 60s). Rebuilds cells faster than they can die.",
        item: new CItem_Device(tile: new ModCTile(3, 0), tileIcon: new ModCTile(3, 0),
            groupId: CItemDeviceGroupIds.potionHPRegen, type: CItem_Device.Type.Consumable, customValue: 3f
        ) { m_cooldown = 120f, m_duration = 60f },
        recipe: new(groupId: "MK III") {
            in1 = GItems.bloodyFlesh2, nb1 = 5
        }
    );

    public static readonly ModItem defenseShieldMK2 = new(codeName: "defenseShieldMK2",
        name: "Defense Shield MK2",
        description: "Projected quantum barrier capable of absorbing kinetic impacts equal to user's maximum HP. Recharges in 2s.",
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
        description: "High-yield electrolytic filtration system extracts breathable gases from liquid environments.",
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
        description: "Dual-thrust VTOL propulsion system.",
        item: new ExtCItem_JetpackDevice(tile: new ModCTile(6, 0), tileIcon: new ModCTile(6, 0)) {
            jetpackEnergyUsageMultiplier = 0.095f,
            jetpackFlyForce = 100f,
        },
        recipe: new(groupId: "MK V", isUpgrade: true) {
            in1 = GItems.aluminium, nb1 = 5,
            in2 = GItems.masterGem, nb2 = 1
        }
    );

    public static readonly ModItem antiGravityWall = new(codeName: "antiGravityWall",
        name: "Anti-Gravity Wall",
        description: "Defies conventional physics by emitting a repulsive wave of synthesized negative mass. Installation requires chrono-stabilized anchoring.",
        item: new CItem_Wall(tile: new ModCTile(7, 0), tileIcon: new ModCTile(7, 0),
            hpMax: 100, mainColor: 12173251U, forceResist: int.MaxValue - 10000, weight: 1000f, type: CItem_Wall.Type.WallBlock
        ),
        recipe: new(groupId: "ULTIMATE")
    );

    public static readonly ModItem turretReparatorMK3 = new(codeName: "turretReparatorMK3",
        name: "Auto-Repair Turret MK3",
        description: "Deploys nano-assembler drones with 7.5m operational radius. Repair rate: 10 HP/s. Consumes 2kW.",
        item: new CItem_Defense(tile: new ModCTile(2, 1), tileIcon: new ModCTile(1, 1),
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
            tileUnit: new ModCTile(0, 1)
        ) {
            m_displayRangeOnCells = true,
            m_electricValue = -2,
            m_light = new Color24(10329710U),
            m_neverUnspawn = true
        },
        recipe: new(groupId: "ULTIMATE")
    );

    public static readonly ModItem megaExplosive = new(codeName: "megaExplosive",
        name: "Mega Explosive",
        description: "Thermonuclear demolition charge (yield: 3000 damage, 10m blast radius).",
        item: new ExtCItem_Explosive(tile: new ModCTile(3, 1), tileIcon: new ModCTile(3, 1),
            hpMax: 250, mainColor: 8947848U, rangeDetection: 0f, angleMin: 0f, angleMax: 360f,
            attack: new CAttackDesc(
                range: 10f,
                damage: 3000,
                nbAttacks: 0,
                cooldown: -1f,
                knockbackOwn: 0f,
                knockbackTarget: 10f,
                projDesc: null,
                sound: SoundIds.rocketExplosion
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
        recipe: new(groupId: "ULTIMATE")
    );

    public static readonly ModItem turretParticlesMK2 = new(codeName: "turretParticlesMK2",
        name: "Particle Turret MK2",
        description: "Magnetized plasma accelerator turret which fires superheated particle bolts.",
        item: new CItem_Defense(tile: new CTile(0, 0) { m_textureName = "items_defenses" }, tileIcon: new ModCTile(4, 1),
            hpMax: 350, mainColor: 8947848U, rangeDetection: 10f,
            angleMin: -9999f, angleMax: 9999f,
            attack: new CAttackDesc(
                range: 12f,
                damage: 50,
                nbAttacks: 1,
                cooldown: 0.5f,
                knockbackOwn: 0f, knockbackTarget: 3f,
                projDesc: new CBulletDesc(
                    CustomBullets.particlesPath, "particlesSnipTurretMK2",
                    radius: 0.45f, dispersionAngleRad: 0f,
                    speedStart: 40f, speedEnd: 30f, light: 0xE10AF5
                ),
                sound: SoundIds.particleTurret
            ),
            tileUnit: new ModCTile(5, 1)
        ) {
            m_anchor = CItemCell.Anchor.Everyside_Small
        },
        recipe: new(groupId: "ULTIMATE")
    );

    public static readonly ModItem turretTeslaMK2 = new(codeName: "turretTeslaMK2",
        name: "Tesla Turret MK2",
        description: "Summons artificial lightning from ionized atmosphere, chaining between targets with fractal precision. Consumes 5kW.",
        item: new CItem_Defense(tile: new ModCTile(6, 1), tileIcon: new ModCTile(6, 1),
            hpMax: 350, mainColor: 8947848U, rangeDetection: 12.5f,
            angleMin: -9999f, angleMax: 9999f,
            attack: new CAttackDesc(
                range: 12f,
                damage: 200,
                nbAttacks: 1,
                cooldown: 2f,
                knockbackOwn: 0f, knockbackTarget: 10f,
                projDesc: null,
                sound: SoundIds.storm
            ),
            tileUnit: null
        ) {
            m_electricValue = -5,
            m_light = new Color24(16, 133, 235)
        },
        recipe: new(groupId: "ULTIMATE")
    );

    public static readonly ModItem collector = new(codeName: "collector",
        name: "Collector",
        description: "Automated botanical harvesting unit. Deploys precision cutting beams, compatible with all known flora.",
        item: new ExtCItem_Collector(tile: new ModCTile(7, 1), tileIcon: new ModCTile(0, 2),
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
            tileUnit: new ModCTile(1, 2)
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
        item: new CItem_Machine(tile: new ModCTile(2, 2), tileIcon: new ModCTile(2, 2),
            hpMax: 100, mainColor: 10066329U, anchor: CItemCell.Anchor.Everywhere_Small
        ) {
            m_light = new Color24(20, 20, 220)
        },
        recipe: new(groupId: "ULTIMATE")
    );

    public static readonly ModItem redLightSticky = new(codeName: "redLightSticky",
        name: "Red Wall Light",
        description: "You can attach this lamp to any surface and it will glow RED!",
        item: new CItem_Machine(tile: new ModCTile(4, 2), tileIcon: new ModCTile(4, 2),
            hpMax: 100, mainColor: 10066329U, anchor: CItemCell.Anchor.Everywhere_Small
        ) {
            m_light = new Color24(220, 20, 20)
        },
        recipe: new(groupId: "ULTIMATE")
    );

    public static readonly ModItem greenLightSticky = new(codeName: "greenLightSticky",
        name: "Green Wall Light",
        description: "You can attach this lamp to any surface and it will glow GREEN!",
        item: new CItem_Machine(tile: new ModCTile(6, 2), tileIcon: new ModCTile(6, 2),
            hpMax: 100, mainColor: 10066329U, anchor: CItemCell.Anchor.Everywhere_Small
        ) {
            m_light = new Color24(20, 220, 20)
        },
        recipe: new(groupId: "ULTIMATE")
    );

    public static readonly ModItem basaltCollector = new(codeName: "basaltCollector",
        name: "Basalt Collector",
        description: "Industrial-grade mineral extraction unit optimized for volcanic rock.",
        item: new ExtCItem_Collector(tile: new ModCTile(7, 1), tileIcon: new ModCTile(0, 3),
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
            tileUnit: new ModCTile(1, 3)
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
        description: "360-degree photon emitter. Penetrates organic matter completely.",
        item: new CItem_Defense(tile: new CTile(0, 0) { m_textureName = "items_defenses" }, tileIcon: new ModCTile(2, 3),
            hpMax: 250, mainColor: 8947848U, rangeDetection: 10f,
            angleMin: -9999f, angleMax: 9999f,
            attack: new CAttackDesc(
                range: 10f,
                damage: 20,
                nbAttacks: 1,
                cooldown: 0.3f,
                knockbackOwn: 0f, knockbackTarget: 0f,
                projDesc: GBullets.laser, sound: SoundIds.laser
            ),
            tileUnit: new CTile(2, 2) { m_textureName = "items_defenses" }
        ),
        recipe: new(groupId: "ULTIMATE")
    );

    public static readonly ModItem gunMeltdown = new(codeName: "gunMeltdown",
        name: "Gun \"Meltdown\"",
        description: "Fires a condensed bolt of pure thermodynamic chaos, forcing targets into rapid atomic decay. The recoil has been known to send users sliding backwards through time (approx. 0.3 nanoseconds).",
        item: new CItem_Weapon(tile: new ModCTile(3, 3), tileIcon: new ModCTile(4, 3),
            heatingPerShot: 2f, isAuto: false,
            attackDesc: new CAttackDesc(
                range: 50f,
                damage: 1500,
                nbAttacks: 1,
                cooldown: 3f,
                knockbackOwn: 60f,
                knockbackTarget: 100f,
                projDesc: CustomBullets.meltdownSnipe,
                sound: SoundIds.plasmaSnipe
            )
        ),
        recipe: new(groupId: "ULTIMATE")
    );

    public static readonly ModItem volcanicExplosive = new(codeName: "volcanicExplosive",
        name: "Volcanic Explosive",
        description: "Tectonic induction device. Upon detonation, generates a localized subduction zone and summons an artificial magma plume. Could potentially trigger an eruption of nearby volcanoes.",
        item: new ExtCItem_Explosive(tile: new ModCTile(5, 3), tileIcon: new ModCTile(5, 3),
            hpMax: 500, mainColor: 8947848U, rangeDetection: 0f, angleMin: 0f, angleMax: 360f,
            attack: new CAttackDesc(
                range: 25f,
                damage: 2000,
                nbAttacks: 0,
                cooldown: -1f,
                knockbackOwn: 0f,
                knockbackTarget: 500f,
                projDesc: null,
                sound: SoundIds.rocketExplosion
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
        recipe: new(groupId: "ULTIMATE")
    );

    public static readonly ModItem wallCompositeReinforced = new(codeName: "wallCompositeReinforced",
        name: "Composite Reinforced Wall",
        description: "Ultra-dense construction material. Layered graphene-ceramic alloy with shock dispersion matrix.",
        item: new CItem_Wall(tile: new ModCTile(6, 3), tileIcon: new ModCTile(6, 3),
            hpMax: 700, mainColor: 12039872U, forceResist: 11000, weight: 560f,
            type: CItem_Wall.Type.WallBlock
        ),
        recipe: new(groupId: "ULTIMATE")
    );

    public static readonly ModItem gunNukeLauncher = new(codeName: "gunNukeLauncher",
        name: "Mini-Nuke Launcher",
        description: "Compact nuclear delivery system (1000 damage, 15m radius). Fires stabilized micro-fusion warheads. Backblast not included.",
        item: new CItem_Weapon(tile: new ModCTile(7, 3), tileIcon: new ModCTile(0, 4),
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
                sound: SoundIds.rocketFire
            )
        ),
        recipe: new(groupId: "ULTIMATE")
    );

    public static readonly ModItem generatorSunMK2 = new(codeName: "generatorSunMK2",
        name: "Solar Panel MK2",
        description: "High-efficiency photovoltaic array (3kW output). Self-cleaning surface maintains 98% light absorption in all conditions.",
        item: new CItem_Machine(tile: new ModCTile(1, 4), tileIcon: new ModCTile(1, 4),
            hpMax: 200, mainColor: 10066329U,
            anchor: CItemCell.Anchor.Bottom_Small
        ) {
            m_electricValue = 3
        },
        recipe: new(groupId: "ULTIMATE")
    );

    public static readonly ModItem RTG = new(codeName: "RTG",
        name: "Radioisotope Thermoelectric Generator",
        description: "Radioactive decay-powered generator (15kW output). Utilizes plutonium-238 core with 87-year half-life. Shielded housing prevents contamination.",
        item: new CItem_Machine(tile: new ModCTile(2, 4), tileIcon: new ModCTile(2, 4),
            hpMax: 200, mainColor: 10066329U,
            anchor: CItemCell.Anchor.Bottom_Small
        ) {
            m_light = new Color24(0xED0CE9),
            m_electricValue = 15
        },
        recipe: new(groupId: "ULTIMATE")
    );

    public static readonly ModItem indestructibleLavaOld = new(codeName: "indestructibleLavaOld",
        name: "Indestructible Ancient Basalt",
        description: "Metastable mineral formation. No known force can compromise structural integrity.",
        item: new ExtCItem_IndestructibleMineral(tile: null, tileIcon: new CTile(3, 5),
            hpMax: 1000, mainColor: 6118492U, surface: GSurfaces.lavaOld, isReplacable: false
        )
    );

    public static readonly ModItem gunRocketGatling = new(codeName: "gunRocketGatling",
        name: "Rocket Launcher Gatling",
        description: "Rotary micro-missile array. Gatling version of standard rocket launcher fires 40-damage projectiles.",
        item: new CItem_Weapon(tile: new CTile(2, 1) { m_textureName = "items_weapons" }, tileIcon: new CTile(6, 3) { m_textureName = "items_icons" },
            heatingPerShot: 0.1f, isAuto: true,
            attackDesc: new CAttackDesc(
                range: 20f, damage: 40, nbAttacks: 1, cooldown: 0.15f,
                knockbackOwn: 3f,
                knockbackTarget: 25f,
                projDesc: GBullets.rocket,
                sound: SoundIds.rocketFire
            )
        ),
        recipe: new(groupId: "ULTIMATE")
    );

    public static readonly ModItem gunRailgun = new(codeName: "gunRailgun",
        name: "Railgun",
        description: "Electromagnetic projectile accelerator. Requires capacitor cooling between discharges.",
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
                sound: SoundIds.plasmaSnipe
            )
        ),
        recipe: new(groupId: "ULTIMATE")
    );

    public static readonly ModItem gunBeamLaser = new(codeName: "gunBeamLaser",
        name: "Laser Beam Gun",
        description: "Continuous-wave photon emitter. Improved over standard laser guns with infinite penetration capability.",
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
        name: "ZF-0 Shotgun",
        description: "Multi-barrel flechette disperser. Upgraded ZF-0 model fires 10 armor-piercing rounds per trigger pull.",
        item: new CItem_Weapon(tile: new CTile(3, 1) { m_textureName = "items_weapons" }, tileIcon: new CTile(7, 3) { m_textureName = "items_icons" },
            heatingPerShot: 0.4f, isAuto: false,
            attackDesc: new CAttackDesc(
                range: 20f, damage: 8, nbAttacks: 10, cooldown: 0.25f,
                knockbackOwn: 11f, knockbackTarget: 2f,
                projDesc: CustomBullets.zf0shotgunBullet,
                sound: SoundIds.shotgun
            )
        ),
        recipe: new(groupId: "ULTIMATE")
    );

    public static readonly ModItem portableTeleport = new(codeName: "portableTeleport",
        name: "Portable Teleporter",
        description: "Personal quantum translocation device utilizing folded-space technology, which is a compact version of a standard teleporter. Safety protocols prevent matter reintegration errors",
        item: new CItem_Device(tile: new ModCTile(5, 4), tileIcon: new ModCTile(5, 4),
            groupId: null, type: CItem_Device.Type.Activable
        ),
        recipe: new(groupId: "ULTIMATE")
    );

    public static readonly ModItem fertileDirt = new(codeName: "fertileDirt",
        name: "Fertile Dirt",
        description: "TODO.",
        item: new ExtCItem_FertileMineralDirt(tile: null, tileIcon: new ModCTile(6, 4),
            hpMax: 30, mainColor: 9465936U, surface: CustomSurfaces.fertileDirt
        ) {
            plantGrowChange = 0.3f,
            inheritedPlantsSupported = [GItems.dirt, GItems.dirtRed, GItems.silt, GItems.dirtBlack, GItems.dirtSky],
        },
        recipe: new(groupId: "MK V")
    );

    public static readonly ModItem autoBuilderMK6 = new(codeName: "autoBuilderMK6",
        name: "Auto-Builder MK VI",
        description: "TODO.",
        item: new ExtCItem_ConditionalMachineAutoBuilder(tile: new ModCTile(7, 4), tileIcon: new ModCTile(7, 4)) {
            m_light = new Color24(220, 20, 220),
            m_customValue = 6f,
            m_electricValue = -10,
            checkCondition = (int x, int y) => {
                return SWorld.Grid[x, y].GetBgSurface() == GSurfaces.bgOrganic;
            }
        },
        recipe: new(groupId: "MK V")
    );

    public static readonly ModItem gunImpactGrenade = new (codeName: "gunImpactGrenade",
        name: "Impact granade",
        description: "TODO.",
        item: new ExtCItem_ConsumableWeapon(tile: new ModCTile(2, 5), tileIcon: new ModCTile(3, 5),
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
        recipe: new(groupId: "ULTIMATE")
    );

    public static readonly ModItem impactShieldMk1 = new(codeName: "impactShieldMk1",
        name: "Impact Shield MK1",
        description: "TODO.",
        item: new ExtCItem_ImpactShield(tile: new ModCTile(0, 5), tileIcon: new ModCTile(0, 5),
            customValue: 0.25f
        ),
        recipe: new(groupId: "ULTIMATE")
    );
    public static readonly ModItem impactShieldMk2 = new(codeName: "impactShieldMk2",
        name: "Impact Shield MK2",
        description: "TODO.",
        item: new ExtCItem_ImpactShield(tile: new ModCTile(1, 5), tileIcon: new ModCTile(1, 5),
            customValue: 0.5f
        ),
        recipe: new(groupId: "ULTIMATE")
    );
    public static readonly ModItem waterVaporizer = new(codeName: "waterVaporizer",
        name: "Water Vaporizer",
        description: "TODO.",
        item: new ExtCItem_WaterVaporizer(tile: new ModCTile(4, 5), tileIcon: new ModCTile(4, 5),
            hpMax: 10, mainColor: 13731096U
        ) {
            evaporationRate = 5f,
            m_electricValue = -5
        },
        recipe: new(groupId: "ULTIMATE")
    );
    public static readonly ModItem turretCeilingMK2 = new(codeName: "turretCeilingMK2",
        name: "Death Pulse Turret MK2",
        description: "TODO.",
        item: new ExtCItem_CeilingTurret(tile: new ModCTile(5, 5), 
            hpMax: 300, mainColor: 8947848U, rangeDetection: 3.8f, angleMin: -120f, angleMax: -60f,
            attack: new CAttackDesc(
                range: 4f, damage: 60, nbAttacks: 2, cooldown: 1f, knockbackOwn: 0f, knockbackTarget: 0f,
                projDesc: null, sound: SoundIds.ceilingTurret
            )
        ) {
            m_colRect = new Rect(0.1f, 0.6f, 0.8f, 0.4f)
        },
        recipe: new(groupId: "ULTIMATE")
    );
    public static readonly ModItem turretSpikesMK2 = new(codeName: "turretSpikesMK2",
        name: "Electrified Spikes MK2",
        description: "TODO.",
        item: new ExtCItem_SpikesTurret(tile: new ModCTile(6, 5),
            hpMax: 400, mainColor: 8947848U, rangeDetection: 1.5f, angleMin: 0f, angleMax: 180f,
            attack: new CAttackDesc(
                range: 1.5f, damage: 30, nbAttacks: 2, cooldown: 0.5f, knockbackOwn: 0f, knockbackTarget: 0f,
                projDesc: null, sound: SoundIds.stormLight
            )
        ) {
            m_colRect = new Rect(0.1f, 0f, 0.8f, 0.35f),
            m_electricValue = -3,
            m_light = new Color24(9724047U)
        },
        recipe: new(groupId: "ULTIMATE")
    );
    public static readonly ModItem gunEnergyDiffuser = new(codeName: "gunEnergyDiffuser",
        name: "MB-X Plasma Diffuser",
        description: "TODO.",
        item: new CItem_Weapon(tile: new ModCTile(0, 6), tileIcon: new ModCTile(7, 5),
            heatingPerShot: 0.4f, isAuto: true,
            attackDesc: new CAttackDesc(
                range: 15f, damage: 20, nbAttacks: 1,
                cooldown: 1f, knockbackOwn: 5f, knockbackTarget: 10f,
                projDesc: CustomBullets.particleEnergyDiffuser, sound: SoundIds.storm
            )
        ),
        recipe: new(groupId: "ULTIMATE")
    );
    public static readonly ModItem waterVaporizerMK2 = new(codeName: "waterVaporizerMK2",
        name: "Water Vaporizer MK2",
        description: "TODO.",
        item: new ExtCItem_WaterVaporizer(tile: new ModCTile(1, 6), tileIcon: new ModCTile(1, 6),
            hpMax: 20, mainColor: 13731096U
        ) {
            evaporationRate = 8f,
            m_electricValue = -10
        },
        recipe: new(groupId: "ULTIMATE")
    );
    public static readonly ModItem advancedMetalDetector = new(codeName: "advancedMetalDetector",
        name: "Advanced Metal Detector",
        description: "TODO.",
        item: new ExtCItem_MetalDetector(tile: new ModCTile(2, 6), range: 120f) {
            m_cooldown = 3f,
            detectableItems = [GItems.iron, GItems.copper, GItems.gold, GItems.aluminium, GItems.uranium, GItems.titanium, GItems.thorium, GItems.sulfur, GItems.sapphire, GItems.diamonds]
        },
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

