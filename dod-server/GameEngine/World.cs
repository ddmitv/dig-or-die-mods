using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace GameEngine;

[Serializable]
public struct CCell {
    public uint m_flags;
    public ushort m_contentId;
    public ushort m_contentHP;
    public short m_forceX;
    public short m_forceY;
    public float m_water;
    public Color24 m_light;
    public byte m_elecProd;
    public byte m_elecCons;
    public Color24 m_temp;

    public static readonly uint Flag_CustomData0 = 1U;
    public static readonly uint Flag_CustomData1 = 2U;
    public static readonly uint Flag_CustomData2 = 4U;
    public static readonly uint Flag_IsXReversed = 16U;
    public static readonly uint Flag_IsBurning = 32U;
    public static readonly uint Flag_IsMapped = 64U;
    public static readonly uint Flag_BackWall_0 = 256U;
    public static readonly uint Flag_BgSurface_0 = 512U;
    public static readonly uint Flag_BgSurface_1 = 1024U;
    public static readonly uint Flag_BgSurface_2 = 2048U;
    public static readonly uint Flag_WaterFall = 4096U;
    public static readonly uint Flag_StreamLFast = 8192U;
    public static readonly uint Flag_StreamRFast = 16384U;
    public static readonly uint Flag_IsLava = 32768U;
    public static readonly uint Flag_HasWireRight = 65536U;
    public static readonly uint Flag_HasWireTop = 131072U;
    public static readonly uint Flag_ElectricAlgoState = 262144U;
    public static readonly uint Flag_IsPowered = 524288U;

    public readonly byte Light => (byte)Math.Clamp((m_light.r + m_light.g + m_light.b) / 3, 0, 255);

    public readonly CItemCell? GetContent() {
        if (!GItems.TryGetItemOrNull(m_contentId, out CItemCell? item)) {
            Logging.Error($"(World.GetContent) Found cell with invalid item id {m_contentId}, replacing it with null");
            return null;
        }
        return item;
    }
    public readonly bool HasFlag(uint flag) {
        return (m_flags & flag) != 0;
    }
    public void SetFlag(uint flag, bool value) {
        m_flags = value ? (m_flags | flag) : (m_flags & ~flag);
    }

    public readonly bool IsPassable() {
        var itemData = GItems.itemsPluginData;
        return itemData[m_contentId].m_isBlock == 0
            || (itemData[m_contentId].m_isBlockDoor != 0 && (m_flags & Flag_CustomData0) != 0);
    }
    public readonly bool IsContentBlock() {
        return !IsPassable();
    }

    public readonly bool IsContentBlockOrPlat() {
        return IsContentBlock() || GetContent() is CItem_Wall { m_type: CItem_Wall.Type.Platform };
    }
    public readonly bool IsLava() {
        return (m_flags & Flag_IsLava) != 0;
    }
    public readonly bool IsLava(float minValue) {
        return (m_flags & Flag_IsLava) != 0 && m_water >= minValue;
    }

    public readonly CItem_Wall? GetBackwall() {
        return HasFlag(Flag_BackWall_0) ? GItems.backwall : null;
    }

    public readonly bool IsContentAddingForces() {
        return !HasBgSurface() && (GetContent()?.IsReceivingForces() ?? false);
    }

    public readonly bool HasBgSurface() {
        return (m_flags & (Flag_BgSurface_0 | Flag_BgSurface_1 | Flag_BgSurface_2)) != 0U;
    }

    public void SetCustomData(uint data) {
        m_flags &= ~(Flag_CustomData0 | Flag_CustomData1 | Flag_CustomData2);
        m_flags |= (data << 9);
    }

    public readonly CItemCell.Anchorable IsContentAnchorableBySmall() {
        return GetContent()?.IsAnchorableBySmall() ?? CItemCell.Anchorable.Nowhere;
    }
    public readonly CItemCell.Anchorable IsContentAnchorableByBig() {
        return GetContent()?.IsAnchorableByBig() ?? CItemCell.Anchorable.Nowhere;
    }
    public readonly bool HasBgSurfaceOrBackwall() {
        return (m_flags & (Flag_BgSurface_0 | Flag_BgSurface_1 | Flag_BgSurface_2 | Flag_BackWall_0)) != 0U;
    }
    public readonly bool IsContentPlatform() {
        return GetContent() is CItem_Wall { m_type: CItem_Wall.Type.Platform };
    }
    public readonly bool IsWaterS() {
        return (m_flags & Flag_WaterFall) == 0U && !IsContentBlock() && m_water > 0.001f;
    }
    public readonly bool IsWaterF() {
        return (m_flags & Flag_WaterFall) != 0U && !IsContentBlock() && m_water > 0.001f;
    }
}

