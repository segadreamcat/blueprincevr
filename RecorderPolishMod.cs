using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BluePrinceVR;
using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using MelonLoader;
using UnityEngine;
using UnityEngine.SceneManagement;

[assembly: MelonInfo(typeof(BPVRRecorderPolish.RecorderPolishMod), "BluePrinceVR Recorder Polish", "1.2.0", "Codex")]
[assembly: MelonGame(null,null)]

namespace BPVRRecorderPolish;

public sealed class RecorderPolishMod : MelonMod
{
    private static readonly Type PackType=typeof(VRPlugin).Assembly.GetType("BluePrinceVR.CelHandPack");
    private static readonly Dictionary<object,IndexRig> Rigs=new();
    private static readonly FieldInfo Recorder=AccessTools.Field(typeof(VRPlugin),"_memoRecorder");
    private static readonly FieldInfo Process=AccessTools.Field(typeof(VRPlugin),"_memoProcess");
    private static readonly PropertyInfo Recording=AccessTools.Property(typeof(VRPlugin),"MemoRecording");
    private static readonly FieldInfo Button=AccessTools.Field(typeof(VRPlugin),"_memoRecordButton");
    private static readonly FieldInfo Mount=AccessTools.Field(typeof(VRPlugin),"_memoRecordMount");
    private static readonly FieldInfo Comfort=AccessTools.Field(typeof(VRPlugin),"_comfort");
    private static readonly PropertyInfo LeftHanded=AccessTools.Property(Comfort.FieldType,"LeftHanded");
    private static readonly FieldInfo ButtonPress=AccessTools.Field(typeof(VRPlugin),"_memoButtonPress");
    private static readonly FieldInfo Pack=AccessTools.Field(typeof(VRPlugin),"_celHands");
    private static readonly FieldInfo QuickMemo=AccessTools.Field(typeof(VRPlugin),"_quickMemo");
    private static readonly FieldInfo MemoShell=AccessTools.Field(typeof(VRPlugin),"_memoShell");
    private static readonly FieldInfo OuterBases=AccessTools.Field(PackType,"_recorderOuterBases");
    private static readonly FieldInfo ThumbJoints=AccessTools.Field(PackType,"_thumbJoints");
    private static readonly FieldInfo ThumbTips=AccessTools.Field(PackType,"_thumbTips");
    private static readonly FieldInfo HandNodes=AccessTools.Field(PackType,"_nodes");
    private static readonly FieldInfo HandPoses=AccessTools.Field(PackType,"_poses");
    private static VRPlugin plugin;
    private static AudioSource clickSource,tapeSource;
    private static AudioClip clickClip,tapeClip;
    private static bool warning;
    private static bool recordHeld;
    private static float visiblePress;
    private static MelonPreferences_Entry<float> ClickVolume,TapeVolume;
    private bool diagnostic,finished;
    private float started;

