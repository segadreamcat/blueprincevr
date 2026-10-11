using System;
using System.Collections.Generic;
using UnityEngine;
using Il2Cpp;
using Il2CppHutongGames.PlayMaker.Actions;
namespace BluePrinceVR;
public partial class VRPlugin
{
    private Transform _pumpRoom;
    private readonly List<PlayMakerFSM> _pumpLevers=new();
    private PlayMakerFSM _heldPump;
    private Hand _pumpHand;
    private Vector3 _pumpStart;
    private readonly Dictionary<int,bool> _pumpNextUp=new();
    private float _pumpPressDeadline;
    private bool _pumpClicked,_pumpMoved;
    private float _pumpScanAt;
    private bool _pumpFailed;
    internal static PlayMakerFSM ActivePumpTap;
    private readonly float[] _pumpHintScanAt=new float[2];
    private readonly PlayMakerFSM[] _pumpHintTarget=new PlayMakerFSM[2];
    private bool PumpGrabReady(Hand hand)
    {
        if(hand==null||!hand.Valid||!Application.isFocused||!PalmPickupAllowed||_heldPump!=null||
            _journalOpen||_comfortOpen||_panelAdjusting||OtherGripHeld(hand))return false;
        int side=hand.IsLeft?0:1;
        // Share the actual tap selector; throttle visual-only discovery to 10 Hz.
        if(Time.unscaledTime>=_pumpHintScanAt[side]) {
            _pumpHintScanAt[side]=Time.unscaledTime+.1f;
            try {_pumpHintTarget[side]=FindPumpLever(hand);}
            catch {_pumpHintTarget[side]=null;}
        }
        var target=_pumpHintTarget[side];
        return target!=null&&target.isActiveAndEnabled&&(PumpListens(target,"hov")||PumpListens(target,"click"));
    }
    private static Transform PumpRoom(Transform t)
    {
        for(int i=0;t!=null&&i<12;i++,t=t.parent)
            if(t.name.StartsWith("Pump Room",StringComparison.OrdinalIgnoreCase))return t;
        return null;
    }
    private static bool PumpListens(PlayMakerFSM fsm,string input)
    {
        if(fsm==null||!fsm.isActiveAndEnabled||fsm.Fsm.ActiveState==null)return false;
        foreach(var transition in fsm.Fsm.ActiveState.Transitions)
            if(transition!=null&&transition.EventName==input&&!string.IsNullOrEmpty(transition.ToState))return true;
        return false;
    }
    private void ReleasePump()
    {
        // Only the current state's documented release input; never force a state.
        if(PumpListens(_heldPump,"exit"))MousePick.Send(_heldPump,"exit");
        _heldPump=null;_pumpHand=null;_pumpClicked=_pumpMoved=false;
        ActivePumpTap=null;
    }
    private PlayMakerFSM FindPumpLever(Hand hand)
    {
        var room=PumpRoom(hand.AimCollider==null?null:hand.AimCollider.transform);
        // Tap acquisition and throttled grab hints discover nearby geometry,
        // not solely from the thin aim ray (which can pass between the controls).
        if(room==null)foreach(var nearby in Physics.OverlapSphere(hand.AimOrigin,2.5f,-1,QueryTriggerInteraction.Collide)) {
            room=PumpRoom(nearby.transform);
            if(room!=null)break;
        }
        if(room==null)return null;
        if(room!=_pumpRoom||Time.unscaledTime>=_pumpScanAt){
            _pumpRoom=room;_pumpScanAt=Time.unscaledTime+2;_pumpLevers.Clear();
            foreach(var fsm in room.GetComponentsInChildren<PlayMakerFSM>(true))
                if(fsm!=null&&fsm.gameObject.name.StartsWith("PUMP ",StringComparison.Ordinal)&&fsm.gameObject.name.EndsWith(" - lever",StringComparison.Ordinal))_pumpLevers.Add(fsm);
        }
        float best=float.MaxValue;PlayMakerFSM selected=null;
        foreach(var fsm in _pumpLevers){
            if(fsm==null||!fsm.isActiveAndEnabled||(!PumpListens(fsm,"hov")&&!PumpListens(fsm,"click")))continue;
            foreach(var renderer in fsm.GetComponentsInChildren<Renderer>()){
                if(renderer==null||!renderer.enabled)continue;
                Vector3 point=renderer.bounds.ClosestPoint(hand.AimOrigin),delta=point-hand.AimOrigin;
                float distance=delta.magnitude;
                if(distance>2.5f||distance>=best)continue;
                float along=Vector3.Dot(delta,hand.AimDirection);
                // Broad close-hand grab, then a forgiving pointed reach zone.
                // Still select only the nearest unobstructed lever.
                bool aboveHandle=delta.y<=.25f&&delta.y>=-1.4f&&new Vector2(delta.x,delta.z).magnitude<=.8f;
                if(!aboveHandle&&distance>.85f&&(along<0||(delta-hand.AimDirection*along).magnitude>.65f))continue;
                bool blocked=false;
                if(distance>.001f)foreach(var hit in Physics.RaycastAll(hand.AimOrigin,delta.normalized,distance,-1,QueryTriggerInteraction.Ignore)){
                    var col=hit.collider;if(col==null||col.name.StartsWith("BPVR_"))continue;
                    // This broad scenery collider covers the controls in the captures.
                    // Exempt only that exact collider in this room, not walls/tanks.
                    if((col.name=="Pumps Collider"||col.name=="right tank catrub collider")&&PumpRoom(col.transform)==room)continue;
                    if(col.transform==fsm.transform||col.transform.IsChildOf(fsm.transform))continue;
                    blocked=true;break;
                }
                if(!blocked){selected=fsm;best=distance;}
            }
        }
        return selected;
    }
    private bool UpdatePumpHand(Hand hand)
    {
        if(_pumpFailed)return false;
        try{return UpdatePumpHandCore(hand);}
        catch(Exception e){
            try{ReleasePump();}catch{_heldPump=null;_pumpHand=null;ActivePumpTap=null;}
            _pumpFailed=true;
            LogWarn("[pump] Optional lever bridge disabled for this session: "+e.Message);
            return false;
        }
    }
    private bool UpdatePumpHandCore(Hand hand)
    {
        if(_heldPump!=null&&_pumpHand==hand){
            if(!hand.Valid||_journalOpen||_comfortOpen||!Application.isFocused||!_inHouse||Time.unscaledTime>_pumpPressDeadline||Vector3.Distance(hand.AimOrigin,_pumpStart)>1.5f){
                Log("[pump] Tap cancelled: state="+_heldPump.Fsm.ActiveStateName+" timedOut="+(Time.unscaledTime>_pumpPressDeadline)+" handTravel="+Vector3.Distance(hand.AimOrigin,_pumpStart));
                ReleasePump();return true;
            }
            if(_pumpMoved){ReleasePump();return true;}
            // Once the native busy check reaches Hover, resolve the documented
            // click -> drag -> direction path and enter its animation directly.
            // No mouse release/axis action can race this controller-only step.
            if(TryPumpTapAnimation(_heldPump,hand))return true;
            if(!_pumpClicked){
                if(PumpListens(_heldPump,"hov"))MousePick.Send(_heldPump,"hov");
                if(TryPumpTapAnimation(_heldPump,hand))return true;
                if(PumpListens(_heldPump,"click")) {
                    MousePick.Send(_heldPump,"click");
                    // Wait's click only enters Busy Check, not the drag listener.
                    // Keep waiting for the native Hover state before consuming the click.
                    _pumpClicked=(_heldPump.Fsm.ActiveStateName??"").StartsWith("Click & Hold",StringComparison.Ordinal);
                }
            }
            string state=_heldPump.Fsm.ActiveStateName??"";
            if(state.StartsWith("Click & Hold",StringComparison.Ordinal)||state.StartsWith("Mouse Y Compare",StringComparison.Ordinal)||state.StartsWith("Controller Listener",StringComparison.Ordinal))_pumpClicked=true;
            // A tap replaces the native mouse-drag wait, using only its declared
            // continuation. No mouse movement, held grip, or forced FSM state.
            if(_pumpClicked&&state.StartsWith("Click & Hold",StringComparison.Ordinal)&&PumpListens(_heldPump,"continue")) {
                MousePick.Send(_heldPump,"continue");state=_heldPump.Fsm.ActiveStateName??"";
            }
            if(_pumpClicked&&!_pumpMoved&&(state.StartsWith("Mouse Y Compare",StringComparison.Ordinal)||state.StartsWith("Controller Listener",StringComparison.Ordinal))){
                bool up=PumpListens(_heldPump,"up"),down=PumpListens(_heldPump,"down");
                int id=_heldPump.GetInstanceID();bool preferUp=!_pumpNextUp.TryGetValue(id,out var next)||next;
                string move=up&&down?(preferUp?"up":"down"):up?"up":down?"down":null;
                if(move!=null){MousePick.Send(_heldPump,move);_pumpNextUp[id]=move=="up";_pumpMoved=true;PatternPulse(hand,1400);Log("[pump] Grip tap stepped lever: "+move);}
            }
            return true;
        }
        bool press=(hand.TriggerDown&&!hand.TriggerWasDown)||(hand.GripDown&&!hand.GripWasDown);
        if(_heldPump!=null||hand.DirectClickConsumed||!hand.Valid||!PalmPickupAllowed||_journalOpen||_comfortOpen||!press||OtherGripHeld(hand))return false;
        var lever=FindPumpLever(hand);if(lever==null){
            if(PumpRoom(hand.AimCollider==null?null:hand.AimCollider.transform)!=null)Log("[pump] No lever acquired at hand="+hand.AimOrigin+" rayTarget="+hand.AimCollider.name);
            return false;
        }
        _heldPump=lever;_pumpHand=hand;_pumpStart=hand.AimOrigin;_pumpClicked=_pumpMoved=false;
        ActivePumpTap=lever;
        _pumpPressDeadline=Time.unscaledTime+3f;
        Log("[pump] Trigger/grip tap on "+lever.gameObject.name+"; one position per press.");
        return UpdatePumpHandCore(hand);
    }
    private bool TryPumpTapAnimation(PlayMakerFSM fsm,Hand hand)
    {
        string state=fsm.Fsm.ActiveStateName??"";
        string Destination(string from,string input) {
            foreach(var s in fsm.Fsm.States)if(s.Name==from)
                foreach(var t in s.Transitions)if(t.EventName==input)return t.ToState;
            return null;
        }
        if(state.StartsWith("Wait",StringComparison.Ordinal)) {
            string check=Destination(state,"hov");
            if(check==null||!check.StartsWith("Busy Check",StringComparison.Ordinal))return false;
            // Sample the very same native busy source without entering the mouse
            // hover states, whose distance checks reject standing VR interaction.
            bool sampled=false;
            foreach(var s in fsm.Fsm.States)if(s.Name==check)
                foreach(var action in s.Actions) {
                    var read=action.TryCast<GetFsmBool>();
                    if(read!=null){read.OnEnter();sampled=true;}
                }
            if(!sampled)return false;
            var busy=fsm.FsmVariables.GetFsmBool("busy");
            if(busy==null||busy.Value)return false;
            state=Destination(check,"cont")??"";
        }
        if(state.StartsWith("Busy Check",StringComparison.Ordinal)) {
            // GetFsmBool has already sampled the game's busy flag on entry.
            var busy=fsm.FsmVariables.GetFsmBool("busy");
            if(busy==null||busy.Value)return false;
            state=Destination(state,"cont")??"";
        }
        if(!state.StartsWith("Hover",StringComparison.Ordinal))return false;
        string drag=Destination(state,"click");
        if(drag==null||!drag.StartsWith("Click & Hold",StringComparison.Ordinal))return false;
        string compare=Destination(drag,"continue");
        if(compare==null||!compare.StartsWith("Mouse Y Compare",StringComparison.Ordinal))return false;
        string up=Destination(compare,"up"),down=Destination(compare,"down");
        int id=fsm.GetInstanceID();bool preferUp=!_pumpNextUp.TryGetValue(id,out var next)||next;
        bool goUp=!string.IsNullOrEmpty(up)&&(preferUp||string.IsNullOrEmpty(down));
        string target=goUp?up:down;
        if(string.IsNullOrEmpty(target))return false;
        fsm.Fsm.SetState(target); // Run the game's own animation, sound and pump updates.
        _pumpNextUp[id]=goUp;_pumpMoved=true;
        PatternPulse(hand,1400);
        Log("[pump] Native tap animation: "+fsm.gameObject.name+" -> "+target);
        return true;
    }
}
