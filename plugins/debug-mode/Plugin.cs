using BepInEx;
using HarmonyLib;
using ModUtils.Extensions;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using UnityEngine;

internal static class EnableDebugModePatch {
    [HarmonyTranspiler]
    [HarmonyPatch(typeof(SScreenDebug), nameof(SScreenDebug.OnUpdate))]
    private static IEnumerable<CodeInstruction> SScreenDebug_OnUpdate(IEnumerable<CodeInstruction> instructions, ILGenerator generator) {
        var codeMatcher = new CodeMatcher(instructions, generator);

        codeMatcher.Start()
            .MatchForward(useEnd: false,
                new(OpCodes.Call, typeof(Application).Method("get_isEditor")),
                new(OpCodes.Brtrue),

                new(OpCodes.Call, typeof(SNetwork).Method("get_MySteamID")),
                new(OpCodes.Ldc_I8),
                new(OpCodes.Beq),

                new(OpCodes.Call, typeof(SNetwork).Method("get_MySteamID")),
                new(OpCodes.Ldc_I8),
                new(OpCodes.Bne_Un))
            .ThrowIfInvalid("(1)")
            .RemoveInstructions(8);

        return codeMatcher.Instructions();
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
        var codeMatcher = new CodeMatcher(instructions, generator);

        codeMatcher.Start()
            .MatchForward(useEnd: false,
                new(OpCodes.Call, typeof(UnityEngine.Application).Method("get_isEditor")),
                new(OpCodes.Brfalse),
                new(OpCodes.Ldsfld, typeof(G).StaticField("m_autoCreateMode")),
                new(OpCodes.Brfalse),
                new(OpCodes.Ldsfld, typeof(G).StaticField("m_autoCreateMode_Fast")),
                new(OpCodes.Brfalse),
                new(OpCodes.Call, typeof(UnityEngine.Time).Method("get_time")),
                new(OpCodes.Ldc_R4, 5.0f),
                new(OpCodes.Bge_Un),
                new(OpCodes.Br))
            .ThrowIfInvalid("(1)")
            .CollapseInstructions(9) // keep last `br` instruction to skip loop body
            .MatchForward(useEnd: false,
                new(OpCodes.Call, typeof(UnityEngine.Application).Method("get_isEditor")),
                new(OpCodes.Brfalse),
                new(OpCodes.Ldsfld, typeof(G).StaticField("m_autoCreateMode")),
                new(OpCodes.Brfalse),
                new(OpCodes.Ldsfld, typeof(G).StaticField("m_autoCreateMode_Fast")),
                new(OpCodes.Brfalse),
                new(OpCodes.Call, typeof(UnityEngine.Time).Method("get_time")),
                new(OpCodes.Ldc_R4, 5.0f),
                new(OpCodes.Bge_Un),
                new(OpCodes.Br))
            .ThrowIfInvalid("(2)")
            .CollapseInstructions(9); // keep last `br` instruction to skip loop body

        return codeMatcher.Instructions();
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

[BepInPlugin("debug-mode", "Debug Mode", "1.0.0")]
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
        InitDebugVarsConfig();

        if (!configEnable.Value) {
            return;
        }
        var harmony = new Harmony(Info.Metadata.GUID);

        harmony.PatchAll(typeof(EnableDebugModePatch));
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
    }
}

