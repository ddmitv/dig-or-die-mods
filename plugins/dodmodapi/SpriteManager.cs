
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using DODModAPI.Extensions;
using HarmonyLib;
using UnityEngine;

namespace DODModAPI;

public readonly struct ModSprite {
    public CAssetSprite Value { get; }

    public ModSprite(string textureName, int x, int y, int width, int height) {
        // we can directly use CAssetSprite ctor to bypass SResources.GetSprite but then, each time ModSprite
        // is re-created a new instance of sprite's texture memory will be duplicated
        Value = SResources.GetSprite(textureName, new Rect(x, y, width, height));
    }
    private ModSprite(string textureName, string spriteName) {
        Value = SResources.GetSprite(textureName, spriteName);
    }

    public static ModSprite Vanilla(string textureName, string spriteName) {
        return new ModSprite(textureName, spriteName);
    }
}

public sealed class ModTile : CTile {
    public ModTile(int i, int j, string textureName, uint mainColor = 0) : base(i, j) {
        base.m_textureName = textureName;
        MainColor = mainColor;
    }

    public uint MainColor { get; }
}

public static class SpriteManager {
    private static readonly Dictionary<string, TextureEntry> _textures = new();

    private readonly record struct TextureEntry(Assembly Assembly, string ResourceName, TextureSettings TextureSettings);

    public readonly struct TextureSettings() {
        public readonly FilterMode filterMode = FilterMode.Bilinear;
        public readonly TextureWrapMode wrapMode = TextureWrapMode.Repeat;
        public readonly int anisoLevel = 1;
        public readonly bool compress = true;
        public readonly bool mipmaps = false;
        public readonly bool isReadable = false;
    }

    // uses Assembly.GetCallingAssembly() to resolve the resource's assembly
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void RegisterTexture(string resourceName) {
        _textures.Add(resourceName, new(Assembly.GetCallingAssembly(), resourceName, new TextureSettings()));
    }
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void RegisterTexture(string resourceName, TextureSettings textureSettings) {
        _textures.Add(resourceName, new(Assembly.GetCallingAssembly(), resourceName, textureSettings));
    }

    public static void RegisterTexture(Assembly assembly, string resourceName) {
        _textures.Add(resourceName, new(assembly, resourceName, new TextureSettings()));
    }
    public static void RegisterTexture(Assembly assembly, string resourceName, TextureSettings textureSettings) {
        _textures.Add(resourceName, new(assembly, resourceName, textureSettings));
    }

    public static Texture2D LoadTexture(Assembly assembly, string resourceName, TextureSettings textureSettings) {
        using var stream = assembly.GetManifestResourceStream(resourceName);

        if (stream is null) {
            throw new InvalidOperationException($"Resource with logical name \"{resourceName}\" not found (in assembly {assembly.GetName().Name})");
        }
        var bytes = new byte[stream.Length];
        // since GetManifestResourceStream returns System.IO.UnmanagedMemoryStream it's safe to just read entire buffer in single call
        _ = stream.Read(bytes, 0, bytes.Length);

        var texture = new Texture2D(2, 2, TextureFormat.RGBA32, mipmap: false);
        if (!texture.LoadImage(bytes)) {
            throw new InvalidOperationException($"Failed to load texture image from logical name \"{resourceName}\" (in assembly {assembly.GetName().Name})");
        }

        texture.filterMode = textureSettings.filterMode;
        texture.wrapMode = textureSettings.wrapMode;
        texture.anisoLevel = textureSettings.anisoLevel;
        texture.mipMapBias = 0f;
        texture.name = resourceName;

        if (textureSettings.compress) {
            texture.Compress(highQuality: true);
        }
        texture.Apply(updateMipmaps: textureSettings.mipmaps, makeNoLongerReadable: !textureSettings.isReadable);

        return texture;
    }

    internal static class Patches {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(CAssetTexture), nameof(CAssetTexture.Texture), MethodType.Getter)]
        private static bool CAssetTexture_get_Texture(CAssetTexture __instance, ref Texture __result) {
            if (__instance.m_asset is null && _textures.TryGetValue(__instance.m_filename, out TextureEntry entry)) {
                var texture = LoadTexture(entry.Assembly, entry.ResourceName, entry.TextureSettings);
                __instance.m_asset = texture;
                __instance.m_lastUseTime = Time.realtimeSinceStartup;
                __result = texture;
                return false;
            }
            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(CTile), nameof(CTile.CreateSprite))]
        private static void CTile_CreateSprite(ref string textureName, CTile __instance) {
            // by default, the this.m_textureName is always assigned to textureName argument
            // this patch removes this behavior for ModCTile instances if this.m_textureName != null
            if (__instance is ModTile tile && tile.m_textureName is not null) {
                textureName = tile.m_textureName;
            }
        }

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(CItem_Mineral), MethodType.Constructor, [typeof(CTile), typeof(CTile), typeof(ushort), typeof(uint), typeof(CSurface), typeof(bool)])]
        private static IEnumerable<CodeInstruction> CItem_Mineral_ctor(IEnumerable<CodeInstruction> instructions, ILGenerator generator) {
            // vanilla CItem_Mineral forcefully assigns m_tileIcon.m_textureName to "items_minerals",
            // which breaks modded tiles (they have custom m_textureName)
            // this patch removes force assign to m_tileIcon.m_textureName which allows tileIcon to have its own texture name
            return new CodeCursor(instructions, generator)
                .FindNextEnd(
                    new(OpCodes.Ldarg_2),
                    new(OpCodes.Brfalse))
                .GetOperand(offset: -1, out Label failLabel)
                .Insert(
                    new(OpCodes.Ldarg_2),
                    new(OpCodes.Isinst, typeof(ModTile)),
                    new(OpCodes.Brtrue, failLabel)) // skip if tileIcon is ModTile
                .Finish();
        }

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(CItem), nameof(CItem.Init))]
        private static IEnumerable<CodeInstruction> CItem_Init(IEnumerable<CodeInstruction> instructions, ILGenerator generator) {
            // this patch removes overwriting of m_tileIcon if m_tileIcon is an instance of ModCTile.
            // by default, the game overwrites m_tileIcon if m_tileIcon.m_textureName != null, which we need
            // to be a custom, non-null value to implement custom sprites for mods
            return new CodeCursor(instructions, generator)
                .MoveToEnd()
                //     else if (this.m_tile != null)
                //          ^^^^^^^^^^^^^^^^^^^^^^^^
                .FindPrevious(
                    new(OpCodes.Ldarg_0),
                    new(OpCodes.Ldfld, typeof(CItem).Field("m_tile")),
                    new(OpCodes.Brfalse))
                .GetOperand(offset: 2, out Label failLabel)
                .Advance(3)
                //     else if (this.m_tile != null && this.m_tileIcon is not ModCTile)
                //                                  ++++++++++++++++++++++++++++++++++
                .Insert(
                    new(OpCodes.Ldarg_0),
                    new(OpCodes.Ldfld, typeof(CItem).Field("m_tileIcon")),
                    new(OpCodes.Isinst, typeof(ModTile)), // m_tileIcon as ModCTile
                    new(OpCodes.Brtrue, failLabel)) // if not null -> skip overwriting m_tileIcon
                .Finish();
        }
    }
}


