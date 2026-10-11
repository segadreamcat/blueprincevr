using System;
using UnityEngine;
using Il2Cpp;
using Il2CppHutongGames.PlayMaker;
using Il2CppTMPro;
namespace BluePrinceVR;
public partial class VRPlugin
{
    private GameObject _heldShovel,_heldParlorKey;
    private float _toolOwnershipAt;
    private bool _ownsShovel,_ownsParlorKey,_digPrompt,_toolInventoryLogged;
    private bool _keyPrompt,_heldToolVisible;
    private float _toolObjectScanAt;
    private readonly System.Collections.Generic.List<PlayMakerFSM> _toolInventorySources=new();
    private static string ToolName(string n)=>System.Text.RegularExpressions.Regex.Replace(n??"",@"[^a-zA-Z]","").ToLowerInvariant();
    private void ReadToolOwnership()
    {
        if(Time.unscaledTime<_toolOwnershipAt)return;
        _toolOwnershipAt=Time.unscaledTime+1f;_ownsShovel=_ownsParlorKey=_digPrompt=_keyPrompt=false;
        void Read(FsmVariables variables) {
            if(variables==null)return;
            foreach(var v in variables.BoolVariables) {
                if(v==null)continue;string n=ToolName(v.Name);
                if(n=="shovel"||n=="hasshovel"||n=="ownsshovel")_ownsShovel|=v.Value;
                if(n=="parlorkey"||n=="hasparlorkey"||n=="puzzlekey"||n=="haspuzzlekey"||n=="twistkey")_ownsParlorKey|=v.Value;
                if(!_toolInventoryLogged&&(n.Contains("shovel")||n.Contains("key")))Log("[tools] ownership candidate "+v.Name+"="+v.Value);
            }
        }
        Read(FsmVariables.GlobalVariables);
        if(Time.unscaledTime>=_toolObjectScanAt) {
            _toolObjectScanAt=Time.unscaledTime+10;_toolInventorySources.Clear();
            foreach(var fsm in UnityEngine.Object.FindObjectsOfType<PlayMakerFSM>()) {
                if(fsm==null)continue;string n=ToolName(fsm.gameObject.name);
                if(n=="standarditems"||n=="inventory"||n=="globalmanager"||n=="globalpersitentmanager")_toolInventorySources.Add(fsm);
            }
        }
        foreach(var fsm in _toolInventorySources)if(fsm!=null)Read(fsm.FsmVariables);
        foreach(var text in UnityEngine.Object.FindObjectsOfType<TMP_Text>()) {
            if(text==null||!text.isActiveAndEnabled)continue;string n=ToolName(text.text);
            if(n=="dig"||n=="dighere"||n=="useshovel")_digPrompt=true;
            if(n=="insertkey"||n=="useparlorkey"||n=="usetwistkey"||n=="turnkey")_keyPrompt=true;
        }
        _toolInventoryLogged=true;
    }
    private void HideHeldTools()
    {
        _heldToolVisible=false;
        if(_heldShovel!=null)_heldShovel.SetActive(false);
        if(_heldParlorKey!=null)_heldParlorKey.SetActive(false);
    }
    private bool UpdateHeldTool(Hand h,bool clickable)
    {
        if(h==null||h.IsLeft)return false;
        HideHeldTools();
        if(!h.Valid||!PalmPickupAllowed||_journalOpen||_visionCamera!=null)return false;
        try {
            ReadToolOwnership();bool parlor=false,dig=IsDigTarget(h.AimCollider);
            for(var t=h.AimCollider==null?null:h.AimCollider.transform;t!=null;t=t.parent) {
                if(t.name.StartsWith("Parlor",StringComparison.OrdinalIgnoreCase))parlor=true;
                if(System.Text.RegularExpressions.Regex.IsMatch(t.name,@"\b(dig|digging)\b",System.Text.RegularExpressions.RegexOptions.IgnoreCase))dig=true;
                if(t.name=="Room Spawn Pools")break;
            }
            // An affirmative native action prompt is also evidence of readiness;
            // do not require guessed inventory aliases when the game offers use.
            bool key=parlor&&clickable&&h.AimCollider.name.Equals("Keyhole",StringComparison.OrdinalIgnoreCase)&&(_ownsParlorKey||_keyPrompt);
            bool shovel=clickable&&dig&&(_ownsShovel||_digPrompt);
            if(!key&&!shovel)return false;
            var wrist=HandRig.Model?.HandR;if(wrist==null)return false;
            var tool=key?_heldParlorKey:_heldShovel;
            if(tool==null) {
                tool=new GameObject(key?"BPVR_HeldParlorKey":"BPVR_HeldShovel");UnityEngine.Object.DontDestroyOnLoad(tool);
                var root=tool.transform;
                if(key) {
                    var gold=new Color(.65f,.49f,.20f);
                    ToolPart(root,"Key shaft",PrimitiveType.Cylinder,new Vector3(0,-.045f,0),new Vector3(.009f,.06f,.009f),gold);
                    ToolPart(root,"Key bit",PrimitiveType.Cube,new Vector3(.012f,-.09f,0),new Vector3(.028f,.020f,.008f),gold);
                    for(int i=0;i<12;i++) {float a=i*Mathf.PI/6;ToolPart(root,"Key bow",PrimitiveType.Sphere,new Vector3(Mathf.Cos(a)*.027f,.04f+Mathf.Sin(a)*.027f,0),Vector3.one*.012f,gold);}
                    _heldParlorKey=tool;
                } else {
                    ToolPart(root,"Wood handle",PrimitiveType.Cylinder,new Vector3(0,-.19f,0),new Vector3(.027f,.29f,.027f),new Color(.36f,.22f,.11f));
                    ToolPart(root,"Steel blade",PrimitiveType.Cube,new Vector3(0,-.52f,.025f),new Vector3(.16f,.20f,.022f),new Color(.25f,.29f,.32f));
                    ToolPart(root,"Grip",PrimitiveType.Cube,new Vector3(0,.12f,0),new Vector3(.13f,.025f,.035f),new Color(.22f,.17f,.12f));_heldShovel=tool;
                }
            }
            tool.transform.SetPositionAndRotation(wrist.TransformPoint(new Vector3(0,.055f,.02f)),wrist.rotation);
            tool.SetActive(true);_heldToolVisible=true;return true;
        }catch(Exception e){_toolOwnershipAt=Time.unscaledTime+5;LogWarn("[tools] Optional cue unavailable: "+e.Message);return false;}
    }
}
