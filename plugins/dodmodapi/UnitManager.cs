
using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;

namespace DODModAPI;

public sealed class ModUnit {
    public ModUnit(string codeName, string name, CUnit.CDesc unitDesc) {
        unitDesc.m_codeName = codeName;
        // m_locTextId is not used anywhere so we ignoring it
        Misc.AddLocalizationText($"U_{codeName}", name); // see CUnit.CDesc.GetName()

        UnitDesc = unitDesc;
    }

    public CUnit.CDesc UnitDesc { get; }
}

public static class UnitManager {
    private static readonly List<ModUnit> _unitDescs = new();

    private static bool _unitDescsLocked = false;

    public static void RegisterUnit(ModUnit modUnit) {
        LateRegistrationException.ThrowIfLocked(_unitDescsLocked);
        _unitDescs.Add(modUnit);
    }
    public static void RegisterAllUnit(Type type) {
        LateRegistrationException.ThrowIfLocked(_unitDescsLocked);
        foreach (var modUnit in type.GetFields(BindingFlags.Static | BindingFlags.Public)) {
            _unitDescs.Add((ModUnit)modUnit.GetValue(null));
        }
    }

    internal static class Patches {
        [HarmonyPatch(typeof(SUnits), nameof(SUnits.OnInit))]
        [HarmonyPostfix]
        private static void SUnits_OnInit() {
            foreach (var modUnit in _unitDescs) {
                var desc = modUnit.UnitDesc;
                desc.m_id = (byte)GUnits.UDescs.Count;
                GUnits.UDescs.Add(desc);

                // type of field CUnit.CDesc.m_id is byte, so max allowed ID is 255
                if (GUnits.UDescs.Count >= 255) {
                    throw new InvalidOperationException($"GUnits.UDescs can only have 255 elements (unit descriptors). Unable to add unit \"{desc.m_codeName}\"");
                }
            }
            _unitDescsLocked = true;
            DODModAPIPlugin.Log.LogInfo($"Added {_unitDescs.Count} custom unit descriptors");
        }
    }
}
