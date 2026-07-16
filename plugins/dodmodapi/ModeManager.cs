
using HarmonyLib;
using System.Collections.Generic;
using System.Reflection.Emit;
using DODModAPI.Extensions;

using Lua = MoonSharp.Interpreter;
using System;

namespace DODModAPI;

public static class ModeManager {
    private static readonly Dictionary<string, ModeLuaSetup> _modeLuaSetups = new();

    public delegate void ModeLuaSetup(Lua.Script script);

    public static void Register<T>(ModeLuaSetup setup, string modeId, string modeName, string modeDescription) where T : CMode {
        if (_modeLuaSetups.ContainsKey(modeId)) {
            throw new ArgumentException($"[ModeManager] Duplicate mode ID \"{modeId}\"", nameof(modeId));
        }

        SOutgame.ModNames.Add(modeId);
        Misc.AddLocalizationText($"GAMEMODE_{modeId}", modeName);
        Misc.AddLocalizationText($"GAMEMODEDESC_{modeId}", modeDescription);

        Lua.UserData.RegisterType<T>();

        _modeLuaSetups.Add(modeId, setup);
    }

    internal static class Patches {
        //private static readonly string _originalAssemblyName = typeof(SDataLua).Assembly.FullName;

        //[HarmonyPatch(typeof(SDataLua), nameof(SDataLua.LuaCreate))]
        //[HarmonyPrefix]
        //private static bool SDataLua_LuaCreate(string className, ref object __result) {
        //    if (_modeFactories.TryGetValue(className, out Func<CMode> instMaker)) {
        //        __result = instMaker();
        //    } else {
        //        __result = Activator.CreateInstance(_originalAssemblyName, className).Unwrap();
        //    }
        //    return false;
        //}

        [HarmonyPatch(typeof(SDataLua), nameof(SDataLua.OnInit))]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> SDataLua_OnInit(IEnumerable<CodeInstruction> instructions, ILGenerator generator) {
            static bool ExecModScripts(Lua.Script script, int modIdx) {
                string modeId = SOutgame.ModNames[modIdx];
                if (_modeLuaSetups.TryGetValue(modeId, out ModeLuaSetup setup)) {
                    setup(script);

                    // verify the required globals "mod" and "params" since it's an error to leave them out

                    var modVal = script.Globals.Get("mod");
                    if (modVal.Type != Lua.DataType.UserData || modVal.UserData.Object is not CMode modeObj) {
                        DODModAPIPlugin.Log.LogError($"[ModeManager] \"{modeId}\": global \"mod\" is not \"CMode\". Found Lua type: \"{modVal.Type}\", as C# object: \"{modVal.UserData?.Object?.GetType().FullName ?? "null"}\"");
                        return true; // skipping lua file module main.lua and params.lua from loading
                    }
                    if (modeObj.m_name is null) {
                        // auto set mode name only if it's left untouched; otherwise, let the user select one (although, it's always an error to assign the non-modeId one)
                        modeObj.m_name = modeId;
                    }

                    var paramsVal = script.Globals.Get("params");
                    if (paramsVal.Type != Lua.DataType.UserData || paramsVal.UserData.Object is not CParams) {
                        DODModAPIPlugin.Log.LogError($"[ModeManager] \"{modeId}\": global \"params\" is not \"CParams\". Found Lua type: \"{paramsVal.Type}\", as C# object: \"{paramsVal.UserData?.Object?.GetType().FullName ?? "null"}\"");
                        return true; // skipping lua file module main.lua and params.lua from loading
                    }
                    return true;
                }
                return false;
            }
            return new CodeCursor(instructions, generator)
                .FindNext(out uint instrsNum,
                    new(OpCodes.Ldloc_S, 4),
                    new(OpCodes.Ldloc_S, 4),
                    new(OpCodes.Ldstr, "main"),
                    new(OpCodes.Ldnull),
                    new(OpCodes.Callvirt, typeof(Lua.Script).Method(nameof(Lua.Script.RequireModule))),
                    new(OpCodes.Callvirt, typeof(Lua.Script).Method<Lua.DynValue>(nameof(Lua.Script.Call))),
                    new(OpCodes.Pop))
                .Insert(
                    new(OpCodes.Ldloc_S, (byte)4), // load "script" local
                    new(OpCodes.Ldloc_S, (byte)6), // load "j" local
                    Transpilers.EmitDelegate(ExecModScripts))
                .InsertBranch(OpCodes.Brtrue, offset: (int)instrsNum) // skip lua file module loading if true
                .Finish();
        }
        [HarmonyPatch(typeof(SDataLua), nameof(SDataLua.WriteDefaultParamsValues))]
        [HarmonyPrefix]
        private static bool SDataLua_WriteDefaultParamsValues(CMode mod) {
            if (_modeLuaSetups.ContainsKey(mod.m_name)) {
                return false;
            }
            return true;
        }
    }
}

