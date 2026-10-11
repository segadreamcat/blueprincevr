using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace BluePrinceVR;

public partial class VRPlugin
{
    [DllImport("user32.dll")] private static extern void keybd_event(byte key,byte scan,uint flags,IntPtr extra);
    private readonly HashSet<byte> _computerKeysDown = new();
    private bool _computerKeyboardMode, _computerToggleHeld, _computerExitPending;
    private bool _computerSawUnlocked;
    private bool _computerSawCloseView;
    private float _computerRetreatSince=-1f;
    private bool _keyboardProbeHeld;
    private Transform _activeComputer;
    private Vector3 _computerBackDirection;
    private float _computerBackDistance;
    private float _computerViewingDistance=.85f;
    private Vector3 _computerScreenLocal;
    private string _computerProfileId="unknown";
    private Vector3 _computerTrackingOrigin;
    private float _computerTrackingYaw;
    private Vector3 TrackingOrigin => _computerKeyboardMode && _activeComputer!=null ? _computerTrackingOrigin : _origin;
    private void NoteComputerOpened(Collider target)
    {
        Transform computer = target == null ? null : target.transform;
        if(computer==null) return;
        Transform room=computer;
        while(room.parent!=null && !room.name.Contains("(Clone)")) room=room.parent;
        string roomKind=room.name.Split(new[]{"(Clone)"},StringSplitOptions.None)[0];
        while (computer != null && !computer.name.Equals("Computer", StringComparison.OrdinalIgnoreCase)) computer=computer.parent;
        // Confirmed by Security capture: the terminal trigger is directly under
        // _GAMEPLAY, unlike Laboratory/Office's Computer container.
        if(computer==null && roomKind.Equals("Security",StringComparison.OrdinalIgnoreCase) && target.name=="Click Trigger" && target.transform.parent!=null && target.transform.parent.name=="_GAMEPLAY")
            computer=target.transform;
        if (computer == null) return;
        if(_activeComputer==computer && _computerKeyboardMode) return;
        _activeComputer=computer;
        _computerProfileId=System.Text.RegularExpressions.Regex.Replace(roomKind+"-"+computer.name,@"[^a-zA-Z0-9_-]","_");
        _computerScreenLocal=computer.InverseTransformPoint(target.bounds.center);
        _computerKeyboardMode=true;
        _computerTrackingOrigin=_headPos;
        _computerTrackingYaw=_headRot.eulerAngles.y;
        _computerExitPending=false;
        _computerSawUnlocked=false;
        _computerSawCloseView=false;_computerRetreatSince=-1f;
        LoadTouchKeyboard();
        // Hand selection need not point along the desktop camera's forward axis.
        // Retreat toward the user's approach side, not wherever the mouse last looked.
        _computerBackDirection=_gameCamForCursor == null ? -computer.forward : _gameCamForCursor.transform.position-target.bounds.center;
        _computerBackDirection.y=0;
        if(_computerBackDirection.sqrMagnitude<.0001f)_computerBackDirection=-computer.forward;
        _computerBackDirection.y=0;
        _computerBackDirection.Normalize();
        Log("[computer] Opened: controller input active; left grip+A calibrates six fingertip menu keys.");
        // The functional Computer object contains only screen click blockers.
        // Read the sibling visual geometry, not just that functional subtree.
        Log("[computer] Profile: "+_computerProfileId);
        int count=0;
        foreach(var renderer in room.GetComponentsInChildren<Renderer>(true)) {
            if(renderer==null || Vector3.Distance(renderer.bounds.center,computer.position)>2f) continue;
            if(count++>=120) break;
            string path=renderer.name;
            for(var p=renderer.transform.parent;p!=null && p!=room;p=p.parent) path=p.name+"/"+path;
            Log("[keyboard geometry] "+path+" bounds="+renderer.bounds+" position="+renderer.transform.position+" rotation="+renderer.transform.eulerAngles);
        }
    }
    private Vector3 ComputerViewOffset(Vector3 position)
    {
        float wanted=0f;
        if(_computerKeyboardMode && _activeComputer!=null) {
            float current=Vector3.Dot(position-_activeComputer.TransformPoint(_computerScreenLocal),_computerBackDirection);
            wanted=Mathf.Clamp(_computerViewingDistance-current,0f,1f);
        }
        Vector3 direction=_computerBackDirection;
        // Back away only in the VR playspace, keeping hand and eye anchors together.
        if(wanted>0 && Physics.SphereCast(position,.12f,direction,out var hit,wanted,-1,QueryTriggerInteraction.Ignore))
            wanted=Mathf.Max(0f,hit.distance-.03f);
        _computerBackDistance=Mathf.MoveTowards(_computerBackDistance,wanted,Time.unscaledDeltaTime*.6f);
        return direction*_computerBackDistance;
    }
    private void ProbeKeyboardForCapture(Hand hand)
    {
        bool held=hand!=null && hand.GripDown && _leftGrip;
        if(held && !_keyboardProbeHeld && hand.AimCollider!=null) {
            var t=hand.AimCollider.transform;
            Log("[keyboard capture] target="+t.name+" bounds="+hand.AimCollider.bounds);
            for(int parent=0;t!=null && parent<4;parent++,t=t.parent) {
                Log("[keyboard capture] ancestor="+t.name+" position="+t.position+" rotation="+t.eulerAngles);
                for(int i=0;i<t.childCount && i<40;i++) {
                    var child=t.GetChild(i);
                    Log("[keyboard capture] child="+child.name+" local="+child.localPosition);
                }
            }
        }
        _keyboardProbeHeld=held;
    }
    private void UpdateComputerMode()
    {
        bool combo = _inHouse && _leftGrip && VRInput.Select;
        if(combo && !_computerToggleHeld) {
            if(_activeComputer==null && _handR!=null) NoteComputerOpened(_handR.AimCollider);
            if(_activeComputer!=null) { _computerKeyboardMode=true; BeginTouchCalibration(); }
            else _computerKeyboardMode=!_computerKeyboardMode;
            Log("[computer] Keyboard mode " + (_computerKeyboardMode ? "ON: A Enter, stick arrows, B Escape; left grip+A toggles." : "OFF"));
        }
        _computerToggleHeld=combo;
        TouchKeyboardInput(VRInput.Select,VRInput.Cancel);
        // Cursor locking / releasing Escape can precede the native camera retreat.
        // Do not remove the VR stand-off while the native eye is still inside the CRT.
        if(_computerKeyboardMode && Cursor.lockState==CursorLockMode.None)_computerSawUnlocked=true;
        bool retreated=false;
        if(_computerKeyboardMode && _activeComputer!=null && _gameCamForCursor!=null) {
            float distance=Vector3.Dot(_gameCamForCursor.transform.position-_activeComputer.TransformPoint(_computerScreenLocal),_computerBackDirection);
            if(distance<.40f)_computerSawCloseView=true;
            retreated=distance>=.55f && (_computerSawCloseView||_computerExitPending||_computerSawUnlocked) && Cursor.lockState==CursorLockMode.Locked;
        }
        if(retreated){if(_computerRetreatSince<0)_computerRetreatSince=Time.unscaledTime;}
        else _computerRetreatSince=-1f;
        if(_computerKeyboardMode && _computerRetreatSince>=0 && Time.unscaledTime-_computerRetreatSince>=.15f && !VRInput.Cancel && !TouchDown(0x1B)) {
            _computerKeyboardMode=false;_computerExitPending=false;_activeComputer=null;
            _computerSawUnlocked=false;_computerBackDistance=0;_touchKey=0;
            _panelAimValid=false;ReleaseComputerKeys();
            Log("[computer] Exit completed after native camera retreated from terminal.");
        }
        if(!_inHouse) { _computerKeyboardMode=false; _activeComputer=null; _computerBackDistance=0; }
        // Let Escape reach the game before dropping keyboard mode. The old path
        // cleared the terminal while the key pulse was still in flight, leaving
        // an inspection-camera zoom with a disappearing VR stand-off offset.
        // Keep terminal input and viewing distance while Escape only changes a submenu.
        if(_computerKeyboardMode && (VRInput.Cancel || TouchDown(0x1B))) _computerExitPending=true;
    }
    private void SetComputerKey(byte key,bool down)
    {
        if(down) {
            if(_computerKeysDown.Add(key)) {
                keybd_event(key,0,0,IntPtr.Zero);
                if(_computerKeyboardMode && _activeComputer!=null && !_calibrationConsumesA) PlayKeyboardClick();
            }
        } else if(_computerKeysDown.Remove(key)) keybd_event(key,0,2,IntPtr.Zero);
    }
    private void UpdateComputerKeys()
    {
        SetComputerKey(0x58,RightXShortcut());
        // Some item placements poll the keyboard directly, rather than Rewired.
        SetComputerKey(0x45,Application.isFocused&&_inHouse&&!_journalOpen&&!_comfortOpen&&!_computerKeyboardMode&&VRInput.UseItem);
        VRInput.NativeKeyboard=_computerKeyboardMode && Application.isFocused;
        // Rewired action aliases don't cover legacy keyboard polls.
        bool active=Application.isFocused && (_mainMenuScene || _draftMenu || _computerKeyboardMode ||
            (!_panelStick.Active && _uiQuad!=null && _uiQuad.activeInHierarchy && Cursor.lockState==CursorLockMode.None));
        SetComputerKey(0x0D,active && ((VRInput.Select && !_computerToggleHeld && !_calibrationConsumesA) || TouchDown(0x0D)));
        bool menuSteps = _draftMenu || NativePanelMenu;
        int draftPulse=menuSteps ? DraftDirectionPulse() : 0;
        float y = Math.Abs(_leftPanelStick.y) > .2f ? _leftPanelStick.y : _navY;
        SetComputerKey(0x26,active && ((!_draftMenu && y > .6f) || TouchDown(0x26)));
        SetComputerKey(0x28,active && ((!_draftMenu && y < -.6f) || TouchDown(0x28)));
        SetComputerKey(0x25,active && ((menuSteps ? draftPulse<0 : VRInput.NavLeft) || TouchDown(0x25)));
        SetComputerKey(0x27,active && ((menuSteps ? draftPulse>0 : VRInput.NavRight) || TouchDown(0x27)));
        SetComputerKey(0x1B,(Application.isFocused && Time.unscaledTime<_floorPlanPauseUntil) || (active && (VRInput.Cancel || TouchDown(0x1B))));
        if (_mainMenuScene || _draftMenu || _computerKeyboardMode || NativePanelMenu) {
            // Keyboard owns this menu: avoid duplicate Rewired moves/confirmation.
            VRInput.NavUp=VRInput.NavDown=VRInput.NavLeft=VRInput.NavRight=false;
            VRInput.NavUpEdge=VRInput.NavDownEdge=VRInput.NavLeftEdge=VRInput.NavRightEdge=false;
            VRInput.Select=VRInput.SelectEdge=VRInput.Interact=false;
            VRInput.InteractDownFrames=0;
            // An unlocked cursor / visible room-name overlay is not proof of a
            // modal menu. Preserve movement; the game owns actual movement locks.
            if (_mainMenuScene || _draftMenu || _computerKeyboardMode)
                VRInput.MoveX=VRInput.MoveY=VRInput.LookX=VRInput.LookY=0f;
        }
    }
    private void ReleaseComputerKeys()
    {
        foreach(byte key in new List<byte>(_computerKeysDown)) SetComputerKey(key,false);
    }
}
