using UnityEngine;
namespace BluePrinceVR;
public partial class VRPlugin
{
    // A lingering rendered HUD is not a modal interaction. Never retain stale
    // stick/button values on failure: retry the live sample, then fail neutral.
    private bool TurnInputBlocked => _mainMenuScene||_journalOpen||_comfortOpen||
        _computerKeyboardMode||_draftMenu||_panelStick.Active||_forcePanel||_roomPanelCamera!=null||
        Cursor.lockState==CursorLockMode.None;
    private int _controllerReadFailures;
    private bool ReadControllerState(uint index,ref VRControllerState_t state,uint size)
    {
        if(_sys.GetControllerState(index,ref state,size))return true;
        state=default;
        if(_sys.GetControllerState(index,ref state,size))return true;
        state=default;
        if(_controllerReadFailures++<8)LogWarn("[input] Controller sample unavailable: "+index+"; released input, retrying next frame.");
        return false;
    }
}
