using UnityEngine;
namespace BluePrinceVR;
public partial class VRPlugin
{
    private bool RightXShortcut()
    {
        if(!Application.isFocused||!_inHouse||_journalOpen||_journalBlockUntilRelease||_comfortOpen||_computerKeyboardMode||_quickMemo)return false;
        uint index=ControllerIndex(ETrackedControllerRole.RightHand);
        VRControllerState_t state=default;
        return index<64&&_sys.GetControllerState(index,ref state,(uint)System.Runtime.InteropServices.Marshal.SizeOf<VRControllerState_t>())&&
            (state.ulButtonPressed&4UL)!=0&&((state.ulButtonPressed&0x200000000UL)!=0||state.rAxis1.x>.6f);
    }
}
