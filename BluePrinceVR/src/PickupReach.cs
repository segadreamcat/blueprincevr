using System;
using UnityEngine;
namespace BluePrinceVR;
public partial class VRPlugin
{
    private static bool IsKeyOrGemPickup(Collider col)
    {
        for(var t=col==null?null:col.transform;t!=null;t=t.parent) {
            if(t.name=="Pickup Spawn Pools"||t.name=="Room Spawn Pools"||t.name=="_GAMEPLAY"||t.name=="_COLLIDERS")break;
            string name=t.name.ToUpperInvariant();
            if(name.StartsWith("KEY ")||name.StartsWith("KEY(")||name.StartsWith("GEM ")||name.StartsWith("GEM(")||name.StartsWith("DIAMOND("))return true;
        }
        return false;
    }
    private readonly Vector3[] _palmOrigins=new Vector3[2], _palmDirections=new Vector3[2];
    private readonly bool[] _palmReady=new bool[2];
    private void UpdatePalmRay(Hand hand,Vector3 trackedPosition,Quaternion trackedRotation)
    {
        int side=hand.IsLeft?0:1;
        // User-requested floor-directed pickup beam: wrist rotation cannot tilt it.
        Quaternion palmRotation=trackedRotation*Quaternion.Euler(HandRig.HandEuler);
        Vector3 direction=Vector3.down;
        var model=HandRig.Model;
        var wrist=model==null?null:(hand.IsLeft?model.HandL:model.HandR);
        Vector3 origin=(wrist==null?trackedPosition+palmRotation*Vector3.up*.055f:wrist.position+wrist.up*.055f)+direction*.012f;
        if(_celHands!=null)origin=trackedPosition+trackedRotation*Vector3.forward*(CelHandPack.PalmLength*.55f-Mathf.Clamp(_comfort.Span*.03f,.035f,.075f))+direction*.012f;
        float dt=Mathf.Clamp(Time.unscaledDeltaTime,0f,.05f);
        float blend=_palmReady[side]?1f-Mathf.Exp(-dt/.12f):1f;
        if(!_palmReady[side] || Vector3.Distance(origin,_palmOrigins[side])>.5f) {
            _palmOrigins[side]=origin;_palmDirections[side]=direction;
        } else {
            _palmOrigins[side]=Vector3.Lerp(_palmOrigins[side],origin,blend);
            _palmDirections[side]=Vector3.Slerp(_palmDirections[side],direction,blend).normalized;
        }
        _palmReady[side]=true;
    }
    private bool PalmPickupAllowed => _inHouse && !_computerKeyboardMode && !_draftMenu && !NativePanelMenu &&
        !_panelStick.Active && !_forcePanel && Cursor.lockState!=CursorLockMode.None;
    private Vector3 PalmOrigin(Hand hand)
    {
        if(_palmReady[hand.IsLeft?0:1]) return _palmOrigins[hand.IsLeft?0:1];
        var model=HandRig.Model;
        var wrist=model==null?null:(hand.IsLeft?model.HandL:model.HandR);
        // Bone Y runs toward the fingers; offset onto the palm surface as well.
        return wrist==null?hand.AimOrigin:wrist.position+wrist.up*.055f+PalmDirection(hand)*.012f;
    }
    private Vector3 PalmDirection(Hand hand)
    {
        return Vector3.down;
    }
    private bool PalmPickupHit(Hand hand,out RaycastHit hit,out bool clickable,out Vector3 origin)
    {
        hit=default;clickable=false;origin=hand.AimOrigin;
        if(!PalmPickupAllowed) return false;
        // Start above the palm: reaching into an apple must not start the cast inside it.
        origin=PalmOrigin(hand)+Vector3.up*.12f;
        Vector3 saved=hand.AimOrigin;
        Vector3 savedDirection=hand.AimDirection;
        try {
            hand.AimOrigin=origin;
            hand.AimDirection=PalmDirection(hand);
            if(AimHit(hand,out hit,out clickable) && clickable && IsPickupTarget(hit.collider)) return true;
            // A palm-sized sweep, not a pin-point laser. Use the supported single
            // SphereCast overload; never SphereCastAll (stripped in this game).
            if(!_palmSweepFailed && Physics.SphereCast(origin,.30f,hand.AimDirection,out var wide,PickupHoverReach,-1,QueryTriggerInteraction.Collide) &&
                wide.collider!=null && IsPickupTarget(wide.collider)) {
                Vector3 toward=wide.point-origin;
                if(toward.sqrMagnitude>.000001f && toward.magnitude<=PickupHoverReach) {
                    hand.AimDirection=toward.normalized;
                    // Verify clear line of sight and the item's own reach limit.
                    if(AimHit(hand,out hit,out clickable) && hit.collider==wide.collider) return true;
                }
            }
            return NearbyPalmPickup(hand,origin,out hit,out clickable);
        } catch(Exception e) {
            if(!_palmSweepFailed) LogWarn("[pickup] Broad palm sweep unavailable; narrow palm pickup retained: "+e.Message);
            _palmSweepFailed=true;hit=default;clickable=false;return false;
        } finally { hand.AimOrigin=saved; hand.AimDirection=savedDirection; }
    }
    private bool _palmSweepFailed;
    private readonly float[] _nearPickupScanAt=new float[2];
    private readonly RaycastHit[] _nearPickupHit=new RaycastHit[2];
    private bool NearbyPalmPickup(Hand hand,Vector3 origin,out RaycastHit hit,out bool clickable)
    {
        hit=default;clickable=false;int side=hand.IsLeft?0:1;
        var cached=_nearPickupHit[side];
        if(cached.collider!=null && cached.collider.enabled && cached.collider.gameObject.activeInHierarchy) {
            var toward=cached.point-origin;
            if(new Vector2(toward.x,toward.z).magnitude<=.45f && toward.y<=.02f) {
                hand.AimOrigin=origin;hand.AimDirection=toward.normalized;
                if(VisibleHoverItem(cached.collider,origin,out hit)){clickable=true;return true;}
            }
        }
        if(Time.unscaledTime<_nearPickupScanAt[side] && !(hand.GripDown&&!hand.GripWasDown))return false;
        _nearPickupScanAt[side]=Time.unscaledTime+.05f;_nearPickupHit[side]=default;
        if(RoomPickupCandidate(hand,origin,out hit,out clickable)) {
            _nearPickupHit[side]=hit;return true;
        }
        // Parallel rays can reach a coin beside a table edge that a wide sphere hits first.
        // Confirm every candidate from the central origin so offsets cannot select through walls.
        for(int i=0;i<16;i++) {
            float angle=i*Mathf.PI/4;
            float radius=i<8?.10f:.20f;
            hand.AimOrigin=origin+new Vector3(Mathf.Cos(angle)*radius,0,Mathf.Sin(angle)*radius);
            hand.AimDirection=Vector3.down;
            if(!AimHit(hand,out var candidate,out var ready)||!ready||!IsPickupTarget(candidate.collider))continue;
            hand.AimOrigin=origin;hand.AimDirection=(candidate.point-origin).normalized;
            if(AimHit(hand,out hit,out clickable)&&clickable&&hit.collider==candidate.collider) {
                _nearPickupHit[side]=hit;return true;
            }
        }
        hit=default;clickable=false;return false;
    }
    private Transform _pickupCandidateRoom;
    private float _pickupCandidateRefresh;
    private readonly System.Collections.Generic.List<Collider> _pickupCandidates=new();
    private bool RoomPickupCandidate(Hand hand,Vector3 origin,out RaycastHit hit,out bool clickable)
    {
        hit=default;clickable=false;
        // Thin coins can sit between the sampling rays, while the sphere hits
        // the tabletop first. Aim at known item colliders, then verify LOS.
        Transform room=hand.AimCollider==null?null:hand.AimCollider.transform;
        if(room!=_pickupCandidateRoom || Time.unscaledTime>=_pickupCandidateRefresh) {
            _pickupCandidateRoom=room;_pickupCandidateRefresh=Time.unscaledTime+2f;
            _pickupCandidates.Clear();
            foreach(var col in Physics.OverlapSphere(origin,PickupHoverReach,-1,QueryTriggerInteraction.Collide))
                if(col!=null && IsPickupTarget(col)&&_pickupCandidates.Count<128)_pickupCandidates.Add(col);
            _pickupCandidateRefresh=Time.unscaledTime+.2f;
        }
        float nearest=float.MaxValue;
        foreach(var col in _pickupCandidates) {
            if(col==null||!col.enabled||!col.gameObject.activeInHierarchy)continue;
            var bounds=col.bounds;
            Vector3 target=bounds.center;
            Vector3 delta=target-origin;
            if(new Vector2(delta.x,delta.z).sqrMagnitude>.45f*.45f || delta.y>.04f || delta.magnitude>PickupHoverReach)continue;
            var pick=MousePick.For(col);
            if(pick==null||!pick.CanClick)continue;
            hand.AimOrigin=origin;hand.AimDirection=delta.normalized;
            if(VisibleHoverItem(col,origin,out var candidate) && candidate.distance<nearest) {
                nearest=candidate.distance;hit=candidate;clickable=true;
            }
        }
        return clickable;
    }
    private bool VisibleHoverItem(Collider item,Vector3 origin,out RaycastHit hit)
    {
        hit=default;
        Vector3 delta=item.bounds.center-origin;
        float distance=delta.magnitude;
        if(distance<.001f||distance>PickupHoverReach)return false;
        Vector3 direction=delta/distance,start=origin;
        if(item.bounds.Contains(start))start=item.bounds.center-direction*(item.bounds.extents.magnitude+.02f);
        if(!item.Raycast(new Ray(start,direction),out hit,PickupHoverReach+.5f))return false;
        float reach=Vector3.Distance(origin,hit.point);
        if(reach>PickupHoverReach)return false;
        foreach(var obstacleHit in Physics.RaycastAll(origin,direction,reach,-1,QueryTriggerInteraction.Ignore)) {
            var obstacle=obstacleHit.collider;
            if(obstacle==null||obstacle==item||obstacle.name.StartsWith("BPVR_")||OpenTrunkShell(obstacle))continue;
            // A thin terrain surface may overlap loot at the bottom of an open
            // outdoor chest. Only forgive the last 12cm, never a wall/closed lid.
            float groundAllowance=IsKeyOrGemPickup(item)?.35f:.12f;
            if(obstacle.name.IndexOf("terrain",StringComparison.OrdinalIgnoreCase)>=0&&reach-obstacleHit.distance<=groundAllowance)continue;
            return false;
        }
        hit.distance=reach;return true;
    }
    private void ShowPalmPickupBeam(Hand hand,Vector3 target,bool show)
    {
        // Reuse the existing beam object/material; no per-frame objects or meshes.
        if(hand==null || hand.RayObj==null) return;
        // Visibility is independent of pickup detection. Keep the successful
        // downward interaction, but hide its debug beam for normal gameplay.
        show=show && _pShowBeam.Value;
        if(!show) { if(hand.RayObj.activeSelf) hand.RayObj.SetActive(false); return; }
        Vector3 start=PalmOrigin(hand), direction=PalmDirection(hand);
        float length=1.5f;
        // Stop at solid scenery. A changing target never rotates this beam.
        if(Physics.Raycast(start,direction,out var stop,length,-1,QueryTriggerInteraction.Ignore)) length=Mathf.Max(.02f,stop.distance);
        Vector3 delta=direction*length;
        if(delta.sqrMagnitude<.000001f) return;
        hand.RayObj.SetActive(true);
        var t=hand.RayObj.transform;
        if(t.parent!=null) { t.SetParent(null,true); UnityEngine.Object.DontDestroyOnLoad(hand.RayObj); }
        t.position=start+delta*.5f;
        t.rotation=Quaternion.FromToRotation(Vector3.up,delta.normalized);
        t.localScale=new Vector3(.025f,delta.magnitude*.5f,.025f);
    }
    private bool _pickupPreviewFailed;
    private readonly float[] _pickupScanAt=new float[2];
    private readonly bool[] _pickupPreview=new bool[2];
    private bool PickupReachHint(Hand hand)
    {
        if(hand==null || !hand.Valid || _computerKeyboardMode || NativePanelMenu || _pickupPreviewFailed) return false;
        int side=hand.IsLeft?0:1;
        if(Time.unscaledTime<_pickupScanAt[side]) return _pickupPreview[side];
        _pickupScanAt[side]=Time.unscaledTime+.05f;
        try { return _pickupPreview[side]=ScanPickupPreview(hand); }
        catch(Exception e) {
            _pickupPreviewFailed=true;
            _pickupPreview[0]=_pickupPreview[1]=false;
            LogWarn("[pickup] Optional anticipation disabled; normal interaction and VR continue: "+e.Message);
            return false;
        }
    }
    private bool ScanPickupPreview(Hand hand)
    {
        // Cast from just above the palm so touching a thin coin/key does not
        // start the ray inside/below its collider and drop the grab pose.
        // This changes visual anticipation only, not pickup reach or clicks.
        Vector3 origin=PalmOrigin(hand)+Vector3.up*.12f;
        Vector3 direction=PalmDirection(hand);
        // Use the same RaycastAll overload as the working interaction ray.
        // Five narrow rays give a small near-aim margin, without changing clicks.
        Vector3 right=Vector3.Cross(direction,Vector3.up);
        if(right.sqrMagnitude<.001f) right=Vector3.right;
        right.Normalize();
        Vector3 up=Vector3.Cross(right,direction).normalized;
        for(int sample=0;sample<5;sample++) {
            Vector3 offset=sample==1?right*.035f:sample==2?-right*.035f:sample==3?up*.035f:sample==4?-up*.035f:Vector3.zero;
            float nearest=2.4f;Collider target=null;
            foreach(var hit in Physics.RaycastAll(origin,(direction+offset).normalized,2.4f,-1,QueryTriggerInteraction.Collide)) {
                var col=hit.collider;
                if(col==null || col.name.StartsWith("BPVR_") || hit.distance>nearest) continue;
                var pick=MousePick.For(col);
                if(col.isTrigger && (pick==null || !pick.CanClick)) continue;
                nearest=hit.distance;target=col;
            }
            if(target==null || !IsPickupTarget(target)) continue;
            var eligible=MousePick.For(target);
            if(eligible!=null && eligible.CanClick) return true;
        }
        return false;
    }
}