public static class StandardLuaDescs {
    public const string ModId = "mod";
    public static CMode? Mod => SDataLua.GetDesc<CMode>(ModId);

    public const string ParamsId = "params";
    public static CParams? Params => SDataLua.GetDesc<CParams>(ParamsId);

    public const string ParamsDefaultId = "paramsDefault";
    public static CParams? ParamsDefault => SDataLua.GetDesc<CParams>(ParamsDefaultId);

    public const string ListBackgroundsId = "list_backgrounds";
    public static List<CBackground>? ListBackgrounds => SDataLua.GetDesc<CDescList>(ListBackgroundsId)?.GetList<CBackground>();

    public const string ListEnvironmentsId = "list_environments";
    public static List<CEnvironment>? ListEnvironments => SDataLua.GetDesc<CDescList>(ListEnvironmentsId)?.GetList<CEnvironment>();

    public const string ListMusicsId = "list_musics";
    public static List<CMusic>? ListMusics => SDataLua.GetDesc<CDescList>(ListMusicsId)?.GetList<CMusic>();

    public const string ListRecipesGroupsId = "list_recipesgroups";
    public static List<CRecipesGroup>? ListRecipesGroups => SDataLua.GetDesc<CDescList>(ListRecipesGroupsId)?.GetList<CRecipesGroup>();

    public const string ListAiMessagesSkippedId = "aiMessagesSkipped";
    public static List<string>? ListAiMessagesSkipped => SDataLua.GetDesc<CDescList>(ListAiMessagesSkippedId)?.GetList<string>();

