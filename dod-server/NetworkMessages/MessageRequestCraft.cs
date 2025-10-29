
using GameEngine;
using System;

public readonly struct MessageRequestCraft : IReceivingNetworkMessage {
    public static byte MessageID => 21;
    public static int MessageSize => 34;

    public void Create(CBuffer buffer) => throw new NotImplementedException();

    public static void Receive(CBufferSpan buffer, NetworkClient client) {
        if (client.Player is not CPlayer player) {
            Logging.Warning($"(MessageRequestCraft) Player doesn't exists for client {client}");
            return;
        }
        ushort outId = buffer.ReadUShort();
        if (!GItems.TryGetItem(outId, out CItem outItem)) {
            Logging.Warning($"(MessageRequestCraft) Invalid output craft item with id={outId}");
            return;
        }
        int nbOut = buffer.ReadInt();
        if (nbOut < 0) {
            Logging.Warning($"(MessageRequestCraft) Negative amount of output craft item nb={nbOut}");
            return;
        }
        ushort in1Id = buffer.ReadUShort();
        if (!GItems.TryGetItemOrNull(in1Id, out CItem? in1Item)) {
            Logging.Warning($"(MessageRequestCraft) Invalid first craft ingredient with id={in1Id}");
            return;
        }
        int nbIn1 = buffer.ReadInt();
        if (nbIn1 < 0) {
            Logging.Warning($"(MessageRequestCraft) Negative amount of first craft ingredient nb={nbIn1}");
            return;
        }
        ushort in2Id = buffer.ReadUShort();
        if (!GItems.TryGetItemOrNull(in2Id, out CItem? in2Item)) {
            Logging.Warning($"(MessageRequestCraft) Invalid second craft ingredient with id={in2Id}");
            return;
        }
        int nbIn2 = buffer.ReadInt();
        if (nbIn2 < 0) {
            Logging.Warning($"(MessageRequestCraft) Negative amount of second craft ingredient nb={nbIn2}");
            return;
        }
        ushort in3Id = buffer.ReadUShort();
        if (!GItems.TryGetItemOrNull(in3Id, out CItem? in3Item)) {
            Logging.Warning($"(MessageRequestCraft) Invalid third craft ingredient with id={in3Id}");
            return;
        }
        int nbIn3 = buffer.ReadInt();
        if (nbIn3 < 0) {
            Logging.Warning($"(MessageRequestCraft) Negative amount of third craft ingredient nb={nbIn3}");
            return;
        }
        bool isUpgrade = buffer.ReadBool();

        CRecipe? recipe = (in1Item, in2Item, in3Item) switch {
            (null, null, null) => new CRecipe(outItem, nbOut, isUpgrade),
            (not null, null, null) => new CRecipe(outItem, nbOut, in1Item, nbIn1, isUpgrade),
            (not null, not null, null) => new CRecipe(outItem, nbOut, in1Item, nbIn1, in2Item, nbIn2, isUpgrade),
            (not null, not null, not null) => new CRecipe(outItem, nbOut, in1Item, nbIn1, in2Item, nbIn2, in3Item, nbIn3, isUpgrade),
            _ => null
        };
        if (recipe is null) {
            Logging.Warning($"(MessageRequestCraft) Invalid recipe ingredients order (in1Id={in1Id}, in2Id={in2Id}, in3Id={in3Id})");
            return;
        }

        int nbToCraft = buffer.ReadInt();
        int2 autobuilderPos = buffer.ReadUShort2();
        bool isAllFree = buffer.ReadBool();
        player.m_inventory.Craft(recipe, nbToCraft, autobuilderPos, isAllFree);
    }
}
