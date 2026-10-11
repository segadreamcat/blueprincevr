using HarmonyLib;
using Il2CppHutongGames.PlayMaker.Actions;
namespace BluePrinceVR;

// The idle -> Tool Check transition still runs every native inventory check.
// Only its desktop-ray enter check is suppressed while a valid VR hand owns it.
[HarmonyPatch(typeof(MousePickEvent), "OnEnter")]
internal static class DigHoverEnterGuard
{
    private static bool Prefix(MousePickEvent __instance)
    {
        if(!VRPlugin.OwnsDigHover(__instance.Fsm?.GameObject))return true;
        // Do not resume an uninitialised desktop action if the lease expires.
        __instance.Finish();
        return false;
    }
}
