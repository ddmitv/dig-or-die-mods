
using GameEngine;

public interface INetworkMessage {
    static abstract byte MessageID { get; }
    static abstract int MessageSize { get; }

    void Create(CBuffer buffer);
}
public interface IReceivingNetworkMessage : INetworkMessage {
    static abstract void Receive(CBufferSpan buffer, NetworkClient client);
}
