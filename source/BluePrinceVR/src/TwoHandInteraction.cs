using UnityEngine;
namespace BluePrinceVR;
public partial class VRPlugin
{
    private Hand _interactionHand;
    private int _directFrame=-1,_directTarget;
    private bool OtherGripHeld(Hand hand)
    {
        uint index=ControllerIndex(hand.IsLeft?ETrackedControllerRole.RightHand:ETrackedControllerRole.LeftHand);
        VRControllerState_t state=default;
        return index<64 && _sys.GetControllerState(index,ref state,(uint)System.Runtime.InteropServices.Marshal.SizeOf<VRControllerState_t>()) && (state.ulButtonPressed&4UL)!=0;
    }
    private bool DirectHandClick(Hand hand,bool parlor)
    {
        if(hand.AimCollider==null)return false;
        int id=hand.AimCollider.GetInstanceID();
        if(_directFrame==Time.frameCount&&_directTarget==id)return true;
        WatchDoorOpening(hand.AimCollider);
        bool used=IsTrunk(hand.AimCollider)?ClickTrunk(hand):IsBoilerPipe(hand.AimCollider)?ClickBoilerPipe(hand):IsDigTarget(hand.AimCollider)?ClickDigTarget(hand):parlor?ParlorLegacyGrab.Click(hand.AimCollider):Grab.Click(hand.AimCollider);
        if(used){_directFrame=Time.frameCount;_directTarget=id;InteractionPulse(hand,true);}
        return used;
    }
    private void UpdateInteractionHand()
    {
        if(RightXShortcut()){ClearClickState();return;}
        if(_heldPump!=null){
            if(_pumpHand==null||!_pumpHand.Valid||_comfortOpen||_journalOpen||!Application.isFocused)ReleasePump();
            else {ClearClickState();return;}
        }
        // There is one OS cursor: retain its owner for a complete press/release.
        // Direct world clicks and palm pickup remain independently available to both hands.
        if(!_mouseHeld) {
            if(_handL!=null&&_handL.Valid&&_handL.TriggerDown)_interactionHand=_handL;
            else if(_handR!=null&&_handR.Valid&&_handR.TriggerDown)_interactionHand=_handR;
            else if(_interactionHand==null||!_interactionHand.Valid)_interactionHand=_handR!=null&&_handR.Valid?_handR:_handL;
        }
        var hand=_interactionHand;
        if(_comfortOpen||_comfortChord||_journalOpen||_journalBlockUntilRelease){ClearClickState();return;}
        if(hand==null||!hand.Valid){ClearClickState();return;}
        bool hit=hand.AimCollider!=null;
        if(hit&&hand.TriggerDown&&!hand.TriggerWasDown&&PalmPickupAllowed)WatchDoorOpening(hand.AimCollider);
        DriveCursorFromHand(_gameCamForCursor,hit,hand.AimPoint,hand);
        bool show=_pShowCursor.Value&&_inHouse&&hit&&!_panelStick.Active&&!_forcePanel&&Cursor.lockState!=CursorLockMode.None;
        if(show)PlaceCursorAt(hand.AimPoint+hand.AimNormal*.006f,hand.AimDistance,hand.AimClickable||NativeParlorTarget(hand));
        else if(_cursorMarker!=null)_cursorMarker.SetActive(false);
        ClickFromTrigger(hand);
    }
}
