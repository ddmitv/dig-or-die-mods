using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using DODModAPI.Extensions;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using UnityEngine;
using DODModAPI;

internal static class Patches {
    private static void TraceFlashLight(CUnitPlayer unit, CItem_Device itemDevice) {
        float length = FocusedFlashlight.configLightDistanceMultipler.Value * itemDevice.m_customValue;
        Color24 color = new Color24(FocusedFlashlight.configLightColor.Value);

        Vector2 startPos = unit.PosCenter;
        Vector2 endPos = startPos + unit.m_lookingDirection.normalized * length;

        int2 pos0 = new(startPos);
        int2 pos1 = new(endPos);
        int2 delta = pos1 - pos0;

        int steps = Math.Max(Math.Abs(delta.x), Math.Abs(delta.y));

        int2 sideOffset = Math.Abs(delta.x) > Math.Abs(delta.y) ? new(0, Math.Sign(delta.y)) : new(Math.Sign(delta.x), 0);

        Vector2 step = (Vector2)delta / steps;
        Vector2 pos = pos0;

        for (int i = 0; i <= steps; i++) {
            int2 cellPos = int2.FromVector2Rounded(pos);

            if (!Misc.IsInWorld(cellPos)) { break; }

            if (SWorld.Grid[cellPos.x, cellPos.y].IsPassable()) {
                float intensity = 0.5f + 0.5f * Mathf.Cos((float)i / steps * Mathf.PI);
                SWorld.Grid[cellPos.x, cellPos.y].m_light.IncreaseTo(color * intensity);
            } else if (!Misc.IsInWorld(cellPos + sideOffset) || !SWorld.Grid[cellPos.x + sideOffset.x, cellPos.y + sideOffset.y].IsPassable()) {
                break;
            }
            pos += step;
        }
    }

    [HarmonyPatch(typeof(SWorld), nameof(SWorld.ProcessLighting_DynamicUnits))]
    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> ProcessLighting_DynamicUnits(IEnumerable<CodeInstruction> instructions, ILGenerator generator) {
        return new CodeCursor(instructions, generator)
            .FindNext(out uint n,
                new(OpCodes.Ldarg_0),
                new(OpCodes.Ldloc_3),
                new(OpCodes.Callvirt, typeof(CUnit).Method("get_PosCenter")),
                new(OpCodes.Ldloc_S),
                new(OpCodes.Ldfld, typeof(CItem_Device).Field("m_customValue")),
                new(OpCodes.Ldloc_0),
                new(OpCodes.Mul),
                new(OpCodes.Ldc_I4, 231),
                new(OpCodes.Ldc_I4, 231),
                new(OpCodes.Ldc_I4_S, (sbyte)95),
                new(OpCodes.Newobj, typeof(Color24).Constructor<byte, byte, byte>()),
                new(OpCodes.Ldloc_0),
                new(OpCodes.Call, typeof(Color24).Method("op_Multiply")),
                new(OpCodes.Call, typeof(SWorld).Method("LightZone"))
            )
            .Remove(n)
            .Insert(
                new(OpCodes.Ldloc_3),
                new(OpCodes.Ldloc_S, (byte)5),
                Transpilers.EmitDelegate(TraceFlashLight))
            .Finish();
    }
}

[BepInPlugin("focused-flashlight", ThisPluginInfo.Name, ThisPluginInfo.Version)]
[BepInDependency(DODModAPIPlugin.GUID)]
public class FocusedFlashlight : BaseUnityPlugin {
    public static ConfigEntry<float> configLightDistanceMultipler = null!;
    public static ConfigEntry<Color> configLightColor = null!;

    private void Awake() {
        var configEnabled = Config.Bind<bool>(
            section: "General", key: "Enabled", defaultValue: true,
            description: "Enables the plugin"
        );
        configLightDistanceMultipler = Config.Bind<float>(
            section: "General", key: "LightDistanceMultipler", defaultValue: 2.2f,
            description: "Multiplies the flashlight's m_customValue and uses the value as light max distance\nFor Flashlight the m_customValue is 4, for Advanced Flashlight is 7"
        );
        configLightColor = Config.Bind<Color>(
            section: "General", key: "LightColorIntensity", defaultValue: new Color24(231, 231, 95).ToColor(),
            description: "23"
        );
        if (!configEnabled.Value) { return; }

        var harmony = new Harmony(Info.Metadata.GUID);
        harmony.PatchAll(typeof(Patches));
    }
}
