namespace GameEngine;

public class CItem {
    public string m_codeName = null!;
    public ushort m_id;
    public string m_categoryId = null!;
    public int m_pickupDuration = 1800;
    public bool m_pickupPreventPick1sec;
    public bool m_pickupAutoPicked;
    public bool m_stopMonsters;
    // private string m_name = null!;
    // private string m_desc = null!;

    public virtual void Use_Local(CPlayer player, Vector2 pos, bool isShift) {}
    public virtual void OnUpdate() {}

    public CItemVars GetVars(CPlayer p) {
        return p.GetItemVars(this);
    }

    public CItemVars? GetVars(CUnitPlayer unitPlayer) {
        return GetVars(PlayerManager.GetPlayerByUnit(unitPlayer)!);
    }

    public override string ToString() => m_codeName;
}

public sealed class CItem_Device : CItem {
    public Group m_group;
    public Type m_type;
    public float m_customValue;
    public float m_cooldown;
    public float m_duration;

    public enum Type {
        None,
        Passive,
        Activable,
        Consumable
    }
    public enum Group {
        Miniaturizor,
        PotionHP,
        PotionHPRegen,
        PotionArmor,
        PotionPheromones,
        PotionCritics,
        PotionInvisibility,
        PotionSpeed,
        Armor,
        Shield,
        Drone,
        FlashLight,
        Minimapper,
        EffeilGlasses,
        MetalDetector,
        WaterDetector,
        WaterBreather,
        Jetpack,
        Invisibility,
        Brush
    }

    public CItem_Device(Group group, Type type = Type.None, float customValue = 0f) : base() {
        m_group = group;
        m_type = type;
        m_customValue = customValue;
        m_categoryId = "CITEM_DEVICE";
    }

    public bool IsDurationItemActive(CPlayer player) {
        return m_duration > 0f && GVars.m_simuTimeD < (double)(player.GetItemVars(this).TimeActivation + m_duration);
    }
}

public sealed class CAttackDesc {
    public float m_range;
    public int m_damage;
    public int m_nbAttacks;
    public float m_cooldown;
    public float m_knockbackOwn;
    public float m_knockbackTarget;
    public CBulletDesc? m_bulletDesc;
    public bool m_isFromMonste;

    public CAttackDesc(float range, int damage, int nbAttacks = 0, float cooldown = 0f, float knockbackOwn = 0f, float knockbackTarget = 0f, CBulletDesc? projDesc = null) {
        m_range = range;
        m_damage = damage;
        m_nbAttacks = nbAttacks;
        m_cooldown = cooldown;
        m_knockbackOwn = knockbackOwn;
        m_knockbackTarget = knockbackTarget;
        m_bulletDesc = projDesc;
    }
    public int GetDamage(CUnit unit) {
        float damageMult = unit is not CUnitMonster ? 1f : GParams.m_monstersDamagesMult * (1f + (NetworkClients.ConnectedClientsCount() - 1) * GParams.m_monstersDamagesAddPerPlayer);
        return (int)(m_damage * damageMult);
    }
}

public sealed class CItem_Weapon : CItem {
    public float m_heatingPerShot;
    public bool m_isAuto;
    public CAttackDesc m_attackDesc;