    private sealed class IndexRig
    {
        internal readonly Transform[,] Joints=new Transform[2,3];
        internal readonly Vector3[] Tips=new Vector3[2];
        internal readonly Quaternion[,,] Poses=new Quaternion[2,2,3];
        internal readonly bool[] Ready=new bool[2];
        private readonly Quaternion[,] Point=new Quaternion[2,3],Fist=new Quaternion[2,3];
        internal IndexRig(object pack) {
            var nodes=(Transform[])AccessTools.Field(PackType,"_nodes").GetValue(pack);
            var names=(string[])AccessTools.Field(PackType,"_names").GetValue(pack);
            var poses=(Dictionary<string,Quaternion[]>)AccessTools.Field(PackType,"_poses").GetValue(pack);
            for(int side=0;side<2;side++)for(int j=0;j<3;j++) {
                int index=Array.IndexOf(names,"index_0"+(j+1)+(side==0?"_l":"_r"));
                Joints[side,j]=nodes[index];Point[side,j]=poses["PosePoint"][index];Fist[side,j]=poses["PoseFist"][index];
            }
            var renderer=(SkinnedMeshRenderer)AccessTools.Field(PackType,"Renderer").GetValue(pack);
            var bones=renderer.bones;var mesh=renderer.sharedMesh;
            var vertices=mesh.vertices;var weights=mesh.boneWeights;var bind=mesh.bindposes;
            for(int side=0;side<2;side++) {
                int bone=-1;
                for(int b=0;b<bones.Length;b++)if(bones[b]==Joints[side,2])bone=b;
                if(bone<0)throw new Exception("Index fingertip bone missing from mesh");
                var candidates=new List<Vector3>();float far=0;
                for(int v=0;v<vertices.Length;v++) {
                    var w=weights[v];float influence=(w.boneIndex0==bone?w.weight0:0)+(w.boneIndex1==bone?w.weight1:0)+(w.boneIndex2==bone?w.weight2:0)+(w.boneIndex3==bone?w.weight3:0);
                    if(influence<0.65f)continue;
                    var local=bind[bone].MultiplyPoint3x4(vertices[v]);candidates.Add(local);far=Mathf.Max(far,local.magnitude);
                }
                var tip=candidates.Where(v=>v.magnitude>=far*0.9f).ToArray();
                if(tip.Length==0)throw new Exception("Index tip vertices missing");
                foreach(var vertex in tip)Tips[side]+=vertex;Tips[side]/=tip.Length;
            }
        }
        internal Vector3 Tip(int side)=>Joints[side,2].TransformPoint(Tips[side]);
        internal void Apply(Transform recorder,bool left,float press) {
            int side=left?0:1;
            if(!Ready[side]) {
                // Solve the two endpoints once. MCP aims, PIP/DIP only curl
                // between authored pointing/fist poses, never bend backwards.
                for(int endpoint=0;endpoint<2;endpoint++) {
                    for(int j=0;j<3;j++)Joints[side,j].localRotation=Point[side,j];
                    Vector3 target=recorder.TransformPoint(ButtonContact(left,endpoint));
                    MelonLogger.Msg("[recorder-rig] "+(left?"left":"right")+" MCP(local recorder)="+recorder.InverseTransformPoint(Joints[side,0].position)+" straight tip="+recorder.InverseTransformPoint(Tip(side))+" target="+ButtonContact(left,endpoint));
                    float radius=Vector3.Distance(Joints[side,0].position,target),bestError=float.MaxValue,bestCurl=0;
                    for(int sample=0;sample<=60;sample++) {
                        float curl=sample/60f;
                        for(int j=1;j<3;j++)Joints[side,j].localRotation=Quaternion.Slerp(Point[side,j],Fist[side,j],curl);
                        float error=Mathf.Abs(Vector3.Distance(Joints[side,0].position,Tip(side))-radius);
                        if(error<bestError){bestError=error;bestCurl=curl;}
                    }
                    for(int j=1;j<3;j++)Joints[side,j].localRotation=Quaternion.Slerp(Point[side,j],Fist[side,j],bestCurl);
                    MelonLogger.Msg("[recorder-rig] radius="+radius+" nearest error="+bestError+" curl="+bestCurl);
                    var root=Joints[side,0];var swing=Quaternion.FromToRotation(Tip(side)-root.position,target-root.position);
                    root.rotation=swing*root.rotation;
                    for(int j=0;j<3;j++)Poses[side,endpoint,j]=Joints[side,j].localRotation;
                }
                Ready[side]=true;
            }
            for(int j=0;j<3;j++)Joints[side,j].localRotation=Quaternion.Slerp(Poses[side,0,j],Poses[side,1,j],press);
        }
    }

    private static Vector3 ButtonCenter(bool left,float press)=>new Vector3((left?1f:-1f)*(0.058f-0.006f*Mathf.Clamp01(press)),0.030f,0.003f);
    private static Vector3 ButtonContact(bool left,float press)=>ButtonCenter(left,press)+Vector3.right*(left?0.004f:-0.004f);

