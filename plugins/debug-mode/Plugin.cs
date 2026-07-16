using BepInEx;
using HarmonyLib;
using DODModAPI.Extensions;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using UnityEngine;

internal static class EnableDebugModePatch {
    [HarmonyTranspiler]
    [HarmonyPatch(typeof(SScreenDebug), nameof(SScreenDebug.OnUpdate))]
    private static IEnumerable<CodeInstruction> SScreenDebug_OnUpdate(IEnumerable<CodeInstruction> instructions, ILGenerator generator) {
        return new DODModAPI.CodeCursor(instructions, generator)
            .FindNext(out uint instrNum,
                new(OpCodes.Call, typeof(Application).Method("get_isEditor")),
                new(OpCodes.Brtrue),

                new(OpCodes.Call, typeof(SNetwork).Method("get_MySteamID")),
                new(OpCodes.Ldc_I8),
                new(OpCodes.Beq),

                new(OpCodes.Call, typeof(SNetwork).Method("get_MySteamID")),
                new(OpCodes.Ldc_I8),
                new(OpCodes.Bne_Un))
            .Remove(instrNum)
            .Finish();
    }
}
internal static class ApplicationIsEditorPatch {
    [HarmonyTranspiler]
    [HarmonyPatch(typeof(Application), nameof(Application.isEditor), MethodType.Getter)]
    private static IEnumerable<CodeInstruction> Application_isEditor() {
        return [new(OpCodes.Ldc_I4_1), new(OpCodes.Ret)];
    }
}
internal static class NoWorldPresimulationPatch {
    [HarmonyTranspiler]
    [HarmonyPatch(typeof(SGameStartEnd), nameof(SGameStartEnd.GenerateWorld), MethodType.Enumerator)]
    private static IEnumerable<CodeInstruction> SGameStartEnd_GenerateWorld(IEnumerable<CodeInstruction> instructions, ILGenerator generator) {
        return new DODModAPI.CodeCursor(instructions, generator)
            .RepeatNTimes(count: 2, cc =>
                cc.FindNext(out uint instrNum,
                    new(OpCodes.Call, typeof(UnityEngine.Application).Method("get_isEditor")),
                    new(OpCodes.Brfalse),
                    new(OpCodes.Ldsfld, typeof(G).StaticField("m_autoCreateMode")),
                    new(OpCodes.Brfalse),
                    new(OpCodes.Ldsfld, typeof(G).StaticField("m_autoCreateMode_Fast")),
                    new(OpCodes.Brfalse),
                    new(OpCodes.Call, typeof(UnityEngine.Time).Method("get_time")),
                    new(OpCodes.Ldc_R4, 5.0f),
                    new(OpCodes.Bge_Un)
                ).Remove(instrNum))
            .Finish();
    }
}

internal static class DebugDrawLinePatch {
    public static readonly List<LineData> _activeLines = [];

    public struct LineData {
        public Vector3 start;
        public Vector3 end;
        public UnityEngine.Color color;
        public float endTime;
    }

    public class DebugLineRenderer : MonoBehaviour {
        private Material _lineMaterial;

        private void Start() {
            _lineMaterial = new Material(Shader.Find("Hidden/Internal-Colored")) {
                hideFlags = HideFlags.HideAndDontSave
            };
            _lineMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            _lineMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            _lineMaterial.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
            _lineMaterial.SetInt("_ZWrite", 1);
            _lineMaterial.SetInt("_ZTest", (int)UnityEngine.Rendering.CompareFunction.LessEqual);
        }
        private void Update() {
            float currentTime = Time.time;
            _activeLines.RemoveAll(line => line.endTime <= currentTime);
        }
        private void OnRenderObject() {
            _lineMaterial.SetPass(0);
            GL.Begin(GL.LINES);
            foreach (var line in _activeLines) {
                GL.Color(line.color);
                GL.Vertex(line.start);
                GL.Vertex(line.end);
            }
            GL.End();
        }
    }

#pragma warning disable Harmony003 // Harmony non-ref patch parameters modified
    [HarmonyPrefix]
    [HarmonyPatch(typeof(UnityEngine.Debug), nameof(UnityEngine.Debug.DrawLine),
        typeof(Vector3), typeof(Vector3), typeof(UnityEngine.Color), typeof(float)
    )]
    private static bool UnityEngine_Debug_DrawLine(Vector3 start, Vector3 end, UnityEngine.Color color, float duration) {
        _activeLines.Add(new LineData() {
            start = start,
            end = end,
            color = color,
            endTime = Time.time + duration
        });
        return false;
    }
    [HarmonyPrefix]
    [HarmonyPatch(typeof(UnityEngine.Debug), nameof(UnityEngine.Debug.DrawLine),
        typeof(Vector3), typeof(Vector3), typeof(UnityEngine.Color)
    )]
    private static bool UnityEngine_Debug_DrawLine(Vector3 start, Vector3 end, UnityEngine.Color color) {
        _activeLines.Add(new LineData() {
            start = start,
            end = end,
            color = color,
            endTime = Time.time + 0.001f
        });
        return false;
    }
#pragma warning restore Harmony003 // Harmony non-ref patch parameters modified
}

internal static class DontIncrementVersionBuildPatch {
    [HarmonyTranspiler]
    [HarmonyPatch(typeof(SMain), nameof(SMain.Awake))]
    private static IEnumerable<CodeInstruction> SMain_Awake(IEnumerable<CodeInstruction> instructions, ILGenerator generator) {
        return new DODModAPI.CodeCursor(instructions, generator)
            .FindNext(out uint n,
                new(OpCodes.Call, typeof(UnityEngine.Application).Method("get_isEditor")),
                new(OpCodes.Brfalse),
                new(OpCodes.Ldc_I4_0),
                new(OpCodes.Br),
                new(OpCodes.Ldc_I4_1),
                new(OpCodes.Add))
            .Remove(6)
            .Finish();
    }
}

