using System;
using System.IO;
using System.Collections.Generic;
using System.Text.Json;
using UnityEngine;
namespace BluePrinceVR;
public partial class VRPlugin
{
    private bool _journalOpen,_journalStickHeld,_journalA,_journalB,_journalTrigger,_journalErase,_journalDirty,_journalBlockUntilRelease;
    private int _journalNav,_journalIndex=-1;
    private float _journalSize=.65f,_journalSaveAt;
    private GameObject _journalBoard,_journalPen,_journalFinder;
    private Il2CppTMPro.TextMeshPro _journalLabel;
    private Texture2D _journalTexture;
    private float _journalFlashUntil;
    private const float JournalFlashDuration=.65f;
    private readonly List<Material> _journalFrameMaterials=new();
    private AudioSource _journalAudio;
    private AudioClip _journalShutter;
    private bool _journalAudioFailed;
    private static readonly Color JournalInk=new Color(.08f,.32f,.78f,1);
    private Color[] _journalBase;
    private readonly List<string> _journalFiles=new();
    private List<List<float[]>> _journalStrokes=new();
    private List<float[]> _journalStroke;
    private Vector2 _journalPoint;
    private bool _journalPointValid;
    private bool _journalFraming;
    private const int JournalWidth=1024,JournalHeight=900;
    private string JournalFolder=>Path.Combine(Application.dataPath,"..","UserData","Codex-PhotoJournal");
    private string JournalInstructions=>"PHOTO JOURNAL — X: close | A: photo | B: undo\nRight stick click: flip pencil/eraser | Left stick: browse | Right stick up/down: size\nLeft grip: position photo | Touch pencil tip to photo + RIGHT trigger: write\nOff-hand trigger: pencil / recorder";
    private void JournalInput()
    {
        try {
            uint left=ControllerIndex(ETrackedControllerRole.LeftHand);
            uint right=ControllerIndex(ETrackedControllerRole.RightHand);
            VRControllerState_t l=default,r=default;
            uint size=(uint)System.Runtime.InteropServices.Marshal.SizeOf<VRControllerState_t>();
            bool lv=left<64 && _sys.GetControllerState(left,ref l,size);
            bool rv=right<64 && _sys.GetControllerState(right,ref r,size);
            // X is the physical left controller's lower face button, even in left-handed mode.
            var physicalLeft=_comfort.LeftHanded?r:l;var physicalRight=_comfort.LeftHanded?l:r;
            bool physicalLeftValid=_comfort.LeftHanded?rv:lv,physicalRightValid=_comfort.LeftHanded?lv:rv;
            bool stick=physicalLeftValid && (physicalLeft.ulButtonPressed & 0x80UL)!=0;
            bool keyboardToggle=Input.GetKeyDown(KeyCode.F10);
            int xAction=_quickMemoX.Update(stick,!_inHouse||!Application.isFocused||_comfortOpen,Time.unscaledTime);
            if(xAction==2 && !_journalOpen)ToggleQuickMemo();
            if((xAction==1 || keyboardToggle) && _inHouse && !_quickMemo) {
                if(_journalOpen) CloseJournal();
                else {
                    if(_journalDirty && !SaveJournal()) return;
                    _journalOpen=true;_journalFraming=true;_journalErase=false;_journalEraseHeld=rv&&(r.ulButtonPressed&0x100000000UL)!=0;_journalA=true;_journalB=true;_journalTrigger=true;
                    _journalChooser=true;_journalNav=0;_memoMode=false;
                    ClearClickState();ReleaseComputerKeys();Log("[journal] Open. "+JournalInstructions);
                }
            }
            _journalStickHeld=stick;
            if(_quickMemo){QuickMemoInput(rv,r,physicalRightValid,physicalRight);return;}
            if(!_journalOpen && _journalBlockUntilRelease) {
                if(l.ulButtonPressed==0 && r.ulButtonPressed==0 && Math.Abs(l.rAxis0.x)+Math.Abs(l.rAxis0.y)+Math.Abs(r.rAxis0.x)+Math.Abs(r.rAxis0.y)<.2f) _journalBlockUntilRelease=false;
                BlockJournalInput();return;
            }
            if(!_journalOpen) return;
            if(!_inHouse || !Application.isFocused) {CloseJournal();return;}
            bool a=physicalRightValid&&(physicalRight.ulButtonPressed&0x80UL)!=0,b=physicalRightValid&&(physicalRight.ulButtonPressed&2UL)!=0;
            if(_journalChooser){
                int choiceNav=lv&&Math.Abs(l.rAxis0.x)>.65f?Math.Sign(l.rAxis0.x):0;
                if(choiceNav!=0&&choiceNav!=_journalNav)_journalChoice=1-_journalChoice;
                _journalNav=choiceNav;
                if(a&&!_journalA)ChooseJournal();
                if(b&&!_journalB)CloseJournal();
                _journalA=a;_journalB=b;BlockJournalInput();return;
            }
            bool memoConsumes=MemoInput(lv&&((l.ulButtonPressed&0x200000000UL)!=0||l.rAxis1.x>.6f),rv&&((r.ulButtonPressed&0x200000000UL)!=0||r.rAxis1.x>.6f),a&&!_journalA,b&&!_journalB,rv&&Math.Abs(r.rAxis0.x)>.65f?Math.Sign(r.rAxis0.x):0);
            if(a&&!_journalA&&!memoConsumes) {
                if(_notebook){if(SaveNotebook()){_book.Pages.Add(new NotebookPage());_book.Current=_book.Pages.Count-1;LoadNotebookPage();SaveNotebook();}}
                else _journalCapturePending=true;
            }
            if(b&&!_journalB&&!memoConsumes && _journalStrokes.Count>0) {_journalStrokes.RemoveAt(_journalStrokes.Count-1);_journalStroke=null;RepaintJournal();MarkJournalDirty();}
            _journalA=a;_journalB=b;
            bool erase=rv&&(r.ulButtonPressed&0x100000000UL)!=0;
            if(erase&&!_journalEraseHeld) {_journalErase=!_journalErase;_journalStroke=null;}
            _journalEraseHeld=erase;
            int nav=lv&&Math.Abs(l.rAxis0.x)>.65f?Math.Sign(l.rAxis0.x):0;
            if(nav!=0 && nav!=_journalNav && !MemoBusy) {
                if(_notebook){if(SaveNotebook()){_book.Current=Math.Clamp(_book.Current+nav,0,_book.Pages.Count-1);LoadNotebookPage();SaveNotebook();}}
                else if(_journalFiles.Count>0&&SaveJournal()) {_journalIndex=(_journalIndex+nav+_journalFiles.Count)%_journalFiles.Count;LoadJournalPhoto();_journalFraming=false;}
            }
            _journalNav=nav;
            if(rv && Math.Abs(r.rAxis0.y)>.25f) _journalSize=Mathf.Clamp(_journalSize+r.rAxis0.y*Time.unscaledDeltaTime*.4f,.35f,1.1f);
            BlockJournalInput();
        } catch(Exception e) {LogWarn("[journal] Input error: "+e.Message);CloseJournal();}
    }
    private bool _journalCapturePending,_journalEraseHeld;
    private void BlockJournalInput()
    {
        _crouchHeld=false;
        VRInput.MoveX=VRInput.MoveY=VRInput.LookX=VRInput.LookY=0;
        VRInput.Select=VRInput.SelectEdge=VRInput.Interact=VRInput.Cancel=VRInput.CancelEdge=VRInput.Menu=false;
        VRInput.Directory=VRInput.DirectoryEdge=VRInput.Blueprint=VRInput.BlueprintEdge=false;
        VRInput.NavUp=VRInput.NavDown=VRInput.NavLeft=VRInput.NavRight=false;
        VRInput.NavUpEdge=VRInput.NavDownEdge=VRInput.NavLeftEdge=VRInput.NavRightEdge=false;
        VRInput.InteractDownFrames=0;ClearClickState();ReleaseComputerKeys();
        VRInput.UseItem=VRInput.UseItemEdge=VRInput.Stroll=VRInput.StrollEdge=VRInput.MenuEdge=false;
    }
    private void CloseJournal()
    {
        ShowJournalChoices(Vector3.zero,Quaternion.identity,false);
        CancelMemo();
        SaveJournal();_journalOpen=false;_journalBlockUntilRelease=true;_journalCapturePending=false;_journalStroke=null;_journalPointValid=false;_journalFlashUntil=0;
        if(_journalBoard!=null)_journalBoard.SetActive(false);
        if(_journalPen!=null)_journalPen.SetActive(false);
        if(_journalLabel!=null)_journalLabel.gameObject.SetActive(false);
        if(_journalFinder!=null)_journalFinder.SetActive(false);
        if(_journalPencil!=null)_journalPencil.SetActive(false);
        if(_journalFlash!=null)_journalFlash.SetActive(false);
    }
    private void JournalFrame(Camera source,Vector3 eye,Quaternion rotation)
    {
        if(!_journalOpen) {QuickMemoFrame(eye,rotation);return;}
        try {
            if(_journalCapturePending) {_journalCapturePending=false;CaptureJournal(source,eye,rotation);}
            JournalFlashFrame(eye,rotation);
            if(_journalLabel==null) {
                var go=new GameObject("BPVR_JournalInstructions");UnityEngine.Object.DontDestroyOnLoad(go);
                _journalLabel=go.AddComponent(Il2CppInterop.Runtime.Il2CppType.Of<Il2CppTMPro.TextMeshPro>()).Cast<Il2CppTMPro.TextMeshPro>();
                _journalLabel.fontSize=1.5f;_journalLabel.alignment=Il2CppTMPro.TextAlignmentOptions.Center;
                _journalLabel.rectTransform.sizeDelta=new Vector2(8,2);go.transform.localScale=Vector3.one*.1f;
            }
            _journalLabel.gameObject.SetActive(true);
            _journalLabel.text=(_memoMode?"MEMO RECORDER — Off-hand trigger: pencil | Hold tool-hand trigger: record | A: keep preview | B: discard":JournalInstructions)+"\nX: close | right stick left/right: note pages\n"+_memoStatus+" | Photo "+(_journalIndex+1)+" / "+_journalFiles.Count;
            _journalLabel.transform.SetPositionAndRotation(eye+rotation*new Vector3(0,-.48f,.9f),rotation);
            if(_journalChooser){
                ShowJournalChoices(eye,rotation,true);
                _journalLabel.transform.SetPositionAndRotation(eye+rotation*new Vector3(0,-.22f,.8f),rotation);
                _journalLabel.fontSize=2.6f;
                _journalLabel.text=(_journalChoice==0?"> NOTEBOOK        CAMERA":"  NOTEBOOK      > CAMERA")+"\nOff-hand stick: choose   A: open   X: close";
                if(_journalBoard!=null)_journalBoard.SetActive(false);
                if(_journalPencil!=null)_journalPencil.SetActive(false);
                ShowJournalFinder(eye,rotation,false);return;
            }
            ShowJournalChoices(eye,rotation,false);
            _journalLabel.fontSize=1.5f;
            if(_notebook)_journalLabel.text="NOTEBOOK — X: close | Off-hand trigger: pencil / recorder\nOff-hand stick: pages | A: new page (pencil) / keep recording | B: undo / discard\nOff-hand grip: position | Tool-hand stick: resize | Click tool-hand stick: eraser\n"+_memoStatus;
            bool framing=_journalBoard==null || _journalFraming;
            ShowJournalFinder(eye,rotation,framing || Time.unscaledTime<_journalFlashUntil);
            if(framing) {
                if(_memoRecorder!=null)_memoRecorder.SetActive(false);
                if(_journalPencil!=null)_journalPencil.SetActive(false);
                if(_journalBoard!=null)_journalBoard.SetActive(false);
                if(_journalPen!=null)_journalPen.SetActive(false);
                _journalStroke=null;_journalPointValid=false;return;
            }
            if(_journalBoard==null) return;
            if(!_journalBoard.activeSelf) {
                _journalBoard.SetActive(true);_journalBoard.transform.SetPositionAndRotation(eye+rotation*new Vector3(0,-.10f,.65f),rotation);
            }
            if(_handL!=null && _handL.Valid && _handL.GripDown) {
                _journalBoard.transform.SetPositionAndRotation(_handL.AimOrigin+rotation*new Vector3(.22f,.08f,.15f),rotation);
                _journalStroke=null;_journalPointValid=false;
            }
            _journalBoard.transform.localScale=new Vector3(_journalSize,_notebook?_journalSize*1.28f:_journalSize*JournalHeight/JournalWidth,1);
            NotebookTextFrame();
            MemoFrame();
            if(_memoMode){
                if(_journalPencil!=null)_journalPencil.SetActive(false);
                _journalStroke=null;_journalPointValid=false;
                if(_journalDirty)SaveJournal();
                return;
            }
            bool down=_handR!=null && _handR.Valid && _handR.TriggerDown;
            UpdateJournalPencil();
            Vector3 tip=_journalPencilTip==null?IndexTip(_handR):_journalPencilTip.position;
            Vector3 rubber=_journalPencilEraser==null?tip:_journalPencilEraser.position;
            Vector3 rubberLocal=_journalBoard.transform.InverseTransformPoint(rubber);
            bool rub=_handR!=null&&_handR.Valid && Math.Abs(rubberLocal.z)<.025f && Math.Abs(rubberLocal.x)<.5f && Math.Abs(rubberLocal.y)<.5f;
            if(rub)tip=rubber;
            Vector3 local=_journalBoard.transform.InverseTransformPoint(tip);
            bool touching=Math.Abs(local.z)<.065f && Math.Abs(local.x)<.5f && Math.Abs(local.y)<.5f;
            if(_journalPen==null) {
                _journalPen=GameObject.CreatePrimitive(PrimitiveType.Sphere);_journalPen.name="BPVR_JournalMarkerTip";
                UnityEngine.Object.Destroy(_journalPen.GetComponent<Collider>());UnityEngine.Object.DontDestroyOnLoad(_journalPen);
                _journalPen.transform.localScale=Vector3.one*.009f;
                var penShader=Shader.Find("Unlit/Color");
                if(penShader!=null)_journalPen.GetComponent<Renderer>().material=new Material(penShader){color=JournalInk};
            }
            _journalPen.SetActive(false); // Replaced by the physical pencil and eraser.
            if((down || rub) && touching && !(_handL!=null&&_handL.GripDown)) {
                Vector2 point=new Vector2((local.x+.5f)*JournalWidth,(local.y+.5f)*JournalHeight);
                if(!_journalPointValid) _journalPoint=point;
                point=Vector2.Lerp(_journalPoint,point,1f-Mathf.Exp(-Time.unscaledDeltaTime/.035f));
                if(rub) {
                    _journalStroke=null;
                    if(JournalNotes.Erase(_journalStrokes,point.x,point.y,16f)) {RepaintJournal();MarkJournalDirty();}
                } else {
                    if(_journalStroke==null && _journalStrokes.Count<1000) {_journalStroke=new List<float[]>();_journalStrokes.Add(_journalStroke);_journalPointValid=false;}
                    if(_journalStroke==null || _journalStroke.Count>=20000) return;
                    if(!_journalPointValid || (point-_journalPoint).sqrMagnitude>=2f) {
                        InkLine(_journalPointValid?_journalPoint:point,point);_journalStroke.Add(new[]{point.x,point.y});_journalTexture.Apply(false);MarkJournalDirty();
                    }
                }
                _journalPoint=point;_journalPointValid=true;
            } else {_journalStroke=null;_journalPointValid=false;}
            _journalTrigger=down;
            if(_journalDirty && !down && Time.unscaledTime>=_journalSaveAt) SaveJournal();
        } catch(Exception e) {LogWarn("[journal] Optional feature error: "+e);CloseJournal();}
    }
    private void ShowJournalFinder(Vector3 eye,Quaternion rotation,bool show)
    {
        if(!show) {if(_journalFinder!=null)_journalFinder.SetActive(false);return;}
        if(_journalFinder==null) {
            _journalFinder=new GameObject("BPVR_JournalViewfinder");UnityEngine.Object.DontDestroyOnLoad(_journalFinder);
            float height=2f*.5f*Mathf.Tan(32.5f*Mathf.Deg2Rad),width=height*4f/3f;
            var shader=Shader.Find("Unlit/Color");
            for(int i=0;i<4;i++) {
                var edge=GameObject.CreatePrimitive(PrimitiveType.Quad);edge.name="BPVR_JournalFrameEdge";
                UnityEngine.Object.Destroy(edge.GetComponent<Collider>());edge.transform.SetParent(_journalFinder.transform,false);
                edge.transform.localPosition=i<2?new Vector3(0,(i==0?-1:1)*height*.5f,0):new Vector3((i==2?-1:1)*width*.5f,0,0);
                edge.transform.localScale=i<2?new Vector3(width,.003f,1):new Vector3(.003f,height,1);
                if(shader!=null) {
                    var material=new Material(shader){color=new Color(.9f,.85f,.65f,1)};
                    edge.GetComponent<Renderer>().material=material;_journalFrameMaterials.Add(material);
                }
            }
        }
        _journalFinder.SetActive(true);_journalFinder.transform.SetPositionAndRotation(eye+rotation*Vector3.forward*.5f,rotation);
        // Localized shutter flash, not a full-headset whiteout. Unscaled time also
        // makes the short fade independent of game simulation/time scale.
        float flash=Mathf.Clamp01((_journalFlashUntil-Time.unscaledTime)/JournalFlashDuration);
        foreach(var material in _journalFrameMaterials)
            material.color=Color.Lerp(new Color(.9f,.85f,.65f,1),Color.white,flash);
    }
    private void JournalShutterFeedback()
    {
        _journalFlashUntil=Time.unscaledTime+JournalFlashDuration;
        if(_journalAudioFailed)return;
        try {
            if(_journalAudio==null) {
                var go=new GameObject("BPVR_JournalShutter");UnityEngine.Object.DontDestroyOnLoad(go);
                _journalAudio=go.AddComponent(Il2CppInterop.Runtime.Il2CppType.Of<AudioSource>()).Cast<AudioSource>();
                _journalAudio.playOnAwake=false;_journalAudio.spatialBlend=0;_journalAudio.volume=.3f;
                const int rate=22050;var samples=new float[26460];var rng=new System.Random(2601);
                for(int i=0;i<samples.Length;i++) {
                    double t=(double)i/rate,noise=rng.NextDouble()*2-1;
                    double open=Math.Exp(-t*100)*(.65*noise+.35*Math.Sin(t*2*Math.PI*1100));
                    double dt=t-.065;
                    double close=dt>=0?Math.Exp(-dt*65)*(.55*noise+.45*Math.Sin(dt*2*Math.PI*620))*Math.Min(1,dt*4000):0;
                    double bulb=.22*noise*Math.Exp(-t*14)*Math.Min(1,t*4000);
                    double charge=t>.10?.08*Math.Sin(2*Math.PI*(1900*t+400*t*t))*Math.Sin(Math.PI*Math.Min(1,(t-.10)/1.10)):0;
                    samples[i]=(float)(.6*(open*Math.Min(1,t*4000)+close+bulb+charge));
                }
                _journalShutter=AudioClip.Create("Codex camera shutter",samples.Length,1,rate,false);
                _journalShutter.SetData(samples,0);
            }
            _journalAudio.PlayOneShot(_journalShutter);
        } catch(Exception e) {_journalAudioFailed=true;LogWarn("[journal] Shutter audio disabled; photos remain available: "+e.Message);}
    }
    private void CaptureJournal(Camera source,Vector3 eye,Quaternion rotation)
    {
        // One extra render only on shutter press. Never sample the desktop camera
        // or include our hands, board or instruction text in the saved image.
        var hidden=new List<Renderer>();var active=RenderTexture.active;
        GameObject root=null;RenderTexture rt=null;Texture2D photo=null;
        try {
            foreach(var r in UnityEngine.Object.FindObjectsOfType<Renderer>()) {
                if(r==null || !r.enabled)continue;
                bool hide=r==HandRig.Model?.Renderer || r==_armOutline;
                for(var t=r.transform;t!=null&&!hide;t=t.parent) hide=t.name.StartsWith("BPVR_")||t.name.StartsWith("Codex_");
                // Keep the actual document/menu/video panel in the photograph.
                if(r==_uiQuadRenderer)hide=false;
                if(hide) {hidden.Add(r);r.enabled=false;}
            }
            root=new GameObject("BPVR_JournalCaptureCamera");var camera=root.AddComponent<Camera>();camera.CopyFrom(source);camera.enabled=false;
            camera.transform.SetPositionAndRotation(eye,rotation);camera.ResetWorldToCameraMatrix();camera.ResetProjectionMatrix();
            camera.fieldOfView=65;camera.aspect=4f/3f;camera.rect=new Rect(0,0,1,1);
            camera.useOcclusionCulling=false;camera.clearFlags=CameraClearFlags.Color;
            camera.cullingMatrix=camera.projectionMatrix*camera.worldToCameraMatrix;
            if(_uiQuad!=null&&_uiQuad.activeInHierarchy)camera.cullingMask|=1<<_uiQuad.layer;
            rt=new RenderTexture(1024,768,24);rt.Create();camera.targetTexture=rt;RenderWithoutBeautify(camera);
            RenderTexture.active=rt;photo=new Texture2D(1024,768,TextureFormat.RGBA32,false);photo.ReadPixels(new Rect(0,0,1024,768),0,0);photo.Apply(false);
            Directory.CreateDirectory(JournalFolder);
            string path=Path.Combine(JournalFolder,"photo-"+DateTime.Now.ToString("yyyyMMdd-HHmmss-fff")+"-"+Guid.NewGuid().ToString("N").Substring(0,6)+".png");
            if(!SaveJournal()) return;
            File.WriteAllBytes(path,photo.EncodeToPNG());_journalFiles.Add(path);_journalIndex=_journalFiles.Count-1;LoadJournalPhoto();
            _journalFraming=false;
            Log("[journal] Captured "+path);
            JournalShutterFeedback();
        } finally {
            RenderTexture.active=active;foreach(var r in hidden)if(r!=null)r.enabled=true;
            if(root!=null)UnityEngine.Object.Destroy(root);if(photo!=null)UnityEngine.Object.Destroy(photo);
            if(rt!=null){rt.Release();UnityEngine.Object.Destroy(rt);}
        }
    }
    private void LoadJournalPhoto()
    {
        if(_journalIndex<0)return;
        var photo=new Texture2D(2,2,TextureFormat.RGBA32,false);
        try {
            if(!photo.LoadImage(File.ReadAllBytes(_journalFiles[_journalIndex])))throw new Exception("Cannot decode photo");
            if(photo.width!=1024||photo.height!=768)throw new Exception("Unexpected photo dimensions");
            if(_journalTexture!=null)UnityEngine.Object.Destroy(_journalTexture);
            _journalTexture=new Texture2D(JournalWidth,JournalHeight,TextureFormat.RGBA32,false);
            _journalBase=new Color[JournalWidth*JournalHeight];Array.Fill(_journalBase,new Color(.76f,.74f,.68f,1));
            var pixels=photo.GetPixels();
            for(int y=0;y<684;y++)for(int x=0;x<912;x++)_journalBase[(y+160)*JournalWidth+x+56]=pixels[(y*768/684)*1024+x*1024/912];
            _journalStrokes=new();string notes=Path.ChangeExtension(_journalFiles[_journalIndex],"json");
            _journalStrokes=JournalNotes.Load(notes);
            _journalStroke=null;_journalPointValid=false;RepaintJournal();
            if(_journalBoard==null) {
                _journalBoard=GameObject.CreatePrimitive(PrimitiveType.Quad);_journalBoard.name="BPVR_PhotoJournal";
                UnityEngine.Object.Destroy(_journalBoard.GetComponent<Collider>());UnityEngine.Object.DontDestroyOnLoad(_journalBoard);
                var shader=Shader.Find("Unlit/Texture");if(shader==null)throw new Exception("Photo shader unavailable");
                _journalBoard.GetComponent<Renderer>().material=new Material(shader);_journalBoard.SetActive(false);
            }
            _journalBoard.GetComponent<Renderer>().material.mainTexture=_journalTexture;
        } finally {UnityEngine.Object.Destroy(photo);}
    }
    private void InkLine(Vector2 a,Vector2 b)
    {
        int steps=Mathf.Clamp(Mathf.CeilToInt(Vector2.Distance(a,b)),1,1500);
        for(int i=0;i<=steps;i++) {
            Vector2 p=Vector2.Lerp(a,b,(float)i/steps);int x=Mathf.RoundToInt(p.x),y=Mathf.RoundToInt(p.y);
            for(int dx=-2;dx<=2;dx++)for(int dy=-2;dy<=2;dy++)if(dx*dx+dy*dy<=4&&x+dx>=0&&x+dx<JournalWidth&&y+dy>=0&&y+dy<JournalHeight)
                _journalTexture.SetPixel(x+dx,y+dy,JournalInk);
        }
    }
    private void RepaintJournal()
    {
        if(_journalTexture==null)return;
        _journalTexture.SetPixels(_journalBase);
        foreach(var s in _journalStrokes)for(int i=0;i<s.Count;i++)InkLine(new Vector2(s[Math.Max(0,i-1)][0],s[Math.Max(0,i-1)][1]),new Vector2(s[i][0],s[i][1]));
        _journalTexture.Apply(false);
    }
    private void MarkJournalDirty(){_journalDirty=true;_journalSaveAt=Time.unscaledTime+1f;}
    private bool SaveJournal()
    {
        if(_notebook)return SaveNotebook();
        if(!_journalDirty||_journalIndex<0)return true;
        try {
            string path=Path.ChangeExtension(_journalFiles[_journalIndex],"json");
            JournalNotes.Save(path,_journalStrokes);_journalDirty=false;return true;
        }catch(Exception e){LogWarn("[journal] Notes not saved: "+e.Message);return false;}
    }
}
