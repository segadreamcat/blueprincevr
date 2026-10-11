using System;
using UnityEngine;
namespace BluePrinceVR;
public partial class VRPlugin
{
    private int _digLogCount;
    private readonly float[] _digHoverAt=new float[2];
    private Collider _pendingDig;
    private Hand _pendingDigHand;
    private float _pendingDigUntil;
    private static readonly GameObject[] _digHoverOwner=new GameObject[2];
    private static readonly float[] _digHoverUntil=new float[2];
    internal static bool OwnsDigHover(GameObject owner)
    {
        if(!Application.isFocused||owner==null)return false;
        for(int i=0;i<2;i++)if(_digHoverOwner[i]==owner&&Time.unscaledTime<_digHoverUntil[i])return true;
        return false;
    }
    private void UpdateDigHover(Hand hand,PickEvents pick)
    {
        int side=hand.IsLeft?0:1;
        _digHoverOwner[side]=null;
        if(!hand.Valid||!Application.isFocused||!PalmPickupAllowed||_journalOpen||_comfortOpen){_pendingDig=null;return;}
        if(_pendingDig!=null&&_pendingDigHand==hand&&
            (hand.AimCollider!=_pendingDig||Time.unscaledTime>_pendingDigUntil))_pendingDig=null;
        if(!IsDigTarget(hand.AimCollider)||pick?.Fsm==null)return;
        _digHoverOwner[side]=pick.Fsm.gameObject;
        _digHoverUntil[side]=Time.unscaledTime+.1f;
        string state=pick.Fsm.Fsm.ActiveStateName;
        // Re-enter only native idle tool-check states, never an active dig.
        if((state=="State 3"||state=="Prestate")&&Time.unscaledTime>=_digHoverAt[side]) {
            _digHoverAt[side]=Time.unscaledTime+.15f;
            // Restore the native tool-check path, not a forced dig. The desktop
            // mouse ray must not immediately cancel a valid tracked-hand hover.
            foreach(var transition in pick.Fsm.Fsm.ActiveState.Transitions)
                if(transition.EventName==pick.Over&&transition.ToState=="Tool Check") {
                    pick.Fsm.Fsm.SetState(transition.ToState);break;
                }
        }
        if(_pendingDig==hand.AimCollider&&_pendingDigHand==hand&&DigReady(pick)) {
            _pendingDig=null;MousePick.Send(pick.Fsm,pick.Down);InteractionPulse(hand,true);
        }
    }
    private static bool DigReady(PickEvents pick)
    {
        if(pick?.Fsm==null || !pick.Fsm.isActiveAndEnabled || string.IsNullOrEmpty(pick.Down))return false;
        var state=pick.Fsm.Fsm.ActiveState;
        if(state?.Transitions==null)return false;
        foreach(var transition in state.Transitions)
            if(transition!=null && transition.EventName==pick.Down && !string.IsNullOrEmpty(transition.ToState))return true;
        // A native global mouse-click transition is valid from this state too.
        // Deliver its event normally so the game's shovel/loot checks still run.
        if(pick.Fsm.Fsm.GlobalTransitions!=null)
            foreach(var transition in pick.Fsm.Fsm.GlobalTransitions)
                if(transition!=null && transition.EventName==pick.Down && !string.IsNullOrEmpty(transition.ToState))return true;
        return false;
    }
    private bool ClickDigTarget(Hand hand)
    {
        // Honor only the native click event while its CURRENT state accepts it.
        // Never force a digging state, award loot, or bypass shovel checks.
        var pick=MousePick.For(hand.AimCollider);
        if(_digLogCount++<40) {
            string transitions="";
            var state=pick?.Fsm?.Fsm.ActiveState;
            if(state?.Transitions!=null)foreach(var t in state.Transitions)
                if(t!=null)transitions+=t.EventName+"->"+t.ToState+"; ";
            Log("[dig] press target="+hand.AimCollider.name+" state="+state?.Name+" down="+pick?.Down+" listening="+transitions);
        }
        if(!DigReady(pick)) {
            if(pick?.Fsm==null)return false;
            _pendingDig=hand.AimCollider;_pendingDigHand=hand;
            _pendingDigUntil=Time.unscaledTime+.75f;
            return true; // Consume the press without a duplicate mouse click.
        }
        // Hover already ran in PoseHand. Re-sending hover here can reset the
        // native ready state immediately before its click is delivered.
        MousePick.Send(pick.Fsm,pick.Down);
        if(_digLogCount<=40)Log("[dig] native click sent; resulting state="+pick.Fsm.Fsm.ActiveStateName);
        return true;
    }

    private readonly GameObject[] _digCues=new GameObject[2];
    private void HideDigCues()
    {
        foreach(var cue in _digCues)if(cue!=null)cue.SetActive(false);
    }
    private void UpdateDigCue(Hand hand,PickEvents pick,bool clickable)
    {
        if(hand==null||!hand.Valid||!Application.isFocused||!PalmPickupAllowed||_journalOpen||_comfortOpen||
            !clickable||!IsDigTarget(hand.AimCollider)||!DigReady(pick))return;
        ReadToolOwnership();
        if(!_ownsShovel)return;
        int side=hand.IsLeft?0:1;
        if(_digCues[side]==null) {
            var cue=new GameObject("BPVR_DigReady_"+side);
            UnityEngine.Object.DontDestroyOnLoad(cue);
            _digCues[side]=cue;
            var color=new Color(.95f,.78f,.30f);
            ToolPart(cue.transform,"Shovel handle",PrimitiveType.Cube,new Vector3(0,.025f,0),new Vector3(.012f,.08f,.008f),color);
            ToolPart(cue.transform,"Shovel grip",PrimitiveType.Cube,new Vector3(0,.065f,0),new Vector3(.045f,.012f,.008f),color);
            ToolPart(cue.transform,"Shovel blade",PrimitiveType.Sphere,new Vector3(0,-.035f,0),new Vector3(.05f,.06f,.008f),color);
            var label=new GameObject("Dig instruction");label.transform.SetParent(cue.transform,false);
            var text=label.AddComponent(Il2CppInterop.Runtime.Il2CppType.Of<Il2CppTMPro.TextMeshPro>()).Cast<Il2CppTMPro.TextMeshPro>();
            text.text="DIG\nGrip / Trigger";text.fontSize=2; text.alignment=Il2CppTMPro.TextAlignmentOptions.Center;
            text.color=color;text.rectTransform.sizeDelta=new Vector2(3,1);
            label.transform.localScale=Vector3.one*.1f;label.transform.localPosition=new Vector3(0,-.11f,0);
        }
        var eye=_eyeCam[0];if(eye==null)return;
        var root=_digCues[side].transform;
        root.SetPositionAndRotation(hand.AimPoint+Vector3.up*.23f,eye.transform.rotation);
        root.gameObject.SetActive(true);
    }
}