    public CItem_Weapon(float heatingPerShot, bool isAuto, CAttackDesc attackDesc) {
        m_heatingPerShot = heatingPerShot;
        m_isAuto = isAuto;
        m_attackDesc = attackDesc;
        m_categoryId = "CITEM_WEAPON";
    }
    // public override void Use_Local(CPlayer player, Vector2 worldPos, bool isShift) {
    //     if (player.m_unitPlayer is not CUnitPlayer playeUnit) {
    //         Logging.Error($"(CItem_Weapon.Use_Local) Player doesn't have a unit");
    //         return;
    //     }
    //     CItemVars vars = GetVars(player);
    //     vars.Heating += m_heatingPerShot;
    //     vars.TimeLastUse = GameVars.SimuTimeF;
    //     if (.m_attackDesc.m_bulletDesc != null) {
    //         for (int i = 0; i < this.m_attackDesc.m_nbAttacks; i++) {
    //             SSingleton<SBullets>.Inst.FireBullet(this.m_attackDesc, playeUnit, playeUnit.PosFire, worldPos);
    //         }
    //     }
    //     Vector2 vector = SMisc.GetVector(playeUnit.GetLookingAngle() + 3.1415927f, this.m_attackDesc.m_knockbackOwn);
    //     if (vector.y < 0f) {
    //         vector.y *= 0.33f;
    //     }
    //     playeUnit.Push(vector);
    //     SMisc.PlaySound(this.m_attackDesc.Sound, playeUnit.PosCenter, 1f);
    //     GVars.m_achievNoShoot = false;
    //     if (this == GItems.gunStorm) {
    //         this.GunStorm_Use(playeUnit, worldPos);
    //     }
    //     int2 @int = new int2(worldPos);
    //     if (SWorld.GridRectCam.Contains(worldPos)) {
    //         if (this == GItems.gunZF0 && isShift) {
    //             vars.ZF0TargetLastTimeHit = float.MinValue;
    //             vars.ZF0TargetId = ushort.MaxValue;
    //         }
    //         if (this == GItems.ultimateWaterPistol) {
    //             SWorld.Grid[@int.x, @int.y].m_water += ((!isShift) ? 1f : 20f);
    //             SWorld.Grid[@int.x, @int.y].SetFlag(CCell.Flag_IsLava, false);
    //         }
    //         if (this == GItems.ultimateLavaPistol) {
    //             SWorld.Grid[@int.x, @int.y].m_water += ((!isShift) ? 1f : 20f);
    //             SWorld.Grid[@int.x, @int.y].SetFlag(CCell.Flag_IsLava, true);
    //         }
    //         if (this == GItems.ultimateSpongePistol) {
    //             if (isShift) {
    //                 for (int j = -5; j <= 5; j++) {
    //                     for (int k = -5; k <= 5; k++) {
    //                         SWorld.Grid[@int.x + j, @int.y + k].m_water = 0f;
    //                     }
    //                 }
    //             } else {
    //                 SWorld.Grid[@int.x, @int.y].m_water = 0f;
    //             }
    //         }
    //         if (this == GItems.ultimateTotoroGun) {
    //             if (isShift) {
    //                 for (int l = -3; l <= 3; l++) {
    //                     for (int m = -3; m <= 3; m++) {
    //                         this.UseTotoroGun(@int + new int2(l, m));
    //                     }
    //                 }
    //             } else {
    //                 this.UseTotoroGun(@int);
    //             }
    //         }
    //     }
    // }
}

public abstract class CItemCell : CItem {
    public ushort m_hpMax;
    public Color24 m_mainColor;
    public Anchor m_anchor;
    public bool m_isActivable;
    public bool m_isModeChangeable;
    public bool m_isReversable;
    public int m_electricValue;
    public int m_electricityOutletFlags;
    public bool m_electricVariablePower;
    public Color24 m_light;
    public bool m_fireProof;

    public enum Anchor {
        Bottom_Small,
        Top_Small,
        Back_Small,
        Everyside_Small,
        Everywhere_Small,
        LeftRightBack_Big,
        Everywhere_Big,
        Special,
        NotPlacable
    }
    public enum Anchorable {
        Nowhere,
        Everywhere,
        OnlyAbove
    }

    public CItemCell(ushort hpMax, uint mainColor, Anchor anchor = Anchor.Bottom_Small) {
        m_hpMax = hpMax;
        m_mainColor = Color24.FromNumber(mainColor);
        m_anchor = anchor;
    }
    public virtual bool IsBlock() => false;
    public virtual bool IsBlockDoor() => false;
    public virtual bool IsReceivingForces() => false;
    public virtual Anchorable IsAnchorableBySmall() => Anchorable.Nowhere;
    public virtual Anchorable IsAnchorableByBig() => Anchorable.Nowhere;
    public virtual CStack[]? GetDroppedItems(int2 pos) => null;

    public virtual void Activate_Local(CPlayer player, int2 pos) {
        World.OnSetContent(pos.x, pos.y);
    }
    public virtual void ChangeMode_Local(int2 pos) {
        if (m_isReversable) {
            ref CCell cell = ref World.Grid[pos.x, pos.y];
            cell.SetFlag(CCell.Flag_IsXReversed, !cell.HasFlag(CCell.Flag_IsXReversed));
        }
        World.OnSetContent(pos.x, pos.y, false, null);
    }

