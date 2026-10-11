using System;
using UnityEngine;

namespace BluePrinceVR;

// Numeric state is independently testable; screen coordinates use bottom-left origin.
internal sealed class PanelStickPointer
{
    internal bool Active;
    internal Vector2 Position;
    internal void Reset() { Active=false; }
    internal bool Tick(bool open, bool cancel, Vector2 stick, Vector2 initial, int width, int height, float dt)
    {
        if(!open || cancel || width<4 || height<4) { Reset(); return false; }
        float magnitude=MathF.Sqrt(stick.x*stick.x+stick.y*stick.y);
        if(!Active) { Active=true; Position=initial; }
        if(!Active) return false;
        if(magnitude>0.18f) {
            float speed=Math.Clamp((magnitude-0.18f)/0.82f,0,1);
            float distance=height*0.22f*speed*speed*Math.Clamp(dt,0,0.05f);
            Position=new Vector2(Position.x+stick.x/magnitude*distance,Position.y+stick.y/magnitude*distance);
        }
        Position=new Vector2(Math.Clamp(Position.x,1,width-2),Math.Clamp(Position.y,1,height-2));
        return true;
    }
}

#if !NUMERIC_TESTS
public partial class VRPlugin
{
    private readonly PanelStickPointer _panelStick = new();
    private Vector2 _leftPanelStick;
    private bool _panelConfirm;
    private bool _magnifierPointer;
    private bool NativePanelMenu => !_magnifierPointer && _uiQuad != null && _uiQuad.activeInHierarchy && Cursor.lockState == CursorLockMode.None;

    private void UpdatePanelStickPointer()
    {
        DetectDraftMenu();
        bool panelOpen=_uiQuad!=null && _uiQuad.activeInHierarchy && Cursor.lockState==CursorLockMode.None && Application.isFocused;
        if(!panelOpen || _mainMenuScene || _draftMenu || _computerKeyboardMode || VRInput.Cancel)
            _magnifierPointer=false;
        else if(VRInput.UseItemEdge) {
            // Explicit item-use gesture enables document pointer control; a
            // generic unlocked panel must not turn room cards into mouse UI.
            _magnifierPointer=!_magnifierPointer;
            Log("[magnifier] Document stick pointer "+(_magnifierPointer?"ON":"OFF"));
        }
        // The title menu uses native keyboard navigation, not room-card pointing.
        if (_mainMenuScene || _draftMenu || _computerKeyboardMode || NativePanelMenu) {
            _panelStick.Reset(); _panelConfirm=false; VRAim.Valid=false;
            return;
        }
        bool open = _uiQuad != null && _uiQuad.activeInHierarchy && Cursor.lockState == CursorLockMode.None && Application.isFocused;
        var initial=new Vector2(Screen.width*0.5f,Screen.height*0.5f);
        bool wasActive=_panelStick.Active;
        _panelConfirm = open && VRInput.Select;
        bool reset=VRInput.Cancel;
        Vector2 stick = _leftPanelStick;
        if(_panelAdjusting) stick=Vector2.zero;
        if(!_panelStick.Tick(open,reset,stick,initial,Screen.width,Screen.height,Time.unscaledDeltaTime)) return;
        if(!wasActive) Log("[Codex] Panel: thumbstick moves selection; A confirms. Hand pointing disabled.");
        // One panel input route: do not also send arrows/Enter/interact through
        // keyboard and Rewired while moving/clicking its native screen pointer.
        VRInput.Select=VRInput.SelectEdge=VRInput.Interact=false;
        VRInput.InteractDownFrames=0;
        VRInput.NavLeft=VRInput.NavRight=VRInput.NavUp=VRInput.NavDown=false;
        VRInput.NavLeftEdge=VRInput.NavRightEdge=VRInput.NavUpEdge=VRInput.NavDownEdge=false;
        VRInput.LookX=VRInput.LookY=0f;
        VRInput.MoveX=VRInput.MoveY=0f;
        // Leave walking permission to the game's actual menu/controller state.
        // A visible HUD/room-name panel alone does not mean the player is frozen.
        _clickFreeze=false; _haveFrozenAim=false;
        PublishPanelStickPointer();
    }

    private void PublishPanelStickPointer()
    {
        if(!_panelStick.Active || !Application.isFocused) return;
        VRAim.ScreenPoint=_panelStick.Position; VRAim.Valid=true;
        _cursorSx=_panelStick.Position.x; _cursorSy=_panelStick.Position.y; _cursorHave=true;
        if(_pMoveOsPointer.Value) MovePointerTo(Mathf.RoundToInt(_cursorSx),Mathf.RoundToInt(Screen.height-_cursorSy));
    }

    private void ClickFromPanelStick(Hand hand)
    {
        _clickFreeze=false; _haveFrozenAim=false;
        PublishPanelStickPointer();
        bool pressed=Application.isFocused && !_panelAdjusting && _panelConfirm;
        if(pressed==_mouseHeld) return;
        mouse_event(pressed ? 2u : 4u,0,0,0u,IntPtr.Zero);
        _mouseHeld=pressed;
    }
}
#endif
