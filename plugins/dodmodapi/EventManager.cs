
using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using DODModAPI.Extensions;
using HarmonyLib;

namespace DODModAPI;

public abstract class ModEnvironment : CEnvironment {
    public ModEnvironment(string id, string name, float duration) {
        m_idNum = -1;
        m_id = id;
        m_duration = duration;
        Misc.AddLocalizationText($"ENV_{id}", name);
    }

    public virtual void OnEventStart() { }
    public virtual void OnEventUpdate() { }
    public virtual void OnEventEnd() { }
}

public static class EventManager {
    private static readonly List<ModEnvironment> _events = new();

    private static bool _eventsLocked = false;

    public static void Register(ModEnvironment env) {
        if (env is null) { throw new ArgumentNullException(nameof(env)); }
        LateRegistrationException.ThrowIfLocked(_eventsLocked);

        _events.Add(env);
    }

    public static void Trigger(CEnvironment env, float delay = 0f) {
        if (env.m_idNum == -1) {
            throw new ArgumentException($"Unregistered event \"{env.m_id}\" (you need to register it with EventManager.Register)", nameof(env));
        }
        GVars.m_eventIdNum = env.m_idNum;
        GVars.m_eventStartTime = GVars.SimuTime + delay;
        env.Reset();
    }

    internal static class Patches {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(SDataLua), nameof(SDataLua.OnInit))]
        private static void SDataLua_OnInit() {
            // SOutgame.Mode == "Solo"

            var eventsDescList = SDataLua.GetDescList<CEnvironment>("list_environments");
            foreach (var env in _events) {
                env.m_mod = "Solo"; // m_mod field is never accessed for the events but just in case setting it to "Solo"
                SDataLua.CDesc_ListOf1Type<CEnvironment>.Add(env);
                eventsDescList.Add(env);
            }

            _eventsLocked = true;
            DODModAPIPlugin.Log.LogInfo($"Added {_events.Count} custom events");
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(CEnvironment), nameof(CEnvironment.Update))]
        private static void CEnvironment_Update(CEnvironment __instance) {
            if (__instance is ModEnvironment modEnv) {
                modEnv.OnEventUpdate();
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(CEnvironment), nameof(CEnvironment.OnStart))]
        private static void CEnvironment_OnStart(CEnvironment __instance) {
            if (__instance is ModEnvironment modEvent) {
                modEvent.OnEventStart();
            }
        }

        [HarmonyPatch(typeof(SEnvironment), nameof(SEnvironment.OnUpdateSimu))]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> SEnvironment_OnUpdateSimu(IEnumerable<CodeInstruction> instructions, ILGenerator generator) {
            return new CodeCursor(instructions, generator)
                //     else if ((@event == null || GVars.SimuTime > GVars.m_eventStartTime + @event.m_duration) && !SNetwork.IsClient())
                //                                 ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
                .FindNextEnd(
                    new(OpCodes.Call, typeof(GVars).Method("get_SimuTime")),
                    new(OpCodes.Ldsfld, typeof(GVars).StaticField("m_eventStartTime")),
                    new(OpCodes.Ldloc_0),
                    new(OpCodes.Ldfld, typeof(CEnvironment).Field("m_duration")),
                    new(OpCodes.Add),
                    new(OpCodes.Ble_Un)
                    // insert here
                )
                .Insert(
                    new(OpCodes.Ldloc_0), // local var "@event"
                    Transpilers.EmitDelegate(EndEvent))
                .Finish();

            static void EndEvent(CEnvironment env) {
                if (env is ModEnvironment modEnv) { // also checks if env is not null
                    modEnv.OnEventEnd();
                }
            }
        }
    }
}
