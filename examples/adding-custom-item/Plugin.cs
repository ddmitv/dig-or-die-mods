using BepInEx;
using BepInEx.Logging;
using HarmonyLib;

namespace adding_custom_item;

[BepInPlugin("adding-custom-item", "Adding custom item", "1.0.0")]
public class Plugin : BaseUnityPlugin
{
    private void Awake()
    {
        Harmony.CreateAndPatchAll(typeof(Plugin));

        System.Console.WriteLine("Plugin 'adding-custom-item' is loaded!");
    }
    [HarmonyPatch(typeof(SItems), "OnInit")]
    [HarmonyPostfix]
    private static void SItems_OnInit() {
        CItem item = new CItem(new CTile(0, 0), null);
        item.m_codeName = "myItem";
        item.m_locTextId = "I_myItem";
        item.m_tileTextureName = "items_walls";
        item.m_id = (ushort)GItems.Items.Count;

        GItems.Items.Add(item);
        item.Init();
        SLoc.ReprocessTexts();
    }
}
