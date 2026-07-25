using System;
using System.Collections.Generic;
using System.Linq;
using DODModAPI;
using DODModAPI.Extensions;
using UnityEngine;

internal static class ItemHelpers {
    public static void DoShockWave(Vector2 center, float radius, float damage, float knockbackTarget) {
        float radiusSqr = radius * radius;
        foreach (CUnit unit in SUnits.Units) {
            if (unit is null || !unit.IsAlive()) { continue; }
            if ((unit.PosCenter - center).sqrMagnitude > radiusSqr) { continue; }

            var distanceFactor = Mathf.Clamp01(1f - Misc.Sqr((unit.PosCenter - center).magnitude / radius));

            var appliedDamage = Mathf.Max(1f, damage * distanceFactor * unit.GetArmorMult() - unit.GetArmor());
            unit.Damage(appliedDamage, attacker: null, showDamage: true, damageCause: "");
            unit.Push((unit.PosCenter - center).normalized * knockbackTarget * distanceFactor);
        }
    }
    public static int2 FindInCircleClamped(int range, int2 pos, Func<int, int, bool> fn) {
        int sqrRange = range * range;
        int minX = Math.Max(pos.x - range, 0), maxX = Math.Min(pos.x + range, SWorld.Gs.x - 1);
        int minY = Math.Max(pos.y - range, 0), maxY = Math.Min(pos.y + range, SWorld.Gs.y - 1);
        for (int i = minX; i <= maxX; ++i) {
            for (int j = minY; j <= maxY; ++j) {
                int2 relative = new int2(i, j) - pos;
                if (relative.sqrMagnitude <= sqrRange) {
                    if (fn(i, j)) {
                        return new int2(i, j);
                    }
                }
            }
        }
        return int2.negative;
    }
    public static void ForEachInCircleClamped(int range, int2 pos, Action<int, int> fn) {
        int sqrRange = range * range;
        int minX = Math.Max(pos.x - range, 0), maxX = Math.Min(pos.x + range, SWorld.Gs.x - 1);
        int minY = Math.Max(pos.y - range, 0), maxY = Math.Min(pos.y + range, SWorld.Gs.y - 1);
        for (int i = minX; i <= maxX; ++i) {
            for (int j = minY; j <= maxY; ++j) {
                int2 relative = new int2(i, j) - pos;
                if (relative.sqrMagnitude <= sqrRange) {
                    fn(i, j);
                }
            }
        }
    }
}

public sealed class ExtCItem_Collector : CItem_Defense {
    public ExtCItem_Collector(CTile? tile, CTile? tileIcon, ushort hpMax, uint mainColor, float rangeDetection, float angleMin, float angleMax, CAttackDesc attack, CTile tileUnit)
        : base(tile, tileIcon, hpMax, mainColor, rangeDetection, angleMin, angleMax, attack, tileUnit) {
        m_attack.m_damage = 0;
    }

    public ushort collectorDamage = 0;
    public bool isBasaltCollector = false;
}
public sealed class ExtCItem_Explosive : CItem_Defense {
    public ExtCItem_Explosive(CTile? tile, CTile? tileIcon, ushort hpMax, uint mainColor, float rangeDetection, float angleMin, float angleMax, CAttackDesc attack, CTile? tileUnit)
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
    public float explosionFlashIntensity = 0f;
    public float explosionFireAroundRadius = 0f;

    public static WeakTable<CUnitDefense, float> lastTimeWeakTable = [];

