
using GameEngine;
using System;

public readonly struct MessageRequestBuild : IReceivingNetworkMessage {
    public static byte MessageID => 20;
    public static int MessageSize => 6;

    public void Create(CBuffer buffer) {
        throw new NotImplementedException();
    }
    public static void Receive(CBufferSpan buffer, NetworkClient client) {
        if (client.Player is not CPlayer player) {
            Logging.Warning($"(MessageRequestBuild) Player doesn't exists for client {client}");
            return;
        }
        if (player.m_unitPlayer is null) {
            Logging.Warning($"(MessageRequestBuild) Player {player} doesn't have a unit");
            return;
        }
        ushort2 pos = buffer.ReadUShort2();
        ushort placedItemId = buffer.ReadUShort();
        if (!GItems.TryGetItem(placedItemId, out CItemCell placedItem)) {
            Logging.Warning($"(MessageRequestBuild) Received invalid item with id={placedItemId}");
            return;
        }
        ref CCell currentCell = ref World.Grid[pos];

        CItem_Wall? placedItemWall = placedItem as CItem_Wall;
        CItemCell? existingContent = currentCell.GetContent();
        CItem_Wall? existingWall = existingContent as CItem_Wall;

        bool isPlacingBackwall = placedItemWall is { m_type: CItem_Wall.Type.Backwall };
        bool hasExistingBackwall = currentCell.GetBackwall() is not null;
        bool shouldReverseItem = placedItem.m_isReversable && player.m_unitPlayer.m_pos.x > pos.x + 0.5f;

        if (existingContent != placedItem && (existingContent is null || (existingWall is not null && placedItemWall is not null) || (!hasExistingBackwall && isPlacingBackwall))) {
            if (player.m_inventory.RemoveFromInventory(placedItem, 1, false)) {
                if (placedItemWall is not null && existingWall is not null && existingWall.IsReceivingForces() && placedItemWall.IsReceivingForces()) {
                    World.ReplaceWall(pos, existingWall, placedItemWall);
                }
                World.SetContent(pos, placedItem, isPlacingBackwall);
                if (shouldReverseItem) {
                    currentCell.SetFlag(CCell.Flag_IsXReversed, true);
                }
            }
        }
    }
}
