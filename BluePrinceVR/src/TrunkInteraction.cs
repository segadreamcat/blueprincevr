using System;
using UnityEngine;
namespace BluePrinceVR;
public partial class VRPlugin
{
    private static bool IsTrunk(Collider col)
    {
        var root=InteractionBoxRoot(col);
        return root!=null&&root.name.StartsWith("TRUNK(Clone)",StringComparison.OrdinalIgnoreCase)&&!IsPickupTarget(col);
    }
    private int _trunkReports;
    private static bool OpenTrunkShell(Collider col)
    {
        if(!IsTrunk(col)||IsPickupTarget(col))return false;
        var root=InteractionBoxRoot(col);
        var code=root.Find("Trunk Code");
        if(code==null)return false;
        foreach(var fsm in code.GetComponents<Il2Cpp.PlayMakerFSM>())
            if(fsm!=null&&fsm.Fsm.ActiveStateName=="Turn off Click Blocker")return true;
        return false;
    }
    private static readonly GameObject[] _trunkHoverOwner=new GameObject[2];
    private static readonly float[] _trunkHoverUntil=new float[2];
    private readonly Collider[] _trunkPending=new Collider[2];
    private readonly float[] _trunkPendingUntil=new float[2];
    internal static bool OwnsTrunkHover(GameObject owner)
    {
        if(!Application.isFocused||owner==null)return false;
        for(int i=0;i<2;i++)if(_trunkHoverOwner[i]==owner&&Time.unscaledTime<_trunkHoverUntil[i])return true;
        return false;
    }
    private void UpdateTrunkHover(Hand hand,PickEvents pick)
    {
        int side=hand.IsLeft?0:1;
        bool eligible=hand.Valid&&Application.isFocused&&PalmPickupAllowed&&!_journalOpen&&!_comfortOpen&&IsTrunk(hand.AimCollider)&&!OpenTrunkShell(hand.AimCollider)&&pick?.Fsm!=null;
        if(!eligible){_trunkHoverOwner[side]=null;_trunkPending[side]=null;return;}
        _trunkHoverOwner[side]=pick.Fsm.gameObject;_trunkHoverUntil[side]=Time.unscaledTime+.1f;
        // Let the native inventory branch choose hammer first, or the key UI.
        string state=pick.Fsm.Fsm.ActiveStateName;
        if(state=="State 1"||state=="State 5") {
            // The native OnEnter mouse-distance check can immediately reject hov.
            // Use only the verified idle -> inventory-check transition instead.
            foreach(var t in pick.Fsm.Fsm.ActiveState.Transitions)
                if(t.EventName=="hov"&&t.ToState=="State 4"){pick.Fsm.Fsm.SetState(t.ToState);break;}
        }
        if(_trunkPending[side]!=null) {
            if(_trunkPending[side]!=hand.AimCollider||Time.unscaledTime>_trunkPendingUntil[side]){_trunkPending[side]=null;return;}
            if(DigReady(pick)) {
                _trunkPending[side]=null;
                MousePick.Send(pick.Fsm,pick.Down);
                InteractionPulse(hand,true);
                Log("[trunk] Queued native click accepted from "+state);
            }
        }
    }
    private bool ClickTrunk(Hand hand)
    {
        var pick=MousePick.For(hand.AimCollider);
        bool ready=DigReady(pick);
        if(_trunkReports++<12)Log("[trunk] native input state="+pick?.Fsm?.Fsm.ActiveStateName+" acceptsClick="+ready);
        if(!ready) {
            if(pick?.Fsm==null)return false;
            int side=hand.IsLeft?0:1;
            _trunkPending[side]=hand.AimCollider;_trunkPendingUntil[side]=Time.unscaledTime+1f;
            return true;
        }
        // Run the original interaction, including inventory checks and consumption.
        // No direct unlock event, forced state, free key or hammer is supplied.
        MousePick.Send(pick.Fsm,pick.Down);
        if(!string.IsNullOrEmpty(pick.Up))MousePick.Send(pick.Fsm,pick.Up);
        return true;
    }
}
