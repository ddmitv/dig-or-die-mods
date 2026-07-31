using System.Collections.Generic;
using System.IO;
using System.Reflection.Emit;
using BepInEx;
using DODModAPI.Extensions;
using HarmonyLib;

public static class ExampleItems {
    public static readonly DODModAPI.ModRecipeGroup exampleRecipeGroup = new(
        groupId: "EXAMPLE GROUP",
        autoBuilders: [GItems.autoBuilderMK1]
    );

    public static readonly DODModAPI.ModItem exampleWall = new(
        codeName: "exampleWall",
        name: "Example Wall",
        description: "A very sturdy wall added via DODModAPI",
        item: new CItem_Wall(
            tile: ExampleAssets.exampleWall,
            tileIcon: null,
            hpMax: 1000,
            mainColor: ExampleAssets.exampleWall.MainColor,
            forceResist: 5000,
            weight: 200f,
            type: CItem_Wall.Type.WallBlock
        ),
        recipe: new(groupId: "EXAMPLE GROUP") {
            nbOut = 2,
            in1 = new(GItems.iron, 10),
            in2 = new(GItems.coal, 5)
        }
    ) {
        // overwrites the corresponding CItem_PluginData for this item
        PluginDataOverwriter = (ref CItem_PluginData data) => {
            // makes the wall immune to lava
            data.m_isFireProof = 1;
        }
    };

    public static readonly DODModAPI.ModItem exampleDevice = new(
        codeName: "exampleDevice",
        name: "Example Device",
        description: "An example passive device that glows like a flashlight",
        item: new CItem_Device(
            tile: ExampleAssets.exampleDevice_tile,
            tileIcon: ExampleAssets.exampleDevice_icon,
            groupId: DODModAPI.DeviceGroupIds.flashLight,
            type: CItem_Device.Type.Passive,
            customValue: 10f
        ),
        // groupId selects for which autobuilders the recipe is avaliable.
        // isUpgrade is for items that are being a upgraded version of the previous item.
        // in1, in2, in3 is for recipe ingredients. for more than 3 ingredients, you need to create intermediate ingredient items
        // that are just used as a crafting material
        // nbOut is the output number of items from crafting a recipe (default is 1)
        recipe: new(groupId: "MK III", isUpgrade: true) {
            in1 = new(GItems.flashLightMK2, 1),
            in2 = new(GItems.gold, 5)
        }
        // if you leave in1, in2 and in3 fields, the item can be crafted without any ingredients
    );

    public static readonly DODModAPI.ModItem exampleSurface = new(
        codeName: "exampleSurface",
        name: "Example Surface",
        description: "Description of example surface",
        item: new CItem_Mineral(
            tile: null,
            tileIcon: ExampleAssets.exampleSurface_icon,
            hpMax: 100,
            mainColor: ExampleAssets.exampleSurface_icon.MainColor,
            surface: new DODModAPI.ModSurface(
                surfaceTexture: ExampleAssets.exampleSurface_surfaceMaterial,
                surfaceSortingOrder: 100,
                surfaceTopTile: ExampleAssets.exampleSurface_surfaceTops,
                hasAltTop: true
            ),
            isReplacable: true
        ),
        recipe: new(groupId: "MK III") {
            in1 = new(GItems.dirt, 1)
        }
    );
}

public static class ExampleUnits {
    public static readonly DODModAPI.ModUnit exampleMonster = new(
        codeName: "exampleMonster",
        name: "Example monster",
        unitDesc: new CUnitMonster.CDesc(
            tier: 3,
            speed: 4.5f,
            size: new UnityEngine.Vector2(1.2f, 1.2f),
            hpMax: 250,
            armor: 10,
            attackDesc: new CAttackDesc(
                range: 10f,
                damage: 5,
                nbAttacks: 2,
                cooldown: 1f,
                knockbackOwn: 10f,
                knockbackTarget: 10f,
                projDesc: new DODModAPI.ModBulletDesc(
                    sprite: ExampleAssets.exampleBullet, // you can use ModSprite.Vanilla to reference built-in sprites
                    radius: 5f,
                    dispersionAngleRad: 0.01f,
                    speedStart: 10f,
                    speedEnd: 20f
                ),
                sound: DODModAPI.GameAssets.SoundID.firefly
            ),
            tiles: ExampleAssets.exampleUnit,
            loot: [
                new(GItems.gold, probability: 0.5f),
                new(GItems.lavaFlower, probability: 1f)
            ]  
        ) {}
    );
}

