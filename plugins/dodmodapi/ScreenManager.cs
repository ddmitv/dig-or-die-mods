
using HarmonyLib;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace DODModAPI;

public static class ScreenManager {
    private static readonly List<ScreenEntry> _screens = new();
    private static bool _screensLocked = false;

    private struct ScreenEntry {
        public Type type;
        public int sortOrder;
        public string name;
    }

    public static void Register<T>(int sortOrder = 100, string? name = null) where T : SSingletonScreen<T> {
        LateRegistrationException.ThrowIfLocked(_screensLocked);

        _screens.Add(new ScreenEntry {
            type = typeof(T),
            sortOrder = sortOrder,
            name = name ?? typeof(T).Name,
        });
    }

    internal static class Patches {
        [HarmonyPatch(typeof(SScreen), nameof(SScreen.InitScreens))]
        [HarmonyPrefix]
        private static void SScreen_InitScreens() {
            Transform? parent = GameObject.Find("_Screens")?.transform;
            if (parent is null) {
                throw new InvalidOperationException("\"_Screens\" GameObject not found");
            }
            _screens.Sort((a, b) => a.sortOrder.CompareTo(b.sortOrder));

            foreach (var entry in _screens) {
                GameObject obj = new($"{entry.sortOrder}_{entry.name}");
                obj.transform.SetParent(parent, worldPositionStays: false);
                obj.AddComponent(entry.type);
            }
            _screensLocked = true;
            DODModAPIPlugin.Log.LogInfo($"Added {_screens.Count} custom screens");
        }
    }
}
