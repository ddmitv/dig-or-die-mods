
using GameEngine;
using System;

public readonly struct MessageServerWorld_Water(RectInt region) : INetworkMessage {
    public static byte MessageID => 28;
    public static int MessageSize => -1;

    public void Create(CBuffer buffer) {
        buffer.WriteFloat_asUShort(GVars.m_clock, 1f);
        buffer.WriteFloat_asUShort(GVars.m_cloudPosRatio, 1f);
        buffer.WriteInt(GVars.m_nbNightsSurvived);
        buffer.WriteBool(GVars.m_achievementsLocked);
        buffer.WriteInt(GVars.m_eventIdNum);
        buffer.WriteFloat(GVars.SimuTime - GVars.m_eventStartTime);

        buffer.WriteUShort((ushort)region.x);
        buffer.WriteUShort((ushort)region.y);
        buffer.WriteByte((byte)region.width);
        buffer.WriteByte((byte)region.height);

        uint waterCellCount = 0;
        uint totalCellCount = 0;
        uint waterBitIndex = buffer.pos * 8;

        // reserve space for water bitmask
        buffer.pos += (uint)MathF.Ceiling(region.width * region.height / 8f);

        for (int i = region.x; i < region.xMax; i++) {
            for (int j = region.y; j < region.yMax; j++) {
                ref readonly CCell cell = ref World.Grid[i, j];

                bool hasWater = cell.m_water > 0.001f;
                Utils.SetBit(buffer.data, waterBitIndex++, hasWater);

                if (hasWater) {
                    float waterAmount = Math.Min(cell.m_water, 255f);
                    ushort waterData = (ushort)(
                        ((!cell.IsLava() ? 0 : 1) << 15) +
                        (((waterAmount < 1f) ? 0 : 1) << 14) +
                        waterAmount * 16384f * ((waterAmount < 1f) ? 1f : 0.00390625f)
                    );
                    buffer.WriteUShort(waterData);
                    waterCellCount++;
                }
                totalCellCount++;
            }
        }

        uint electricityCellCountPos = buffer.pos;
        buffer.pos += 4; // reserve space for count
        int electricityCellCount = 0;

        for (int i = region.x; i < region.xMax; i++) {
            for (int j = region.y; j < region.yMax; j++) {
                ref readonly CCell cell = ref World.Grid[i, j];

                if (cell.m_elecCons != 0 || cell.m_elecProd != 0) {
                    electricityCellCount += 1;
                    ushort cellIndex = (ushort)((i - region.x) * region.height + (j - region.y));
                    buffer.WriteUShort(cellIndex);
                    buffer.WriteByte(cell.m_elecCons);
                    buffer.WriteByte(cell.m_elecProd);
                }
            }
        }
        buffer.WriteIntAt(electricityCellCount, pos: electricityCellCountPos);

        uint burningCellCountPos = buffer.pos;
        buffer.pos += 4; // reserve space for count
        int burningCellCount = 0;

        for (int i = region.x; i < region.xMax; i++) {
            for (int j = region.y; j < region.yMax; j++) {
                if (World.Grid[i, j].HasFlag(CCell.Flag_IsBurning)) {
                    burningCellCount++;
                    ushort cellIndex = (ushort)((i - region.x) * region.height + (j - region.y));
                    buffer.WriteUShort(cellIndex);
                }
            }
        }
        buffer.WriteIntAt(burningCellCount, pos: burningCellCountPos);

        buffer.WriteFloat(GVars.SimuTime - GVars.m_eruptionTime);
    }
}
