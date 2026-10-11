using System;
using System.IO;
using System.Text.Json;
using UnityEngine;
using Il2CppTMPro;
namespace BluePrinceVR;
public partial class VRPlugin
{
    private sealed class ComfortProfile { public bool LeftHanded{get;set;} public bool Snap{get;set;} public float SnapAngle{get;set;}=30f; public float Span{get;set;}=1.8f; public bool Forearms{get;set;}=true; }
    private ComfortProfile _comfort=new();
    private bool _comfortLoaded,_comfortOpen,_comfortChord,_comfortA,_comfortCalibrate,_snapLatched;
    private int _comfortRow,_comfortNav;
    private float _comfortHold,_calibrateHold,_comfortYaw;
    private TextMeshPro _comfortText;
    private string _comfortStatus="";
    private string ComfortPath=>Path.Combine(Application.dataPath,"..","UserData","Codex-comfort.json");
    private uint ControllerIndex(ETrackedControllerRole role)
    {
        if(_comfort.LeftHanded)role=role==ETrackedControllerRole.LeftHand?ETrackedControllerRole.RightHand:role==ETrackedControllerRole.RightHand?ETrackedControllerRole.LeftHand:role;
        return _sys.GetTrackedDeviceIndexForControllerRole(role);
    }
    private void SaveComfort()
    {
        try {File.WriteAllText(ComfortPath+".tmp",JsonSerializer.Serialize(_comfort));File.Move(ComfortPath+".tmp",ComfortPath,true);}
        catch(Exception e){_comfortStatus="Settings not saved: "+e.Message;}
    }
    private bool ComfortInput()
    {
        if(!_comfortLoaded) {
            _comfortLoaded=true;
            try {if(File.Exists(ComfortPath)){var p=JsonSerializer.Deserialize<ComfortProfile>(File.ReadAllText(ComfortPath));if(p!=null&&float.IsFinite(p.Span)&&p.Span>=.9f&&p.Span<=2.4f)_comfort=p;}}
            catch(Exception e){LogWarn("[comfort] Defaults used: "+e.Message);}
        }
        uint l=ControllerIndex(ETrackedControllerRole.LeftHand),r=ControllerIndex(ETrackedControllerRole.RightHand);
        VRControllerState_t ls=default,rs=default;uint size=(uint)System.Runtime.InteropServices.Marshal.SizeOf<VRControllerState_t>();
        bool valid=l<64&&r<64&&_sys.GetControllerState(l,ref ls,size)&&_sys.GetControllerState(r,ref rs,size);
        bool chord=valid&&(ls.ulButtonPressed&0x100000000UL)!=0&&(rs.ulButtonPressed&0x100000000UL)!=0;
        if(chord&&!_journalOpen) {
            _comfortHold+=Time.unscaledDeltaTime;
            if(_comfortHold>.8f&&!_comfortChord){_comfortOpen=!_comfortOpen;_comfortChord=true;_comfortCalibrate=false;_comfortStatus="";ClearClickState();ReleaseComputerKeys();}
        }else {_comfortHold=0;_comfortChord=false;}
        if(!Application.isFocused){_comfortOpen=false;_comfortCalibrate=false;}
        if(!_comfortOpen)return chord;
        bool a=valid&&(rs.ulButtonPressed&0x80UL)!=0;
        bool b=valid&&(rs.ulButtonPressed&2UL)!=0;
        int nav=valid&&Math.Abs(ls.rAxis0.y)>.65f?Math.Sign(ls.rAxis0.y):0;
        if(!_comfortCalibrate&&nav!=0&&nav!=_comfortNav)_comfortRow=(_comfortRow-nav+9)%9;
        _comfortNav=nav;
        if(a&&!_comfortA&&!_comfortCalibrate) {
            if(_comfortRow==0){_comfort.Snap=!_comfort.Snap;SaveComfort();}
            if(_comfortRow==1){
                if(_celHands!=null){_comfort.LeftHanded=!_comfort.LeftHanded;SaveComfort();_comfortOpen=false;_journalBlockUntilRelease=true;}
                else _comfortStatus="New hand pack must load before reversing hands.";
            }
            if(_comfortRow==2){_comfortCalibrate=true;_calibrateHold=0;}
            if(_comfortRow==3){
                if(_pNativeVrs==null)InitNativeFoveation();
                // Off -> Normal -> Extra strong -> Off. Preserve the old bool preference.
                if(!_pNativeVrs.Value){_pNativeVrs.Value=true;_pExtraStrongVrs.Value=false;}
                else if(!_pExtraStrongVrs.Value)_pExtraStrongVrs.Value=true;
                else {_pNativeVrs.Value=false;_pExtraStrongVrs.Value=false;}
                _pFoveated.Value=false;
                MelonLoader.MelonPreferences.Save();
                _comfortStatus=_pNativeVrs.Value?(_pExtraStrongVrs.Value?"Extra strong: softer outer view.\nGPU benefit varies by scene.":"NVIDIA D3D11 required.\nFixed shading, not eye tracking."):"Full-rate shading.";
            }
            if(_comfortRow==4){_origin=_headPos;_comfortStatus="View re-centered.";}
            if(_comfortRow==5){_pHaptics.Value=!_pHaptics.Value;MelonLoader.MelonPreferences.Save();}
            if(_comfortRow==6){_comfort.SnapAngle=_comfort.SnapAngle<40?45:_comfort.SnapAngle<55?60:30;SaveComfort();}
            if(_comfortRow==7)_comfortStatus="Reprojection is controlled outside the mod.\nSteamVR motion smoothing / Virtual Desktop SSW.\nThis menu does not change those settings.";
            if(_comfortRow==8){_comfortOpen=false;_journalBlockUntilRelease=true;}
        }
        _comfortA=a;
        if(b){_comfortOpen=false;_comfortCalibrate=false;_journalBlockUntilRelease=true;}
        if(_comfortCalibrate&&valid&&_renderPoses[l].bPoseIsValid&&_renderPoses[r].bPoseIsValid) {
            var lp=_renderPoses[l].mDeviceToAbsoluteTracking;var rp=_renderPoses[r].mDeviceToAbsoluteTracking;
            var left=new Vector3(lp.m3,lp.m7,-lp.m11);var right=new Vector3(rp.m3,rp.m7,-rp.m11);
            float span=Vector3.Distance(left,right);
            bool pose=span>=.9f&&span<=2.4f&&Math.Abs(left.y-right.y)<.15f&&Math.Abs((left.y+right.y)*.5f-(_headPos.y-.20f))<.25f;
            if(pose&&(ls.ulButtonPressed&4UL)!=0&&(rs.ulButtonPressed&4UL)!=0)_calibrateHold+=Mathf.Min(Time.unscaledDeltaTime,.05f);else _calibrateHold=0;
            if(_calibrateHold>=2f){_comfort.Span=span;SaveComfort();_comfortCalibrate=false;_comfortStatus=$"Wrist fit saved: {span:F2} m controller span. Tracking remains 1:1.";}
        }
        BlockJournalInput();return true;
    }
    private void ComfortFrame(Vector3 eye,Quaternion rotation)
    {
        if(!_comfortOpen){if(_comfortText!=null)_comfortText.gameObject.SetActive(false);return;}
        if(_comfortText==null) {
            var go=new GameObject("BPVR_ComfortMenu");UnityEngine.Object.DontDestroyOnLoad(go);
            _comfortText=go.AddComponent(Il2CppInterop.Runtime.Il2CppType.Of<TextMeshPro>()).Cast<TextMeshPro>();
            _comfortText.fontSize=1.1f;_comfortText.enableAutoSizing=false;_comfortText.enableWordWrapping=false;
            _comfortText.transform.localScale=Vector3.one*.65f;
            _comfortText.alignment=TextAlignmentOptions.Center;_comfortText.rectTransform.sizeDelta=new Vector2(1.15f,.8f);_comfortText.color=new Color(.88f,.94f,1);
        }
        _comfortText.gameObject.SetActive(true);_comfortText.transform.SetPositionAndRotation(eye+rotation*new Vector3(0,-.08f,1.4f),rotation);
        string foveation=_pNativeVrs!=null&&_pNativeVrs.Value?((_pExtraStrongVrs.Value?"Extra strong":"Normal")+(_lastVrsStatus==1?" (active)":_lastVrsStatus<0&&_lastVrsStatus!=int.MinValue?" (unavailable)":" (requested)")):"Off";
        string[] rows={"Turning: "+(_comfort.Snap?"Snap":"Smooth"),"Dominant hand: "+(_comfort.LeftHanded?"Left":"Right"),"T-pose wrist calibration","Foveation: "+foveation,"Re-center view","Haptics: "+(_pHaptics.Value?"On":"Off"),"Snap angle: "+_comfort.SnapAngle+" degrees","Reprojection: runtime-controlled (info)","Close"};
        _comfortText.text=_comfortCalibrate?"Extend both arms sideways at shoulder height.\nHold BOTH GRIPS for 2 seconds.\nB: cancel":
            "VR SETTINGS\n\n"+string.Join("\n",System.Linq.Enumerable.Select(rows,(s,i)=>(_comfortRow==i?"> ":"  ")+s))+"\n\nOff-hand stick: select | "+(_comfort.LeftHanded?"X: choose | Y: close":"A: choose | B: close")+"\n"+_comfortStatus;
    }
}