    public override void OnInitializeMelon()
    {
        diagnostic=Environment.GetCommandLineArgs().Contains("--bpvr-recorder-polish-test");started=Time.realtimeSinceStartup;
        var cfg=MelonPreferences.CreateCategory("BluePrinceVR_RecorderPolish");
        ClickVolume=cfg.CreateEntry("ClickVolume",0.55f,description:"Mechanical record-button click volume, 0 to 1.");
        TapeVolume=cfg.CreateEntry("TapeVolume",0.16f,description:"Soft tape motor loop volume, 0 to 1. Keep low to avoid microphone bleed.");
        cfg.SetFilePath("UserData/BluePrinceVR-RecorderPolish.cfg");
        // Migrate only our old defaults; preserve custom volumes and mute.
        if(Mathf.Abs(ClickVolume.Value-0.32f)<0.0001f)ClickVolume.Value=0.55f;
        if(Mathf.Abs(TapeVolume.Value-0.045f)<0.0001f)TapeVolume.Value=0.16f;
        cfg.SaveToFile(false);
        var thumb=PackType==null?null:AccessTools.Method(PackType,"RestThumbOnRecorder");
        if(thumb!=null)HarmonyInstance.Patch(thumb,prefix:new HarmonyMethod(typeof(RecorderPolishMod),nameof(IndexButtonPose)));
        else MelonLogger.Warning("[recorder-polish] Thumb hook missing; keeping original pose.");
        HarmonyInstance.Patch(AccessTools.Method(typeof(VRPlugin),"MemoRecorderFrame"),postfix:new HarmonyMethod(typeof(RecorderPolishMod),nameof(RecorderFrame)));
        HarmonyInstance.Patch(AccessTools.Method(typeof(VRPlugin),"StartMemo"),postfix:new HarmonyMethod(typeof(RecorderPolishMod),nameof(RecordClick)));
        HarmonyInstance.Patch(AccessTools.Method(typeof(VRPlugin),"StopMemo"),postfix:new HarmonyMethod(typeof(RecorderPolishMod),nameof(StopTape)));
        HarmonyInstance.Patch(AccessTools.Method(typeof(VRPlugin),"CancelMemo"),postfix:new HarmonyMethod(typeof(RecorderPolishMod),nameof(StopTape)));
        if(diagnostic)HarmonyInstance.Patch(AccessTools.Method(typeof(VRPlugin),"OnUpdate"),postfix:new HarmonyMethod(typeof(RecorderPolishMod),nameof(TestPlugin)));
        MelonLogger.Msg("[recorder-polish] Upper opposite-side index-finger button, 6 mm press, unattenuated mechanical click/tape enabled. Recorder grip unchanged.");
    }

    private static void TestPlugin(VRPlugin __instance)=>plugin=__instance;
    private static bool IndexButtonPose(object __instance,Transform recorder,bool left,float press)
    {
        if(recorder==null)return true;
        try {
            GetRig(__instance).Apply(recorder,left,Mathf.Clamp01(press));return false;
        } catch(Exception ex) {Warn("Index refinement skipped: "+ex.Message);return true;}
    }

    private static IndexRig GetRig(object pack)
    {
        if(!Rigs.TryGetValue(pack,out var rig)){rig=new IndexRig(pack);Rigs.Add(pack,rig);}return rig;
    }
    public override void OnSceneWasLoaded(int buildIndex,string sceneName) {Rigs.Clear();StopTape();visiblePress=0;}