    public void DoFireAround(Vector2 pos) {
        if (this.explosionFireAroundRadius <= 0f) { return; }

        SWorld.SetFireAround(pos, this.explosionFireAroundRadius);

        var radiusSqr = this.explosionFireAroundRadius * this.explosionFireAroundRadius;
        foreach (CUnit unit in SUnits.Units) {
            if (unit is null || !unit.IsAlive() || unit.m_uDesc.m_immuneToFire) {
                continue;
            }
            if ((unit.PosCenter - pos).sqrMagnitude <= radiusSqr
                && SMiscCols.CheckCol_SegmentGround(pos, unit.PosCenter) == Vector2.zero) {
                unit.m_burnStartTime = GVars.SimuTime;
            }
        }
    }
    public void DoShockWave(Vector2 pos) {
        if (this.shockWaveRange <= 0f) { return; }

        ItemHelpers.DoShockWave(pos, this.shockWaveRange, this.shockWaveDamage, this.shockWaveKnockback);
    }
    public void DoFlashEffect(Vector2 pos) {
        if (this.explosionFlashIntensity <= 0f) { return; }

        if ((G.m_player.PosCenter - pos).sqrMagnitude <= this.m_attack.m_range * this.m_attack.m_range * 6) {
            FlashEffect.TriggerFlash(this.explosionFlashIntensity);
        }
    }
    public void DoExplosionLavaRelease(ref CCell currentCell) {
        if (this.lavaReleaseTime >= 0f) { return; }

        Misc.AddLava(ref currentCell, this.lavaQuantity);
    }
    public void DoExplosionBgChange(int2 posCell) {
        var range = this.destroyBackgroundRadius + this.explosionBasaltBgRadius;
        if (range <= 0) { return; }

        var destroyBackgroundRadiusSqr = this.destroyBackgroundRadius * this.destroyBackgroundRadius;
        for (int i = posCell.x - range; i <= posCell.x + range; ++i) {
            for (int j = posCell.y - range; j <= posCell.y + range; ++j) {
                int2 relative = new int2(i, j) - posCell;
                if (relative.sqrMagnitude > range * range) {
                    continue;
                }
                if (!Misc.IsInWorld(i, j)) { continue; }

                ref var cell = ref SWorld.Grid[i, j];
                if (!cell.IsPassable()) { continue; }

                if (relative.sqrMagnitude > destroyBackgroundRadiusSqr) {
                    var bgSurface = cell.GetBgSurface();
                    if (bgSurface is not null && bgSurface != GSurfaces.bgOrganic) {
                        cell.SetBgSurface(GSurfaces.bgLava);
                    }
                } else {
                    cell.SetBgSurface(null);
                }
            }
        }
    }
    public void StartVolcanoEruption() {
        if (!this.alwaysStartEruption || (GVars.m_eruptionTime != 0f && GVars.SimuTime <= GVars.m_eruptionTime + SOutgame.Params.m_eruptionDurationTotal)) {
            return;
        }
        SAudio.Get("lavaEruption").Play(G.m_player.Pos, volumeMult: 1.5f);
        GVars.m_eruptionStartPressure = SGame.LavaPressure;
        GVars.m_eruptionTime = GVars.SimuTime;
    }
    public void DoDamageAround(Vector2 explosionPos, CAttackDesc attack) {
        SUnits.DoDamageAOE(explosionPos, attack.m_range, attack.m_damage);
        SWorld.DoDamageAOE(explosionPos, (int)attack.m_range, attack.m_damage);

        SParticles.common_Explosion.EmitNb(explosionPos, nb: 150, isLighted: false, speed: 50f);
        SParticles.common_Smoke.EmitNb(explosionPos, nb: 120, isLighted: false, speed: 60f);
    }
    public void PlayExplosionSound(CSound sound, Vector2 explosionPos) {
        sound.Play(explosionPos, this.explosionSoundMultiplier);
    }
    public void DestoryItself(int2 posCell) {
        SSingleton<SWorld>.Inst.DestroyCell(posCell, loot: 0);
        if (this.indestructible) {
            SSingleton<SWorld>.Inst.DestroyCell(posCell - int2.up, loot: 0);
        }
    }
    public static void ExplosiveLogic(CUnitDefense self) {
        if (self.GetLastFireTime() <= 0f) {
            return;
        }
        var item = (ExtCItem_Explosive)self.m_item;

        ref var currentCell = ref SWorld.Grid[self.PosCell.x, self.PosCell.y];

        if (item.lavaReleaseTime >= 0
            && GVars.SimuTime >= self.GetLastFireTime() + item.lavaReleaseTime
            && GVars.SimuTime > ExtCItem_Explosive.lastTimeWeakTable.GetValueOrDefault(self)
        ) {
            const float dt = ExtCItem_Explosive.deltaTime;
            ExtCItem_Explosive.lastTimeWeakTable.AddOrUpdate(self, GVars.SimuTime + dt);

            float releaseTime = GVars.SimuTime - (self.GetLastFireTime() + item.lavaReleaseTime);
            float completionPercentage = releaseTime / (item.explosionTime - item.lavaReleaseTime);

            Misc.AddLava(ref currentCell, item.lavaQuantity * Mathf.Pow(3f, releaseTime));

            var fireRange = Mathf.Lerp(0f, item.m_attack.m_range * 5f, completionPercentage);
            SWorld.SetFireAround(self.PosCell, fireRange);

            var evaporationRange = Mathf.Lerp(0f, item.m_attack.m_range * 4f, completionPercentage);
            EvaporateWaterAround(Mathf.CeilToInt(evaporationRange), self.PosCell, evaporationRate: 0.6f);

            var destructionRange = Mathf.CeilToInt(Mathf.Lerp(0f, item.m_attack.m_range / 3f, completionPercentage));
            SWorld.DoDamageAOE(self.Pos, destructionRange, Misc.CeilDiv(item.m_attack.m_damage, 20));
        }
        if (GVars.m_simuTimeD <= (double)(self.GetLastFireTime() + item.explosionTime)) {
            return;
        }
        item.DestoryItself(self.PosCell);
        item.DoDamageAround(self.PosCenter, item.m_attack);
        item.PlayExplosionSound(item.m_attack.Sound, self.PosCenter);
        item.StartVolcanoEruption();
        item.DoExplosionBgChange(self.PosCell);
        item.DoExplosionLavaRelease(ref currentCell);
        item.DoFlashEffect(self.PosCenter);
        item.DoShockWave(self.m_pos);
        item.DoFireAround(self.m_pos);
    }

