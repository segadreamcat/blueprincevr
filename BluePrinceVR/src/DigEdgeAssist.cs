using System.Collections.Generic;
using UnityEngine;
namespace BluePrinceVR;
public partial class VRPlugin
{
    private readonly List<Collider>[] _digEdgeCandidates={new(),new()};
    private readonly float[] _digEdgeScanAt=new float[2];
    private bool DigEdgeAssist(Hand hand,out RaycastHit best)
    {
        best=default;
        // Hover must reach the native Tool Check first: inventory is stored in
        // native arrays, not reliably in a global "owns shovel" boolean.
        // ClickDigTarget only accepts the subsequent native ready state.
        int side=hand.IsLeft?0:1;
        var candidates=_digEdgeCandidates[side];
        if(Time.unscaledTime>=_digEdgeScanAt[side]) {
            _digEdgeScanAt[side]=Time.unscaledTime+.2f;candidates.Clear();
            foreach(var col in Physics.OverlapSphere(hand.AimOrigin,ShovelReach,-1,QueryTriggerInteraction.Collide)) {
                if(candidates.Count>=48)break;
                if(col!=null&&IsDigTarget(col))candidates.Add(col);
            }
        }
        float scoreBest=float.MaxValue;
        foreach(var col in candidates) {
            if(col==null||!col.enabled||!col.gameObject.activeInHierarchy)continue;
            var pick=MousePick.For(col);
            if(pick==null||!pick.CanClick)continue;
            Vector3 toward=col.bounds.center-hand.AimOrigin;
            float distance=toward.magnitude;
            if(distance<.001f||distance>ShovelReach)continue;
            float along=Vector3.Dot(toward,hand.AimDirection);
            float off=(toward-hand.AimDirection*Mathf.Max(0,along)).magnitude;
            if(distance>.65f&&(along<0||off>.65f))continue;
            float score=off+distance*.1f;
            if(score>=scoreBest)continue;
            var direction=toward/distance;
            var start=hand.AimOrigin;
            if(col.bounds.Contains(start))start=col.bounds.center-direction*(col.bounds.extents.magnitude+.1f);
            if(!col.Raycast(new Ray(start,direction),out var hit,ShovelReach+1f))continue;
            float reach=Vector3.Distance(hand.AimOrigin,hit.point);
            if(reach>ShovelReach)continue;
            bool blocked=false;
            foreach(var obstruction in Physics.RaycastAll(hand.AimOrigin,direction,reach,-1,QueryTriggerInteraction.Ignore)) {
                var obstacle=obstruction.collider;
                if(obstacle==null||obstacle==col||obstacle.name.StartsWith("BPVR_")||IsDigTarget(obstacle))continue;
                // Allow only the final 25cm around a partly embedded pile. A wall
                // encountered earlier along the shovel's path still blocks digging.
                if(reach-obstruction.distance>.25f){blocked=true;break;}
            }
            if(blocked)continue;
            scoreBest=score;hit.distance=reach;best=hit;
        }
        // Native click handling still checks shovel ownership and pile availability.
        return best.collider!=null;
    }
}
