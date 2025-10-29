
using System;
using System.IO;
using System.Linq;
using System.Net.Sockets;

public static class Server {
    private const string ConfigPath = "config.toml";
    public static ServerConfig Config { get; private set; } = null!;

    static void Main() {
        AppDomain.CurrentDomain.UnhandledException += (sender, e) => {
            var exception = (Exception)e.ExceptionObject;
            try {
                Logging.Error($"Unhandled exception: {exception}");
            } catch { }
        };

        Config = ServerConfig.Load(ConfigPath);

        GameEngine.GItems.Init();

        FileInfo? lastestSavePath = null;
        try {
            lastestSavePath = new DirectoryInfo(Path.GetDirectoryName(Config.SavePath)!)
                .GetFiles()
                .OrderByDescending(x => x.LastWriteTime)
                .First();
        } catch {
            lastestSavePath = null;
        }
        if (lastestSavePath is null) {
            Logging.Error($"Save file was not found in \"{Path.GetFullPath(Path.GetDirectoryName(Config.SavePath)!)}\"");
            return;
        }
        Logging.Info($"Loading game save file from \"{lastestSavePath}\" (LastWriteTime={lastestSavePath.LastWriteTime})");
        GameEngine.SaveManager.Load(File.ReadAllBytes(lastestSavePath.FullName));

        TimeSpan sleepModeTimeout = TimeSpan.Parse(Config.SleepModeTimeout);

        GameEngine.Game.Init();
        GameEngine.World.Init();
        // GameEngine.UnitManager.CleanAll();

        TcpListener server = new(System.Net.IPAddress.Any, port: Config.Port);
        server.Start();
        Logging.Info($"Server hosted at: {server.LocalEndpoint}");

        GameEngine.GParams.m_respawnDelay = 5;
        GameEngine.GParams.m_gameOverIfAllDead = false;

        // MessageStartInfos.Dbg(File.OpenWrite("./dbg.bin"));

        while (true) {
            if (server.Pending()
                || (NetworkClients.ConnectedClientsCount() == 0 && GameEngine.Game.HasRealtimeElapsed(NetworkClients.TimeSinceLastDisconnected, sleepModeTimeout))) {
                NetworkClient newClient = NetworkClients.AcceptClient(server.AcceptTcpClient());
                Logging.Info($"Client connected: {newClient.Socket.RemoteEndPoint?.ToString() ?? "[INVALID ENDPOINT]"}");
            }

            GameEngine.Game.Update();
            GameEngine.PickupManager.Update();
            GameEngine.World.Update();
            GameEngine.UnitManager.Update();

            foreach (var client in NetworkClients.GetConnectedClients()) {
                if (!client.IsCompleteJoining && GameEngine.Game.RealtimeSinceStartup.Seconds > client.JoinTime.Seconds + 10.0) {
                    Logging.Warning($"Client {client} is not completing joining in a long time, force disconnecting them...");
                    client.Disconnect();
                }
                MessageProcessing.ReceiveAll(client);
            }
            NetworkClients.UpdateConnectedClients();
        }
    }
}
