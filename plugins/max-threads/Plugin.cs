using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using DODModAPI.Extensions;
using System.Collections.Generic;
using System.Reflection.Emit;
using DODModAPI;

[BepInPlugin("max-threads", ThisPluginInfo.Name, ThisPluginInfo.Version)]
[BepInDependency(DODModAPI.DODModAPIPlugin.GUID)]
public class MaxThreads : BaseUnityPlugin {
    private static ConfigEntry<uint> configOverrideThreadsNumber = null!;

    private void Awake() {
        configOverrideThreadsNumber = Config.Bind<uint>(
            section: "General", key: "OverrideThreadsNumber",
            defaultValue: 0, description: "If non-zero, overrides the number of threads used in world simulation"
        );
        var configEnabled = Config.Bind<bool>(
            section: "General", key: "Enabled", defaultValue: true
        );
        if (!configEnabled.Value) { return; }

        var harmony = new Harmony(Info.Metadata.GUID);
        harmony.PatchAll(typeof(MaxThreads));
    }

    [HarmonyTranspiler]
    [HarmonyPatch(typeof(SWorldDll), nameof(SWorldDll.OnInit))]
    private static IEnumerable<CodeInstruction> SWorldDll_OnInit_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator) {
        static int GetThreadsCount() {
            if (configOverrideThreadsNumber.Value != 0) {
                return (int)configOverrideThreadsNumber.Value;
            }
            return System.Environment.ProcessorCount;
        }
        return new CodeCursor(instructions, generator)
            .FindNext(
                new(OpCodes.Ldc_I4_4),
                new(OpCodes.Call, typeof(SWorldDll).Method("DllInit")))
            .Replace(Transpilers.EmitDelegate(GetThreadsCount), out _)
            .Finish();
    }
}
