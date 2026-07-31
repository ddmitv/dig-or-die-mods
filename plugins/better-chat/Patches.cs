
using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using DODModAPI;
using DODModAPI.Extensions;
using HarmonyLib;
using UnityEngine;

internal static class RepeatLastCommandPatch {
    [HarmonyPatch(typeof(SScreenHudChat), nameof(SScreenHudChat.OnUpdate))]
    [HarmonyPostfix]
    private static void SScreenHudChat_OnUpdate() {
        if (SInputs.GetKeyDown(BetterChat.configRepeatLastCommand.Value.MainKey)) {
            var networkCommands = SSingleton<SNetworkCommands>.Inst;

            if (networkCommands.m_historyCommands.Count == 0) { return; }

            string prevCommand = networkCommands.m_historyCommands[networkCommands.m_historyIndex - 1];
            networkCommands.ProcessCommand(prevCommand, SNetwork.GetMyPlayer());
        }
    }
}

internal static class FullChatHistoryPatch {
    [HarmonyPatch(typeof(SNetworkCommands), nameof(SNetworkCommands.ProcessCommand))]
    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> SNetworkCommands_ProcessCommand(IEnumerable<CodeInstruction> instructions, ILGenerator generator) {
        // Disable updating command history so that it doesn't interfere with new approach
        return new CodeCursor(instructions, generator)
            .FindNext(out uint n,
                new(OpCodes.Ldarg_0),
                new(OpCodes.Ldfld, typeof(SNetworkCommands).Field("m_historyCommands")),
                new(OpCodes.Ldarg_1),
                new(OpCodes.Callvirt, typeof(List<string>).Method("Add")),
                new(OpCodes.Ldarg_0),
                new(OpCodes.Ldarg_0),
                new(OpCodes.Ldfld, typeof(SNetworkCommands).Field("m_historyCommands")),
                new(OpCodes.Callvirt, typeof(List<string>).Method("get_Count")),
                new(OpCodes.Stfld, typeof(SNetworkCommands).Field("m_historyIndex")))
            .Remove(n)
            .Finish();
    }
    [HarmonyPatch(typeof(SScreenHudChat), nameof(SScreenHudChat.OnUpdate))]
    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> SScreenHudChat_OnUpdate(IEnumerable<CodeInstruction> instructions, ILGenerator generator) {
        static void AddToChatHistory(SScreenHudChat self) {
            var networkCommands = SSingleton<SNetworkCommands>.Inst;
            var historyCommands = networkCommands.m_historyCommands;

            if (historyCommands.Count == 0 || historyCommands[historyCommands.Count - 1] != self.m_inputChat.m_text) {
                networkCommands.m_historyCommands.Add(self.m_inputChat.m_text);
            }
            networkCommands.m_historyIndex = networkCommands.m_historyCommands.Count;
        }

        return new CodeCursor(instructions, generator)
            .FindNextEnd(
                new(OpCodes.Ldc_I4_S, (sbyte)13),
                new(OpCodes.Call, typeof(SInputs).Method("GetKeyDown")),
                new(OpCodes.Brfalse),
                new(OpCodes.Ldarg_0),
                new(OpCodes.Ldfld, typeof(SScreenHudChat).Field("m_inputChat")),
                new(OpCodes.Ldfld, typeof(CGuiInput).Field("m_text")),
                new(OpCodes.Ldsfld, typeof(string).StaticField("Empty")),
                new(OpCodes.Call, typeof(string).Method("op_Inequality")),
                new(OpCodes.Brfalse))
            .Insert(
                new(OpCodes.Ldarg_0),
                Transpilers.EmitDelegate(AddToChatHistory))
            .Finish();
    }
}

internal static class FreecamModePatch {
    public static bool isInFreecamMode = false;
    public static Vector2 cameraPos = Vector2.zero;
    public static float cameraSpeed = 100f;

    [HarmonyPrefix]
    [HarmonyPatch(typeof(SGame), nameof(SGame.SetCameraPos))]
    private static bool SGame_SetCameraPos() {
        if (!isInFreecamMode) {
            return true;
        }
        float simuDeltaTime = Time.unscaledDeltaTime;
        float playerSpeed = cameraSpeed * (SInputs.shift.IsKey() ? 0.3f : 1f);
        if (SInputs.left.IsKey()) {
            cameraPos.x -= playerSpeed * simuDeltaTime;
        } else if (SInputs.right.IsKey()) {
            cameraPos.x += playerSpeed * simuDeltaTime;
        }
        if (SInputs.up.IsKey()) {
            cameraPos.y += playerSpeed * simuDeltaTime;
        } else if (SInputs.down.IsKey()) {
            cameraPos.y -= playerSpeed * simuDeltaTime;
        }
        Vector2 cameraSize = new Vector2(G.m_cameraWorld.orthographicSize * G.m_cameraWorld.aspect, G.m_cameraWorld.orthographicSize);
        cameraPos = SMisc.Clamp(cameraPos, cameraSize + Vector2.one * 2f, SWorld.Gs - cameraSize - Vector2.one * 4f);

        G.m_cameraWorld.orthographicSize = 12f / G.m_zoom;
        G.m_cameraWorld.transform.position = new Vector3(cameraPos.x, cameraPos.y, -10f);

        G.m_camMin = G.m_cameraWorld.ViewportToWorldPoint(Vector3.zero);
        G.m_camMax = G.m_cameraWorld.ViewportToWorldPoint(Vector3.one);
        G.m_camMin = SMisc.Clamp(G.m_camMin, SWorld.GridRectM2.min, SWorld.GridRectM2.max);
        G.m_camMax = SMisc.Clamp(G.m_camMax, SWorld.GridRectM2.min, SWorld.GridRectM2.max);

        return false;
    }
    private class FakeKeyBinding : SInputs.KeyBinding {
        public FakeKeyBinding() : base("", KeyCode.None, KeyCode.None, hideFromOptions: true, activeInMenus: false) { }

