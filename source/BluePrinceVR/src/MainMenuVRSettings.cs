using System;
using UnityEngine;
using UnityEngine.UI;
using Il2CppTMPro;
namespace BluePrinceVR;
public partial class VRPlugin
{
    private GameObject _vrSettingsEntry;
    private float _vrSettingsScan;
    private bool _vrSettingsX;
    private void OpenVRSettings()
    {
        _comfortOpen=true;_comfortCalibrate=false;_comfortRow=0;_comfortStatus="";
        ClearClickState();ReleaseComputerKeys();
    }
    private void MainMenuVRSettings()
    {
        if(!_mainMenuScene){if(_vrSettingsEntry!=null)_vrSettingsEntry.SetActive(false);_vrSettingsX=false;return;}
        var state=default(VRControllerState_t);
        uint left=_sys.GetTrackedDeviceIndexForControllerRole(ETrackedControllerRole.LeftHand);
        bool x=Application.isFocused&&left<64&&_sys.GetControllerState(left,ref state,(uint)System.Runtime.InteropServices.Marshal.SizeOf<VRControllerState_t>())&&(state.ulButtonPressed&0x80UL)!=0;
        if(x&&!_vrSettingsX&&!_comfortOpen)OpenVRSettings();
        _vrSettingsX=x;
        if(_vrSettingsEntry!=null){_vrSettingsEntry.SetActive(!_comfortOpen);return;}
        if(Time.unscaledTime<_vrSettingsScan)return;_vrSettingsScan=Time.unscaledTime+1f;
        foreach(var label in UnityEngine.Object.FindObjectsOfType<TextMeshProUGUI>()) {
            if(label==null||!label.isActiveAndEnabled)continue;
            string caption=System.Text.RegularExpressions.Regex.Replace(label.text??"", "<[^>]+>", "").Trim().ToUpperInvariant();
            if(caption!="OPTIONS"&&caption!="SETTINGS")continue;
            var canvas=label.GetComponentInParent<Canvas>();if(canvas==null)continue;
            var go=new GameObject("BPVR_MainMenuVRSettings");_vrSettingsEntry=go;go.layer=label.gameObject.layer;
            var rect=go.AddComponent<RectTransform>();rect.SetParent(canvas.transform,false);
            rect.anchorMin=rect.anchorMax=new Vector2(.5f,0);rect.pivot=new Vector2(.5f,0);
            rect.anchoredPosition=new Vector2(0,45);rect.sizeDelta=new Vector2(420,60);
            var background=go.AddComponent<Image>();background.color=new Color(.02f,.04f,.06f,.9f);
            var button=go.AddComponent<Button>();button.targetGraphic=background;
            button.onClick.AddListener((UnityEngine.Events.UnityAction)(Action)OpenVRSettings);
            var child=new GameObject("Label");child.layer=go.layer;child.transform.SetParent(rect,false);
            var text=child.AddComponent(Il2CppInterop.Runtime.Il2CppType.Of<TextMeshProUGUI>()).Cast<TextMeshProUGUI>();
            text.font=label.font;text.fontSize=Mathf.Clamp(label.fontSize,22,36);text.color=label.color;
            text.alignment=TextAlignmentOptions.Center;text.text="VR SETTINGS  [X]";text.raycastTarget=false;
            text.rectTransform.anchorMin=Vector2.zero;text.rectTransform.anchorMax=Vector2.one;
            text.rectTransform.offsetMin=text.rectTransform.offsetMax=Vector2.zero;
            Log("[comfort] Main-menu VR Settings entry added; X shortcut active.");break;
        }
    }
}