public static class World {
    public record struct GridProxy(CCell[,] array) {
        public static implicit operator CCell[,](GridProxy self) => self.array;

        public readonly ref CCell this[int i, int j] {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get {
                if (i < 0 || i >= gridWidth || j < 0 || j >= gridHeight) {
                    ThrowHelper(i, j);
                }
                return ref Unsafe.Add(
                    ref Unsafe.As<byte, CCell>(ref MemoryMarshal.GetArrayDataReference(array)),
                    i + gridWidth * j
                );
            }
        }
        public readonly ref CCell this[int2 pos] {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref this[pos.x, pos.y];
        }
        [DoesNotReturn]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowHelper(int i, int j) {
            throw new IndexOutOfRangeException($"World.Grid coordinate was outside the world (x={i}, y={j})");
        }
    }

    private const int m_invincibleBlockYmax = 13;

    private const int gridWidth = 1024;
    private const int gridHeight = 1024;

    private static readonly int2 gridSize = new(gridWidth, gridHeight);

    public const int ChunkSize = 4;
    private static readonly int2 m_chunksSize = new(gridSize.x / ChunkSize, gridSize.y / ChunkSize);
    private static float[,]? m_chunksUpdateTime;
    private static float m_lastTimeChunksSent = -1f;
    private static float m_lastTimeWaterSent = -1f;
    private static float m_lastMinimapUpdate = -1f;
    private const float MinimapUpdatePeriod = 3f;

    public static GridProxy Grid { get; } = new(new CCell[gridWidth, gridHeight]);
    public static int2 Gs => gridSize;

    public static void Init() {
        InitChunksSendTime(ref m_chunksUpdateTime);
        // for (int i = 0; i < Gs.x; ++i) {
        //     for (int j = 0; j < Gs.y; ++j) {
        //         grid[i, j].m_water = 0f;
        //     }
        // }
    }

    public static void Update() {
        if (GVars.SimuTime > m_lastTimeChunksSent) {
            m_lastTimeChunksSent = GVars.SimuTime + 0.1f;

            int2 worldMaxPos = Gs - int2.one;
            foreach (CPlayer player in PlayerManager.players) {
                if (player.m_unitPlayer is null || player.networkClient is null) { continue; }

                if (player.m_chunksSendTime is null) {
                    InitChunksSendTime(ref player.m_chunksSendTime);
                }
                Vector2 viewBoundsSize = 12f * new Vector2(player.m_cameraAspect + 1f, 1f + 1f);
                Vector2 playerPos = player.m_unitPlayer.m_pos;
                int2 minPos = new((int)Math.Max(0f, playerPos.x - viewBoundsSize.x) / 4, (int)Math.Max(0f, playerPos.y - viewBoundsSize.y) / 4);
                int2 maxPos = new((int)Math.Min(worldMaxPos.x, playerPos.x + viewBoundsSize.x) / 4, (int)Math.Min(worldMaxPos.y, playerPos.y + viewBoundsSize.y) / 4);
                MessageProcessing.Send(player.networkClient, new MessageServerWorld_OnChange(player, minPos, maxPos, m_chunksUpdateTime!));
            }
        }
        if (GVars.SimuTime > m_lastTimeWaterSent) {
            m_lastTimeWaterSent = GVars.SimuTime + 0.3f;

            foreach (CPlayer player in PlayerManager.players) {
                if (player.m_unitPlayer is null || player.networkClient is null) { continue; }

                MessageProcessing.Send(player.networkClient, new MessageServerWorld_Water(player.GetRectAroundScreen(5)));
            }
        }
        UpdateMinimap();
    }

    private static void UpdateMinimap() {
        if (Game.SimuTime <= m_lastMinimapUpdate) {
            return;
        }
        m_lastMinimapUpdate = Game.SimuTime + MinimapUpdatePeriod;

        foreach (var player in PlayerManager.players) {
            if (!player.HasUnitPlayerAlive()) {
                continue;
            }
            RectInt screenRect = player.GetRectAroundScreen(0);
            for (int i = screenRect.y; i < screenRect.yMax; i++) {
                for (int j = screenRect.x; j < screenRect.xMax; j++) {
                    ref CCell cell = ref Grid[i, j];
                    // if (cell.Light > 15 || !cell.HasBgSurface()) {
                    //     cell.m_flags |= CCell.Flag_IsMapped;
                    // }
                    cell.m_flags |= CCell.Flag_IsMapped;
                }
            }
        }
    }