    public static class Sounds {
        public const string JumpId = "jump";
        public static CSound? Jump => SDataLua.GetDesc<CSound>(JumpId);
        public const string FallId = "fall";
        public static CSound? Fall => SDataLua.GetDesc<CSound>(FallId);
        public const string FallWaterId = "fall_water";
        public static CSound? FallWater => SDataLua.GetDesc<CSound>(FallWaterId);
        public const string HurtId = "hurt";
        public static CSound? Hurt => SDataLua.GetDesc<CSound>(HurtId);
        public const string OutOfAmmoId = "outOfAmmo";
        public static CSound? OutOfAmmo => SDataLua.GetDesc<CSound>(OutOfAmmoId);
        public const string PlasmaId = "plasma";
        public static CSound? Plasma => SDataLua.GetDesc<CSound>(PlasmaId);
        public const string ShotgunId = "shotgun";
        public static CSound? Shotgun => SDataLua.GetDesc<CSound>(ShotgunId);
        public const string PlasmaSnipeId = "plasmaSnipe";
        public static CSound? PlasmaSnipe => SDataLua.GetDesc<CSound>(PlasmaSnipeId);
        public const string LaserId = "laser";
        public static CSound? Laser => SDataLua.GetDesc<CSound>(LaserId);
        public const string ParticleId = "particle";
        public static CSound? Particle => SDataLua.GetDesc<CSound>(ParticleId);
        public const string ParticleShotgunId = "particleShotgun";
        public static CSound? ParticleShotgun => SDataLua.GetDesc<CSound>(ParticleShotgunId);
        public const string StormId = "storm";
        public static CSound? Storm => SDataLua.GetDesc<CSound>(StormId);
        public const string StormLightId = "stormLight";
        public static CSound? StormLight => SDataLua.GetDesc<CSound>(StormLightId);
        public const string RocketFireId = "rocketFire";
        public static CSound? RocketFire => SDataLua.GetDesc<CSound>(RocketFireId);
        public const string RocketHitId = "rocketHit";
        public static CSound? RocketHit => SDataLua.GetDesc<CSound>(RocketHitId);
        public const string DefensePlasmaId = "defensePlasma";
        public static CSound? DefensePlasma => SDataLua.GetDesc<CSound>(DefensePlasmaId);
        public const string ParticleTurretId = "particleTurret";
        public static CSound? ParticleTurret => SDataLua.GetDesc<CSound>(ParticleTurretId);
        public const string MineId = "mine";
        public static CSound? Mine => SDataLua.GetDesc<CSound>(MineId);
        public const string CeilingTurretId = "ceilingTurret";
        public static CSound? CeilingTurret => SDataLua.GetDesc<CSound>(CeilingTurretId);
        public const string FireflyId = "firefly";
        public static CSound? Firefly => SDataLua.GetDesc<CSound>(FireflyId);
        public const string HoundId = "hound";
        public static CSound? Hound => SDataLua.GetDesc<CSound>(HoundId);
        public const string DwellerId = "dweller";
        public static CSound? Dweller => SDataLua.GetDesc<CSound>(DwellerId);
        public const string DwellerBossId = "dwellerBoss";
        public static CSound? DwellerBoss => SDataLua.GetDesc<CSound>(DwellerBossId);
        public const string FishId = "fish";
        public static CSound? Fish => SDataLua.GetDesc<CSound>(FishId);
        public const string BirdBombId = "birdBomb";
        public static CSound? BirdBomb => SDataLua.GetDesc<CSound>(BirdBombId);
        public const string MonsterBatId = "monsterBat";
        public static CSound? MonsterBat => SDataLua.GetDesc<CSound>(MonsterBatId);
        public const string AntId = "ant";
        public static CSound? Ant => SDataLua.GetDesc<CSound>(AntId);
        public const string BossCrabId = "bossCrab";
        public static CSound? BossCrab => SDataLua.GetDesc<CSound>(BossCrabId);
        public const string BossCrabScreamId = "bossCrabScream";
        public static CSound? BossCrabScream => SDataLua.GetDesc<CSound>(BossCrabScreamId);
        public const string BossBirdId = "bossBird";
        public static CSound? BossBird => SDataLua.GetDesc<CSound>(BossBirdId);
        public const string MiniBalrogId = "miniBalrog";
        public static CSound? MiniBalrog => SDataLua.GetDesc<CSound>(MiniBalrogId);
        public const string SpidersId = "spiders";
        public static CSound? Spiders => SDataLua.GetDesc<CSound>(SpidersId);
        public const string BalrogId = "balrog";
        public static CSound? Balrog => SDataLua.GetDesc<CSound>(BalrogId);
        public const string MonsterParticleGroundId = "monsterParticleGround";
        public static CSound? MonsterParticleGround => SDataLua.GetDesc<CSound>(MonsterParticleGroundId);
        public const string MonsterParticleId = "monsterParticle";
        public static CSound? MonsterParticle => SDataLua.GetDesc<CSound>(MonsterParticleId);
        public const string MiniaturizorId = "miniaturizor";
        public static CSound? Miniaturizor => SDataLua.GetDesc<CSound>(MiniaturizorId);
        public const string RainId = "rain";
        public static CSound? Rain => SDataLua.GetDesc<CSound>(RainId);
        public const string RocketCinematicId = "rocketCinematic";
        public static CSound? RocketCinematic => SDataLua.GetDesc<CSound>(RocketCinematicId);
        public const string RocketExplosionId = "rocketExplosion";
        public static CSound? RocketExplosion => SDataLua.GetDesc<CSound>(RocketExplosionId);
        public const string JetpackId = "jetpack";
        public static CSound? Jetpack => SDataLua.GetDesc<CSound>(JetpackId);
        public const string WaterfallId = "waterfall";
        public static CSound? Waterfall => SDataLua.GetDesc<CSound>(WaterfallId);
        public const string LavaId = "lava";
        public static CSound? Lava => SDataLua.GetDesc<CSound>(LavaId);
        public const string FireForestId = "fireForest";
        public static CSound? FireForest => SDataLua.GetDesc<CSound>(FireForestId);
        public const string DoorOpenId = "doorOpen";
        public static CSound? DoorOpen => SDataLua.GetDesc<CSound>(DoorOpenId);
        public const string DoorCloseId = "doorClose";
        public static CSound? DoorClose => SDataLua.GetDesc<CSound>(DoorCloseId);
        public const string TeleportId = "teleport";
        public static CSound? Teleport => SDataLua.GetDesc<CSound>(TeleportId);
        public const string PotionsId = "potions";
        public static CSound? Potions => SDataLua.GetDesc<CSound>(PotionsId);
        public const string FireImpactId = "fireImpact";
        public static CSound? FireImpact => SDataLua.GetDesc<CSound>(FireImpactId);
        public const string LavaEruptionId = "lavaEruption";
        public static CSound? LavaEruption => SDataLua.GetDesc<CSound>(LavaEruptionId);
        public const string AlarmId = "alarm";
        public static CSound? Alarm => SDataLua.GetDesc<CSound>(AlarmId);
    }
}
