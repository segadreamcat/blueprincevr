using System;
using UnityEngine;
namespace BluePrinceVR;
public partial class VRPlugin
{
    private MelonLoader.MelonPreferences_Entry<bool> _pHaptics;
    private readonly int[] _hapticCandidate=new int[2],_hapticNoted=new int[2];
    private readonly float[] _hapticSince=new float[2],_hapticLastSeen=new float[2],_hapticNext=new float[2],_hapticSuccessNext=new float[2];
    private bool _hapticsFailed;
    private void InteractionPulse(Hand hand,bool accepted)
    {
        if(_hapticsFailed||_pHaptics==null||!_pHaptics.Value||hand==null||!hand.Valid||hand.DeviceIndex>=64||!Application.isFocused||_sys.TriggerHapticPulse==null)return;
        int side=hand.IsLeft?0:1;float now=Time.unscaledTime;
        if(accepted?now<_hapticSuccessNext[side]:now<_hapticNext[side])return;
        try {
            // OpenVR axis 0; short bounded pulses, no repeating vibration loop.
            _sys.TriggerHapticPulse(hand.DeviceIndex,0,(ushort)(accepted?2400:650));
            _hapticNext[side]=now+.3f;
            if(accepted)_hapticSuccessNext[side]=now+.08f;
        }catch(Exception e){_hapticsFailed=true;LogWarn("[haptics] Feedback disabled; controls unaffected: "+e.Message);}
    }
    private void TargetFeedback(Hand hand,PickEvents pick,bool ready)
    {
        int side=hand.IsLeft?0:1;float now=Time.unscaledTime;
        if(!ready||!PalmPickupAllowed||_journalOpen||_comfortOpen||pick?.Fsm==null) {
            _hapticCandidate[side]=0;
            if(now-_hapticLastSeen[side]>.2f)_hapticNoted[side]=0;
            return;
        }
        int id=pick.Fsm.GetInstanceID();_hapticLastSeen[side]=now;
        if(_hapticCandidate[side]!=id){_hapticCandidate[side]=id;_hapticSince[side]=now;}
        if(_hapticNoted[side]==id||now-_hapticSince[side]<.08f||now<_hapticNext[side])return;
        _hapticNoted[side]=id;InteractionPulse(hand,false);
    }
}
