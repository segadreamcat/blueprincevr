using System;
using System.Collections.Generic;
using UnityEngine;
namespace BluePrinceVR;
public partial class VRPlugin
{
    private readonly List<Collider>[] _boxCandidates={new(),new()};
    private readonly float[] _boxScanAt=new float[2];
    private static float BoxReach(Transform root)=>root!=null&&root.name.StartsWith("TRUNK(Clone)",StringComparison.OrdinalIgnoreCase)?ShovelReach:2f;
    private static Transform InteractionBoxRoot(Collider col)
    {
        for(var t=col==null?null:col.transform;t!=null;t=t.parent) {
            if(t.name.StartsWith("TRUNK(Clone)",StringComparison.OrdinalIgnoreCase) ||
               t.name.StartsWith("ParlorBox ",StringComparison.OrdinalIgnoreCase))return t;
            if(t.name=="_GAMEPLAY"||t.name=="Room Spawn Pools"||t.name=="Pickup Spawn Pools")break;
        }
        return null;
    }
    private bool BoxInteractionAssist(Hand hand,out RaycastHit best)
    {
        best=default;
        if(hand==null||!hand.Valid||!PalmPickupAllowed||_journalOpen)return false;
        int side=hand.IsLeft?0:1;
        var candidates=_boxCandidates[side];
        if(Time.unscaledTime>=_boxScanAt[side]) {
            _boxScanAt[side]=Time.unscaledTime+.2f;candidates.Clear();
            var roots=new HashSet<int>();
            foreach(var nearby in Physics.OverlapSphere(hand.AimOrigin,ShovelReach,-1,QueryTriggerInteraction.Collide)) {
                var root=InteractionBoxRoot(nearby);
                if(root==null||!roots.Add(root.GetInstanceID()))continue;
                foreach(var col in root.GetComponentsInChildren<Collider>(false)) {
                    if(candidates.Count>=48)break;
                    var pick=MousePick.For(col);
                    bool parlor=root.name.StartsWith("ParlorBox ",StringComparison.OrdinalIgnoreCase);
                    if(col.enabled&&pick!=null&&pick.CanClick&&!IsPickupTarget(col)&&
                       (!parlor||col.name.Equals("Keyhole",StringComparison.OrdinalIgnoreCase)))candidates.Add(col);
                }
            }
        }
        float closest=float.MaxValue;
        foreach(var col in candidates) {
            if(col==null||!col.enabled||!col.gameObject.activeInHierarchy||OpenTrunkShell(col))continue;
            var root=InteractionBoxRoot(col);if(root==null)continue;
            var pick=MousePick.For(col);if(pick==null||!pick.CanClick)continue;
            Vector3 toward=col.bounds.center-hand.AimOrigin;
            float distance=toward.magnitude;
            if(distance>BoxReach(root)||distance<.001f)continue;
            float along=Vector3.Dot(toward,hand.AimDirection);
            float off=(toward-hand.AimDirection*Mathf.Max(0,along)).magnitude;
            // Broad lock area, plus near-hand access without threading a ray into the lid.
            bool trunk=root.name.StartsWith("TRUNK(Clone)",StringComparison.OrdinalIgnoreCase);
            float closeReach=trunk?.70f:.35f, radius=trunk?.60f:.30f;
            if(distance>closeReach&&(along<0||off>radius))continue;
            bool blocked=false;
            foreach(var obstruction in Physics.RaycastAll(hand.AimOrigin,toward/distance,distance,-1,QueryTriggerInteraction.Collide)) {
                var obstacle=obstruction.collider;
                if(obstacle==null||obstacle.name.StartsWith("BPVR_")||obstacle.isTrigger)continue;
                if(InteractionBoxRoot(obstacle)!=root){blocked=true;break;}
            }
            if(blocked)continue;
            // Use the real collider and native events; never resize physical geometry or force an unlock.
            var direction=toward/distance;
            var start=hand.AimOrigin;
            if(col.bounds.Contains(start))start=col.bounds.center-direction*(col.bounds.extents.magnitude+.1f);
            if(!col.Raycast(new Ray(start,direction),out var hit,4f))continue;
            float score=off+distance*.1f;
            if(score>=closest)continue;
            closest=score;hit.distance=Vector3.Distance(hand.AimOrigin,hit.point);best=hit;
        }
        return best.collider!=null;
    }
}
