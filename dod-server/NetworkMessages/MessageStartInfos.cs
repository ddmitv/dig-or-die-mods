
using GameEngine;
using System;
using System.IO;

public readonly struct MessageStartInfos(ushort playerUnitId, Vector2 playerUnitPos) : INetworkMessage {
    public static byte MessageID => 25;
    public static int MessageSize => -1;

    public void Create(CBuffer buffer) {
        buffer.WriteByteArray(GParams.Serialize());

        byte[]? compressedWorldData = null;
        using (BinaryWriter worldDataWriter = new BinaryWriter(new MemoryStream())) {
            byte[] changeFlags = new byte[(int)Math.Ceiling((World.Gs.x * World.Gs.y) / 2.0)];
            worldDataWriter.Seek(changeFlags.Length, SeekOrigin.Begin);
            uint bitIndex = 0;

            ushort prevWater = 0;
            uint prevFlags = 0;
            ushort prevContent = 0;
            ushort prevHP = 0;

            for (int i = 0; i < World.Gs.y; ++i) {
                for (int j = 0; j < World.Gs.x; ++j) {
                    ref CCell cell = ref World.Grid[j, i];
                    ushort water = (ushort)Utils.Clamp(cell.m_water * 20f, 0f, 65533f);
                    Utils.SetBit(changeFlags, bitIndex++, prevWater != water);
                    Utils.SetBit(changeFlags, bitIndex++, prevFlags != cell.m_flags);
                    Utils.SetBit(changeFlags, bitIndex++, prevContent != cell.m_contentId);
                    Utils.SetBit(changeFlags, bitIndex++, prevHP != cell.m_contentHP);

                    if (prevWater != water) {
                        worldDataWriter.Write(water);
                        prevWater = water;
                    }
                    if (prevFlags != cell.m_flags) {
                        worldDataWriter.Write(cell.m_flags);
                        prevFlags = cell.m_flags;
                    }
                    if (prevContent != cell.m_contentId) {
                        worldDataWriter.Write(cell.m_contentId);
                        prevContent = cell.m_contentId;
                    }
                    if (prevHP != cell.m_contentHP) {
                        worldDataWriter.Write(cell.m_contentHP);
                        prevHP = cell.m_contentHP;
                    }
                }
            }
            worldDataWriter.Seek(0, SeekOrigin.Begin);
            worldDataWriter.Write(changeFlags);
            worldDataWriter.Seek(0, SeekOrigin.End);

            compressedWorldData = CLZF2.Compress(((MemoryStream)worldDataWriter.BaseStream).ToArray());
        }
        buffer.WriteByteArray(compressedWorldData);

        buffer.WriteUShort(playerUnitId);
        buffer.WriteVector2(playerUnitPos);
    }
}
