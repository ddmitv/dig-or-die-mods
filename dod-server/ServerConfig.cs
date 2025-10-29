
using System.IO;
using Tomlyn;

public sealed class ServerConfig {
    public static ServerConfig Load(string path) {
        if (!File.Exists(path)) {
            var defaultConfig = new ServerConfig();
            File.WriteAllText(path, Toml.FromModel(defaultConfig));
            return defaultConfig;
        }
        return Toml.ToModel<ServerConfig>(File.ReadAllText(path), sourcePath: path);
    }
    public sealed class ClientSocketConfig {
        public bool NoDelay { get; private set; } = true;
        public int ReceiveTimeout { get; private set; } = 5000;
        public int SendTimeout { get; private set; } = 5000;
    }
    // public sealed class GameParams {
    //     public int Difficulty { get; set; } = 2;
    //     public int RespawnDelay { get; set; } = 5;
    //     public int MonstersDayNb { get; set; } = 7;
    //     public int MonstersDayNbAddPerPlayer { get; set; } = 2;
    //     public float MonstersNightSpawnRateMult { get; set; } = 1f;
    //     public float MonstersNightSpawnRateAddPerPlayer { get; set; } = 0.35f;
    //     public float MonstersHpMult { get; set; } = 1f;
    //     public float MonstersHpAddPerPlayer { get; set; } = 0.2f;
    //     public float MonstersDamagesMult { get; set; } = 1f;
    //     public float MonstersDamagesAddPerPlayer { get; set; } = 0.1f;
    // }

    public ushort Port { get; private set; } = 3776;
    public string SavePath { get; private set; } = "saves/slot-{0}.save";
    public string LogPath { get; private set; } = "logs/{dd-MM-yyyy_HH_mm_ss}.log";
    public string AutoSaveInterval { get; private set; } = "00:05:00";
    public string SleepModeTimeout { get; private set; } = "00:01:00";
    public string JoinCompletionTimeout { get; private set; } = "00:00:10";

    public ClientSocketConfig ClientSocket { get; private set; } = new();
}