public sealed class ExampleEvent : DODModAPI.ModEnvironment {
    public static ExampleEvent inst = new ExampleEvent();

    private ExampleEvent() : base(id: "exampleEvent", name: "Example Event", duration: 10f /*seconds*/) { }

    public override void OnEventStart() {
        DODModAPI.Misc.SendChatMessageLocal("Example event starts");
        var playerPos = G.m_player.PosCell;
        SWorld.Grid[playerPos.x, playerPos.y].m_water = 100f;
    }

    public override void OnEventUpdate() {
        var pos = SGame.MouseWorldPosInt;
        SWorld.Inst.SetContent(pos, GItems.diamonds);
        SWorld.Grid[pos.x, pos.y].SetBgSurface(GSurfaces.bgLava);
    }

    public override void OnEventEnd() {
        DODModAPI.Misc.SendChatMessageLocal("Example event ends");
        GVars.m_clock = 0.5f;
    }
}

public sealed class ExampleSaveHandler : DODModAPI.IModSaveHandler {
    public string ModId => "example_mod";
    public uint CurrentVersion => 1; // save handler version

    public DODModAPI.IModSaveHandler.SaveResult Save(BinaryWriter writer) {
        var value = UnityEngine.Random.value;
        if (value < 0.1) {
            // do not include this mod save data
            return DODModAPI.IModSaveHandler.SaveResult.Skip;
        }
        UnityEngine.Debug.Log($"example save state value: {value} (saving)");
        writer.Write(value);
        return DODModAPI.IModSaveHandler.SaveResult.Continue;
    }

    public void Load(BinaryReader reader, uint savedVersion) {
        if (savedVersion == 2) {
            int intValue = reader.ReadInt32();
            UnityEngine.Debug.Log($"new version value: {intValue} (loading), version: {savedVersion}");
        } else {
            float value = reader.ReadSingle();
            UnityEngine.Debug.Log($"example save state value: {value} (loading), version: {savedVersion}");
        }
    }
}

// for more examples, see vanilla modes: CModeDefense, CModeMulti, CModeSkyWorld, CModeSolo, CModeUnderTheSea
public sealed class ExampleMode : CModeSolo {
    public ExampleMode() {
        // some predefined parameters, can alternatively be set via the Lua mode setup function
        m_modParent = "Solo";
        m_isMulti = false;
    }

    public override void GenerateGround() {
        // generates the surface line: a 1D array of Y-coordinates defining the ground level at each X position.
        m_surfaceLine = GenerateHeightLine(
            min: 660, max: 760, slopeVariability: 0.6f, slopeMax: 1.0f, specialMiddleFlat: true
        );
        // for example, m_surfaceLine[305] is ground level at X=305

        // iterate each cell in a world
        for (int i = 0; i < SWorld.Gs.x; i++) {
            for (int j = 0; j < SWorld.Gs.y; j++) {
                CCell cell = default;
                if (j <= m_surfaceLine[i]) { // if should place dirt
                    cell.m_contentId = GItems.dirt.m_id;
                    cell.SetBgSurface(GSurfaces.bgDirt);
                }
                SWorld.Grid[i, j] = cell;
            }
        }

        int midX = SWorld.Gs.x / 2;
        SOutgame.Params.m_spawnPos = new int2(midX - 4, m_surfaceLine[midX - 4] + 1);
        SOutgame.Params.m_shipPos = new int2(midX + 3, m_surfaceLine[midX + 3] + 1);

        // disable monster spawning
        SOutgame.Params.m_monstersDayNb = 0;
        SOutgame.Params.m_monstersDayNbAddPerPlayer = 0;
        SOutgame.Params.m_monstersNightSpawnRateMult = 0f;

        SOutgame.Params.m_rainQuantity = 0f;
        SOutgame.Params.m_eventsActive = false;
    }