    public bool CanItemBeAncheredAt(int2 p) {
        ref readonly CCell currentCell = ref World.Grid[p.x, p.y];
        ref readonly CCell leftCell = ref World.Grid[p.x - 1, p.y];
        ref readonly CCell rightCell = ref World.Grid[p.x + 1, p.y];
        ref readonly CCell bottomCell = ref World.Grid[p.x, p.y - 1];
        ref readonly CCell topCell = ref World.Grid[p.x, p.y + 1];
        CItem_Wall? thisWallItem = this as CItem_Wall;
        if (thisWallItem is not null && thisWallItem.m_type == CItem_Wall.Type.Backwall) {
            if (currentCell.HasBgSurface()) {
                return true;
            }
            for (int i = 1; i < 5; i++) {
                if (World.Grid[p.x, p.y - i].IsContentBlock() || (World.Grid[p.x, p.y - i].GetBackwall() is not null && World.Grid[p.x, p.y - i].HasBgSurface())) {
                    return true;
                }
                if (World.Grid[p.x, p.y - i].GetBackwall() is null) {
                    return false;
                }
            }
            return false;
        } else {
            if (this is CItem_MachineWire) {
                return true;
            }
            if (currentCell.GetContent() is CItem_Wall currentWall && this != currentWall && currentWall.IsReceivingForces() && thisWallItem != null && thisWallItem.IsReceivingForces()) {
                return true;
            }
            if (currentCell.GetContent() != null && currentCell.GetContent() != this) {
                return false;
            }
            if (m_anchor == Anchor.Special && (this == GItems.rocketTank && bottomCell.GetContent() != GItems.rocketEngine && bottomCell.GetContent() != GItems.rocketTank || this == GItems.rocketTop && bottomCell.GetContent() != GItems.rocketTank || this is CItem_Plant && bottomCell.GetContent() is not CItem_Mineral && (this is not CItem_Tree || bottomCell.GetContent() is not CItem_Tree) || this is CItem_Mineral && !currentCell.HasBgSurface())) {
                return false;
            }
            if (m_anchor == Anchor.Back_Small && !currentCell.HasBgSurfaceOrBackwall()) {
                return false;
            }
            if (m_anchor == Anchor.LeftRightBack_Big && !currentCell.HasBgSurface() && leftCell.IsContentAnchorableByBig() != Anchorable.Everywhere && rightCell.IsContentAnchorableByBig() != Anchorable.Everywhere) {
                return false;
            }
            if (m_anchor == Anchor.Bottom_Small && bottomCell.IsContentAnchorableBySmall() == Anchorable.Nowhere) {
                return false;
            }
            if (m_anchor == Anchor.Top_Small && topCell.IsContentAnchorableBySmall() != Anchorable.Everywhere) {
                return false;
            }
            if (m_anchor == Anchor.Everyside_Small && bottomCell.IsContentAnchorableBySmall() == Anchorable.Nowhere && topCell.IsContentAnchorableBySmall() != Anchorable.Everywhere && leftCell.IsContentAnchorableBySmall() != Anchorable.Everywhere && rightCell.IsContentAnchorableBySmall() != Anchorable.Everywhere) {
                return false;
            }
            if (m_anchor == Anchor.Everywhere_Small && !currentCell.HasBgSurfaceOrBackwall() && bottomCell.IsContentAnchorableBySmall() == Anchorable.Nowhere && topCell.IsContentAnchorableBySmall() != Anchorable.Everywhere && leftCell.IsContentAnchorableBySmall() != Anchorable.Everywhere && rightCell.IsContentAnchorableBySmall() != Anchorable.Everywhere) {
                return false;
            }
            if (m_anchor == Anchor.Everywhere_Big && !currentCell.HasBgSurface() && bottomCell.IsContentAnchorableByBig() != Anchorable.Everywhere && topCell.IsContentAnchorableByBig() != Anchorable.Everywhere && leftCell.IsContentAnchorableByBig() != Anchorable.Everywhere && rightCell.IsContentAnchorableByBig() != Anchorable.Everywhere) {
                return false;
            }
            return true;
        }
    }
}

public class CItem_Material : CItemCell {
    public CItem_Material(ushort hpMax = 0, uint mainColor = 16777215U, Anchor anchor = Anchor.NotPlacable)
        : base(hpMax, mainColor, anchor) {
        m_pickupDuration = 180;
    }
}
public class CItem_Mineral : CItemCell {
    // public List<CItem_Plant> m_plantsSupported;
    public bool m_isCrystalGrowingOnWater = false;
    public bool m_canBeMetalDetected = false;

