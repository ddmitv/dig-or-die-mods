
using HarmonyLib;

namespace DODModAPI;

public class ModBulletDesc : CBulletDesc {
    public ModBulletDesc(ModSprite sprite, float radius, float dispersionAngleRad, float speedStart, float speedEnd, Color24 light = default)
        : base(default, default, default, default, default, default, default) {

        m_sprite = sprite.Value;
        m_radius = radius;
        m_dispertionAngleRad = dispersionAngleRad;
        m_speedStart = speedStart;
        m_speedEnd = speedEnd;
        m_light = light;
    }

    internal static class Patches {
        [HarmonyPatch(typeof(CBulletDesc), MethodType.Constructor, [typeof(string), typeof(string), typeof(float), typeof(float), typeof(float), typeof(float), typeof(uint)])]
        [HarmonyPrefix]
        private static bool CBulletDesc_ctor(CBulletDesc __instance) {
            if (__instance is ModBulletDesc) {
                return false; // skip
            }
            return true;
        }
    }
}
