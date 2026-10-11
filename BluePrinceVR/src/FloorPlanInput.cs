using UnityEngine;
namespace BluePrinceVR;
public partial class VRPlugin
{
    private readonly TapHoldButton _floorPlanButton=new();
    private float _floorPlanTabUntil;
    private float _floorPlanPauseUntil;
    private void PollFloorPlanButton()
    {
        // Physical Y stays Y even when dominant-hand controls are reversed.
        uint index=_sys.GetTrackedDeviceIndexForControllerRole(ETrackedControllerRole.LeftHand);
        VRControllerState_t state=default;
        bool valid=index<64 && _sys.GetControllerState(index,ref state,(uint)System.Runtime.InteropServices.Marshal.SizeOf<VRControllerState_t>());
        bool down=valid && (state.ulButtonPressed&2UL)!=0;
        bool blocked=!valid||!Application.isFocused||!_inHouse||_mainMenuScene||_journalOpen||_journalBlockUntilRelease||_comfortOpen||_comfortChord||_draftMenu;
        int action=_floorPlanButton.Update(down,blocked,Time.unscaledTime);
        if(blocked){_floorPlanTabUntil=0;_floorPlanPauseUntil=0;}
        if(_computerKeyboardMode)_floorPlanTabUntil=0;
        if(action==1 && !_computerKeyboardMode)_floorPlanTabUntil=Time.unscaledTime+.08f;
        if(action==2)_floorPlanPauseUntil=Time.unscaledTime+.08f;
        SetComputerKey(0x09,!blocked && Time.unscaledTime<_floorPlanTabUntil);
        // Escape has one owner: UpdateComputerKeys. Releasing it here each
        // frame interrupted terminal Escape pulses and retriggered them.
        VRInput.Menu=VRInput.MenuEdge=false;
        if(!blocked && _comfort.LeftHanded && down)VRInput.Cancel=VRInput.CancelEdge=false;
        if(!blocked && Time.unscaledTime<_floorPlanPauseUntil){VRInput.Cancel=true;VRInput.CancelEdge=action==2;}
    }
}
