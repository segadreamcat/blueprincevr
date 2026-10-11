using System;
using System.IO;
using System.Text.Json;
using UnityEngine;
namespace BluePrinceVR;
public partial class VRPlugin
{
    private static readonly byte[] TouchKeys={0x0D,0x1B,0x26,0x28,0x25,0x27};
    private static readonly string[] TouchNames={"ENTER","ESC","UP","DOWN","LEFT","RIGHT"};
    private readonly Vector3[] _keyPoints=new Vector3[6];
    private readonly float[] _keyDistances=new float[6];
    private readonly KeyTouchGate[] _keyGates={new(),new(),new(),new(),new(),new()};
    private int _keyCalibration=-1;
    private bool _keysCalibrated,_calibrateAWasDown;
    private bool _calibrationConsumesA;
    private Il2CppTMPro.TextMeshPro _typingHint;
    private GameObject _typingHintRoot;
    private bool _typingHintFailed;
    private byte _touchKey;
    private float _touchKeyUntil;
    private string KeyProfile => Path.Combine(Application.dataPath,"..","UserData","Codex-keyboard-"+_computerProfileId+".json");
    private void LoadTouchKeyboard()
    {
        _keysCalibrated=false;_keyCalibration=-1;_touchKey=0;
        try {
            // The user confirmed the old profile on the Laboratory only.
            // Preserve it there; never silently reuse it on Security/Office.
            string legacy=Path.Combine(Application.dataPath,"..","UserData","Codex-keyboard-points.json");
            if(!File.Exists(KeyProfile) && _computerProfileId.StartsWith("Laboratory-",StringComparison.OrdinalIgnoreCase) && File.Exists(legacy))
                File.Copy(legacy,KeyProfile,false);
            if(File.Exists(KeyProfile)) {
                float[] data=JsonSerializer.Deserialize<float[]>(File.ReadAllText(KeyProfile));
                if(data!=null && data.Length==18) {
                    for(int i=0;i<6;i++) {
                        var p=new Vector3(data[i*3],data[i*3+1],data[i*3+2]);
                        if(!float.IsFinite(p.x+p.y+p.z) || p.magnitude>2f) throw new Exception("Invalid keyboard calibration");
                        _keyPoints[i]=p;_keyGates[i].Reset();
                    }
                    _keysCalibrated=true;
                }
            }
        } catch(Exception e) { LogWarn("[keyboard] Calibration load: "+e.Message); }
    }
    private void BeginTouchCalibration()
    {
        _keyCalibration=0;_keysCalibrated=false;_calibrateAWasDown=true;_touchKey=0;
        Log("[keyboard] Mark ENTER, ESC, UP, DOWN, LEFT, RIGHT with right fingertip; tap A for each. B cancels.");
    }
    private Vector3 IndexTip(Hand hand)
    {
        if(_celHands!=null && hand!=null && hand.Valid)return _celHands.IndexTip(hand.IsLeft ^ _comfort.LeftHanded);
        var model=HandRig.Model;
        if(model==null || hand==null || !hand.Valid) return new Vector3(10000,10000,10000);
        string side=hand.IsLeft ? (ArmsAsset.SwapSides?"R":"L") : (ArmsAsset.SwapSides?"L":"R");
        var bone=model.Find("f_index.03."+side);
        return bone==null ? new Vector3(10000,10000,10000) : bone.position+bone.up*.018f;
    }
    private void TouchKeyboardInput(bool a,bool cancel)
    {
        _calibrationConsumesA=_keyCalibration>=0;
        if(_activeComputer==null || !_computerKeyboardMode || !Application.isFocused) {
            _touchKey=0;_keyCalibration=-1;
            for(int i=0;i<6;i++) _keyGates[i].Reset();
            return;
        }
        if(cancel) {
            // Cancel must discard partially marked points and restore the saved
            // working profile, not leave typing disabled until another calibration.
            if(_keyCalibration>=0) LoadTouchKeyboard();
            _keyCalibration=-1;_touchKey=0;return;
        }
        if(_keyCalibration>=0) {
            if(a && !_calibrateAWasDown && _handR!=null && _handR.Valid) {
                var local=_activeComputer.InverseTransformPoint(IndexTip(_handR));
                bool valid=local.magnitude<1.5f;
                for(int i=0;i<_keyCalibration;i++) if(Vector3.Distance(local,_keyPoints[i])<.025f) valid=false;
                if(valid) {
                    _keyPoints[_keyCalibration++]=local;
                    if(_keyCalibration==6) {
                        _keyCalibration=-1;_keysCalibrated=true;
                        float[] data=new float[18];
                        for(int i=0;i<6;i++) {data[i*3]=_keyPoints[i].x;data[i*3+1]=_keyPoints[i].y;data[i*3+2]=_keyPoints[i].z;_keyGates[i].Reset();}
                        try {File.WriteAllText(KeyProfile,JsonSerializer.Serialize(data));} catch(Exception e){LogWarn(e.Message);}
                        Log("[keyboard] Six menu keys calibrated. Lift fingertip before tapping; no trigger required.");
                    }
                } else Log("[keyboard] Key too far away or overlaps another key; reposition fingertip and tap A again.");
            }
            _calibrateAWasDown=a;return;
        }
        if(!_keysCalibrated) return;
        var left=IndexTip(_handL);var right=IndexTip(_handR);
        float[] distances=_keyDistances;int nearest=0;
        for(int i=0;i<6;i++) {
            var p=_activeComputer.TransformPoint(_keyPoints[i]);
            distances[i]=Mathf.Min(Vector3.Distance(p,left),Vector3.Distance(p,right));
            if(distances[i]<distances[nearest]) nearest=i;
        }
        for(int i=0;i<6;i++) {
            if(_keyGates[i].Tick(distances[i],i==nearest && Time.unscaledTime>=_touchKeyUntil+.06f)) {
                _touchKey=TouchKeys[i];_touchKeyUntil=Time.unscaledTime+.09f;
                var keyPosition=_activeComputer.TransformPoint(_keyPoints[i]);
                InteractionPulse(Vector3.Distance(keyPosition,left)<Vector3.Distance(keyPosition,right)?_handL:_handR,true);
                Log("[keyboard] Fingertip "+TouchNames[i]);
            }
        }
    }
    private bool TouchDown(byte key) => _touchKey==key && Time.unscaledTime<_touchKeyUntil;
    private void ShowTypingHint(Vector3 eye,Quaternion rotation)
    {
        if(_typingHintFailed) return;
        try { ShowTypingHintCore(eye,rotation); }
        catch(Exception e) {
            _typingHintFailed=true;
            if(_typingHintRoot!=null) UnityEngine.Object.Destroy(_typingHintRoot);
            _typingHintRoot=null;_typingHint=null;
            LogWarn("[keyboard] Instruction label unavailable; VR rendering continues. See monitor for calibration prompts. "+e);
        }
    }
    private string TypingInstruction() => _keyCalibration>=0 ? "Place RIGHT index tip on "+TouchNames[_keyCalibration]+"\nTap A to mark it (B cancels)" :
        !_keysCalibrated ? "Fingertip keyboard: LEFT grip + A to calibrate" :
        TouchDown(_touchKey) ? "Pressed "+TouchNames[Array.IndexOf(TouchKeys,_touchKey)] : "Touch Enter / Esc / arrows - Lift finger between taps";
    private void ShowTypingHintCore(Vector3 eye,Quaternion rotation)
    {
        bool show=_computerKeyboardMode && _activeComputer!=null && (!_keysCalibrated || _keyCalibration>=0);
        if(!show) {if(_typingHint!=null)_typingHint.gameObject.SetActive(false);return;}
        if(_typingHint==null) {
            _typingHintRoot=new GameObject("BPVR_KeyboardHint");UnityEngine.Object.DontDestroyOnLoad(_typingHintRoot);
            // Legacy TextMesh's generated generic AddComponent binding is absent
            // in this IL2CPP build. Use the game's TMP type and native type overload.
            _typingHint=_typingHintRoot.AddComponent(Il2CppInterop.Runtime.Il2CppType.Of<Il2CppTMPro.TextMeshPro>()).Cast<Il2CppTMPro.TextMeshPro>();
            _typingHint.fontSize=1.5f;
            _typingHint.alignment=Il2CppTMPro.TextAlignmentOptions.Center;
            _typingHint.rectTransform.sizeDelta=new Vector2(5f,1f);
            _typingHint.transform.localScale=Vector3.one*.12f;
            _typingHint.color=Color.white;
        }
        _typingHint.gameObject.SetActive(true);
        _typingHint.transform.SetPositionAndRotation(eye+rotation*new Vector3(0,-.18f,.65f),rotation);
        _typingHint.text=TypingInstruction();
    }
}