    private static void Warn(string message) {if(!warning){warning=true;MelonLogger.Warning("[recorder-polish] "+message);}}
    private static void RecorderFrame(VRPlugin __instance)
    {
        plugin=__instance;
        try {
            var recorder=Recorder.GetValue(plugin) as GameObject;
            if(recorder==null||!recorder.activeInHierarchy) {StopTape();return;}
            bool left=(bool)LeftHanded.GetValue(Comfort.GetValue(plugin));
            bool running=(bool)Recording.GetValue(plugin);
            visiblePress=Mathf.MoveTowards(visiblePress,recordHeld||running?1f:0f,Time.unscaledDeltaTime*10f);
            StyleButton(plugin,left,visiblePress);
            var pack=Pack.GetValue(plugin);
            if(pack!=null&&(bool)QuickMemo.GetValue(plugin)) {
                // Put the casing just in front of the palm so the skinned
                // hand cannot visually pass through its front face.
                // Nest the casing into the palm instead of leaving it floating
                // in front of the hand. PlaceRecorder runs before this postfix
                // every frame, so this is a stable placement offset rather
                // than an accumulating drift.
                recorder.transform.position+=recorder.transform.TransformDirection(Vector3.back)*0.010f;
                GetRig(pack).Apply(recorder.transform,left,visiblePress);
                PoseThumbOnButton(pack,recorder.transform,left,visiblePress);
                RaiseGripFingers(pack,left);
            }
            ResizeRecorderShell(plugin);
            EnsureAudio(recorder);
            tapeSource.volume=Mathf.Clamp01(TapeVolume.Value);
            if(running&&!tapeSource.isPlaying){tapeSource.Play();MelonLogger.Msg("[recorder-polish] Tape motor ON volume="+tapeSource.volume+" playing="+tapeSource.isPlaying);}
            else if(!running&&tapeSource.isPlaying)StopMotor();
        } catch(Exception ex){StopTape();Warn("Audio skipped: "+ex.Message);}
    }