    public override void OnNewGame() {
        // create player unit. without it, the game will softlock
        SUnits.SpawnUnit(GUnits.player, SWorld.GetBestValidSpawnPoint(), SNetwork.GetMyPlayer().m_unitPlayerId);

        CreateInitialPlayerItems(SNetwork.GetMyPlayer(), addInInventory: true);
    }

    public override void CreateInitialPlayerItems(CPlayer player, bool addInInventory = false) {
        player.m_inventory.AddToInventory(GItems.miniaturizorMK1);
    }

    public override void OnPresimulationFinished() { }
    public override void SpawnInitialDeadMonsters() { }

    // we cannot return empty array since SAudio.GetGameTension will throw an exception.
    // but, if we try to return null while monster spawning is enabled, SUnits.SpawnMonster_
    // will also throw an exception. so, we need to disable unit spawning and return null to avoid
    // having those issues
    public override CUnitMonster.CDesc[]? GetMonstersList(UnityEngine.Vector2 pos) => null;
}

public sealed class ExampleNetworkMessage : SMessageSingleton<ExampleNetworkMessage> {
    // message length in bytes
    public override int GetBodySize() => 4;

    // you can pass any info to this method via its arguments
    public void Send(float someValue1, int someValue2) {
        // 0 to send this message to everyone, anything else is treated as a destanation client's steam ID
        Send_Start(steamIdRemote: 0);

        // here you can write anything to a buffer (serialization)
        m_buffer.WriteFloat(UnityEngine.Random.value + someValue1 + someValue2 * 2);
        Send_End();
    }

    public override void OnReceived(ulong steamIdRemote, uint bufferEndPos) {
        // here you need to read from a buffer (deserialization)
        float value = m_buffer.ReadFloat();
        UnityEngine.Debug.Log($"Recevied message with value: {value} from {steamIdRemote}");

        // use bufferEndPos for dynamically lengthed messages, to know where to stop reading buffer
    }
}

public sealed class ExampleDynamicNetworkMessage : SMessageSingleton<ExampleDynamicNetworkMessage> {
    // you can force message ID to be a specific one.
    // if you leave it as 0, the DODModAPI will automatically assign a free ID
    public ExampleDynamicNetworkMessage() => m_messageId = 50;

    // -1 for dynamic length (the message length will be stored in the message metadata)
    public override int GetBodySize() => -1;

    public void Send(int[] values) {
        Send_Start(steamIdRemote: 0);
        // write values from array to message buffer (total length is calculated automatically)
        foreach (int value in values) {
            m_buffer.WriteInt(value);
        }
        Send_End();
    }
    public override void OnReceived(ulong steamIdRemote, uint bufferEndPos) {
        int sum = 0;
        // read ints until buffer is fully read
        while (m_buffer.m_pos < bufferEndPos) {
            sum += m_buffer.ReadInt();
        }
        DODModAPI.Misc.SendChatMessageLocal($"Sum of values: {sum}");
    }
}

enum ExampleMessageType {
    Simple,
    Dynamic,
}

public sealed class ExampleScreen : SSingletonScreen<ExampleScreen> {
    // all CGui subtypes fields are gathered with reflection (see SScreen.Start) and stored in a tree-like structure
    // SScreen.m_guiRoot holds all GUI elements as its childrens (CGui.m_children) and each GUI element knowns its parent (CGui.m_parent)

    // GUI positioning uses anchors: m_parentAnchor picks a pivot on the parent container,
    // m_coordsOrigin picks a pivot on current element to align with it, and m_x/m_y apply a pixel offset.

    // tip: use RuntimeUnityEditor BepInEx plugin to, at runtime, quickly experiment with the GUI:
    // in the REPL console type "seti(<SCREEN TYPE>.Inst)" to view the fields of screen object

    // screen background
    public CGuiBitmap bmpBack = new() {
        m_sprite = DODModAPI.GameAssets.UI.Black75p, // DODModAPI.GameAssets.UI contains all built-in sprites in the game
        m_scale = new(200f, 200f) // some large value just to make sure screen is fully covered in it
    };

