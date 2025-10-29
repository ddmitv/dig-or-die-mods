
# Dig or Die Server

This directory contains source code for Dig or Die dedicated server.

- [`Main.cs`](./Main.cs) - server's update loop.
- [`ServerConfig.cs`](./ServerConfig.cs) - layout and default options of `config.toml`.
- [`GameEngine/`](./GameEngine/) - contains code for simulating game world: units, world updates, items, pickups, save files, etc.
- [`NetworkMessages/`](./NetworkMessages/) - contains creation/handling packet logic from/to connected clients.
- [`Logging.cs`](./Logging.cs) - logging of actions to file and to **stdout**.
- [`LZF.cs`](./LZF.cs) - decompiled implementation of LZF compression algorithm.

Requires C# 12 and .NET 8.0, works on Windows and Linux. \
Depends on [**Tomlyn**](https://github.com/xoofx/Tomlyn) library for storing server configuration (`config.toml`).

To start server (on default configuration):
1. Put save file to `saves/` directory (the server will load on startup the most recent and autosave them to it).
2. Create `logs/` directory (example log file name: `10-10-2025_15_09_01.log`).
3. Start server using `dotnet run` command (requires dotnet installed).
4. Join server from Dig or Die client with `127.0.0.1:3776` endpoint.

