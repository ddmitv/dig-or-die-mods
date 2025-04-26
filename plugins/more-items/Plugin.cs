using BepInEx;
using HarmonyLib;
using ModUtils;
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

[BepInPlugin("more-items", "More Items", "0.0.0")]
public class MoreItemsPlugin : BaseUnityPlugin {
    private void Start() {
        Utils.UniqualizeVersionBuild(ref G.m_versionBuild, this);

        using var textureStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("more-items.textures.combined_textures.png");

        ModCTile.texture = new Texture2D(width: 1, height: 1, format: TextureFormat.ARGB32, mipmap: false);
        ModCTile.texture.LoadImage(Utils.ReadAllBytes(textureStream));
        ModCTile.texture.filterMode = FilterMode.Trilinear;
        ModCTile.texture.wrapMode = TextureWrapMode.Clamp;

        Harmony.CreateAndPatchAll(typeof(Patches));

        Utils.RunStaticConstructor(typeof(CustomItems));

        gameObject.AddComponent<FlashEffect>();
    }
}