    private static void InitChunksSendTime(ref float[,]? chunks) {
        chunks = new float[m_chunksSize.x, m_chunksSize.y];
        float simuTime = GVars.SimuTime;
        for (int i = 0; i < m_chunksSize.x; i++) {
            for (int j = 0; j < m_chunksSize.y; j++) {
                chunks[i, j] = simuTime;
            }
        }
    }

    public static bool DoDamageToCell(int2 cellPos, ushort damage, int loot, bool showDamage = false, CUnitPlayer? attacker = null) {
        ref CCell cell = ref Grid[cellPos];
        if (cellPos.y < m_invincibleBlockYmax || cell.m_contentHP <= 0) {
            return false;
        }
        cell.m_contentHP = (ushort)Math.Max(cell.m_contentHP - damage, 0);
        if (cell.m_contentHP == 0) {
            DestroyCell(cellPos, loot, false, attacker);
        }
        OnSetContent(cellPos.x, cellPos.y);
        return cell.m_contentHP == 0;
    }

    public static void RepairCell(int2 cellPos, ushort repairHp) {
        ref CCell cell = ref Grid[cellPos];
        if (cell.GetContent()?.m_hpMax is not ushort cellMaxHp) {
            Logging.Error($"(World.RepairCell) trying to repair an invalid cell at {cellPos} with content id {cell.m_contentId}");
            return;
        }
        cell.m_contentHP = Math.Min((ushort)(cell.m_contentHP + repairHp), cellMaxHp);
        if (cell.m_contentHP == cellMaxHp) {
            cell.SetFlag(CCell.Flag_IsBurning, false);
        }
        OnSetContent(cellPos.x, cellPos.y);
    }

    public static void DestroyCell(int2 pos, int loot, bool isBackwall = false, CUnitPlayer? destroyer = null) {
        ref readonly CCell cell = ref Grid[pos];
        CItemCell? item = isBackwall ? cell.GetBackwall() : cell.GetContent();
        if (item is not null && (loot == 2 || item.m_pickupDuration < 0)) {
            CStack[]? droppedItems = item.GetDroppedItems(pos);
            if (droppedItems is null) {
                PickupManager.CreatePickup(item, 1, (Vector2)pos + Vector2.one * 0.5f, moveToPlayer: destroyer);
            } else {
                foreach (CStack droppedItem in droppedItems) {
                    if (Random.Float() < droppedItem.m_probability) {
                        PickupManager.CreatePickup(droppedItem.m_item, droppedItem.m_nb, (Vector2)pos + Vector2.one * 0.5f, moveToPlayer: destroyer);
                    }
                }
            }
        }
        SetContent(pos, null, isBackwall);
        CItem_Wall? backwall = Grid[pos.x, pos.y + 1].GetBackwall();
        if (backwall is not null && !backwall.CanItemBeAncheredAt(pos + int2.up)) {
            DestroyCell(pos + int2.up, (loot != 1) ? loot : 2, true, destroyer);
        }
        if (isBackwall) {
            if (cell.GetContent() is not null) {
                DestroyCell(pos, (loot != 1) ? loot : 2, false, destroyer);
            }
        } else {
            foreach (int2 dir in GameState.Dirs0to3) {
                int2 nearPos = pos + dir;
                CItemCell? content = Grid[nearPos].GetContent();
                if (content is not null && !content.CanItemBeAncheredAt(nearPos)) {
                    DestroyCell(nearPos, (loot != 1) ? loot : 2, false, destroyer);
                }
            }
            if (cell.GetContent() is CItem_Tree && Grid[pos.x, pos.y - 1].GetContent() == cell.GetContent()) {
                DestroyCell(new int2(pos.x, pos.y - 1), (loot != 1) ? loot : 2, false, destroyer);
            }
        }
        for (int i = 0; i < 4; i++) {
            int2 nearPos = pos + int2.Min(GameState.Dirs0to3[i], int2.zero);
            int wireDir = i & 1;
            if (Grid[nearPos].HasFlag(CCell.Flag_HasWireRight << wireDir) && !CItem_MachineWire.CanWireBePlacedAt(nearPos, wireDir)) {
                Grid[nearPos].SetFlag(CCell.Flag_HasWireRight << wireDir, false);
                PickupManager.CreatePickup(GItems.electricWire, nb: 1, (Vector2)nearPos, moveToPlayer: destroyer);
            }
        }
        OnSetContent(pos.x, pos.y, false, null);
    }
    public static void SetContent(int i, int j, CItemCell? item, bool isBackwall = false) {
        ref CCell cell = ref Grid[i, j];
        CItemCell? content = cell.GetContent();
        if (isBackwall) {
            cell.SetFlag(CCell.Flag_BackWall_0, item is not null);
        } else {
            bool isAddingForces = cell.IsContentAddingForces();
            cell.m_contentId = item?.m_id ?? 0;
            cell.m_contentHP = item?.m_hpMax ?? 0;
            cell.SetCustomData(0);
            cell.SetFlag(CCell.Flag_IsXReversed, false);
            cell.SetFlag(CCell.Flag_IsBurning, false);
            cell.m_elecCons = 0;
            cell.m_elecProd = 0;
            if (item is not null && item.IsBlock() && item is not CItem_MineralDirt && !cell.IsLava()) {
                cell.m_water = 0f;
            }
            UpdateForcesOfModifiedCell(i, j, isAddingForces);
            // if (item is CItem_MachineAutoBuilder) {
            //     SSingleton<SItems>.Inst.OnMachineAutobuilderFound(new int2(i, j));
            // }
        }
        OnSetContent(i, j, true, content);
    }
    public static void SetContent(int2 pos, CItemCell? item, bool isBackwall = false) {
        SetContent(pos.x, pos.y, item, isBackwall);
    }
    public static void ReplaceWall(int2 pos, CItem_Wall oldWall, CItem_Wall newWall) {
        PickupManager.CreatePickup(oldWall, 1, (Vector2)pos + Vector2.one * 0.5f);
        SetContent(pos, newWall);

        CItem_Wall? backwall = Grid[pos.x, pos.y + 1].GetBackwall();
        if (backwall is not null && !backwall.CanItemBeAncheredAt(pos + int2.up)) {
            DestroyCell(pos + int2.up, 2, true);
        }
        foreach (int2 dir in GameState.Dirs0to3) {
            int2 nearPos = pos + dir;
            CItemCell? content = Grid[nearPos].GetContent();
            if (content is not null && !content.CanItemBeAncheredAt(nearPos)) {
                DestroyCell(nearPos, 2, false);
            }
        }
    }

