
using GameEngine;
using System;

public readonly struct MessageServerWorld_OnChange(CPlayer player, int2 min, int2 max, float[,] chunksUpdateTime) : INetworkMessage {
    public static byte MessageID => 27;
    public static int MessageSize => -1;

    private const int ChunkSize = World.ChunkSize;

    public void Create(CBuffer buffer) {
        if (player.m_chunksSendTime is null) {
            throw new InvalidOperationException($"player.m_chunksSendTime is null");
        }
        int chunksSend = 0;
        for (int chunkI = min.x; chunkI <= max.x; ++chunkI) {
            for (int chunkJ = min.y; chunkJ < max.y; ++chunkJ) {
                if (chunksSend++ >= 254) {
                    return;
                }
                if (chunksUpdateTime[chunkI, chunkJ] <= player.m_chunksSendTime[chunkI, chunkJ]) {
                    continue;
                }
                buffer.WriteByte((byte)chunkI);
                buffer.WriteByte((byte)chunkJ);
                for (int cellI = 0; cellI < ChunkSize; cellI++) {
                    for (int cellJ = 0; cellJ < ChunkSize; cellJ++) {
                        ref CCell cell = ref World.Grid[chunkI * ChunkSize + cellI, chunkJ * ChunkSize + cellJ];
                        buffer.WriteUInt(cell.m_flags);
                        buffer.WriteUShort(cell.m_contentId);
                        buffer.WriteUShort(cell.m_contentHP);
                    }
                }
                player.m_chunksSendTime[chunkI, chunkJ] = GVars.SimuTime;
            }
        }
    }
}
