
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using ModUtils;
using System;
using System.Reflection;
using UnityEngine;

public class FlashEffect : MonoBehaviour {
    private static Texture2D flashTexture;
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

[BepInPlugin("more-items", "More Items", "1.0.0")]
public class MoreItemsPlugin : BaseUnityPlugin {
    public static ConfigEntry<float> configBossRespawnDelay = null;

    private UnityEngine.Texture2D LoadTexture2DFromManifest(Assembly assembly, string logicalName) {
        using var stream = assembly.GetManifestResourceStream(logicalName);

        var texture = new Texture2D(width: 1, height: 1,
            format: TextureFormat.DXT5, mipmap: true
        );
        if (!texture.LoadImage(Utils.ReadAllBytes(stream))) {
            throw new InvalidOperationException($"Failed to load texture image from '{logicalName}'");
        }

        texture.filterMode = FilterMode.Bilinear;
        texture.anisoLevel = 1;
        texture.wrapMode = TextureWrapMode.Repeat;
        texture.Apply(updateMipmaps: true);

        return texture;
    }
    private UnityEngine.Texture2D LoadSurfaceFromManifest(Assembly assembly, string logicalName) {
        using var stream = assembly.GetManifestResourceStream(logicalName);

        var texture = new Texture2D(width: 512, height: 512,
            format: TextureFormat.DXT1, mipmap: true
        );
        if (!texture.LoadImage(Utils.ReadAllBytes(stream))) {
            throw new InvalidOperationException($"Failed to load texture image from '{logicalName}'");
        }
        texture.filterMode = FilterMode.Bilinear;
        texture.anisoLevel = 1;
        texture.wrapMode = TextureWrapMode.Repeat;
        texture.Apply(updateMipmaps: true);

        return texture;
    }

    private void Start() {
        configBossRespawnDelay = Config.Bind<float>("General", "BossRespawnDelay", defaultValue: 360f,
            "Respawn delay for bosses. Can't be turned off because boss's loot is used in multiple recipes"
        );
        var configUniqualizeVersionBuild = Config.Bind<bool>("General", "UniqualizeVersionBuild", defaultValue: false,
            "Safe guard to prevent joining to server with different mod version"
        );
        if (configUniqualizeVersionBuild.Value) {
            Utils.UniqualizeVersionBuild(ref G.m_versionBuild, this);
        }

        var currectAssembly = Assembly.GetExecutingAssembly();
        ModCTile.texture = LoadTexture2DFromManifest(currectAssembly, "more-items.textures.combined_textures.png");
        ModCSurface.fertileDirtTexture = LoadSurfaceFromManifest(currectAssembly, "more-items.textures.surfaces.surface_fertileDirt.png");
        ModCSurface.surfaceTops = LoadSurfaceFromManifest(currectAssembly, "more-items.textures.surfaces.surface_tops.png");
        CustomBullets.particlesTexture = LoadTexture2DFromManifest(currectAssembly, "more-items.textures.combined_particles.png");

        var harmony = Harmony.CreateAndPatchAll(currectAssembly, Info.Metadata.GUID);

        Utils.RunStaticConstructor(typeof(CustomItems));

        gameObject.AddComponent<FlashEffect>();
    }
}