    public static void UpdateForcesOfModifiedCell(int i, int j, bool oldIsAddingForces) {
        Grid[i, j].m_forceX = 0;
        Grid[i, j].m_forceY = 0;
        Grid[Math.Min(i + 1, Gs.x - 1), j].m_forceX = 0;
        Grid[i, Math.Min(j + 1, Gs.y - 1)].m_forceY = 0;
        // bool flag = Grid[i, j].IsContentAddingForces();
        // if (flag != oldIsAddingForces) {
        //     int num = i * Gs.y + j;
        //     if (!flag) {
        //         this.m_cellsWithForce.RemoveFirst(num);
        //     } else {
        //         this.m_cellsWithForce.Add(num);
        //         if (SOutgame.Mode.m_name == "Solo" && !GVars.m_postGame) {
        //             this.CheckBridgeAchievements(i, j);
        //         }
        //     }
        // }
    }
    public static void OnSetContent(int2 pos, bool checkTeleportAndAutoBuilder = false, CItemCell? previousContent = null) {
        OnSetContent(pos.x, pos.y, checkTeleportAndAutoBuilder, previousContent);
    }
    public static void OnSetContent(int i, int j, bool checkTeleportAndAutoBuilder = false, CItemCell? previousContent = null) {
        m_chunksUpdateTime![i / 4, j / 4] = GVars.SimuTime;
        if (checkTeleportAndAutoBuilder &&
            ((previousContent == GItems.teleport) != (Grid[i, j].GetContent() == GItems.teleport)
            || (previousContent is CItem_MachineAutoBuilder) != (Grid[i, j].GetContent() is CItem_MachineAutoBuilder))) {
            MessageProcessing.SendToAll(new MessageOneBlock(new ushort2((ushort)i, (ushort)j)));
        }
        // MessageProcessing.SendToAll(new MessageOneBlock(new ushort2((ushort)i, (ushort)j)));
    }
    public static bool IsInRectM2(Vector2 pos) {
        return pos.x >= 2 && pos.y >= 2 && pos.x < (Gs.x - 4) && pos.y < (Gs.y - 4);
    }
    public static bool IsInRectM2(int2 pos) {
        return pos.x >= 2 && pos.y >= 2 && pos.x < (Gs.x - 4) && pos.y < (Gs.y - 4);
    }
    public static bool IsInRectCam(Vector2 pos) {
        return pos.x >= 13 && pos.y >= 13 && pos.x < (Gs.x - 26) && pos.y < (Gs.y - 26);
    }

    public static bool IsInWater(Vector2 pos) {
        return Grid[(int)pos.x, (int)pos.y].IsWaterS() && pos.y % 1f < Grid[(int)pos.x, (int)pos.y].m_water;
    }

    public static Vector2 ClampRectM2(Vector2 pos) {
        return Vector2.Clamp(pos, Vector2.zero, new Vector2(Gs.x - 4, Gs.y - 4));
    }
}
