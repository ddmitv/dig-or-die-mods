using HarmonyLib;
using ModUtils;
using ModUtils.Extensions;
using System.Collections.Generic;
using System.Reflection.Emit;
using UnityEngine;

[HarmonyPatch]
internal static class CInventoryPatches {
    [HarmonyPatch(typeof(CInventory), "InventorySorting")]
    [HarmonyPrefix]
    private static bool CInventory_InventorySorting(CStack a, CStack b, ref int __result) {
        static int categoryToOrdinal(string categoryId) {
            return categoryId switch {
                "CITEM_DEVICE" => 0,
                "CITEM_WEAPON" => 1,
                "CITEM_DEFENSE" => 2,
                "CITEM_WALL" => 3,
                "CITEM_MACHINE" => 4,
                "CITEM_MINERAL" => 5,
                "CITEM_MATERIAL" => 6,
                _ => 7,
            };
        }
        const ushort lastItemId = 201;

        var a_item = a.m_item;
        var b_item = b.m_item;
        if (a_item.m_id > lastItemId || b_item.m_id > lastItemId) {
            if (a_item.m_categoryId == b_item.m_categoryId) {
                __result = a_item.m_id - b_item.m_id;
            } else {
                __result = categoryToOrdinal(a_item.m_categoryId) - categoryToOrdinal(b_item.m_categoryId);
            }
            return false;
        }
        return true;
    }
}
