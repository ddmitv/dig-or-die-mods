
using GameEngine;

public readonly struct MessagePlayerInfos(CPlayer player) : IReceivingNetworkMessage {
    public static byte MessageID => 5;
    public static int MessageSize => -1;

    public void Create(CBuffer buffer) {
        buffer.WriteULong(player.m_steamId);
        buffer.WriteBool(player.m_skinIsFemale);
        buffer.WriteFloat(player.m_skinColorSkin);
        buffer.WriteInt(player.m_skinHairStyle);
        buffer.WriteByte(player.m_skinColorHair.r);
        buffer.WriteByte(player.m_skinColorHair.g);
        buffer.WriteByte(player.m_skinColorHair.b);
        buffer.WriteByte(player.m_skinColorEyes.r);
        buffer.WriteByte(player.m_skinColorEyes.g);
        buffer.WriteByte(player.m_skinColorEyes.b);

        foreach (CStack stack in player.m_inventory.m_items) {
            buffer.WriteUShort(stack.m_item.m_id);
            buffer.WriteUShort((ushort)stack.m_nb);
            buffer.WriteInt(player.m_inventory.GetItemBarSlot(stack));
            CItemVars itemVars = player.GetItemVars(stack.m_item);
            buffer.WriteFloat(GVars.SimuTime - itemVars.TimeActivation);
            buffer.WriteFloat(GVars.SimuTime - itemVars.TimeLastUse);
            buffer.WriteBool(itemVars.IsPassiveItemOn);
            buffer.WriteBool(itemVars.IsJetpackActive);
        }
    }

    public static void Receive(CBufferSpan buffer, NetworkClient client) {
        Logging.Info($"Received MessagePlayerInfos message from client {client}");
        if (client.Player is not CPlayer player) {
            Logging.Warning($"(MessagePlayerInfos) Player doesn't exists for client {client}");
            return;
        }
        ulong steamId = buffer.ReadULong();
        if (PlayerManager.GetPlayerBySteamId(steamId) is null) {
            Logging.Warning($"(MessagePlayerInfos) Player doesn't exists for steam id {steamId}");
            return;
        }
        if (steamId != player.m_steamId) {
            Logging.Warning($"(MessagePlayerInfos) Recieved message from client {client} that they are not allowed to send");
            return;
        }
        bool skinIsFemale = buffer.ReadBool();
        float skinColorSkin = buffer.ReadFloat();
        int skinHairStyle = buffer.ReadInt();
        if (skinHairStyle < 1 || skinHairStyle > 5) {
            Logging.Warning($"(MessagePlayerInfos) skinHairStyle field must be in range [1, 5], got {skinHairStyle}");
            return;
        }
        player.m_skinIsFemale = skinIsFemale;
        player.m_skinColorSkin = skinColorSkin;
        player.m_skinHairStyle = skinHairStyle;
        player.m_skinColorHair = new Color24(buffer.ReadByte(), buffer.ReadByte(), buffer.ReadByte());
        player.m_skinColorEyes = new Color24(buffer.ReadByte(), buffer.ReadByte(), buffer.ReadByte());

        // skip player inventory parsing: client should not able to change that

        MessageProcessing.SendToAllExcept(client, new MessagePlayerInfos(player));
    }
}