    public CItem_Mineral(ushort hpMax, uint mainColor, bool isReplacable = false)
        : base(hpMax, mainColor, isReplacable ? Anchor.Special : Anchor.NotPlacable) {
        m_pickupDuration = 600;
    }
    public override bool IsBlock() => true;
    public override bool IsReceivingForces() => true;
    public override Anchorable IsAnchorableByBig() => Anchorable.Everywhere;
    public override Anchorable IsAnchorableBySmall() => Anchorable.Everywhere;
    // public override CStack[]? GetDroppedItems(int2 pos) => 
}

public sealed class CItem_Defense : CItemCell {
    public CAttackDesc m_attack;
    public float m_rangeDetection;
    public float m_angleMinDeg;
    public float m_angleMaxDeg;
    public bool m_displayRangeOnCells;
    public Rect m_colRect = new(0.1f, 0f, 0.8f, 0.8f);
    public bool m_neverUnspawn;

    public CItem_Defense(ushort hpMax, uint mainColor, float rangeDetection, float angleMin, float angleMax, CAttackDesc attack)
        : base(hpMax, mainColor, Anchor.Bottom_Small) {
        m_rangeDetection = rangeDetection;
        m_angleMinDeg = angleMin;
        m_angleMaxDeg = angleMax;
        m_attack = attack;
        m_categoryId = "CITEM_DEFENSE";
    }
}
public sealed class CItem_Wall : CItemCell {
    public int m_forceResist;
    public float m_weight;
    public Type m_type;
    public bool m_isDoor;

    public enum Type {
        WallBlock,
        WallPassable,
        Backwall,
        Platform
    }

    public CItem_Wall(ushort hpMax, uint mainColor, int forceResist, float weight, Type type = Type.WallBlock)
        : base(hpMax, mainColor, (type != Type.Platform) ? ((type != Type.Backwall) ? Anchor.Everywhere_Big : Anchor.Special) : Anchor.LeftRightBack_Big) {
        m_forceResist = forceResist;
        m_weight = weight;
        m_type = type;
        m_categoryId = "CITEM_WALL";
    }

    public override void Activate_Local(CPlayer player, int2 pos) {
        if (m_isDoor) {
            bool isOpen = World.Grid[pos.x, pos.y].HasFlag(CCell.Flag_CustomData0);
            if (!isOpen || !UnitManager.IsUnitInCell(pos)) {
                World.Grid[pos.x, pos.y].SetFlag(CCell.Flag_CustomData0, !isOpen);
            }
        }
        base.Activate_Local(player, pos);
    }
}
public class CItem_Machine : CItemCell {
    public float m_customValue;

    public CItem_Machine(ushort hpMax, uint mainColor, Anchor anchor = Anchor.Bottom_Small)
        : base(hpMax, mainColor, anchor) {
        m_categoryId = "CITEM_MACHINE";
    }
}
public sealed class CItem_MachineAutoBuilder : CItem_Machine {
    public CRecipe[] recipes = null!;
    public bool m_allFree;

    public CItem_MachineAutoBuilder() : base(50, 10066329U, Anchor.Bottom_Small) {
        m_isActivable = true;
        m_pickupDuration = -1;
    }
}
public sealed class CItem_MachineWire : CItem_Machine {
    public CItem_MachineWire() : base(0, 10066329U, Anchor.Special) { }

    public static bool CanWireBePlacedAt(int2 p, int wireDir) {
        ref readonly CCell currentCell = ref World.Grid[p.x, p.y];
        ref readonly CCell rightCell = ref World.Grid[p.x + 1, p.y];
        ref readonly CCell topCell = ref World.Grid[p.x + 1, p.y];
        return currentCell.HasBgSurfaceOrBackwall()
            || currentCell.IsContentAnchorableByBig() == Anchorable.Everywhere
            || (wireDir == 0 && (rightCell.HasBgSurfaceOrBackwall() || rightCell.IsContentAnchorableByBig() == Anchorable.Everywhere))
            || (wireDir == 1 && (topCell.HasBgSurfaceOrBackwall() || topCell.IsContentAnchorableByBig() == Anchorable.Everywhere || currentCell.IsContentPlatform()));
    }
}
public sealed class CItem_MachineTeleport : CItem_Machine {
    public CItem_MachineTeleport(ushort hpMax, uint mainColor, Anchor anchor = Anchor.Bottom_Small)
        : base(hpMax, mainColor, anchor) {}
}
public sealed class CLifeConditions {
    public int m_altMin;
    public int m_altMax;
    public int m_lightMin;
    public int m_lightMax;
    public float m_waterAboveMin;
    public float m_waterAboveMax;
    public float m_waterInMineralMin;
    public float m_waterInMineralMax;
    public bool m_nograss;
    public bool m_grass;
    public bool m_isFireProof;
    public CItem_Mineral[] m_minerals;