    private static void EvaporateWaterAround(int range, int2 pos, float evaporationRate) {
        int sqrRange = range * range;
        RectInt rect = Misc.ClampRect(new RectInt(pos.x - range, pos.y - range, range * 2, range * 2), 0, 0, SWorld.Gs.x, SWorld.Gs.y);
        for (int i = rect.x; i <= rect.xMax; ++i) {
            for (int j = rect.y; j <= rect.yMax; ++j) {
                if ((new int2(i, j) - pos).sqrMagnitude <= sqrRange) {
                    ref var cell = ref SWorld.Grid[i, j];
                    if (!cell.IsLava() && cell.m_water > 0) {
                        cell.m_water = Mathf.Max(0f, cell.m_water - SMain.SimuDeltaTime * evaporationRate);
                    }
                }
            }
        }
    }
}

public sealed class ExtCBulletDesc : ModBulletDesc {
    public int explosionBasaltBgRadius = 0;
    public bool emitLavaBurstParticles = true;
    public float shockWaveRange = 0f;
    public float shockWaveKnockback = 0f;
    public float shockWaveDamage = 0f;

    public float explosionEnergyRadius = 0f;
    public float explosionEnergyDamage = 0f;

    public ExtCBulletDesc(ModSprite sprite, float radius, float dispersionAngleRad, float speedStart, float speedEnd, uint light = 0)
        : base(sprite, radius, dispersionAngleRad, speedStart, speedEnd, light) {}

    public void DoEnergyExplosion(CBullet bullet) {
        SUnits.DoDamageAOE(bullet.m_pos, explosionEnergyRadius, explosionEnergyDamage,
            strikeMode: SItems.LightningStrikeMode.Big);
    }
}

public sealed class ExtCItem_IndestructibleMineral : CItem_Mineral {
    public ExtCItem_IndestructibleMineral(CTile? tile, CTile? tileIcon, ushort hpMax, uint mainColor, CSurface surface, bool isReplacable = false)
        : base(tile, tileIcon, hpMax, mainColor, surface, isReplacable) { }
}

public sealed class ExtCItem_FertileMineralDirt : CItem_MineralDirt {
    public ExtCItem_FertileMineralDirt(
        CTile? tile, CTile? tileIcon, ushort hpMax, uint mainColor, CSurface surface, CLifeConditions? grassConditions = null
    ) : base(null, null, hpMax, mainColor, surface, grassConditions) {
        this.m_tile = tile;
        this.m_tileIcon = tileIcon;
    }

    public override void Init() {
        base.Init();
        m_plantsSupported = inheritedPlantsSupported?.SelectMany(x => x.m_plantsSupported ?? []).ToList();
        // System.Console.WriteLine($"RESULT: {string.Join(", ", m_plantsSupported.Select(x => x.Name).ToArray())}");
    }

    public float plantGrowChange;
    public CItem_Mineral[] inheritedPlantsSupported = null!;
}

public sealed class ExtCItem_ConditionalMachineAutoBuilder : CItem_MachineAutoBuilder {
    public ExtCItem_ConditionalMachineAutoBuilder(CTile? tile, CTile? tileIcon) : base(tile, tileIcon) { }

    public override void Init() {
        // skip creating a sprite for m_tileAlternative
        var citemCellInitFn = Misc.CreateNonVirtualDelegate<Action, CItemCell>(this, typeof(CItemCell).Method("Init"));
        citemCellInitFn();
    }

