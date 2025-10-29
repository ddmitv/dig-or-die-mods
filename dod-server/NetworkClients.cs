
using GameEngine;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;

public class NetworkClient(TcpClient client) : IDisposable {
    private readonly CBuffer _recvBuffer = new();
    private uint _recvBufferLength = 0;
    private const uint HEADER_SIZE = 4;

    public CPlayer? Player { set; get; }
    public bool IsCompleteJoining { set; get; } = false;
    public Socket Socket => client.Client;
    public TcpClient TcpClient => client;
    public string IPAddress => ((IPEndPoint?)client.Client.RemoteEndPoint)?.Address.ToString() ?? "[DISCONNECTED]";
    public bool IsConnected { get; private set; } = true;
    public Realtime JoinTime { get; } = Game.RealtimeSinceStartup;

    private bool _isDisposed = false;

    public CBufferSpan? Receive() {
        if (!IsConnected || Socket.Available == 0) { return null; }
        
        if (_recvBuffer.pos < HEADER_SIZE) {
            int bytesRead = Socket.Receive(_recvBuffer.data, (int)_recvBuffer.pos, (int)(HEADER_SIZE - _recvBuffer.pos), SocketFlags.None, out SocketError err);
            if (err != SocketError.Success) {
                Logging.Error($"Client {this}: error while receiving length header packet: {Utils.EnumToString(err)}. Buffer pos: {_recvBuffer.pos}, avaliable bytes: {Socket.Available}");
                IsConnected = false;
                return null;
            }
            _recvBuffer.pos += (uint)bytesRead;
            if (_recvBuffer.pos < HEADER_SIZE) {
                return null;
            }
            _recvBufferLength = Unsafe.ReadUnaligned<uint>(ref _recvBuffer.data[0]);
        }
        if (_recvBuffer.data.Length < HEADER_SIZE + _recvBufferLength) {
            Logging.Error($"Client {this}: Receiving packet is too large. Buffer size: {_recvBuffer.data.Length}, total packet size: {HEADER_SIZE + _recvBufferLength}, avaliable bytes: {Socket.Available}");
            IsConnected = false;
            return null;
        }
        if (_recvBuffer.pos < HEADER_SIZE + _recvBufferLength) {
            uint bytesToRead = HEADER_SIZE + _recvBufferLength - _recvBuffer.pos;
            int bytesRead = Socket.Receive(_recvBuffer.data, (int)_recvBuffer.pos, (int)bytesToRead, SocketFlags.None, out SocketError err);
            if (err != SocketError.Success) {
                Logging.Error($"Client {this}: error while receiving packet body: {Utils.EnumToString(err)}. Buffer pos: {_recvBuffer.pos}, avaliable bytes: {Socket.Available})");
                IsConnected = false;
                return null;
            }
            if (bytesRead == 0) {
                return null;
            }
            _recvBuffer.pos += (uint)bytesRead;
            if (_recvBuffer.pos < HEADER_SIZE + _recvBufferLength) {
                return null;
            }
        }
        _recvBuffer.pos = (int)HEADER_SIZE;
        CBufferSpan res = new(_recvBuffer, _recvBufferLength);
        _recvBuffer.pos = 0;
        return res;
    }
    public void Send(CBuffer buffer, bool force = false) {
        if (!IsConnected) {
            Logging.Warning($"Client {this} is disconnected, packet sending is canceled (buffer size={buffer.pos})");
            return;
        }
        if (Socket is null) {
            IsConnected = false;
            return;
        }
        if (!force && !IsCompleteJoining) {
            IsConnected = Socket.Poll(-1, SelectMode.SelectWrite);
            return;
        }
        try {
            IsConnected = Socket.Connected && Socket.Send(buffer.data, (int)buffer.pos, SocketFlags.None) == buffer.pos;
            return;
        } catch (SocketException ex) {
            Logging.Warning($"Connection aborted with client {this} with error code: {Utils.EnumToString(ex.SocketErrorCode)}");
            IsConnected = false;
            return;
        }
    }

    public void Disconnect() {
        IsConnected = false;
    }

    public override string ToString() {
        return (Player, IsConnected) switch {
            (not null, true) => $"{{name={Player.m_name}, ip={IPAddress}}}",
            (null, true) => $"{{ip={IPAddress}}}",
            (not null, false) => $"{{name={Player.m_name}, ip={IPAddress} [disconnected]}}",
            (null, false) => $"{{ip={IPAddress} [disconnected]}}",
        };
    }

    public void Dispose() {
        if (_isDisposed) { return; }
        _isDisposed = true;
        IsConnected = false;
        if (Player is not null) {
            Player.networkClient = null;
            if (Player.m_unitPlayer is not null) {
                Player.m_posSaved = Player.m_unitPlayer.m_pos;
                UnitManager.RemoveUnit(Player.m_unitPlayer);
                Player.m_unitPlayer = null;
            }
        }
        client.Dispose();
        GC.SuppressFinalize(this);
    }
}

public static class NetworkClients {
    private static readonly List<NetworkClient> connectedClients = new(capacity: 4);
    private static readonly List<NetworkClient> disconnectingClients = new(capacity: 1);

    public static Realtime TimeSinceLastDisconnected { get; private set; } = new();

    // public static readonly IReadOnlyList<NetworkClient> ConnectedClients = connectedClients;

    public static NetworkClient AcceptClient(TcpClient client) {
        client.NoDelay = Server.Config.ClientSocket.NoDelay;
        client.ReceiveTimeout = Server.Config.ClientSocket.ReceiveTimeout;
        client.SendTimeout = Server.Config.ClientSocket.SendTimeout;
        var newNetworkClient = new NetworkClient(client);
        connectedClients.Add(newNetworkClient);
        return newNetworkClient;
    }
    public static uint ConnectedClientsCount() {
        return (uint)connectedClients.Count;
    }
    public static void DisconnectClient(NetworkClient client) {
        TimeSinceLastDisconnected = Game.RealtimeSinceStartup;
        Logging.Info($"Client disconnected: {client}");
        connectedClients.Remove(client);
        client.Dispose();
    }
    public static bool HasConnectedClients() {
        return connectedClients.Count != 0;
    }
    public static IReadOnlyList<NetworkClient> GetConnectedClients() {
        return connectedClients;
    }
    public static void SendToClient(CBuffer buffer, NetworkClient client) {
        client.Send(buffer, force: true);
    }
    public static void SendToAllClients(CBuffer buffer) {
        foreach (var client in connectedClients) {
            client.Send(buffer);
        }
    }
    public static void SendToAllClientsExcept(CBuffer buffer, NetworkClient excludedClient) {
        foreach (var client in connectedClients) {
            if (client == excludedClient) { continue; }
            client.Send(buffer);
        }
    }

    public static void UpdateConnectedClients() {
        foreach (var client in connectedClients) {
            if (!client.IsConnected) {
                disconnectingClients.Add(client);
            }
        }
        if (disconnectingClients.Count == 0) { return; }

        foreach (var disconnectingClient in disconnectingClients) {
            DisconnectClient(disconnectingClient);
        }
        disconnectingClients.Clear();

        MessageProcessing.SendToAll(new CustomMessagePlayerSession());
    }
}

