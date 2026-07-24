
using BepInEx;
using BepInEx.Configuration;
using DODModAPI;
using HarmonyLib;
using System;
using System.Reflection;
using UnityEngine;

public class FlashEffect : MonoBehaviour {
    private static Texture2D flashTexture = null!;
    private static float flashIntensity = 0f;

    private void Start() {
        flashTexture = new Texture2D(1, 1);
        flashTexture.SetPixel(0, 0, Color.white);
        flashTexture.Apply();
    }

    private void Update() {
        if (flashIntensity <= 0) { return; }

        flashIntensity -= SMain.SimuDeltaTime * 2f;
    }
    private void OnGUI() {
        if (flashIntensity <= 0) { return; }

        var color = Color.white;
        color.a = Mathf.Clamp(flashIntensity, 0f, 1f);

        GUI.color = color;
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), flashTexture);
    }
    public static void TriggerFlash(float intensity) {
        flashIntensity = intensity;
    }
}

public class TestEvent : ModEnvironment {
    public TestEvent() : base("testEvent", "Test Event", 10f) {
        m_shakeCam = true;
    }

    public override void OnEventStart() => Misc.SendChatMessageLocal("starting event!!");
    public override void OnEventUpdate() => SNetwork.GetMyPlayer().m_unitPlayer.m_pos += Vector2.up * SMain.SimuDeltaTime;
    public override void OnEventEnd() => Misc.SendChatMessageLocal("ending event!!");
}

[BepInPlugin("more-items", ThisPluginInfo.Name, ThisPluginInfo.Version)]
[BepInDependency(ReplacementorPluginGUID, BepInDependency.DependencyFlags.SoftDependency)]
[BepInDependency(DODModAPIPlugin.GUID)]
public class MoreItemsPlugin : BaseUnityPlugin {
    private const string ReplacementorPluginGUID = "replacementor";

    public static ConfigEntry<float> configBossRespawnDelay = null!;

    private void InitReplacementorDependency(PluginInfo pluginInfo) {
        try {
            var pluginType = pluginInfo.Instance.GetType();
            var replaceTypeEnumType = pluginType.Assembly.GetType("ReplaceType");
            if (replaceTypeEnumType == null) {
                Logger.LogWarning($"Failed to find 'ReplaceType' enum in '{ReplacementorPluginGUID}' plugin");
                return;
            }
            object replaceTypeLight = Enum.Parse(replaceTypeEnumType, "Light");
            if (replaceTypeLight == null) {
                Logger.LogWarning($"Failed to parse string 'Light' as enum 'ReplaceType' in '{ReplacementorPluginGUID}' plugin");
                return;
            }

            MethodInfo addReplaceableItemMethod = pluginType.GetMethod("AddReplaceableItem", BindingFlags.Public | BindingFlags.Instance);
            if (addReplaceableItemMethod == null) {
                Logger.LogWarning($"Failed to find 'AddReplaceableItem' method in '{ReplacementorPluginGUID}' plugin type");
                return;
            }
            addReplaceableItemMethod.Invoke(pluginInfo.Instance, [CustomItems.redLightSticky.item, replaceTypeLight]);
            addReplaceableItemMethod.Invoke(pluginInfo.Instance, [CustomItems.greenLightSticky.item, replaceTypeLight]);
            addReplaceableItemMethod.Invoke(pluginInfo.Instance, [CustomItems.blueLightSticky.item, replaceTypeLight]);
            Logger.LogInfo($"Successfully added custom lights for replacable items into '{ReplacementorPluginGUID}' plugin");
        } catch (Exception ex) {
            Logger.LogWarning($"Failed to add custom lights for replacable items into '{ReplacementorPluginGUID}' plugin: {ex}");
        }
    }

    private void Start() {
        configBossRespawnDelay = Config.Bind<float>("General", "BossRespawnDelay", defaultValue: 360f,
            "Respawn delay for bosses. Can't be turned off because boss's loot is used in multiple recipes"
        );
        var configEnable = Config.Bind<bool>("General", "Enable", defaultValue: true,
            description: "Enables the plugin"
        );
        if (!configEnable.Value) { return; }

        DODModAPI.SpriteManager.RegisterTexture(Textures.TileSpritesheetResource);
        DODModAPI.SpriteManager.RegisterTexture(Textures.fertileDirt_surfaceMaterial);
        DODModAPI.SpriteManager.RegisterTexture(Textures.SurfaceTopsResource);
        DODModAPI.SpriteManager.RegisterTexture(Textures.SpritesAtlasResource);

        DODModAPI.ItemManager.RegisterAllItems(typeof(CustomItems));
        DODModAPI.UnitManager.RegisterUnit(CustomUnits.waterVaporizer);

        EventManager.Register(new TestEvent());

        var harmony = Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), Info.Metadata.GUID);

        gameObject.AddComponent<FlashEffect>();

        if (BepInEx.Bootstrap.Chainloader.PluginInfos.TryGetValue(ReplacementorPluginGUID, out var pluginInfo)) {
            InitReplacementorDependency(pluginInfo);
        }
    }
}
