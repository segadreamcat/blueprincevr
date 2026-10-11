using System;
using System.IO;
using System.Runtime.InteropServices;
using Il2Cpp;
using Il2CppHutongGames.PlayMaker.Actions;
using Il2CppInterop.Runtime;
using UnityEngine;
using UnityEngine.EventSystems;
namespace BluePrinceVR;

public partial class VRPlugin
{
    [DllImport("user32.dll")] private static extern short GetAsyncKeyState(int key);
    private bool _devChecked,_devInstalled,_devIWasDown,_devFWasDown;
    private GameObject _devShortcutRoot;
    private readonly PlayMakerFSM[] _devShortcutFsms=new PlayMakerFSM[2];
    private readonly string[] _devPendingState=new string[2];
    private readonly string[] _devLastState=new string[2];
    private readonly float[] _devPendingAt=new float[2];
    private float _devNextScan;
    private int _devShortcutLogs;

    private void UpdateDevShortcuts()
    {
        if(!_devChecked) {
            _devChecked=true;
            _devInstalled=File.Exists(Path.Combine(Application.dataPath,"..","Mods","KeepDevObjects.dll"));
        }
        if(!_devInstalled)return;
        if(!Application.isFocused||UnityEngine.SceneManagement.SceneManager.GetActiveScene().name!="Mount Holly Estate") {
            _devIWasDown=_devFWasDown=false;
            _devPendingState[0]=_devPendingState[1]=null;
            return;
        }
        bool i=(GetAsyncKeyState(0x49)&0x8000)!=0;
        bool f=(GetAsyncKeyState(0x46)&0x8000)!=0;
        bool iEdge=i&&!_devIWasDown, fEdge=f&&!_devFWasDown;
        _devIWasDown=i;_devFWasDown=f;
        if(_devShortcutRoot==null && Time.unscaledTime>=_devNextScan) {
            _devNextScan=Time.unscaledTime+2f;
            foreach(var obj in Resources.FindObjectsOfTypeAll(Il2CppType.Of<GameObject>())) {
                var go=obj.TryCast<GameObject>();
                if(go!=null&&go.name=="Cheaters Failz (Back Doors)"&&go.scene.IsValid()) {
                    _devShortcutRoot=go;
                    foreach(var component in go.GetComponentsInChildren<PlayMakerFSM>(true)) {
                        if(component==null)continue;
                        int slot=component.gameObject.name=="I"?0:component.gameObject.name=="F"?1:-1;
                        if(slot>=0)_devShortcutFsms[slot]=component;
                    }
                    Log("[dev shortcuts] Native listeners: I="+(_devShortcutFsms[0]!=null)+" F="+(_devShortcutFsms[1]!=null));
                    break;
                }
            }
        }
        var events=EventSystem.current;
        var selected=events==null?null:events.currentSelectedGameObject;
        if(selected!=null&&!selected.activeInHierarchy) {
            events.SetSelectedGameObject(null);
            selected=null;
        }
        bool typing=selected!=null &&
            (selected.GetComponent<UnityEngine.UI.InputField>()!=null || selected.GetComponent<Il2CppTMPro.TMP_InputField>()!=null);
        if(typing||_computerKeyboardMode||_journalOpen||_comfortOpen) {
            _devPendingState[0]=_devPendingState[1]=null;
            return;
        }
        for(int slot=0;slot<2;slot++) {
            var listener=_devShortcutFsms[slot];
            if(listener==null)continue;
            string name=slot==0?"I":"F";
            string currentState=listener.Fsm.ActiveStateName;
            string previousState=_devLastState[slot];
            _devLastState[slot]=currentState;
            if(slot==0?iEdge:fEdge) {
                // Restore only these two native menu listeners. Their graphs
                // retain the game's open, close, freeze and unfreeze operations.
                if(!listener.gameObject.activeInHierarchy) {
                    for(var t=listener.transform;t!=null&&t!=_devShortcutRoot.transform;t=t.parent)
                        if(!t.gameObject.activeSelf)t.gameObject.SetActive(true);
                }
                if(!listener.enabled)listener.enabled=true;
                // Native Update may have handled this same edge before Melon's
                // callback. Never interpret its new open state as a failed press.
                if(previousState==currentState) {
                    _devPendingState[slot]=listener.Fsm.ActiveStateName;
                    _devPendingAt[slot]=Time.unscaledTime;
                } else _devPendingState[slot]=null;
                if(_devShortcutLogs++<20)Log("[dev shortcuts] "+name+" pressed; native state="+_devPendingState[slot]+" enabled="+listener.isActiveAndEnabled);
            }
            var pending=_devPendingState[slot];
            if(pending==null)continue;
            if(listener.Fsm.ActiveStateName!=pending) {_devPendingState[slot]=null;continue;}
            if(Time.unscaledTime-_devPendingAt[slot]<.12f)continue;
            _devPendingState[slot]=null;
            if(!listener.isActiveAndEnabled)continue;
            // Give native polling first refusal. Retry only an unchanged idle
            // state with a matching key and a real transition for its event.
            var state=listener.Fsm.ActiveState;
            if(state?.Actions==null||state.Transitions==null)continue;
            foreach(var action in state.Actions) {
                var key=action?.TryCast<GetKeyDown>();
                if(key==null||key.key!=(slot==0?KeyCode.I:KeyCode.F)||key.sendEvent==null)continue;
                bool accepts=false;
                foreach(var transition in state.Transitions)
                    if(transition.EventName==key.sendEvent.Name)accepts=true;
                if(!accepts)continue;
                listener.SendEvent(key.sendEvent.Name);
                Log("[dev shortcuts] Retried native "+name+" event; "+pending+" -> "+listener.Fsm.ActiveStateName);
                break;
            }
        }
    }
}
