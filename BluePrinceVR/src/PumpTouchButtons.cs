using System;
using System.Collections.Generic;
using UnityEngine;
using Il2Cpp;
namespace BluePrinceVR;
public partial class VRPlugin
{
    private sealed class PumpTouchButton
    {
        public PlayMakerFSM Fsm;
        public Collider[] Colliders;
        public readonly KeyTouchGate[] Gates={new(),new()};
        public float LastPress=-1;
    }
    private readonly List<PumpTouchButton> _pumpTouchButtons=new();
    private Transform _pumpTouchRoom;
    private float _pumpTouchScan;
    private bool _pumpTouchFailed;
    private readonly PumpTouchButton[] _pendingPumpButton=new PumpTouchButton[2];
    private readonly float[] _pendingPumpButtonUntil=new float[2];
    private readonly Vector3[] _pumpPreviousTip=new Vector3[2];
    private readonly bool[] _pumpPreviousValid=new bool[2];
    private readonly float[] _pumpPointUntil=new float[2];
    private bool PumpPointPose(Hand hand)
    {
        if(!PalmPickupAllowed||_journalOpen||_comfortOpen||_heldPump!=null)return false;
        int side=hand.IsLeft?0:1;
        foreach(var button in _pumpTouchButtons) {
            if(button.Fsm==null||!button.Fsm.isActiveAndEnabled)continue;
            foreach(var col in button.Colliders)if(col!=null&&col.enabled&&col.gameObject.activeInHierarchy&&
                Vector3.Distance(hand.AimOrigin,col.ClosestPoint(hand.AimOrigin))<.4f)_pumpPointUntil[side]=Time.unscaledTime+.35f;
        }
        return Time.unscaledTime<_pumpPointUntil[side];
    }
    private bool UpdatePumpButtons(Hand hand)
    {
        if(_pumpTouchFailed)return false;
        try {return UpdatePumpButtonsCore(hand);}
        catch(Exception e){_pumpTouchFailed=true;LogWarn("[pump touch] Disabled; trigger interaction retained: "+e.Message);return false;}
    }
    private bool UpdatePumpButtonsCore(Hand hand)
    {
        if(!hand.Valid||!PalmPickupAllowed||_heldPump!=null||_journalOpen||_comfortOpen||!Application.isFocused) {
            _pendingPumpButton[hand.IsLeft?0:1]=null;
            _pumpPreviousValid[hand.IsLeft?0:1]=false;
            foreach(var button in _pumpTouchButtons)button.Gates[hand.IsLeft?0:1].Reset();
            return false;
        }
        var room=PumpRoom(hand.AimCollider==null?null:hand.AimCollider.transform);
        if(room==null&&Time.unscaledTime>=_pumpTouchScan)foreach(var col in Physics.OverlapSphere(hand.AimOrigin,2f,-1,QueryTriggerInteraction.Collide)) {
            room=PumpRoom(col.transform);if(room!=null)break;
        }
        if(room!=null && (room!=_pumpTouchRoom||Time.unscaledTime>=_pumpTouchScan)) {
            _pumpTouchRoom=room;_pumpTouchScan=Time.unscaledTime+2;
            // Retain gates while staying in the same room: holding contact must
            // never become a new press just because the discovery timer elapsed.
            var previous=new List<PumpTouchButton>(_pumpTouchButtons);_pumpTouchButtons.Clear();
            foreach(var fsm in room.GetComponentsInChildren<PlayMakerFSM>(false)) {
                if(fsm==null||!fsm.gameObject.name.StartsWith("Pump Button ",StringComparison.Ordinal))continue;
                var existing=previous.Find(b=>b.Fsm==fsm);
                if(existing!=null){_pumpTouchButtons.Add(existing);continue;}
                var cols=new List<Collider>();
                foreach(var col in fsm.GetComponentsInChildren<Collider>(false))if(col!=null)cols.Add(col);
                _pumpTouchButtons.Add(new PumpTouchButton{Fsm=fsm,Colliders=cols.ToArray()});
            }
        }
        Vector3 tip=IndexTip(hand);
        int side=hand.IsLeft?0:1;
        Vector3 previousTip=_pumpPreviousTip[side];
        bool sweep=_pumpPreviousValid[side]&&Vector3.Distance(previousTip,tip)<.35f;
        _pumpPreviousTip[side]=tip;_pumpPreviousValid[side]=true;
        PumpTouchButton nearest=null,pointed=null;float best=float.MaxValue,pointScore=float.MaxValue;
        foreach(var button in _pumpTouchButtons) {
            if(button.Fsm==null||!button.Fsm.isActiveAndEnabled)continue;
            foreach(var col in button.Colliders) {
                if(col==null||!col.enabled||!col.gameObject.activeInHierarchy)continue;
                float distance=Vector3.Distance(tip,col.ClosestPoint(tip));
                if(sweep)for(int sample=1;sample<5;sample++) {
                    var p=Vector3.Lerp(previousTip,tip,sample/5f);
                    distance=Mathf.Min(distance,Vector3.Distance(p,col.ClosestPoint(p)));
                }
                if(distance<best){best=distance;nearest=button;}
                Vector3 delta=col.bounds.center-hand.AimOrigin;
                float along=Vector3.Dot(delta,hand.AimDirection);
                float off=(delta-hand.AimDirection*along).magnitude;
                if(along>0&&delta.magnitude<2f&&off<.12f&&off<pointScore) {
                    bool blocked=false;
                    foreach(var hit in Physics.RaycastAll(hand.AimOrigin,delta.normalized,delta.magnitude,-1,QueryTriggerInteraction.Ignore))
                        if(hit.collider!=null&&!hit.collider.name.StartsWith("BPVR_")&&hit.collider!=col&&!hit.collider.transform.IsChildOf(button.Fsm.transform)){blocked=true;break;}
                    if(!blocked){pointed=button;pointScore=off;}
                }
            }
        }
        bool pressed=false;
        if(hand.TriggerDown&&!hand.TriggerWasDown&&!hand.GripDown&&pointed!=null) {
            _pendingPumpButton[side]=pointed;_pendingPumpButtonUntil[side]=Time.unscaledTime+1f;pressed=true;
        }
        foreach(var button in _pumpTouchButtons) {
            float distance=button==nearest?best:float.MaxValue;
            if(distance<.14f && PumpListens(button.Fsm,"hov"))MousePick.Send(button.Fsm,"hov");
            bool eligible=button==nearest && !hand.GripDown && !hand.TriggerDown && Time.unscaledTime-button.LastPress>.25f;
            if(button.Gates[hand.IsLeft?0:1].Tick(distance,eligible,.075f,.11f)) {
                _pendingPumpButton[side]=button;_pendingPumpButtonUntil[side]=Time.unscaledTime+1f;pressed=true;
            }
        }
        var pending=_pendingPumpButton[side];
        if(pending!=null) {
            pressed=true;
            if(pending.Fsm==null||Time.unscaledTime>_pendingPumpButtonUntil[side])_pendingPumpButton[side]=null;
            else {
                if(PumpListens(pending.Fsm,"hov"))MousePick.Send(pending.Fsm,"hov");
                if(!PumpListens(pending.Fsm,"hov")&&PumpListens(pending.Fsm,"click")) {
                    MousePick.Send(pending.Fsm,"click");pending.LastPress=Time.unscaledTime;
                    _pendingPumpButton[side]=null;PatternPulse(hand,1000);
                    Log("[pump button] Ready-state press: "+pending.Fsm.gameObject.name);
                }
            }
        }
        return pressed;
    }
}
