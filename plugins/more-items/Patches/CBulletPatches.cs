using HarmonyLib;
using ModUtils;
using ModUtils.Extensions;
using System.Collections.Generic;
using System.Reflection.Emit;
using UnityEngine;

[HarmonyPatch(typeof(CBullet))]
internal static class CBulletPatches {
    [HarmonyPatch(nameof(CBullet.CheckColWithUnits))]
    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> CBullet_CheckColWithUnits(IEnumerable<CodeInstruction> instructions, ILGenerator generator) {
        var codeMatcher = new CodeMatcher(instructions, generator);

        PatchZF0Bullet(codeMatcher, "(1)");
        PatchZF0Bullet(codeMatcher, "(2)");
        PatchZF0Bullet(codeMatcher, "(3)");

        return codeMatcher.Instructions();
    }

    [HarmonyTranspiler]
    [HarmonyPatch(nameof(CBullet.Update))]
    private static IEnumerable<CodeInstruction> CBullet_Update(IEnumerable<CodeInstruction> instructions, ILGenerator generator) {
        var codeMatcher = new CodeMatcher(instructions, generator);

        codeMatcher.Start()
            .MatchForward(useEnd: true,
                new(OpCodes.Ldarg_0),
                new(OpCodes.Call, typeof(CBullet).Method("get_Desc")),
                new(OpCodes.Ldfld, typeof(CBulletDesc).Field("m_lavaQuantity")),
                new(OpCodes.Ldc_R4, 0.0f),
                new(OpCodes.Ble_Un))
            .ThrowIfInvalid("(1)")
            .GetOperand(out Label failLabel)
            .Advance(1)
            .Insert(
                new(OpCodes.Ldarg_0),
                Transpilers.EmitDelegate((CBullet self) => {
                    if (self.Desc is ExtCBulletDesc cbulletdesc) {
                        return cbulletdesc.emitLavaBurstParticles;
                    }
                    return true;
                }),
                new(OpCodes.Brfalse, failLabel)
            );

        PatchZF0Bullet(codeMatcher.Start(), "(2)");

        return codeMatcher.Instructions();
    }

    [HarmonyPostfix]
    [HarmonyPatch(nameof(CBullet.Explosion))]
    private static void CBullet_Explosion(CBullet __instance, CUnit unitHit) {
        if (__instance.Desc is not ExtCBulletDesc extDesc) { return; }

        if (extDesc.explosionBasaltBgRadius > 0) {
            Utils.ForEachInCircleClamped(extDesc.explosionBasaltBgRadius, new int2(__instance.m_pos), (int x, int y) => {
                if (SWorld.Grid[x, y].GetBgSurface() != null) {
                    SWorld.Grid[x, y].SetBgSurface(GSurfaces.bgLava);
                }
            });
        }
        if (extDesc.shockWaveRange > 0) {
            Utils.DoShockWave(__instance.m_pos, extDesc.shockWaveRange, extDesc.shockWaveDamage, extDesc.shockWaveKnockback);
        }
        if (extDesc.explosionEnergyRadius > 0f) {
            extDesc.DoEnergyExplosion(__instance);
        }
    }

    private static void PatchZF0Bullet(CodeMatcher codeMatcher, string explanation) {
        codeMatcher
            .MatchForward(useEnd: true,
                new(OpCodes.Ldarg_0),
                new(OpCodes.Call, typeof(CBullet).Method("get_Desc")),
                new(OpCodes.Ldsfld, typeof(GBullets).StaticField("zf0bullet")),
                new(OpCodes.Bne_Un))
            .ThrowIfInvalid(explanation)
            .Advance(1)
            .CreateLabel(out Label successLabel)
            .Advance(-4)
            .InjectAndAdvance(OpCodes.Ldarg_0)
            .InsertAndAdvance(
                new(OpCodes.Call, typeof(CBullet).Method("get_Desc")),
                new(OpCodes.Ldsfld, typeof(CustomBullets).StaticField("zf0shotgunBullet")),
                new(OpCodes.Beq, successLabel))
            .Advance(4);
    }
}
