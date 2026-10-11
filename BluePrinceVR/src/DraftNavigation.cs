using System;
using System.Collections.Generic;
using UnityEngine;
using Il2CppTMPro;

namespace BluePrinceVR;

public partial class VRPlugin
{
    private bool _draftMenu;
    private float _draftScanAt, _draftPulseUntil;
    private int _draftDirection, _draftPulse;
    private readonly List<TMP_Text> _draftLabels=new();
    private float _draftLabelsRefresh;
    private void DetectDraftMenu()
    {
        if (!_inHouse || _uiQuad == null || !_uiQuad.activeInHierarchy || Cursor.lockState != CursorLockMode.None) {
            _draftMenu=false; _draftDirection=_draftPulse=0; return;
        }
        if (Time.unscaledTime < _draftScanAt) return;
        _draftScanAt=Time.unscaledTime+.25f;
        bool found=false;
        // Reuse label references instead of allocating a scene-wide search four times/sec.
        if(Time.unscaledTime>=_draftLabelsRefresh) {
            _draftLabelsRefresh=Time.unscaledTime+2f;
            _draftLabels.Clear();
            foreach(var label in UnityEngine.Object.FindObjectsOfType<TMP_Text>()) _draftLabels.Add(label);
        }
        foreach(var text in _draftLabels) {
            if(text==null || !text.isActiveAndEnabled) continue;
            string t=(text.text??"").Replace(" ","").Replace("\n","").ToUpperInvariant();
            if(t.Contains("SELECTAROOM") || t.Contains("CHOOSEROOM") || t.Contains("CHOOSEAROOM")) { found=true; break; }
        }
        if(found && !_draftMenu) Log("[draft] Native joystick choices: left/right moves one room, A confirms; no mouse pointer.");
        _draftMenu=found;
    }
    private int DraftDirectionPulse()
    {
        float x=Math.Abs(_leftPanelStick.x)>.2f ? _leftPanelStick.x : _navX;
        int direction=Math.Abs(x)>.55f ? Math.Sign(x) : 0;
        float now=Time.unscaledTime;
        if(direction==0) { _draftDirection=0; }
        else if(direction!=_draftDirection) {
            _draftDirection=direction; _draftPulse=direction; _draftPulseUntil=now+.07f;
            // NativePanelMenu is also used by room cards whose heading differs.
            // Tick the same edge that dispatches the navigation pulse, not held frames.
            if((_draftMenu || NativePanelMenu) && !_computerKeyboardMode)
                PatternPulse(Math.Abs(_leftPanelStick.x)>.2f?_handL:_handR,1800);
        }
        return now<_draftPulseUntil ? _draftPulse : 0;
    }
}