    public CLifeConditions(int altMin = 0, int altMax = 1024, int lightMin = 0, int lightMax = 1, float waterAboveMin = 0f, float waterAboveMax = 10f, float waterInMineralMin = 0f, float waterInMineralMax = 1f, bool okIfNoGrass = true, bool okIfGrass = true, bool isFireProof = false, params CItem_Mineral[] minerals) {
        m_altMin = altMin;
        m_altMax = altMax;
        m_lightMin = lightMin;
        m_lightMax = lightMax;
        m_waterAboveMin = waterAboveMin;
        m_waterAboveMax = waterAboveMax;
        m_waterInMineralMin = waterInMineralMin;
        m_waterInMineralMax = waterInMineralMax;
        m_nograss = okIfNoGrass;
        m_grass = okIfGrass;
        m_minerals = minerals;
        m_isFireProof = isFireProof;
    }
}
public sealed class CItem_MineralDirt : CItem_Mineral {
    public CLifeConditions? m_grassConditions;

    public CItem_MineralDirt(ushort hpMax, uint mainColor, CLifeConditions? grassConditions = null)
        : base(hpMax, mainColor, isReplacable: true) {
        m_grassConditions = grassConditions;
    }
}
public class CItem_Plant : CItem_Material {
    public CLifeConditions m_conditions;
    public int m_nbNearbyPlantsMax;
    public float m_spontaneousGrowAdditionnalChance;
    // private CStack[] m_droppedItemDead;

    public CItem_Plant(CLifeConditions conditions, ushort hpMax, uint mainColor = 16777070U, int nbNearbyPlantsMax = 1)
        : base(hpMax, mainColor, Anchor.Special) {
        m_conditions = conditions;
        m_nbNearbyPlantsMax = nbNearbyPlantsMax;
        // m_droppedItemDead = new CStack[]
        // {
        //     new CStack(GItems.deadPlant, 1, 1f)
        // };
        m_pickupDuration = 60;
    }
}
public sealed class CItem_Tree : CItem_Plant {
    public int m_heightMax = 4;
    private CStack[] m_droppedItemYoung;
    private CStack[] m_droppedItem;

    public CItem_Tree(CLifeConditions conditions, ushort hpMax, uint mainColor, CItem droppedWood, int height = 4, int nbNearbyPlantsMax = 0)
        : base(conditions, hpMax, mainColor, nbNearbyPlantsMax) {
        m_heightMax = height;
        m_droppedItemYoung = [];
        m_droppedItem =
        [
            new CStack(this, probability: 0.3f),
            new CStack(droppedWood, probability: 1f)
        ];
    }

    public override CStack[] GetDroppedItems(int2 pos) {
        return World.Grid[pos.x, pos.y - 1].IsPassable() ? m_droppedItem : m_droppedItemYoung;
    }
}

public struct CItem_PluginData {
    public float m_weight;
    public int m_electricValue;
    public int m_electricOutletFlags;
    public int m_elecSwitchType;
    public int m_elecVariablePower;
    public int m_anchor;
    public Color24 m_light;
    public int m_isBlock;
    public int m_isBlockDoor;
    public int m_isReceivingForces;
    public int m_isMineral;
    public int m_isDirt;
    public int m_isPlant;
    public int m_isFireProof;
    public int m_isWaterGenerator;
    public int m_isWaterPump;
    public int m_isLightGenerator;
    public int m_isBasalt;
    public int m_isLightonium;
    public int m_isOrganicHeart;
    public int m_isSunLamp;
    public int m_isAutobuilder;
    public float m_customValue;
}
