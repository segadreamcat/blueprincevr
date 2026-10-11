using System;
using System.IO;
using System.Diagnostics;
using System.Text.Json;
using UnityEngine;
using Il2CppTMPro;
namespace BluePrinceVR;
public partial class VRPlugin
{
    private bool _memoMode,_memoX,_memoTrigger;
    private Process _memoProcess;
    private string _memoJob,_memoPhoto,_memoPreview,_memoStatus="Off-hand trigger: pencil / recorder";
    private float _memoStarted;
    private int _memoPage,_memoPageHeld;
    private GameObject _memoRecorder;
    private Transform _memoShell;
    private Renderer _memoLamp;
    private Transform _memoRecordButton;
    private Transform _memoRecordMount;
    private float _memoButtonPress,_memoNextPoll,_memoNextRecordingCheck;
    private bool _memoRecordingCached;
    private readonly Transform[] _memoReels=new Transform[2];
    private bool MemoRecording {
        get {
            if(_memoProcess==null||_memoJob==null)return false;
            if(Time.unscaledTime>=_memoNextRecordingCheck) {
                _memoNextRecordingCheck=Time.unscaledTime+.1f;
                _memoRecordingCached=File.Exists(_memoJob+".ready")&&!File.Exists(_memoJob+".stop")&&!File.Exists(_memoJob+".processing");
            }
            return _memoRecordingCached;
        }
    }
    private TextMeshPro _memoText;
    private string MemoPhoto=>_notebook&&_book!=null?Path.Combine(Path.GetDirectoryName(NotebookPath),_book.Pages[_book.Current].Id):_journalIndex>=0&&_journalIndex<_journalFiles.Count?_journalFiles[_journalIndex]:null;
    private string _memoLoadedPhoto,_memoSaved="";
    private bool MemoBusy=>_memoProcess!=null||_memoPreview!=null;
    private void StartMemo()
    {
        if(MemoPhoto==null||MemoBusy)return;
        string script=Path.GetFullPath(Path.Combine(Application.dataPath,"..","Mods","BluePrinceVR-Codex","OfflineMemo.ps1"));
        if(!File.Exists(script)){_memoStatus="Recorder helper missing: reinstall this build.";return;}
        try {
            string folder=Path.Combine(JournalFolder,"MemoTemp");Directory.CreateDirectory(folder);
            _memoJob=Path.Combine(folder,Guid.NewGuid().ToString("N"));_memoPhoto=MemoPhoto;
            var info=new ProcessStartInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System),"WindowsPowerShell","v1.0","powershell.exe"));
            info.UseShellExecute=false;info.CreateNoWindow=true;info.WindowStyle=ProcessWindowStyle.Hidden;
            foreach(string arg in new[]{"-NoProfile","-NonInteractive","-ExecutionPolicy","Bypass","-File",script,"-Job",_memoJob})info.ArgumentList.Add(arg);
            _memoProcess=Process.Start(info);_memoNextPoll=_memoNextRecordingCheck=0;_memoRecordingCached=false;_memoStarted=Time.unscaledTime;_memoStatus="Starting microphone — wait for red REC";
        }catch(Exception){CancelMemo();_memoStatus="Cannot start offline recorder. Check installed helper.";}
    }
    private void StopMemo()
    {
        if(_memoProcess==null)return;
        try{File.WriteAllText(_memoJob+".stop","");_memoRecordingCached=false;_memoNextRecordingCheck=0;_memoStatus="Finishing — microphone stopping";}catch{CancelMemo();}
    }
    private void CleanMemoJob()
    {
        if(_memoJob==null)return;
        foreach(string suffix in new[]{".ready",".stop",".text",".error",".processing",".wav",".whisper.txt",".engine.log"})try{File.Delete(_memoJob+suffix);}catch{}
        _memoJob=null;
    }
    private void CancelMemo()
    {
        if(_memoProcess!=null){try{if(!_memoProcess.HasExited)_memoProcess.Kill(true);}catch{} _memoProcess.Dispose();_memoProcess=null;}
        CleanMemoJob();_memoPreview=null;
        if(_memoRecorder!=null)_memoRecorder.SetActive(false);
        if(_memoText!=null)_memoText.gameObject.SetActive(false);
    }
    private void PollMemo()
    {
        if(_memoProcess==null)return;
        if(Time.unscaledTime<_memoNextPoll)return;
        _memoNextPoll=Time.unscaledTime+.1f;
        if(!_memoProcess.HasExited){
            if(Time.unscaledTime-_memoStarted>240){CancelMemo();_memoStatus="Recorder timed out; try a shorter note.";return;}
            if(File.Exists(_memoJob+".processing"))_memoStatus="Transcribing with Whisper — microphone off. B: cancel";
            else if(MemoRecording)_memoStatus="REC — release RIGHT trigger to finish (60 sec max)";
            return;
        }
        try {
            if(File.Exists(_memoJob+".error"))_memoStatus=File.ReadAllText(_memoJob+".error");
            else if(File.Exists(_memoJob+".text")){
                string text=File.ReadAllText(_memoJob+".text").Trim();
                if(text.Length>0){_memoPreview=text;_memoPage=0;_memoStatus="REVIEW: A keep | B discard | right stick left/right: pages";}
                else _memoStatus="No speech recognized. Hold trigger, wait for REC, then speak.";
            }else _memoStatus="Recorder stopped without a transcript.";
        }finally{_memoProcess.Dispose();_memoProcess=null;CleanMemoJob();}
    }
    // Called before normal journal A/B routing so accepting a note cannot take a photo.
    private bool MemoInput(bool x,bool trigger,bool aEdge,bool bEdge,int page)
    {
        PollMemo();
        if(x&&!_memoX&&!MemoBusy){_memoMode=!_memoMode;_memoStatus="Hold tool-hand trigger; speak after red REC. Off-hand trigger: pencil";_memoTrigger=trigger;}
        _memoX=x;
        if(MemoBusy&&bEdge){CancelMemo();_memoStatus="Recording / preview discarded.";}
        else if(_memoPreview!=null&&aEdge){
            if(_notebook){KeepNotebookMemo();}
            else {
            try {
                string path=Path.ChangeExtension(_memoPhoto,"voice.json");
                string previous=File.Exists(path)?JsonSerializer.Deserialize<string>(File.ReadAllText(path)):"";
                string combined=string.IsNullOrWhiteSpace(previous)?_memoPreview:previous+"\n\n"+_memoPreview;
                string temp=path+".tmp";File.WriteAllText(temp,JsonSerializer.Serialize(combined));File.Move(temp,path,true);
                _memoPreview=null;_memoLoadedPhoto=null;_memoStatus="Note saved. Hold trigger for another note; off-hand trigger: pencil.";
            }catch{_memoStatus="Note NOT saved. A: retry | B: discard";}
            }
        }
        if(_memoMode&&!_journalFraming){
            if(trigger&&!_memoTrigger&&!MemoBusy)StartMemo();
            if(!trigger&&_memoTrigger)StopMemo();
        }
        if(page!=0&&page!=_memoPageHeld)_memoPage=Math.Max(0,_memoPage+page);
        _memoPageHeld=page;_memoTrigger=trigger;
        return (_memoMode&&!_journalFraming)||MemoBusy;
    }
    private void MemoFrame()
    {
        PollMemo();
        if(_journalBoard==null||!_journalBoard.activeSelf||_journalFraming)return;
        if(_memoLoadedPhoto!=MemoPhoto){
            _memoLoadedPhoto=MemoPhoto;_memoSaved="";_memoPage=0;
            try{string path=Path.ChangeExtension(MemoPhoto,"voice.json");if(!_notebook&&File.Exists(path))_memoSaved=JsonSerializer.Deserialize<string>(File.ReadAllText(path))??"";}catch{_memoStatus="Saved voice note could not be read.";}
        }
        if(_memoText==null){
            var go=new GameObject("BPVR_PhotoVoiceNote");
            go.layer=_journalBoard.layer;
            go.transform.SetParent(_journalBoard.transform,false);go.transform.localPosition=new Vector3(0,-.402f,-.006f);go.transform.localScale=new Vector3(.01f,.01f,1);
            _memoText=go.AddComponent(Il2CppInterop.Runtime.Il2CppType.Of<TextMeshPro>()).Cast<TextMeshPro>();
            // TMP world text already scales its font into world units; 1.35 at
            // a 0.01 transform scale made the previous transcript microscopic.
            if(_journalLabel!=null&&_journalLabel.font!=null)_memoText.font=_journalLabel.font;
            _memoText.fontSize=14f;_memoText.color=Color.black;_memoText.richText=false;_memoText.enableWordWrapping=true;
            _memoText.rectTransform.sizeDelta=new Vector2(90,14);_memoText.alignment=TextAlignmentOptions.TopLeft;
        }
        string note=_memoPreview??_memoSaved;const int pageSize=200;
        int pages=Math.Max(1,(note.Length+pageSize-1)/pageSize);if(!_notebook)_memoPage=Math.Min(_memoPage,pages-1);
        int photoPage=_notebook?0:_memoPage;
        string chunk=note.Length==0?"":note.Substring(photoPage*pageSize,Math.Min(pageSize,note.Length-photoPage*pageSize));
        string heading=_memoPreview!=null?"A: KEEP NOTE   B: DISCARD":(_memoMode?_memoStatus:"VOICE NOTE");
        string display=note.Length>0?heading+"\n"+(_memoPage+1)+"/"+pages+"  "+chunk:(_memoMode?_memoStatus:"");
        _memoText.transform.localPosition=new Vector3(0,_notebook?-.59f:-.402f,-.006f);
        _memoText.gameObject.SetActive(display.Length>0);
        if(_memoText.text!=display){_memoText.text=display;_memoText.ForceMeshUpdate();}
        if(_notebook)_memoText.gameObject.SetActive(false); // Ruled-page text handles notebook preview and saved notes.
        MemoRecorderFrame();
    }
    private void MemoRecorderFrame()
    {
        if(!_memoMode){if(_memoRecorder!=null)_memoRecorder.SetActive(false);return;}
        if(_memoRecorder==null){
            _memoRecorder=new GameObject("BPVR_MemoRecorder");UnityEngine.Object.DontDestroyOnLoad(_memoRecorder);
            _memoShell=new GameObject("Recorder casing size").transform;
            _memoShell.SetParent(_memoRecorder.transform,false);var root=_memoShell;
            var shell=new Color(.045f,.055f,.065f);var trim=new Color(.11f,.14f,.17f);
            ToolPart(root,"Black microcassette casing",PrimitiveType.Cube,Vector3.zero,new Vector3(.054f,.116f,.023f),shell);
            ToolPart(root,"Left edge highlight",PrimitiveType.Cube,new Vector3(-.025f,0,-.0118f),new Vector3(.002f,.11f,.002f),trim);
            ToolPart(root,"Cassette door rim",PrimitiveType.Cube,new Vector3(-.004f,.014f,-.012f),new Vector3(.039f,.060f,.003f),trim);
            ToolPart(root,"Recessed cassette window",PrimitiveType.Cube,new Vector3(-.004f,.014f,-.014f),new Vector3(.034f,.054f,.002f),Color.black);
            ToolPart(root,"Ivory tape label",PrimitiveType.Cube,new Vector3(-.004f,.014f,-.0152f),new Vector3(.021f,.043f,.001f),new Color(.72f,.73f,.66f));
            ToolPart(root,"Red tape label",PrimitiveType.Cube,new Vector3(-.009f,.014f,-.016f),new Vector3(.010f,.043f,.001f),new Color(.53f,.055f,.09f));
            for(int i=0;i<2;i++){
                var hub=new GameObject("BPVR_CassetteReel"+i).transform;hub.SetParent(root,false);hub.localPosition=new Vector3(-.004f,i==0?.027f:.001f,-.017f);_memoReels[i]=hub;
                ToolPart(hub,"Reel rim",PrimitiveType.Sphere,Vector3.zero,new Vector3(.017f,.017f,.0017f),new Color(.26f,.30f,.33f));
                ToolPart(hub,"Reel center",PrimitiveType.Sphere,new Vector3(0,0,-.001f),new Vector3(.012f,.012f,.001f),shell);
                for(int spoke=0;spoke<3;spoke++){
                    var bar=ToolPart(hub,"Reel spoke",PrimitiveType.Cube,new Vector3(0,0,-.0017f),new Vector3(.010f,.0013f,.0005f),new Color(.43f,.47f,.49f));bar.transform.localRotation=Quaternion.Euler(0,0,spoke*60);
                }
            }
            ToolPart(root,"Speaker inset",PrimitiveType.Cube,new Vector3(-.002f,-.038f,-.012f),new Vector3(.042f,.032f,.002f),new Color(.065f,.08f,.095f));
            for(int i=0;i<12;i++)ToolPart(root,"Speaker slots",PrimitiveType.Cube,new Vector3(-.002f,-.052f+i*.0025f,-.0133f),new Vector3(.035f,.0008f,.0006f),Color.black);
            for(int i=0;i<4;i++)ToolPart(root,"Side transport key",PrimitiveType.Cube,new Vector3(.029f,.032f-i*.015f,0),new Vector3(.007f,.012f,.015f),new Color(.075f,.08f,.085f));
            // Keep the button mount and cap one assembly. Its base penetrates
            // the casing slightly; the cap never moves outside the housing.
            _memoRecordMount=new GameObject("Record button assembly").transform;_memoRecordMount.SetParent(_memoRecorder.transform,false);
            ToolPart(_memoRecordMount,"Recessed record mount",PrimitiveType.Cube,Vector3.zero,new Vector3(.006f,.014f,.018f),trim);
            _memoRecordButton=ToolPart(_memoRecordMount,"Thumb record button",PrimitiveType.Cube,Vector3.zero,new Vector3(.008f,.010f,.014f),new Color(.65f,.04f,.035f)).transform;
            ToolPart(root,"Top microphone",PrimitiveType.Cube,new Vector3(0,.059f,0),new Vector3(.012f,.003f,.013f),trim);
            for(int i=0;i<5;i++)ToolPart(root,"Mic slit",PrimitiveType.Cube,new Vector3(-.004f+i*.002f,.0607f,0),new Vector3(.0008f,.0005f,.009f),Color.black);
            var badge=new GameObject("BPVR_MemoBadge");badge.transform.SetParent(root,false);badge.transform.localPosition=new Vector3(-.002f,.049f,-.013f);badge.transform.localScale=Vector3.one*.01f;
            var label=badge.AddComponent(Il2CppInterop.Runtime.Il2CppType.Of<TextMeshPro>()).Cast<TextMeshPro>();label.text="MEMO  •  27";label.fontSize=3;label.alignment=TextAlignmentOptions.Center;label.rectTransform.sizeDelta=new Vector2(4,.8f);label.color=new Color(.65f,.72f,.73f);
            _memoLamp=ToolPart(root,"REC lamp",PrimitiveType.Sphere,new Vector3(.020f,.050f,-.013f),Vector3.one*.003f,Color.black).GetComponent<Renderer>();
            DetailMemoRecorder(root);
            try{BatchMemoShell(root);}catch(Exception e){LogWarn("[memo] Keeping unbatched recorder details: "+e.Message);}
        }
        var wrist=_celHands?.ToolAnchors[_comfort.LeftHanded?0:1]??HandRig.Model?.HandR;bool show=wrist!=null&&_handR!=null&&_handR.Valid;
        _memoRecorder.SetActive(show);
        // Restore the pre-2.10.23 face width, growing away from the approved
        // thumb-side edge. The button and thumb stay in the unchanged tool frame.
        if(_memoShell!=null) {
            const float width=1.25f/.85f;
            float edge=(_comfort.LeftHanded?-1f:1f)*.027f;
            _memoShell.localScale=new Vector3(width,1f,1f);
            _memoShell.localPosition=new Vector3(edge*(1f-width),0,0);
        }
        if(show) {
            if(_celHands!=null)_celHands.PlaceRecorder(_memoRecorder.transform,_comfort.LeftHanded);
            else _memoRecorder.transform.SetPositionAndRotation(wrist.TransformPoint(new Vector3(0,.075f,.02f)),wrist.rotation*Quaternion.Euler(25,0,25));
        }
        bool recording=MemoRecording;
        _memoButtonPress=Mathf.MoveTowards(_memoButtonPress,recording?1f:0f,Time.unscaledDeltaTime*8f);
        if(_memoRecordButton!=null && _memoRecordMount!=null) {
            var rest=CelHandPack.RecorderButtonPosition(_comfort.LeftHanded,0);
            _memoRecordMount.localPosition=rest-CelHandPack.RecorderButtonNormal(_comfort.LeftHanded)*.004f;
            _memoRecordButton.localPosition=CelHandPack.RecorderButtonPosition(_comfort.LeftHanded,_memoButtonPress)-_memoRecordMount.localPosition;
        }
        if(show && _quickMemo && _celHands!=null)_celHands.RestThumbOnRecorder(_memoRecorder.transform,_comfort.LeftHanded,_memoButtonPress);
        _memoLamp.material.color=recording?Color.red:new Color(.10f,.025f,.02f);
        if(recording)for(int i=0;i<_memoReels.Length;i++)if(_memoReels[i]!=null)_memoReels[i].Rotate(0,0,(i==0?100f:85f)*Time.unscaledDeltaTime,Space.Self);
    }
}
