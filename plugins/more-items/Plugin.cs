using BepInEx;
using HarmonyLib;
using ModUtils;
using System.Reflection;
using UnityEngine;

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
    }
}