        public static FakeKeyBinding Inst = new();
    }
    private struct PrevKeyBindings {
        public SInputs.KeyBinding left;
        public SInputs.KeyBinding right;
        public SInputs.KeyBinding up;
        public SInputs.KeyBinding down;
    }
    [HarmonyPrefix]
    [HarmonyPatch(typeof(CUnitPlayerLocal), nameof(CUnitPlayerLocal.Update))]
    private static void CUnitPlayerLocal_Update_Prefix(ref PrevKeyBindings __state) {
        if (isInFreecamMode) {
            __state = new() {
                left = SInputs.left,
                right = SInputs.right,
                up = SInputs.up,
                down = SInputs.down
            };
            SInputs.left = FakeKeyBinding.Inst;
            SInputs.right = FakeKeyBinding.Inst;
            SInputs.up = FakeKeyBinding.Inst;
            SInputs.down = FakeKeyBinding.Inst;
        }
    }
    [HarmonyPostfix]
    [HarmonyPatch(typeof(CUnitPlayerLocal), nameof(CUnitPlayerLocal.Update))]
    private static void CUnitPlayerLocal_Update_Postfix(ref PrevKeyBindings __state) {
        if (isInFreecamMode) {
            SInputs.left = __state.left;
            SInputs.right = __state.right;
            SInputs.up = __state.up;
            SInputs.down = __state.down;
        }
    }
}

internal static class ClockCommandPatch {
    public static bool isPaused = false;

    [HarmonyPrefix]
    [HarmonyPatch(typeof(SGame), nameof(SGame.OnUpdateSimu))]
    private static void SGame_OnUpdateSimu_Prefix(ref float __state) {
        if (isPaused) {
            __state = SOutgame.Params.m_dayDurationTotal;
            SOutgame.Params.m_dayDurationTotal = float.PositiveInfinity;
        }
    }
    [HarmonyPostfix]
    [HarmonyPatch(typeof(SGame), nameof(SGame.OnUpdateSimu))]
    private static void SGame_OnUpdateSimu_Postfix(ref float __state) {
        if (isPaused) {
            SOutgame.Params.m_dayDurationTotal = __state;
        }
    }
}

internal static class FullbrightPatch {
    public static bool isEnabled = false;

    [HarmonyPostfix]
    [HarmonyPatch(typeof(SWorld), nameof(SWorld.GetLightColor))]
    private static void SWorld_GetLightColor_Postfix(ref Color __result) {
        if (!isEnabled) { return; }
        __result.r = Math.Max(__result.r, 0.45f);
        __result.g = Math.Max(__result.g, 0.45f);
        __result.b = Math.Max(__result.b, 0.45f);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(SWorld), nameof(SWorld.GetLightColor32))]
    private static void SWorld_GetLightColor32_Postfix(ref Color32 __result) {
        if (!isEnabled) { return; }
        __result.r = Math.Max(__result.r, (byte)115);
        __result.g = Math.Max(__result.g, (byte)115);
        __result.b = Math.Max(__result.b, (byte)115);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(SWorld), nameof(SWorld.GetLight))]
    private static void SWorld_GetLight_Postfix(ref float __result) {
        if (!isEnabled) { return; }
        __result = Math.Max(__result, 0.9f);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(CMeshPoly), nameof(CMeshPoly.DrawTextureQuad))]
    private static void CMeshPoly_DrawTextureQuad_Postfix(CMeshPoly __instance) {
        if (!isEnabled) { return; }

        Color32[] colors = __instance.m_colors;
        int iVertex = __instance.m_iVertex;

        for (int i = iVertex - 4; i < iVertex; i++) {
            if (i >= 0 && i < colors.Length) {
                colors[i].r = Math.Max(colors[i].r, (byte)115);
                colors[i].g = Math.Max(colors[i].g, (byte)115);
                colors[i].b = Math.Max(colors[i].b, (byte)115);
                colors[i].a = 255;
            }
        }
    }
}
