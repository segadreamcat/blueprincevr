using HarmonyLib;
using Il2CppHutongGames.PlayMaker.Actions;
namespace BluePrinceVR;
// Native mouse release and ray-hover actions must not cancel a controller tap
// between its busy check and directional event. All other FSMs run unchanged.
[HarmonyPatch(typeof(GetMouseButtonUp), "OnUpdate")]
internal static class PumpTapMouseReleaseGuard
{
    private static bool Prefix(GetMouseButtonUp __instance)
        => VRPlugin.ActivePumpTap==null || __instance.Fsm==null ||
           __instance.Fsm.GameObject!=VRPlugin.ActivePumpTap.gameObject;
}
