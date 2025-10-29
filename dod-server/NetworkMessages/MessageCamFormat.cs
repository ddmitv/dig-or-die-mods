
using GameEngine;

public readonly struct MessageCamFormat(float cameraAspect, ulong steamId) : IReceivingNetworkMessage {
    public static byte MessageID => 4;
    public static int MessageSize => 4;

    public void Create(CBuffer buffer) {
        buffer.WriteULong(steamId);
        buffer.WriteFloat(cameraAspect);
    }
    public static void Receive(CBufferSpan buffer, NetworkClient client) {
        if (client.Player is null) {
            Logging.Warning($"(MessageCamFormat) Player doesn't exists for client {client}");
            return;
        }
        client.Player.m_cameraAspect = buffer.ReadFloat();
        MessageProcessing.SendToAllExcept(client, new MessageCamFormat(client.Player.m_cameraAspect, client.Player.m_steamId));
    }
}