    public delegate bool CheckConditionFn(int x, int y);

    public CheckConditionFn? checkCondition = null;
}
public sealed class ExtCItem_ConsumableWeapon : CItem_Weapon {
    public ExtCItem_ConsumableWeapon(CTile? tile, CTile? tileIcon, float heatingPerShot, bool isAuto, CAttackDesc attackDesc)
        : base(tile, tileIcon, heatingPerShot, isAuto, attackDesc) { }

    public override void Use_Local(CPlayer player, Vector2 worldPos, bool isShift) {
        CStack stack = player.m_inventory.GetStack(this);
        if (stack is null || stack.m_nb <= 0) {
            return;
        }
        stack.m_nb -= 1;

        base.Use_Local(player, worldPos, isShift);
    }
}
public sealed class ExtCItem_JetpackDevice : CItem_Device {
    public ExtCItem_JetpackDevice(CTile? tile, CTile? tileIcon, bool isInfinite = false)
        : base(tile, tileIcon, DODModAPI.DeviceGroupIds.jetpack, CItem_Device.Type.Passive, isInfinite ? 1f : 0f) { }

    public float jetpackEnergyUsageMultiplier = 0.19f;
    public float jetpackFlyForce = 85f;
}
public sealed class ExtCItem_ImpactShield : CItem_Device {
    public static readonly string GroupId = "more-items_ImpactShield";

    public ExtCItem_ImpactShield(CTile tile, float customValue = 0f)
        : base(tile, null, GroupId, CItem_Device.Type.Passive, customValue) { }
}
public sealed class ExtCUnitWaterVaporizer : CUnit {
    private ExtCUnitWaterVaporizer(CDesc desc, Vector2 pos)
        : base(desc, pos) {}

    public override bool Update() {
        ref var cell = ref SWorld.Grid[PosCell.x, PosCell.y];
        if (cell.GetContent() is not ExtCItem_WaterVaporizer waterVaporizerItem) { return false; }

        if (cell.IsPowered() && !cell.IsLava()) {
            cell.m_water = Mathf.Max(cell.m_water - waterVaporizerItem.evaporationRate * SMain.SimuDeltaTime, 0f);
        }
        return true;
    }

    public new class CDesc : CUnit.CDesc {
        public CDesc(int tier, float speed, Vector2 size, int hpMax, int armor)
            : base(tier, speed, size, hpMax, armor) { }

        public override void LoadSprites(string texture) { }
    }
}
public sealed class ExtCItem_WaterVaporizer : CItem_Machine {
    public ExtCItem_WaterVaporizer(CTile? tile, CTile? tileIcon, ushort hpMax, uint mainColor)
        : base(tile, tileIcon, hpMax, mainColor, CItemCell.Anchor.Bottom_Small) { }

    public float evaporationRate;
}
public sealed class ExtCItem_CeilingTurret : CItem_Defense {
    public ExtCItem_CeilingTurret(CTile tile, ushort hpMax, uint mainColor, float rangeDetection, float angleMin, float angleMax, CAttackDesc attack)
        : base(tile, tile, hpMax, mainColor, rangeDetection, angleMin, angleMax, attack, tileUnit: null) {
        m_anchor = CItemCell.Anchor.Top_Small;
    }
}
public sealed class ExtCItem_SpikesTurret : CItem_Defense {
    public ExtCItem_SpikesTurret(CTile tile, ushort hpMax, uint mainColor, float rangeDetection, float angleMin, float angleMax, CAttackDesc attack)
        : base(tile, tile, hpMax, mainColor, rangeDetection, angleMin, angleMax, attack, tileUnit: null) { }
}
public sealed class ExtCItem_MetalDetector : CItem_Device {
    public ExtCItem_MetalDetector(CTile tile, float range)
        : base(tile, tile, DeviceGroupIds.metalDetector, CItem_Device.Type.Activable, range) { }

    public CItemCell[] detectableItems = [];

