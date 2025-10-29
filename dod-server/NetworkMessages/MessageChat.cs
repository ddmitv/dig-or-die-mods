
using GameEngine;
using System;
using System.IO;
using System.Linq;

public readonly struct MessageChat(string text, CPlayer playerSender) : IReceivingNetworkMessage {
    public static byte MessageID => 35;
    public static int MessageSize => -1;

    // each string serialized with BinaryFormatter should have this prefixed bytes
    private static readonly byte[] messagePrefixMagic = [0, 1, 0, 0, 0, 255, 255, 255, 255, 1, 0, 0, 0, 0, 0, 0, 0, 6, 1, 0, 0, 0];
    private static readonly byte messagePostfixMagic = 11;

    public void Create(CBuffer buffer) {
        var strWriterMemory = new MemoryStream(capacity: messagePrefixMagic.Length + text.Length);

        strWriterMemory.Write(messagePrefixMagic, 0, messagePrefixMagic.Length);
        new BinaryWriter(strWriterMemory).Write(text);
        strWriterMemory.Write([messagePostfixMagic]);

        buffer.WriteULong(playerSender.m_steamId);
        buffer.WriteByteArray(strWriterMemory.ToArray());
    }
    public static void Receive(CBufferSpan buffer, NetworkClient client) {
        ReadOnlySpan<byte> messageBytes = buffer.ReadByteSpan();
        
        var strReader = new BinaryReader(new MemoryStream(messageBytes.ToArray()));

        byte[] receivedMagicBytes = strReader.ReadBytes(messagePrefixMagic.Length);
        if (!receivedMagicBytes.SequenceEqual(messagePrefixMagic)) {
            Logging.Warning($"(MessageChat) Magic bytes of chat message string are invalid. " +
                $"Received: [{string.Join(", ", messagePrefixMagic)}], expected: [{string.Join(", ", messagePrefixMagic)}]");
            return;
        }
        string chatMessageText = strReader.ReadString();
        byte receivedMagicPostfixByte = strReader.ReadByte();
        if (receivedMagicPostfixByte != messagePostfixMagic) {
            Logging.Warning($"(MessageChat) Magic postfix byte of chat message string is invalid. Received: [{receivedMagicPostfixByte}], expected: [{messagePostfixMagic}]");
            return;
        }
        if (client.Player is not CPlayer player) {
            Logging.Warning("(MessageChat) Received chat message from non-existing player");
            return;
        }
        Logging.Chat($"{player.m_name} ({client.IPAddress}): {chatMessageText}");
        MessageProcessing.SendToAllExcept(client, new MessageChat(chatMessageText, client.Player));
    }
}
