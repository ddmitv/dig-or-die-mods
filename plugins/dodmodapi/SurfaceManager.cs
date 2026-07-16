
using HarmonyLib;
using UnityEngine;

namespace DODModAPI;

public sealed class ModSurface : CSurface {
    public ModSurface(string surfaceTexture, int surfaceSortingOrder, ModTile surfaceTopTile, bool hasAltTop = false, CSurface? surfaceGrass = null, CSurface? surfaceGrassWet = null)
        : base(default, default, default, default, default, default, default, default) /*base ctor is skipped*/ {
        m_surfaceTexture = surfaceTexture;
        m_surfaceMat = SResources.GetMaterial("SurfaceOpaque", surfaceTexture);
        m_topIcon = surfaceTopTile;

        m_matTop = SResources.GetMaterial("SurfaceBorders", m_topIcon.m_textureName);
        m_sortingOrder = surfaceSortingOrder;
        m_hasAltTop = hasAltTop;

        m_surfaceGrass = surfaceGrass;
        m_surfaceGrassWet = surfaceGrassWet;
    }
}

public static class SurfaceManager {
    internal static class Patches {
        [HarmonyPatch(typeof(CSurface), MethodType.Constructor, [typeof(string), typeof(int), typeof(int), typeof(int), typeof(bool), typeof(CSurface), typeof(CSurface), typeof(bool)])]
        [HarmonyPrefix]
        private static bool CSurface_ctor(CSurface __instance) {
            if (__instance is ModSurface) {
                return false; // skip base ctor
            }
            return true;
        }

        [HarmonyPatch(typeof(CSurface), nameof(CSurface.InitSprites))]
        [HarmonyPrefix]
        private static bool CSurface_InitSprites(CSurface __instance) {
            if (__instance is ModSurface) {
                Texture2D texture = (Texture2D)SResources.GetTexture(__instance.m_topIcon.m_textureName).Texture;

                int tileX = __instance.m_topIcon.m_tileIndex.x;
                int tileY = __instance.m_topIcon.m_tileIndex.y;
                int textureHeight = texture.height;

                __instance.m_spTop = Sprite.Create(texture,
                    rect: new(tileX * 128 + 16, textureHeight - (tileY + 1) * 128, 96f, 64f),
                    pivot: new(0.5f, 0f),
                    pixelsPerUnit: 100f,
                    extrude: 0,
                    meshType: SpriteMeshType.FullRect
                );
                if (__instance.m_hasAltTop) {
                    __instance.m_spTopAlt = Sprite.Create(texture,
                        rect: new(tileX * 128 + 16, textureHeight - (tileY + 1) * 128 - 64, 96f, 64f),
                        pivot: new(0.5f, 0f),
                        pixelsPerUnit: 100f,
                        extrude: 0,
                        meshType: SpriteMeshType.FullRect
                    );
                }

                __instance.m_spTopL = Sprite.Create(texture,
                    rect: new(tileX * 128, textureHeight - (tileY + 1) * 128, 16f, 64f),
                    pivot: new(1f, 0f),
                    pixelsPerUnit: 100f,
                    extrude: 0,
                    meshType: SpriteMeshType.FullRect
                );
                __instance.m_spTopR = Sprite.Create(texture,
                    rect: new(tileX * 128 + 112, textureHeight - (tileY + 1) * 128, 16f, 64f),
                    pivot: new(0f, 0f),
                    pixelsPerUnit: 100f,
                    extrude: 0,
                    meshType: SpriteMeshType.FullRect
                );

                return false;
            }
            return true;
        }
    }
}