    public override void OnDisplayHud(SScreen screen) {
        CItemVars myVars = base.GetMyVars();
        float pulseDuration = this.m_customValue / 80f;
        float timeSinceLastUse = GVars.SimuTime - myVars.TimeLastUse;

        List<Vector2> itemsDetectedPos = myVars.ItemsDetectedPos;
        if (itemsDetectedPos is null || timeSinceLastUse < 0f || timeSinceLastUse >= pulseDuration * 2f) {
            return;
        }
        CAssetSprite assetSprite = SResources.GetSprite("particles/particles_big", "radar");
        foreach (var itemDetectedPos in itemsDetectedPos) {
            float travelTime = (itemDetectedPos - itemsDetectedPos[0]).magnitude / 80f;

            if (timeSinceLastUse < travelTime || timeSinceLastUse >= travelTime + pulseDuration) {
                continue;
            }
            if (timeSinceLastUse < travelTime + SMain.SimuDeltaTime) {
                SAudio.Get("ceilingTurret").Play(itemDetectedPos, 1f);
            }

            Vector2 guiPosition = SMisc.WorldToGuiPoint(itemDetectedPos);
            float pulseRadius = SMisc.WorldToGuiDist((timeSinceLastUse - travelTime) * 80f);

            Color pulseColor = new Color(1f, 1f, 1f,
                0.5f * (1f - travelTime / pulseDuration) * Mathf.Clamp01((travelTime + pulseDuration - timeSinceLastUse) / pulseDuration));
            Rect guiRect = new Rect(
                x: guiPosition.x - pulseRadius, y: guiPosition.y - pulseRadius,
                height: 2f * pulseRadius, width: 2f * pulseRadius);

            CMesh<CMeshSprite>.Get(screen, false).DrawGui(guiRect, assetSprite.Sprite, pulseColor);
        }
    }
    public override void Use_Local(CPlayer player, Vector2 mousePos, bool isShift) {
        CStack stack = player.m_inventory.GetStack(this);
        CUnitPlayer unitPlayer = player.m_unitPlayer;
        if (m_groupId is null || stack is null || stack.m_nb <= 0 || unitPlayer is null) {
            return;
        }
        CItemVars vars = base.GetVars(player);
        List<Vector2> detectedPositions = [unitPlayer.PosCenter];

        float range = SInputs.shift.IsKey() ? this.m_customValue / 3f : this.m_customValue;
        float detectionRangeSqr = range * range;
        int detectionRange = Mathf.CeilToInt(range);
        SMisc.DrawRect(SWorld.GridRectCam, Color.red, 1f);

        RectInt scanRect = Misc.RectIntIntersection(Misc.MakeCenterRectInt(unitPlayer.PosCell, detectionRange), Misc.WorldIntRects.GridRectCamInt);
        byte tag = (byte)(Time.frameCount & 255);

        for (int x = scanRect.x; x <= scanRect.xMax; x++) {
            for (int y = scanRect.y; y <= scanRect.yMax; y++) {
                int2 cellPos = new int2(x, y);

                ref CCell cell = ref SWorld.Grid[x, y];
                if (cell.m_temp.r == tag) { continue; }

                CItemCell itemCell = cell.GetContent();
                if (itemCell is null || !detectableItems.Contains(itemCell) || (cellPos - unitPlayer.PosCell).sqrMagnitude >= detectionRangeSqr) {
                    continue;
                }
                detectedPositions.Add(new Vector2(cellPos.x + UnityEngine.Random.value, cellPos.y + UnityEngine.Random.value));

                cell.m_temp.r = tag;
                _bfsQueue.Enqueue(cellPos);
                while (_bfsQueue.Count > 0) {
                    int2 current = _bfsQueue.Dequeue();
                    foreach (int2 dir in SMisc.Dirs0to3) {
                        int2 neighborPos = current + dir;
                        if (!scanRect.Contains(neighborPos)) { continue; }

                        ref CCell neighborCell = ref SWorld.Grid[neighborPos.x, neighborPos.y];
                        if (neighborCell.m_temp.r == tag) { continue; }

                        CItemCell neighborItem = neighborCell.GetContent();
                        if (neighborItem is not null && detectableItems.Contains(neighborItem)) {
                            neighborCell.m_temp.r = tag;
                            _bfsQueue.Enqueue(neighborPos);
                        }
                    }
                }
            }
        }
        vars.ItemsDetectedPos = detectedPositions;
        this.ActivateDevice(player, stack, SAudio.Get("ceilingTurret"));
    }

    private static readonly Queue<int2> _bfsQueue = new();
}
public sealed class ExtDuoCAttackDesc : CAttackDesc {
    public ExtDuoCAttackDesc(float range, int damage, int nbAttacks = 0, float cooldown = 0f, float knockbackOwn = 0f, float knockbackTarget = 0f, CBulletDesc? projDesc = null, string? sound = null)
        : base(range, damage, nbAttacks, cooldown, knockbackOwn, knockbackTarget, projDesc, sound) { }

}
