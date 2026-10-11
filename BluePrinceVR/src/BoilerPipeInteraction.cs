using System;
using UnityEngine;
namespace BluePrinceVR;
public partial class VRPlugin
{
    private int _pipeReports;
    private static bool IsBoilerPipe(Collider col)
    {
        if(col==null||!col.name.StartsWith("Pipe Switcher ",StringComparison.Ordinal))return false;
        for(var t=col.transform.parent;t!=null;t=t.parent) {
            if(t.name.StartsWith("Boiler Room",StringComparison.Ordinal))return true;
            if(t.name=="Room Spawn Pools")break;
        }
        return false;
    }
    private bool ClickBoilerPipe(Hand hand)
    {
        var pick=MousePick.For(hand.AimCollider);
        // Shared native-event eligibility includes local and global click transitions.
        // Do not restart hover or require a MousePick action in the post-animation state.
        bool ready=DigReady(pick);
        if(_pipeReports++<8)Log("[boiler pipe] press "+hand.AimCollider.name+" state="+pick?.Fsm?.Fsm.ActiveStateName+" ready="+ready);
        if(!ready) {
            if(_pipeReports<=2)Log(InteractionDiagnostics.Capture(hand.AimCollider));
            return false;
        }
        MousePick.Send(pick.Fsm,pick.Down);
        if(!string.IsNullOrEmpty(pick.Up))MousePick.Send(pick.Fsm,pick.Up);
        return true;
    }
}
