using System;
using System.Reflection;
using BluePrinceVR;
using HarmonyLib;
using Il2Cpp;
using Il2CppHutongGames.PlayMaker.Actions;
using MelonLoader;
using UnityEngine;

[assembly: MelonInfo(typeof(BPVRFountain.FountainMod), "BluePrinceVR Fountain Only - No Hand Collision", "1.0.0", "Codex")]
[assembly: MelonGame(null,null)]

namespace BPVRFountain;

// Deliberately independent of ContactMod. No hand-pose hooks, hand position
// writes, capsule queries, contact curl or collision settings are present.
public sealed class FountainMod : MelonMod
{
    private static VRPlugin plugin;
    private static readonly Type PluginType=typeof(VRPlugin);
    private static readonly FieldInfo InHouse=AccessTools.Field(PluginType,"_inHouse"),
        Journal=AccessTools.Field(PluginType,"_journalOpen"),Comfort=AccessTools.Field(PluginType,"_comfortOpen"),
        Computer=AccessTools.Field(PluginType,"_computerKeyboardMode"),Draft=AccessTools.Field(PluginType,"_draftMenu"),
        Left=AccessTools.Field(PluginType,"_handL"),Right=AccessTools.Field(PluginType,"_handR"),
        Exploration=AccessTools.Field(PluginType,"_lastExplorationCamera");
    private static readonly PropertyInfo Panel=AccessTools.Property(PluginType,"NativePanelMenu");
    private static MelonPreferences_Entry<bool> fountain;
    private static int errors;
    private static PlayMakerFSM wish;
    private static Collider wishCollider;
    private static bool wishActive;
    private static float nextWishScan,nextWishHover;

    public override void OnInitializeMelon()
    {
        var cfg=MelonPreferences.CreateCategory("BluePrinceVR_Fountain");
        fountain=cfg.CreateEntry("FountainWish",true);
        HarmonyInstance.Patch(AccessTools.Method(PluginType,"OnUpdate"),postfix:new HarmonyMethod(typeof(FountainMod),nameof(Tick)));
        HarmonyInstance.Patch(AccessTools.Method(typeof(MousePickEvent),"OnEnter"),prefix:new HarmonyMethod(typeof(FountainMod),nameof(WishEnter)));
        HarmonyInstance.Patch(AccessTools.Method(typeof(MousePickEvent),"OnUpdate"),prefix:new HarmonyMethod(typeof(FountainMod),nameof(WishUpdate)));
        MelonLogger.Msg("[fountain-only] Fountain fix enabled. No hand-collision hooks installed.");
    }
    private static bool Flag(FieldInfo field)=>plugin!=null&&(bool)field.GetValue(plugin);
    private static bool Playing=>plugin!=null&&Application.isFocused&&Flag(InHouse)&&!Flag(Journal)&&!Flag(Comfort)&&!Flag(Computer)&&!Flag(Draft)&&!(bool)Panel.GetValue(plugin);
    private static void Tick(VRPlugin __instance)
    {
        plugin=__instance;
        try { UpdateWish(); }
        catch(Exception e){if(errors++<5)MelonLogger.Warning("[fountain-only] "+e.Message);}
    }
    private static void UpdateWish()
    {
        if(!fountain.Value||!Playing){EndWish();return;}
        if(wish==null&&Time.unscaledTime>=nextWishScan) {
            nextWishScan=Time.unscaledTime+3f;
            var go=GameObject.Find("Wishing Well Trigger");
            if(go!=null){wish=go.GetComponent<PlayMakerFSM>();wishCollider=go.GetComponent<Collider>();}
        }
        if(wish==null||wishCollider==null||!wish.isActiveAndEnabled||!wishCollider.enabled){EndWish();return;}
        var camera=Exploration.GetValue(plugin) as Camera;
        bool near=false;
        if(camera!=null&&Vector3.Distance(camera.transform.position,wishCollider.bounds.ClosestPoint(camera.transform.position))<2.5f) {
            foreach(var field in new[]{Left,Right}) {
                var hand=field.GetValue(plugin) as Hand;
                if(hand==null||!hand.Valid)continue;
                var point=wishCollider.bounds.ClosestPoint(hand.AimOrigin);
                if(Vector3.Distance(hand.AimOrigin,point)>1.8f)continue;
                var root=wish.transform.parent?.parent;
                bool blocked=false;
                foreach(var hit in Physics.RaycastAll(hand.AimOrigin,point-hand.AimOrigin,Vector3.Distance(hand.AimOrigin,point),-1,QueryTriggerInteraction.Ignore)) {
                    if(!BlocksWishSight(hit.collider)||hit.collider==wishCollider||(root!=null&&hit.collider.transform.IsChildOf(root)))continue;
                    blocked=true;break;
                }
                if(!blocked){near=true;break;}
            }
        }
        if(!near){EndWish();return;}
        wishActive=true;
        // Preserve the existing native gold check, prompt, spending and count.
        if(wish.Fsm.ActiveStateName=="Prestate 2"&&Time.unscaledTime>=nextWishHover) {
            nextWishHover=Time.unscaledTime+.25f;
            wish.SendEvent("hov");
            MelonLogger.Msg("[fountain] Native coin check: "+wish.Fsm.ActiveStateName+"; use left grip + left trigger for E.");
        }
    }
    private static void EndWish()
    {
        bool was=wishActive;wishActive=false;
        if(was&&wish!=null&&wish.isActiveAndEnabled)wish.SendEvent("hovoff");
    }
    private static bool WishUpdate(MousePickEvent __instance)=>!wishActive||wish==null||__instance.Fsm?.GameObject!=wish.gameObject;
    private static bool WishEnter(MousePickEvent __instance)
    {
        if(WishUpdate(__instance))return true;
        __instance.Finish();return false;
    }
    // This is solely a fountain line-of-sight check; it never moves a hand.
    private static bool BlocksWishSight(Collider col)
    {
        if(col==null||!col.enabled||col.isTrigger)return false;
        if(col is CharacterController||col.GetIl2CppType().Name=="CharacterController")return false;
        if(col.gameObject.layer==2||col.gameObject.layer==5)return false;
        bool pantryProxy=col.GetIl2CppType().Name=="BoxCollider"&&
            col.name.Length==10&&col.name.StartsWith("Collider ",StringComparison.Ordinal)&&
            col.name[9]>='A'&&col.name[9]<='D';
        for(var t=col.transform;t!=null;t=t.parent) {
            string n=t.name;
            if(n.StartsWith("BPVR_",StringComparison.OrdinalIgnoreCase)||n.StartsWith("Codex_",StringComparison.OrdinalIgnoreCase)||
               n=="FPS Prince"||n=="FPS Home"||n.StartsWith("FPSController",StringComparison.OrdinalIgnoreCase)||
               n=="FirstPersonCharacter"||n=="Wishing Well Trigger"||n=="Coin Blocker"||
               n.IndexOf("click blocker",StringComparison.OrdinalIgnoreCase)>=0)return false;
            if(pantryProxy&&(n=="Pantry"||n.StartsWith("Pantry(Clone)",StringComparison.Ordinal)))return false;
        }
        return true;
    }
}
