using HarmonyLib;
using System.Reflection;

[HarmonyPatch]
internal static class SDataLuaPatches {
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
}
