using System;
using System.Text;
using UnityEngine;
using Il2Cpp;
using Il2CppTMPro;
namespace BluePrinceVR;
// Only runs on an explicitly requested bug screenshot, never on the frame path.
internal static class InteractionDiagnostics
{
    internal static string Capture(Collider target)
    {
        var report=new StringBuilder("\nINTERACTION DETAILS\n");
        try {
            Transform root=null;
            for(var t=target==null?null:target.transform;t!=null;t=t.parent) {
                report.AppendLine("ancestor: "+t.name);
                if(t.name.IndexOf("Pump",StringComparison.OrdinalIgnoreCase)>=0)root=t;
                if(t.name.StartsWith("DIRT PILE",StringComparison.OrdinalIgnoreCase))root=t;
                if(t.name.StartsWith("TRUNK(Clone)",StringComparison.OrdinalIgnoreCase))root=t;
                if(t.name.StartsWith("Pipe Switcher ",StringComparison.Ordinal))root=t;
                if(t.name=="Room Spawn Pools")break;
            }
            if(root!=null) {
                int count=0;
                foreach(var fsm in root.GetComponentsInChildren<PlayMakerFSM>(true)) {
                    if(fsm==null||count++>=120)break;
                    report.AppendLine($"FSM {fsm.gameObject.name} / {fsm.FsmName} enabled={fsm.isActiveAndEnabled} state={fsm.Fsm.ActiveStateName}");
                    foreach(var variable in fsm.FsmVariables.BoolVariables)
                        if(variable!=null)report.AppendLine("  bool "+variable.Name+"="+variable.Value);
                    foreach(var transition in fsm.Fsm.GlobalTransitions)
                        if(transition!=null)report.AppendLine("  global "+transition.EventName+" -> "+transition.ToState);
                    int states=0;
                    foreach(var state in fsm.Fsm.States) {
                        if(state==null||states++>=40)break;
                        report.Append("  "+state.Name+": ");
                        foreach(var action in state.Actions)if(action!=null)report.Append(action.GetIl2CppType().Name+" ");
                        foreach(var transition in state.Transitions)if(transition!=null)report.Append(" ["+transition.EventName+" -> "+transition.ToState+"]");
                        report.AppendLine();
                    }
                }
            }
            int labels=0;
            foreach(var label in UnityEngine.Object.FindObjectsOfType<TMP_Text>()) {
                if(label==null||!label.isActiveAndEnabled)continue;
                string text=label.text??"";
                if(text.Length>120)continue;
                if(labels++>=120)break;
                report.Append("UI label: "+text.Replace('\n',' ')+" | ");
                for(var t=label.transform;t!=null;t=t.parent)report.Append(t.name+"/");
                report.AppendLine();
            }
        }catch(Exception e){report.AppendLine("diagnostic unavailable: "+e.Message);}
        return report.ToString();
    }
}
