using System;
using UnityEngine;
namespace BluePrinceVR;
public partial class VRPlugin
{
    private readonly float[] _toolAssistScanAt=new float[2];
    private readonly RaycastHit[] _toolAssistCached=new RaycastHit[2];
    private static bool IsDigTarget(Collider collider)
    {
        for(var t=collider==null?null:collider.transform;t!=null;t=t.parent) {
            if(t.name.StartsWith("DIRT PILE",StringComparison.OrdinalIgnoreCase) ||
                System.Text.RegularExpressions.Regex.IsMatch(t.name,@"\b(dig|digging)\b",System.Text.RegularExpressions.RegexOptions.IgnoreCase))return true;
            if(t.name=="_GAMEPLAY"||t.name=="_COLLIDERS"||t.name=="Room Spawn Pools")break;
        }
        return false;
    }
    private const float ShovelReach=3.2f;
    private const float PickupHoverReach=2.6f;
    private static float WorldInteractionReach(Collider collider)=>IsPickupTarget(collider)?PickupHoverReach:IsDigTarget(collider)?ShovelReach:InteractionBoxRoot(collider)!=null?BoxReach(InteractionBoxRoot(collider)):1.5f;
    private static bool WithinInteractionReach(Collider collider,PickEvents pick,float distance)
    {
        // Dirt uses the extended VR shovel reach, rather than the shorter mouse reach.
        // Native dig events still enforce shovel ownership and whether the pile is usable.
        return distance<=WorldInteractionReach(collider) &&
            (IsPickupTarget(collider)||IsDigTarget(collider)||BoxReach(InteractionBoxRoot(collider))==ShovelReach||pick.Reach<=0f||distance<=pick.Reach);
    }
    private static bool IsToolTarget(Collider collider)
    {
        if(IsDigTarget(collider))return true;
        for(var t=collider==null?null:collider.transform;t!=null;t=t.parent) {
            if(collider.name.Equals("Keyhole",StringComparison.OrdinalIgnoreCase)&&t.name.StartsWith("Parlor",StringComparison.OrdinalIgnoreCase))return true;
            if(t.name.StartsWith("puzshog",StringComparison.OrdinalIgnoreCase) ||
                System.Text.RegularExpressions.Regex.IsMatch(t.name,@"\b(dig|digging)\b",System.Text.RegularExpressions.RegexOptions.IgnoreCase))return true;
            if(t.name=="_GAMEPLAY"||t.name=="Room Spawn Pools")break;
        }
        return false;
    }
    private bool ToolAimAssist(Hand hand,out RaycastHit best,out bool clickable)
    {
        best=default;clickable=false;
        if(hand==null||!hand.Valid||!PalmPickupAllowed||_journalOpen)return false;
        if(DigEdgeAssist(hand,out best)){clickable=true;return true;}
        int side=hand.IsLeft?0:1;
        var direction=hand.AimDirection;
        if(Time.unscaledTime<_toolAssistScanAt[side]) {
            if(_toolAssistCached[side].collider==null)return false;
            Vector3 toward=_toolAssistCached[side].point-hand.AimOrigin;
            float allowed=Mathf.Atan(.075f/Mathf.Max(.25f,toward.magnitude))*Mathf.Rad2Deg+1f;
            if(toward.sqrMagnitude<.000001f || Vector3.Angle(direction,toward)>allowed)return false;
            try {
                hand.AimDirection=toward.normalized;
                return AimHit(hand,out best,out clickable)&&clickable&&best.collider==_toolAssistCached[side].collider;
            } finally {hand.AimDirection=direction;}
        }
        _toolAssistScanAt[side]=Time.unscaledTime+.05f;_toolAssistCached[side]=default;
        Vector3 right=Vector3.Cross(direction,Vector3.up).normalized;
        if(right.sqrMagnitude<.01f)right=Vector3.right;
        Vector3 up=Vector3.Cross(right,direction).normalized;
        float nearest=ShovelReach;
        float spread=.075f;
        if(AimHit(hand,out var surface,out _)&&IsToolTarget(surface.collider))spread=.075f/Mathf.Max(.25f,surface.distance);
        try {
            // Eight nearby rays widen ONLY these tool targets. Every ray retains
            // the original solid-occlusion and item reach tests in AimHit.
            for(int i=0;i<8;i++) {
                float angle=i*Mathf.PI/4;
                hand.AimDirection=(direction+(right*Mathf.Cos(angle)+up*Mathf.Sin(angle))*spread).normalized;
                if(AimHit(hand,out var hit,out var ready)&&ready&&hit.distance<nearest&&IsToolTarget(hit.collider)) {
                    best=hit;nearest=hit.distance;clickable=true;
                }
            }
            _toolAssistCached[side]=best;return clickable;
        } finally {hand.AimDirection=direction;}
    }
}
