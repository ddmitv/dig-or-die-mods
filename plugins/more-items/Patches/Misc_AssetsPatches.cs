using HarmonyLib;
using ModUtils;
using ModUtils.Extensions;
using System.Collections.Generic;
using System.Reflection.Emit;
using UnityEngine;
using System;

[HarmonyPatch]
internal static class Misc_AssetsPatches {
    [HarmonyPatch(typeof(CTile), nameof(CTile.CreateSprite))]
    [HarmonyPrefix]
    private static void CTile_CreateSprite(CTile __instance, ref string textureName) {
        if (__instance.m_textureName != null) {
            textureName = __instance.m_textureName;
        }
    }
    [HarmonyPatch(typeof(CSurface), nameof(CSurface.InitSprites))]
    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> CSurface_InitSprites(IEnumerable<CodeInstruction> instructions, ILGenerator generator) {
        static string ReplaceTextureStr(string origStr, CSurface self) {
            if (self is ModCSurface.TaggedCSurface) {
                return $"{ModCSurface.surfacePath}/{ModCSurface.surfaceTopsPath}";
            } else {
                return origStr;
            }
        }
        return new CodeMatcher(instructions, generator)
            .Start()
            .MatchForward(useEnd: false,
                new CodeMatch(OpCodes.Ldstr, "surfaces/_surface_tops"))
            .ThrowIfInvalid("(1)")
            .Advance(1)
            .Insert(
                new(OpCodes.Ldarg_0),
                Transpilers.EmitDelegate(ReplaceTextureStr))
            .Instructions();
    }

    [HarmonyPatch(typeof(UnityEngine.Resources), nameof(UnityEngine.Resources.Load), [typeof(string)])]
    [HarmonyPrefix]
    private static bool Resources_Load(string path, ref UnityEngine.Object __result) {
        if (!path.StartsWith("Textures/")) { return true; }
        string prefixlessPath = path.Substring("Textures/".Length);

        if (prefixlessPath == ModCTile.texturePath) {
            __result = ModCTile.texture;
            return false;
        }
        if (prefixlessPath == $"{ModCSurface.surfacePath}/{ModCSurface.fertileDirtTexturePath}") {
            __result = ModCSurface.fertileDirtTexture;
            return false;
        }
        if (prefixlessPath == $"{ModCSurface.surfacePath}/{ModCSurface.surfaceTopsPath}") {
            __result = ModCSurface.surfaceTops;
            return false;
        }
        return true;
    }
    [HarmonyPatch(typeof(UnityEngine.Resources), nameof(UnityEngine.Resources.LoadAll), [typeof(string), typeof(Type)])]
    [HarmonyPrefix]
    private static bool Resources_LoadAll(string path, ref UnityEngine.Object[] __result) {
        static Sprite CreateSprite(string name, Rect rect) {
            var pivot = new Vector2(0.5f, 0.5f);

            var spriteRect = new Rect(rect.x, CustomBullets.particlesTexture.height - rect.yMax, rect.width, rect.height);
            var sprite = Sprite.Create(CustomBullets.particlesTexture, spriteRect, pivot, 100, 0, SpriteMeshType.FullRect);
            sprite.name = name;
            return sprite;
        }

        if (path == $"Textures/{CustomBullets.particlesPath}") {
            __result = [
                CreateSprite("meltdownSnipe", rect: new Rect(0, 0, 255, 119)),
                CreateSprite("particlesSnipTurretMK2", rect: new Rect(255, 0, 209, 98)),
                CreateSprite("impactGrenade", rect: new Rect(464, 0, 40, 40)),
                CreateSprite("particleEnergyDiffuser", rect: new Rect(504, 0, 100, 100)),
            ];
            return false;
        }
        return true;
    }
}