    private static void RecordClick(VRPlugin __instance)
    {
        plugin=__instance;
        try {
            recordHeld=Process.GetValue(plugin)!=null;
            var recorder=Recorder.GetValue(plugin) as GameObject;
            if(recorder==null||!recorder.activeInHierarchy)return;
            EnsureAudio(recorder);clickSource.PlayOneShot(clickClip,Mathf.Clamp01(ClickVolume.Value));
            MelonLogger.Msg("[recorder-polish] Record latch click volume="+ClickVolume.Value+" playing="+clickSource.isPlaying+" listenerPause="+AudioListener.pause+" listenerVolume="+AudioListener.volume);
        } catch(Exception ex){Warn("Click skipped: "+ex.Message);}
    }
    private static void StopTape() {recordHeld=false;StopMotor();}
    private static void StopMotor() {if(tapeSource!=null&&tapeSource.isPlaying){tapeSource.Stop();MelonLogger.Msg("[recorder-polish] Tape motor OFF");}}
    private static void StyleButton(VRPlugin instance,bool left,float press) {
        var button=(Transform)Button.GetValue(instance);var mount=(Transform)Mount.GetValue(instance);
        if(button==null||mount==null)return;
        // Keep the switch on the upper side wall, like a real pocket
        // recorder.  It is deliberately below the casing top so it reads as
        // connected to the side instead of a red block sticking upward.
        mount.localPosition=new Vector3(left?0.053f:-0.053f,0.030f,0.003f);
        mount.localRotation=Quaternion.identity;
        var housing=mount.Find("Recessed record mount");
        if(housing!=null)housing.localScale=new Vector3(0.006f,0.014f,0.018f);
        button.name="Index record button";button.localScale=new Vector3(0.008f,0.010f,0.014f);
        button.localPosition=ButtonCenter(left,press)-mount.localPosition;
        button.GetComponent<Renderer>().material.color=Color.Lerp(new Color(0.65f,0.04f,0.035f),new Color(0.95f,0.07f,0.03f),press);
        ButtonPress.SetValue(instance,press);
    }
    private static void ResizeRecorderShell(VRPlugin instance) {
        var shell=MemoShell?.GetValue(instance) as Transform;
        if(shell==null)return;
        // The authored shell is tall and thin. Keep its width/button side,
        // but make the casing and its tape window a more ordinary handheld
        // recorder proportion without changing recorder placement.
        shell.localScale=new Vector3(1.4705882f,0.72f,1f);
    }
    private static void RaiseGripFingers(object pack,bool left) {
        try {
            var bases=(Transform[,])OuterBases.GetValue(pack);int side=left?0:1;
            // Native recorder pose rotates the support row downward. A modest
            // lift keeps the fingers wrapped beside the casing and closer to
            // the index without allowing a distal joint to cross the face.
            Vector3 axis=bases[side,0].rotation*Vector3.right;
            float[] lift={24.0f,22.0f,20.0f};
            for(int i=1;i<4;i++)bases[side,i].rotation=Quaternion.AngleAxis(lift[i-1],axis)*bases[side,i].rotation;
        } catch(Exception ex){Warn("Grip-finger spacing skipped: "+ex.Message);}
    }
    private static void PoseThumbOnButton(object pack,Transform recorder,bool left,float press) {
        try {
            var joints=(Transform[,])ThumbJoints.GetValue(pack);var tips=(Vector3[])ThumbTips.GetValue(pack);
            var nodes=(Transform[])HandNodes.GetValue(pack);var poses=(Dictionary<string,Quaternion[]>)HandPoses.GetValue(pack);
            var hold=poses["PoseHoldFlashlight"];var fist=poses["PoseFist"];int side=left?0:1;
            for(int j=0;j<3;j++) {
                int index=Array.IndexOf(nodes,joints[side,j]);
                if(index>=0)joints[side,j].localRotation=Quaternion.Slerp(hold[index],fist[index],.58f);
            }
            var target=recorder.TransformPoint(ButtonCenter(left,press));var end=joints[side,2];
            var baseJoint=joints[side,0];var from=end.TransformPoint(tips[side])-baseJoint.position;var to=target-baseJoint.position;
            if(from.sqrMagnitude>.000001f&&to.sqrMagnitude>.000001f) {
                var delta=Quaternion.FromToRotation(from,to);
                // The button is on the far upper edge in the holding hand;
                // allow the thumb base enough swing to reach it instead of
                // stopping on the recorder's top corner.
                baseJoint.rotation=Quaternion.RotateTowards(Quaternion.identity,delta,140f)*baseJoint.rotation;
            }
        } catch(Exception ex){Warn("Thumb grip spacing skipped: "+ex.Message);}
    }
    private static void EnsureAudio(GameObject owner)
    {
        if(clickClip==null) {
            clickClip=MakeClip("BPVR cassette record latch",ClickSamples());
            tapeClip=MakeClip("BPVR soft tape transport",TapeSamples());
        }
        if(clickSource!=null&&clickSource.gameObject.transform.parent==owner.transform)return;
        if(clickSource!=null)UnityEngine.Object.Destroy(clickSource.gameObject);
        var audio=new GameObject("BPVR_RecorderMechanicalAudio");audio.transform.SetParent(owner.transform,false);
        clickSource=audio.AddComponent<AudioSource>();Configure(clickSource);
        tapeSource=audio.AddComponent<AudioSource>();Configure(tapeSource);tapeSource.clip=tapeClip;tapeSource.loop=true;
    }
    private static void Configure(AudioSource source)
    {
        source.playOnAwake=false;source.spatialBlend=0f;source.volume=1f;
        source.ignoreListenerPause=true;source.rolloffMode=AudioRolloffMode.Linear;source.dopplerLevel=0f;
        source.bypassReverbZones=true;source.priority=32;
    }
    private static AudioClip MakeClip(string name,float[] samples)
    {
        var clip=AudioClip.Create(name,samples.Length,1,22050,false);
        if(!clip.SetData((Il2CppStructArray<float>)samples,0))throw new Exception("Failed to upload recorder audio samples");
        UnityEngine.Object.DontDestroyOnLoad(clip);return clip;
    }
    private static float[] ClickSamples()
    {
        var data=new float[2866];var rng=new System.Random(913);double low=0;
        for(int i=0;i<data.Length;i++) {
            double t=i/22050.0,noise=rng.NextDouble()*2-1;low=low*0.62+noise*0.38;
            double a=Math.Exp(-t*145),b=t<0.027?0:Math.Exp(-(t-0.027)*105);
            double sample=(noise-low)*a*0.42+low*b*0.25+Math.Sin(2*Math.PI*730*t)*a*0.2+Math.Sin(2*Math.PI*240*t)*b*0.2;
            data[i]=(float)(sample*Math.Min(1,t/0.0007)*Math.Min(1,(0.13-t)/0.008));
        }
        return data;
    }
    private static float[] TapeSamples()
    {
        // Integer-cycle harmonics and periodic flutter make a seamless loop;
        // narrow-band textured motor sound, not speech or a loud tape hiss.
        var data=new float[44100];var rng=new System.Random(427);var phases=new double[12];
        for(int n=0;n<phases.Length;n++)phases[n]=rng.NextDouble()*Math.PI*2;
        for(int i=0;i<data.Length;i++) {
            double t=i/22050.0,flutter=0.2*Math.Sin(2*Math.PI*3*t);
            double sample=0.12*Math.Sin(2*Math.PI*72*t+flutter)+0.035*Math.Sin(2*Math.PI*144*t+flutter);
            for(int n=0;n<phases.Length;n++)sample+=0.007*Math.Sin(2*Math.PI*(240+n*37)*t+phases[n]);
            data[i]=(float)(sample*(0.92+0.08*Math.Cos(2*Math.PI*4*t)));
        }
        return data;
    }

