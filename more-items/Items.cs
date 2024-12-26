using UnityEngine;

namespace more_items;

public class Items {
    public static CustomItem[] customItems = null;

    public static CBulletDesc meltdownSnipe = null;

    public static void Init() {
        customItems = [
            new CustomItem(name: "flashLightMK3",
                item: new CItem_Device(tile: new CustomCTile(0, 0), tileIcon: new CustomCTile(0, 0),
                    groupId: "FlashLight", type: CItem_Device.Type.Passive, customValue: 10f
                )
            ),
            new CustomItem(name: "miniaturizorMK6",
                item: new CItem_Device(tile: new CustomCTile(2, 0), tileIcon: new CustomCTile(1, 0),
                    groupId: "Miniaturizor", type: CItem_Device.Type.None, customValue: 999f
                    // Above 999 the miniaturizor would break Ancient Basalt (oldLava)
                ){ m_pickupDuration = -1 }
            ),
            new CustomItem(name: "betterPotionHpRegen",
                item: new CItem_Device(tile: new CustomCTile(3, 0), tileIcon: new CustomCTile(3, 0),
                    groupId: "potionHpRegen", type: CItem_Device.Type.Consumable, customValue: 3f
                    // "potionHpRegen" has 1.5f customValue
                ){ m_cooldown = 120f, m_duration = 60f }
            ),
            new CustomItem(name: "defenseShieldMK2",
                item: new CItem_Device(tile: new CustomCTile(4, 0), tileIcon: new CustomCTile(4, 0),
                    groupId: "Shield", type: CItem_Device.Type.Passive, customValue: 1f
                    // "defenseShield" has 0.5f customValue
                )
            ),
            new CustomItem(name: "waterBreatherMK2",
                item: new CItem_Device(tile: new CustomCTile(5, 0), tileIcon: new CustomCTile(5, 0),
                    groupId: "WaterBreather", type: CItem_Device.Type.Passive, customValue: 7f
                    // "waterBreather" has 3f customValue
                )
            ),
            new CustomItem(name: "jetpackMK2",
                item: new CItem_Device(tile: new CustomCTile(6, 0), tileIcon: new CustomCTile(6, 0),
                    groupId: "Jetpack", type: CItem_Device.Type.Passive, customValue: 1f
                )
            ),
            new CustomItem(name: "antiGravityWall",
                item: new CItem_Wall(tile: new CustomCTile(7, 0), tileIcon: new CustomCTile(7, 0),
                    hpMax: 100, mainColor: 12173251U, forceResist: int.MaxValue - 10000, weight: 1000f, type: CItem_Wall.Type.WallBlock
                )
            ),
            new CustomItem(name: "turretReparatorMK3",
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
            ),
            new CustomItem(name: "megaExplosive",
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
            ),
            new CustomItem(name: "turretParticlesMK2",
                item: new CItem_Defense(tile: new CTile(0, 0) { m_textureName = "items_defenses" }, tileIcon: new CustomCTile(12, 0),
                    hpMax: 350, mainColor: 8947848U, rangeDetection: 10f,
                    angleMin: -9999f, angleMax: 9999f,
                    attack: new CAttackDesc(
                        range: 12f,
                        damage: 50,
                        nbAttacks: 1,
                        cooldown: 0.5f,
                        knockbackOwn: 0f, knockbackTarget: 3f,
                        projDesc: GBullets.particlesSnipTurret,
                        sound: "particleTurret"
                    ),
                    tileUnit: new CustomCTile(13, 0)
                ) {
                    m_anchor = CItemCell.Anchor.Everyside_Small
                }
            ),
            new CustomItem(name: "turretTeslaMK2",
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
            ),
            new CustomItem(name: "collector",
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
            ),
            new CustomItem(name: "blueLightSticky",
                item: new CItem_Machine(tile: new CustomCTile(18, 0), tileIcon: new CustomCTile(18, 0),
                    hpMax: 100, mainColor: 10066329U, anchor: CItemCell.Anchor.Everywhere_Small
                ) {
                    m_light = new Color24(20, 20, 220)
                }
            ),
            new CustomItem(name: "redLightSticky",
                item: new CItem_Machine(tile: new CustomCTile(20, 0), tileIcon: new CustomCTile(20, 0),
                    hpMax: 100, mainColor: 10066329U, anchor: CItemCell.Anchor.Everywhere_Small
                ) {
                    m_light = new Color24(220, 20, 20)
                }
            ),
            new CustomItem(name: "greenLightSticky",
                item: new CItem_Machine(tile: new CustomCTile(22, 0), tileIcon: new CustomCTile(22, 0),
                    hpMax: 100, mainColor: 10066329U, anchor: CItemCell.Anchor.Everywhere_Small
                ) {
                    m_light = new Color24(20, 220, 20)
                }
            ),
            new CustomItem(name: "basaltCollector",
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
            ),
            new CustomItem(name: "turretLaser360",
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
            ),
            new CustomItem(name: "gunMeltdown",
                item: new CItem_Weapon(tile: new CustomCTile(27, 0), tileIcon: new CustomCTile(28, 0),
                    heatingPerShot: 2f, isAuto: false,
                    attackDesc: new CAttackDesc(
                        range: 50f,
                        damage: 1500,
                        nbAttacks: 1,
                        cooldown: 3f,
                        knockbackOwn: 60f,
                        knockbackTarget: 100f,
                        projDesc: (meltdownSnipe = new CustomCBulletDesc(
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
                        }),
                        sound: "plasmaSnipe"
                    )
                )
            ),
            new CustomItem(name: "volcanicExplosive",
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
                }
            ),
            new CustomItem(name: "wallCompositeReinforced",
                item: new CItem_Wall(tile: new CustomCTile(30, 0), tileIcon: new CustomCTile(30, 0),
                    hpMax: 700, mainColor: 12039872U, forceResist: 11000, weight: 560f,
                    type: CItem_Wall.Type.WallBlock
                )
            ),
            new CustomItem(name: "gunNukeLauncher",
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
            ),
            new CustomItem(name: "generatorSunMK2",
                item: new CItem_Machine(tile: new CustomCTile(33, 0), tileIcon: new CustomCTile(33, 0),
                    hpMax: 200, mainColor: 10066329U,
                    anchor: CItemCell.Anchor.Bottom_Small
                ) {
                    m_electricValue = 3
                }
            ),
            new CustomItem(name: "RTG",
                item: new CItem_Machine(tile: new CustomCTile(34, 0), tileIcon: new CustomCTile(34, 0),
                    hpMax: 200, mainColor: 10066329U,
                    anchor: CItemCell.Anchor.Bottom_Small
                ) {
                    m_light = new Color24(0xED0CE9),
                    m_electricValue = 15
                }
            ),
            new CustomItem(name: "gunPlasmaThrower",
                item: new CItem_Weapon(tile: new CustomCTile(35, 0), tileIcon: new CustomCTile(36, 0),
                    heatingPerShot: 0f, isAuto: true,
                    attackDesc: new CAttackDesc(
                        range: 16f,
                        damage: 20,
                        nbAttacks: 1,
                        cooldown: 0.1f,
                        knockbackOwn: 0f,
                        knockbackTarget: 1f,
                        projDesc: new CustomCBulletDesc(
                            CustomCTile.texturePath, "particlePlasmaCloud",
                            radius: 0.5f, dispersionAngleRad: 0.1f,
                            speedStart: 25f, speedEnd: 15f, light: 0x770BDB
                        ) {
                            m_goThroughEnnemies = true,
                            m_pierceArmor = true,
                            m_inflame = true,
                        },
                        sound: null
                    )
                )
            )
        ];
    }
}