internal static class ExtraDebugChecksPatch {
    [HarmonyTranspiler]
    [HarmonyPatch(typeof(CUnitDefense), nameof(CUnitDefense.GetUnitTargetPos))]
    private static IEnumerable<CodeInstruction> CUnitDefense_GetUnitTargetPos(IEnumerable<CodeInstruction> instructions, ILGenerator generator) {
        return new DODModAPI.CodeCursor(instructions, generator)
            .FindNext(
                // insert here
                new(OpCodes.Ldloc_S),
                new(OpCodes.Call),
                new(OpCodes.Ldloc_S),
                new(OpCodes.Callvirt),
                new(OpCodes.Call),
                new(OpCodes.Call),
                new(OpCodes.Call, typeof(UnityEngine.Debug).Method<Vector3, Vector3, Color>("DrawLine"))
            // label1:
            )
            .CreateLabel(offset: 7, out Label label1)
            .Insert(
                new(OpCodes.Ldsfld, typeof(G).StaticField("m_debugDefenses")),
                new(OpCodes.Ldsfld, typeof(G).StaticField("m_debug")),
                new(OpCodes.And),
                new(OpCodes.Brfalse, label1))
            .Advance(7)
            .FindNext(
                new(OpCodes.Ldloc_S),
                new(OpCodes.Call),
                new(OpCodes.Ldloc_S),
                new(OpCodes.Callvirt),
                new(OpCodes.Call),
                new(OpCodes.Call),
                new(OpCodes.Call, typeof(UnityEngine.Debug).Method<Vector3, Vector3, Color>("DrawLine"))
            // label2:
            )
            .CreateLabel(offset: 7, out Label label2)
            .Inject(
                new(OpCodes.Ldsfld, typeof(G).StaticField("m_debugDefenses")),
                new(OpCodes.Ldsfld, typeof(G).StaticField("m_debug")),
                new(OpCodes.And),
                new(OpCodes.Brfalse, label2))
            .Finish();
    }
}

[BepInPlugin("debug-mode", ThisPluginInfo.Name, ThisPluginInfo.Version)]
[BepInDependency(DODModAPI.DODModAPIPlugin.GUID)]
public class DebugMode : BaseUnityPlugin {

    private void InitDebugVarsConfig() {
        void RegisterDebugConfig(string name, Action<bool> setter) {
            var entry = Config.Bind<bool>(section: "Debug", key: name, defaultValue: false);
            entry.SettingChanged += (sender, args) => setter(entry.Value);
            setter(entry.Value);
        }

        RegisterDebugConfig("DrawAllBackgrounds", v => G.m_debugDrawAllBackgrounds = v);
        RegisterDebugConfig("Bullets", v => G.m_debugBullets = v);
        RegisterDebugConfig("Pathfinding", v => G.m_debugPF = v);
        RegisterDebugConfig("PathfindingDetails", v => G.m_debugPFDetails = v);
        RegisterDebugConfig("Collisions", v => G.m_debugCols = v);
        RegisterDebugConfig("Units", v => G.m_debugUnits = v);
        RegisterDebugConfig("UnitNetworkControl", v => G.m_debugUnitNetworkControl = v);
        RegisterDebugConfig("Defenses", v => G.m_debugDefenses = v);
        RegisterDebugConfig("Water", v => G.m_debugWater = v);
        RegisterDebugConfig("Light", v => G.m_debugLight = v);
        RegisterDebugConfig("Crashes", v => G.m_debugCrashes = v);
        RegisterDebugConfig("CrashesFull", v => G.m_debugCrashesFull = v);
    }

    private void Awake() {
        var configEnable = Config.Bind<bool>(
            section: "General", key: "Enable", defaultValue: true,
            description: "Enables the plugin"
        );
        var configIsEditor = Config.Bind<bool>(
            section: "StartUp", key: "IsEditor", defaultValue: true,
            description: "Forces `Application.isEditor` to always return `true`"
        );
        var configNoWorldPresimulation = Config.Bind<bool>(
            section: "StartUp", key: "NoWorldPresimulation", defaultValue: false,
            description: "Disables world presimulation (e.g. no initial water and plants are generated)"
        );
        var configInterceptDebugRendering = Config.Bind<bool>(
            section: "StartUp", key: "InterceptDebugRendering", defaultValue: true,
            description: "Use custom drawer for UnityEngine.Debug.DrawLine methods. " +
                         "Note that without intercepting Debug.DrawLine calls they do basically nothing"
        );
        var configIncrementVersionBuild = Config.Bind<bool>(
            section: "StartUp", key: "IncrementVersionBuild", defaultValue: false,
            description: "If config `IsEditor` is enabled, will increment the version build number (this is the default behavior in the game)"
        );

        InitDebugVarsConfig();

        if (!configEnable.Value) {
            return;
        }
        var harmony = new Harmony(Info.Metadata.GUID);

        harmony.PatchAll(typeof(EnableDebugModePatch));
        harmony.PatchAll(typeof(ExtraDebugChecksPatch));
        if (configNoWorldPresimulation.Value) {
            harmony.PatchAll(typeof(NoWorldPresimulationPatch));
        }
        if (configIsEditor.Value) {
            harmony.PatchAll(typeof(ApplicationIsEditorPatch));
        }
        if (configInterceptDebugRendering.Value) {
            gameObject.AddComponent<DebugDrawLinePatch.DebugLineRenderer>();
            harmony.PatchAll(typeof(DebugDrawLinePatch));
        }
        if (!configIncrementVersionBuild.Value) {
            harmony.PatchAll(typeof(DontIncrementVersionBuildPatch));
        }
    }
}