    public override void OnUpdate()
    {
        if(tapeSource!=null&&tapeSource.isPlaying&&(!tapeSource.gameObject.activeInHierarchy||plugin==null))StopTape();
        if(!diagnostic||finished)return;
        if(Time.realtimeSinceStartup-started>60) {finished=true;MelonLogger.Error("[recorder-polish-test] FAIL timeout");Application.Quit();return;}
        if(plugin==null||SceneManager.GetActiveScene().name!="Main Menu")return;
        finished=true;
        try {RunTest();}catch(Exception ex){MelonLogger.Error("[recorder-polish-test] FAIL "+ex);}
        finally {Application.Quit();}
    }

    private static void RunTest()
    {
        var owner=new GameObject("BPVRRecorderAudioTest");
        try {
            EnsureAudio(owner);if(clickClip==null||tapeClip==null||!tapeSource.loop)throw new Exception("Audio clips missing");
            float peak=ClickSamples().Select(Math.Abs).Max();
            if(peak<=0||peak>=1)throw new Exception("Click sample clipped or silent");
            var tape=TapeSamples();if(Math.Abs(tape[0]-tape[^1])>0.015)throw new Exception("Tape loop discontinuity");
            tapeSource.volume=Mathf.Clamp01(TapeVolume.Value);tapeSource.Play();StopTape();
            if(tapeSource.isPlaying)throw new Exception("Tape failed to stop");
            string output=Path.Combine(Application.dataPath,"..","UserData","RecorderPolish-Test");Directory.CreateDirectory(output);
            WriteWave(Path.Combine(output,"record-click.wav"),ClickSamples());WriteWave(Path.Combine(output,"soft-tape-loop.wav"),tape);
            MelonLogger.Msg("[recorder-polish-test] PASS click generation, seamless loop and stop cleanup. No microphone opened.");
            TestAudioGate(owner);
            TestIndex(output);
        } finally {StopTape();UnityEngine.Object.Destroy(owner);}
    }
    private static void WriteWave(string path,float[] samples)
    {
        using var writer=new BinaryWriter(File.Create(path));
        writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));writer.Write(36+samples.Length*2);
        writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));writer.Write(16);writer.Write((short)1);writer.Write((short)1);
        writer.Write(22050);writer.Write(44100);writer.Write((short)2);writer.Write((short)16);
        writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));writer.Write(samples.Length*2);
        foreach(float s in samples)writer.Write((short)(Mathf.Clamp(s,-1f,1f)*32767));
    }

    private static void TestAudioGate(GameObject owner)
    {
        var fields=new[]{Recorder,Process,AccessTools.Field(typeof(VRPlugin),"_memoJob"),AccessTools.Field(typeof(VRPlugin),"_memoNextRecordingCheck"),AccessTools.Field(typeof(VRPlugin),"_memoRecordingCached"),QuickMemo};
        var original=fields.Select(field=>field.GetValue(plugin)).ToArray();bool paused=AudioListener.pause;
        using var process=System.Diagnostics.Process.GetCurrentProcess();
        try {
            Recorder.SetValue(plugin,owner);Process.SetValue(plugin,process);fields[2].SetValue(plugin,"diagnostic-no-microphone");
            fields[3].SetValue(plugin,Time.unscaledTime+60f);fields[4].SetValue(plugin,false);QuickMemo.SetValue(plugin,false);
            AudioListener.pause=true;
            RecordClick(plugin);
            if(!clickSource.isPlaying||!clickSource.ignoreListenerPause||clickSource.spatialBlend!=0f)throw new Exception("Record click not playable while paused");
            RecorderFrame(plugin);
            if(tapeSource.isPlaying)throw new Exception("Tape playing during startup/transcription");
            fields[4].SetValue(plugin,true);RecorderFrame(plugin);
            if(!tapeSource.isPlaying||!tapeSource.ignoreListenerPause||tapeSource.spatialBlend!=0f)throw new Exception("REC did not start audible tape source");
            fields[4].SetValue(plugin,false);RecorderFrame(plugin);
            if(tapeSource.isPlaying)throw new Exception("Tape not stopped after REC");
            fields[4].SetValue(plugin,true);RecorderFrame(plugin);owner.SetActive(false);RecorderFrame(plugin);
            if(tapeSource.isPlaying)throw new Exception("Tape playing while hidden");
            MelonLogger.Msg("[recorder-polish-test] PASS actual click hook, paused-listener playback, REC-only loop, hiding/stop. No microphone opened.");
        } finally {
            StopTape();AudioListener.pause=paused;owner.SetActive(true);
            for(int i=0;i<fields.Length;i++)fields[i].SetValue(plugin,original[i]);
        }
    }

    private static void TestIndex(string output)
    {
        object pack=AccessTools.Method(PackType,"Load").Invoke(null,new object[]{Path.Combine(Application.dataPath,"..","Mods","BluePrinceVR-Codex","hands_fp_cel.glb"),Shader.Find("Unlit/Texture")});
        var pose=AccessTools.Method(PackType,"Pose");var place=AccessTools.Method(PackType,"PlaceRecorder");var rest=AccessTools.Method(PackType,"RestThumbOnRecorder");
        var rig=GetRig(pack);
        var nodes=(Transform[])AccessTools.Field(PackType,"_nodes").GetValue(pack);
        AccessTools.Field(typeof(VRPlugin),"_celHands").SetValue(plugin,pack);
        AccessTools.Field(typeof(VRPlugin),"_memoMode").SetValue(plugin,true);
        AccessTools.Field(typeof(VRPlugin),"_quickMemo").SetValue(plugin,true);
        var handField=AccessTools.Field(typeof(VRPlugin),"_handR");var hand=Activator.CreateInstance(handField.FieldType);
        AccessTools.Field(handField.FieldType,"Valid").SetValue(hand,true);handField.SetValue(plugin,hand);
        var comfort=AccessTools.Field(typeof(VRPlugin),"_comfort").GetValue(plugin);
        var dominant=AccessTools.Property(comfort.GetType(),"LeftHanded");bool oldLeft=(bool)dominant.GetValue(comfort);
        GameObject recorder=null;
        try {
            for(int side=0;side<2;side++)for(int step=0;step<3;step++) {
                bool left=side==0;float press=step*0.5f;
                dominant.SetValue(comfort,left);
                pose.Invoke(pack,new object[]{!left,new Vector3(100,100,100),Quaternion.identity,0f,0f,1f,false,1f,0.055f,false,true,false});
                pose.Invoke(pack,new object[]{left,Vector3.zero,Quaternion.identity,0f,0f,1f,false,1f,0.055f,false,true,false});
                AccessTools.Method(typeof(VRPlugin),"MemoRecorderFrame").Invoke(plugin,null);
                recorder=(GameObject)Recorder.GetValue(plugin);
                place.Invoke(pack,new object[]{recorder.transform,left});
                recorder.transform.position+=recorder.transform.TransformDirection(Vector3.back)*0.010f;
                Rigs.Remove(pack);rig=GetRig(pack);rig.Apply(recorder.transform,left,press);
                var before=nodes.Select(node=>node.localRotation).ToArray();
                rest.Invoke(pack,new object[]{recorder.transform,left,press});
                for(int n=0;n<nodes.Length;n++) {
                    bool index=false;
                    for(int j=0;j<3;j++)if(nodes[n]==rig.Joints[side,j])index=true;
                    if(!index&&Quaternion.Angle(before[n],nodes[n].localRotation)>0.1f)
                        throw new Exception("Index press changed another joint: "+nodes[n].name);
                }
                StyleButton(plugin,left,press);
                var target=recorder.transform.TransformPoint(ButtonContact(left,press));
                float distance=Vector3.Distance(rig.Tip(side),target);
                var root=(GameObject)AccessTools.Field(PackType,"Root").GetValue(pack);
                CaptureThumbPreview(root,recorder,Path.Combine(output,(left?"left":"right")+"-index-"+step+".png"));
                if(!float.IsFinite(distance)||distance>0.006f)throw new Exception("Index missed record button: "+distance);
                MelonLogger.Msg("[recorder-polish-test] PASS "+(left?"left":"right")+" index press="+press+" contact error="+(distance*1000).ToString("F2")+"mm; thumb/middle/ring/pinky unchanged");
            }
        } finally {dominant.SetValue(comfort,oldLeft);Rigs.Remove(pack);AccessTools.Method(PackType,"Dispose").Invoke(pack,null);if(recorder!=null)UnityEngine.Object.Destroy(recorder);}
    }

    private static void CaptureThumbPreview(GameObject hand,GameObject recorder,string path)
    {
        foreach(var tr in hand.GetComponentsInChildren<Transform>())tr.gameObject.layer=30;
        foreach(var tr in recorder.GetComponentsInChildren<Transform>())tr.gameObject.layer=30;
        var obj=new GameObject("BPVRRecorderThumbPreviewCamera");var camera=obj.AddComponent<Camera>();camera.enabled=false;
        var rt=new RenderTexture(1024,1024,24);var image=new Texture2D(1024,1024,TextureFormat.RGBA32,false);var previous=RenderTexture.active;
        // Explicit Camera.Render calls in one frame otherwise reuse an old
        // skinning result. Bake the current bones for truthful pose previews.
        var skin=hand.GetComponentInChildren<SkinnedMeshRenderer>();var mesh=new Mesh();
        var frozen=new GameObject("BPVRCurrentHandPosePreview");frozen.layer=30;frozen.transform.SetParent(skin.transform,false);
        skin.BakeMesh(mesh);frozen.AddComponent<MeshFilter>().sharedMesh=mesh;
        frozen.AddComponent<MeshRenderer>().sharedMaterials=skin.sharedMaterials;bool wasEnabled=skin.enabled;skin.enabled=false;
        try {
            Vector3 center=recorder.transform.position;
            // Opposite-side button: show the actual pressing index, not the
            // thumb side that the old preview used.
            bool left=path.Contains("left-index");
            camera.transform.position=center+recorder.transform.TransformDirection(new Vector3(left?0.16f:-0.16f,0.08f,-0.24f));
            camera.transform.LookAt(center);camera.fieldOfView=45f;camera.aspect=1f;
            camera.nearClipPlane=0.01f;camera.farClipPlane=2f;camera.cullingMask=1<<30;
            camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(0.085f,0.105f,0.135f,1f);
            rt.Create();camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;
            image.ReadPixels(new Rect(0,0,1024,1024),0,0);image.Apply();File.WriteAllBytes(path,image.EncodeToPNG());
        } finally {skin.enabled=wasEnabled;UnityEngine.Object.Destroy(frozen);UnityEngine.Object.Destroy(mesh);RenderTexture.active=previous;camera.targetTexture=null;rt.Release();UnityEngine.Object.Destroy(rt);UnityEngine.Object.Destroy(image);UnityEngine.Object.Destroy(obj);}
    }
}
