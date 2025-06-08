using HarmonyLib;
using ModUtils;
using ModUtils.Extensions;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using System;

[HarmonyPatch]
internal static class ItemPatches {
    [HarmonyPostfix]
    [HarmonyPatch(nameof(SItems.OnInit))]
    private static void SItems_OnInit() {
        foreach (var itemField in typeof(CustomItems).GetFields(BindingFlags.Static | BindingFlags.Public)) {
            var modItem = (ModItem)itemField.GetValue(null);
            modItem.Item.m_tileTextureName = ModCTile.texturePath;
            ItemTools.RegisterItem(modItem.Item);
        }
        SLoc.ReprocessTexts();
    }
    
    [HarmonyPatch(typeof(CTile), nameof(CTile.CreateSprite))]
    [HarmonyPrefix]
    private static void CTile_CreateSprite(CTile __instance, ref string textureName) {
        if (__instance.m_textureName != null) {
            textureName = __instance.m_textureName;
        }
    }

    [HarmonyPatch(typeof(CItem), nameof(CItem.Init))]
    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> CItem_Init(IEnumerable<CodeInstruction> instructions, ILGenerator generator) {
        var codeMatcher = new CodeMatcher(instructions, generator);

        codeMatcher.End()
            .MatchBack(useEnd: true,
                new(OpCodes.Ldarg_0),
                new(OpCodes.Ldfld, typeof(CItem).Field("m_tile")),
                new(OpCodes.Brfalse))
            .GetOperand(out Label failLabel)
            .Advance(1)
            .Insert(
                new(OpCodes.Ldarg_0),
                new(OpCodes.Ldfld, typeof(CItem).Field("m_tileIcon")),
                new(OpCodes.Brtrue, failLabel));

        return codeMatcher.Instructions();
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

    [HarmonyPatch(typeof(SDataLua), nameof(SDataLua.OnInit))]
    [HarmonyPostfix]
    private static void SDataLua_OnInit() {
        // SOutgame.Mode is "Solo"

        foreach (var recipeGroupField in typeof(CustomRecipeGroups).GetFields(BindingFlags.Static | BindingFlags.Public)) {
            var recipeGroup = (ModRecipeGroup)recipeGroupField.GetValue(null);
            ItemTools.RegisterRecipeGroup(recipeGroup.GroupId, recipeGroup.Autobuilders);
        }
        foreach (var itemField in typeof(CustomItems).GetFields(BindingFlags.Static | BindingFlags.Public)) {
            var modItem = (ModItem)itemField.GetValue(null);
            if (modItem.Recipe is null) { continue; }
            ItemTools.RegisterRecipe(modItem.Recipe);
        }
    }

    [HarmonyPatch(typeof(CInventory), "InventorySorting")]
    [HarmonyPrefix]
    private static bool CInventory_InventorySorting(CStack a, CStack b, ref int __result) {
        static int categoryToOrdinal(string categoryId) {
            return categoryId switch {
                "CITEM_DEVICE" => 0,
                "CITEM_WEAPON" => 1,
                "CITEM_DEFENSE" => 2,
                "CITEM_WALL" => 3,
                "CITEM_MACHINE" => 4,
                "CITEM_MINERAL" => 5,
                "CITEM_MATERIAL" => 6,
                _ => 7,
            };
        }
        const ushort lastItemId = 201;

        var a_item = a.m_item;
        var b_item = b.m_item;
        if (a_item.m_id > lastItemId || b_item.m_id > lastItemId) {
            if (a_item.m_categoryId == b_item.m_categoryId) {
                __result = a_item.m_id - b_item.m_id;
            } else {
                __result = categoryToOrdinal(a_item.m_categoryId) - categoryToOrdinal(b_item.m_categoryId);
            }
            return false;
        }
        return true;
    }
}
