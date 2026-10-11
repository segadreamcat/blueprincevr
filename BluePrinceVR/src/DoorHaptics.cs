using System;
using System.Collections.Generic;
using UnityEngine;
namespace BluePrinceVR;
public partial class VRPlugin
{
    private readonly List<Transform> _doorHapticParts=new();
    private readonly List<Vector3> _doorHapticPositions=new();
    private readonly List<Quaternion> _doorHapticRotations=new();
    private Transform _doorHapticRoot;
    private float _doorWatchUntil,_doorRumbleStart=-1,_doorPulseAt,_doorRumbleCooldown;
    private void WatchDoorOpening(Collider collider)
    {
        if(collider==null||_pHaptics==null||!_pHaptics.Value)return;
        Transform root=null;
        for(var t=collider.transform;t!=null;t=t.parent) {
            if(t.name.Equals("Door Code",StringComparison.OrdinalIgnoreCase)){root=t;break;}
            if(t.name=="_DOORS"||t.name=="Room Spawn Pools")break;
        }
        if(root==null||root==_doorHapticRoot)return;
        _doorHapticRoot=root;_doorWatchUntil=Time.unscaledTime+90;
        _doorHapticParts.Clear();_doorHapticPositions.Clear();_doorHapticRotations.Clear();
        // Watch actual door leaves, not handles, click volumes or room movement.
        foreach(var r in root.GetComponentsInChildren<Renderer>(false)) {
            if(_doorHapticParts.Count>=16)break;
            string n=r.name;
            if(n.IndexOf("door",StringComparison.OrdinalIgnoreCase)<0||n.IndexOf("frame",StringComparison.OrdinalIgnoreCase)>=0)continue;
            _doorHapticParts.Add(r.transform);
            _doorHapticPositions.Add(root.InverseTransformPoint(r.transform.position));
            _doorHapticRotations.Add(Quaternion.Inverse(root.rotation)*r.transform.rotation);
        }
    }
    private void TickDoorHaptics()
    {
        float now=Time.unscaledTime;
        if(!Application.isFocused||!_inHouse||_comfortOpen||_journalOpen||_pHaptics==null||!_pHaptics.Value) {
            _doorHapticRoot=null;_doorRumbleStart=-1;return;
        }
        if(_doorHapticRoot!=null) {
            bool moved=false;
            for(int i=0;i<_doorHapticParts.Count;i++) {
                var part=_doorHapticParts[i];if(part==null||!part.gameObject.activeInHierarchy)continue;
                moved|=Vector3.Distance(_doorHapticRoot.InverseTransformPoint(part.position),_doorHapticPositions[i])>.025f ||
                    Quaternion.Angle(Quaternion.Inverse(_doorHapticRoot.rotation)*part.rotation,_doorHapticRotations[i])>3f;
            }
            if(moved&&now>=_doorRumbleCooldown) {
                _doorRumbleStart=now;_doorPulseAt=now;_doorRumbleCooldown=now+1f;
                _doorHapticRoot=null;
            } else if(now>_doorWatchUntil)_doorHapticRoot=null;
        }
        if(_doorRumbleStart<0)return;
        float elapsed=now-_doorRumbleStart;
        if(elapsed>.12f){_doorRumbleStart=-1;return;}
        if(now<_doorPulseAt)return;
        _doorPulseAt=now+.012f;
        // Short, gentle latch/movement tail rather than a series of heavy knocks.
        PatternPulse(_handR,(ushort)Mathf.Lerp(1500f,250f,elapsed/.12f));
    }
    private void PatternPulse(Hand hand,ushort duration)
    {
        if(_hapticsFailed||_pHaptics==null||!_pHaptics.Value||hand==null||!hand.Valid||hand.DeviceIndex>=64||!Application.isFocused||_sys.TriggerHapticPulse==null)return;
        try{_sys.TriggerHapticPulse(hand.DeviceIndex,0,(ushort)Math.Min(3000,(int)duration));}
        catch(Exception e){_hapticsFailed=true;LogWarn("[haptics] Pattern disabled: "+e.Message);}
    }
}
