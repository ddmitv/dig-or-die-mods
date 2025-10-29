
using GameEngine;
using System.Text;

public readonly struct CustomMessagePlayerSession : IReceivingNetworkMessage {
    public static byte MessageID => 46; // unused message id
    public static int MessageSize => -1;

    public void Create(CBuffer buffer) {
        foreach (CPlayer player in PlayerManager.players) {
            if (player.networkClient is null) { continue; }

            buffer.WriteULong(player.m_steamId);
            buffer.WriteByteArray(Encoding.UTF8.GetBytes(player.m_name));
        }
    }
    public static void Receive(CBufferSpan buffer, NetworkClient client) {
        ulong steamId = buffer.ReadULong();
        string playerName = Encoding.UTF8.GetString(buffer.ReadByteSpan());

        Logging.Info($"(CustomMessagePlayerSession) Received message from client {client} (steam id={steamId}, name={playerName})");

        (CPlayer newPlayer, bool alreadyExists) = PlayerManager.FindOrAddPlayer(steamId);
        newPlayer.m_name = playerName;
        newPlayer.networkClient = client;
        client.Player = newPlayer;
        client.IsCompleteJoining = true;

        MessageProcessing.SendToAll(new CustomMessagePlayerSession());

        MessageProcessing.Send(client, new MessageStartInfos(newPlayer.m_unitPlayerId, Vector2.zero));

        foreach (CUnit unit in UnitManager.units) {
            Logging.Info($"Sending MessageSpawnUnit (desc={unit.m_uDesc.m_codeName}, pos={unit.m_pos}, id={unit.m_id})");
            MessageProcessing.Send(client, new MessageSpawnUnit(unit.m_uDesc, unit.m_pos, unit.m_id));
        }
        if (!alreadyExists) {
            Logging.Info($"Player {newPlayer} joined first time, giving initial items");
            newPlayer.m_inventory.AddToInventory(GItems.dirt, 10);
            newPlayer.m_inventory.AddToInventory(GItems.miniaturizorMK1, 1);
            newPlayer.m_inventory.AddToInventory(GItems.autoBuilderMK1, 1);
            newPlayer.m_inventory.AddToInventory(GItems.gunRifle, 1);
            // newPlayer.m_inventory.AddToInventory(GItems.electricWire, 10);
        }
        // newPlayer.m_inventory.AddToInventory(GItems.autoBuilderUltimate, 1);
        Vector2 playerUnitPos = newPlayer.m_posSaved != Vector2.zero ? newPlayer.m_posSaved : UnitManager.GetBestSpawnPoint();
        CUnitPlayer? playerUnit = (CUnitPlayer?)UnitManager.SpawnUnit(GUnits.player, playerUnitPos, newPlayer.m_unitPlayerId);
        if (playerUnit is not null) {
            newPlayer.m_unitPlayer = playerUnit;
        }
        foreach (var player in PlayerManager.players) {
            if (player.networkClient is null) { continue; }

            MessageProcessing.Send(player.networkClient, new MessagePlayerInfos(newPlayer));
            if (player != newPlayer) {
                MessageProcessing.Send(client, new MessagePlayerInfos(player));
            }
        }

        MessageProcessing.Send(client, new MessageCamFormat(GameState.cameraAspect, newPlayer.m_steamId));

        Utils.FillWithDefault(newPlayer.m_pickupsSendTime);
    }
}