    // button
    public CGuiButton btBack = new() {
        m_x = 0, m_y = 0, m_width = 250, m_height = 80,
        // for custom texts you would need to add new localization strings (use DODModAPI.Misc.AddLocalizationText)
        m_textId = "COMMON_CANCEL",
        // both are defaulted to EAnchor.Center but just for clearity we're explicitly including them
        m_parentAnchor = EAnchor.Center, m_coordsOrigin = EAnchor.Center,
    };

    // to see the list of all built-in GUI elements see subtypes of CGui.
    // for more examples of using GUI elements see SScreen* classes

    public override void OnInit() {
        // modal screen is a screen which blocks all inputs in every other active screen
        m_isModal = true;
    }
    public override void OnUpdate() {
        if (btBack.IsClicked() || SInputs.IsEscapePressedInScreen(this)) {
            Deactivate();
        }
    }
}

[BepInPlugin("example-mod", ThisPluginInfo.Name, ThisPluginInfo.Version)]
// You need to necessarily include dependency to DODModAPI plugin
[BepInDependency(DODModAPI.DODModAPIPlugin.GUID)]
public sealed class ExampleMod : BaseUnityPlugin {
    private void Awake() {
        // Register textures generated by AssetPacker
        DODModAPI.SpriteManager.RegisterTexture(ExampleAssets.TileSpritesheetResource); // from ITEM directive
        DODModAPI.SpriteManager.RegisterTexture(ExampleAssets.SpritesAtlasResource); // from SPRITE directive
        DODModAPI.SpriteManager.RegisterTexture(ExampleAssets.SurfaceTopsResource); // from SURFACE directive
        DODModAPI.SpriteManager.RegisterTexture(ExampleAssets.exampleSurface_surfaceMaterial); // from SURFACE directive
        DODModAPI.SpriteManager.RegisterTexture(ExampleAssets.UnitsAtlasResource_100); // from UNIT directive

        // Register items and recipes
        DODModAPI.ItemManager.RegisterAllItems(typeof(ExampleItems));
        DODModAPI.ItemManager.RegisterRecipeGroup(ExampleItems.exampleRecipeGroup);

        // Register units
        DODModAPI.UnitManager.RegisterUnit(ExampleUnits.exampleMonster);

        // Register events
        // adds new event (Hazardous Environment) to the game
        DODModAPI.EventManager.Register(ExampleEvent.inst);

        // Register new mode (custom world)
        // the setup parameter is used as a replacement for lua files (see built-in modes in DigOrDie_Data/StreamingAssets/Mods for examples)
        DODModAPI.ModeManager.Register<ExampleMode>(
            setup: script => {
                script.Globals["mod"] = new ExampleMode();
                script.Globals["params"] = new CParams();
            },
            modeId: "ExampleMode",
            modeName: "Example Mode (DODModAPI)",
            modeDescription: ": Example Mode description" // you need to include prefix ": " to make world description look cleaner
        );

        // Register save handler
        // allows to store/read custom mod data directly in the save files
        DODModAPI.SaveManager.Register(new ExampleSaveHandler());

        // Register a custom network message
        // they are used to transfer information between clients (P2P), allowing the clients to synchronize between eachother
        // since we're didn't assign an specific message ID to it, the DODModAPI will automatically assign it itself later
        DODModAPI.NetworkManager.Register(ExampleNetworkMessage.Inst);
        DODModAPI.NetworkManager.Register(ExampleDynamicNetworkMessage.Inst);

        // Register new screen
        DODModAPI.ScreenManager.Register<ExampleScreen>();

        // Add custom command
        // for more examples see better-chat plugin
        DODModAPI.CommandManager.Register("/example-command", new() {
            Local = true, // Only executes on the client, doesn't sync to server
            DisableAchievements = true, // disables achievements on a command use
            TabCompleter = argIdx => argIdx == 0 ? ["spawn", "heal", "event", "network", "screen"] : null, // Provide tab-completion suggestions
            Overwrite = false, // if true, force overwrites command with the identical name
        }, (args) => {
            // you can use args.Arg* methods for easier parsing of input command arguments
            string subCommand = args.ArgString("subcommand");

            if (subCommand == "spawn") {
                // supports relative coordinates (the "~" character)
                UnityEngine.Vector2 pos = args.ArgWorldPos("spawn position");
                args.ArgNone(); // none arguments after are expected. if user passed one, it will be threated as error

                SUnits.SpawnUnit(ExampleUnits.exampleMonster.UnitDesc, pos);
                DODModAPI.Misc.SendChatMessageLocal($"Spawned Example Monster at {pos}");
            } else if (subCommand == "heal") {
                args.ArgNone();

                // args.PlayerSender - implicit command argument that denotes the player which executed command
                // since the game command sync is done via sending identical commands to everyone in the lobby
                // to ensure everyone have the same game state, the command is executed from each client's perspective.
                // if you use local client state in the commands, they will apply independently to everyone in the lobby,
                // breaking the game state sync between all clients.
                args.PlayerSender.m_unitPlayer.m_hp = 100f;
                DODModAPI.Misc.SendChatMessageLocal("Set health to 100 HP!");
            } else if (subCommand == "event") {
                args.ArgNone();

                // start event after 5 seconds
                DODModAPI.EventManager.Trigger(ExampleEvent.inst, delay: 5f);

                DODModAPI.Misc.SendChatMessageLocal("Triggered example event!");
            } else if (subCommand == "network") {
                var value = args.ArgEnum<ExampleMessageType>();
                args.ArgNone();

                if (value == ExampleMessageType.Simple) {
                    // send example network message with the following info to everyone in the lobby (passed 0 to Send_Start)
                    ExampleNetworkMessage.Inst.Send(123f, 456);
                    DODModAPI.Misc.SendChatMessageLocal($"Sended {nameof(ExampleNetworkMessage)} to everyone");
                } else if (value == ExampleMessageType.Dynamic) {
                    // send example network message with the following info to everyone in the lobby (passed 0 to Send_Start)
                    ExampleDynamicNetworkMessage.Inst.Send([2, 3, 5, 7, 11, 13, 17]);
                    DODModAPI.Misc.SendChatMessageLocal($"Sended {nameof(ExampleDynamicNetworkMessage)} to everyone");
                }
            } else if (subCommand == "screen") {
                args.ArgNone();
                // Show the example screen
                ExampleScreen.Inst.Activate();
            } else {
                // will display error message to the chat
                throw new DODModAPI.CommandException("Unknown subcommand. Use 'spawn', 'heal', 'event', 'network' or 'screen'.", args.Index);
            }
        });

        // Register a chat preprocessor which will run before the command is executed directly on the chat message text
        DODModAPI.CommandManager.RegisterChatPreprocessor(priority: 10, (ref string text) => {
            // replace :) text with a star symbol
            text = text.Replace(":)", "\u2605");

            if (text.Contains(":(")) {
                // block message
                return false;
            }
            return true;
        });

        // Add custom localization
        DODModAPI.Misc.AddLocalizationText("EXAMPLE_HELLO", "Hello from the Example Mod!");

        // Similar to System.Runtime.CompilerServices.ConditionalWeakTable from NET 4.0 (we're on NET 3.5)
        // use it to attach additional information for specific object instances that you cannot add fields to.
        // WeakTable will skip and remove dead references to objects instead of memory leaking them
        var weakTable = new DODModAPI.WeakTable<CUnit, int>();
        // TODO: weakTable examples

        // Apply Harmony patches
        var harmony = new Harmony(Info.Metadata.GUID);
        harmony.PatchAll(typeof(ExampleMod));
    }

    // Example usage of CodeCursor (the replacement of Harmony.CodeMatcher with better API)
    [HarmonyTranspiler]
    [HarmonyPatch(typeof(CUnitPlayerLocal), nameof(CUnitPlayerLocal.Update))]
    private static IEnumerable<CodeInstruction> CUnitPlayerLocal_Update(IEnumerable<CodeInstruction> instructions, ILGenerator generator) {
        // roughly doubles the jump of player character
        return new DODModAPI.CodeCursor(instructions, generator)
            .RepeatNTimes(2, cc => cc
                .FindNext(
                    new(OpCodes.Ldfld, typeof(UnityEngine.Vector2).Field("y")),
                    new(OpCodes.Ldc_R4, 7f))
                .Advance(1)
                .Replace(OpCodes.Ldc_R4, 14f)
            )
            .Finish();
    }
}
