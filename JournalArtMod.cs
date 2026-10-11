using System;
using System.IO;
using System.Linq;
using System.Reflection;
using BluePrinceVR;
using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using MelonLoader;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

[assembly: MelonInfo(typeof(BPVRJournalArt.JournalArtMod), "BluePrinceVR Journal Chooser Art", "1.0.1", "Codex")]
[assembly: MelonGame(null,null)]

namespace BPVRJournalArt;

// Visual-only extension: no input, notebook data, recording, inventory or
// renderer-loop changes. Preserve the installed base VR DLL and its fixes.
public sealed class JournalArtMod : MelonMod
{
    private static readonly Texture2D[] Textures=new Texture2D[2];
    private static readonly bool[] Attempted=new bool[2];
    private static readonly FieldInfo Pictures=AccessTools.Field(typeof(VRPlugin),"_journalChoicePictures");
    private static readonly FieldInfo Choice=AccessTools.Field(typeof(VRPlugin),"_journalChoice");
    private static Shader transparent;
    private static bool warned;
    private static VRPlugin testPlugin;
    private bool diagnostic,testFinished;
    private float testStart;

    public override void OnInitializeMelon()
    {
        diagnostic=Environment.GetCommandLineArgs().Contains("--bpvr-journal-art-test");
        testStart=Time.realtimeSinceStartup;
        var textureMethod=AccessTools.Method(typeof(VRPlugin),"JournalChoiceTexture");
        var showMethod=AccessTools.Method(typeof(VRPlugin),"ShowJournalChoices");
        if(textureMethod==null||showMethod==null||Pictures==null||Choice==null) {
            MelonLogger.Warning("[journal-art] Chooser hooks unavailable; retaining original artwork.");return;
        }
        HarmonyInstance.Patch(textureMethod,prefix:new HarmonyMethod(typeof(JournalArtMod),nameof(ReplaceTexture)));
        HarmonyInstance.Patch(showMethod,postfix:new HarmonyMethod(typeof(JournalArtMod),nameof(StyleChoices)));
        if(diagnostic)HarmonyInstance.Patch(AccessTools.Method(typeof(VRPlugin),"OnUpdate"),postfix:new HarmonyMethod(typeof(JournalArtMod),nameof(CaptureTestPlugin)));
        MelonLogger.Msg("[journal-art] Illustrated notebook/camera chooser enabled. Controls unchanged.");
    }

    private static void CaptureTestPlugin(VRPlugin __instance)=>testPlugin=__instance;

    public override void OnUpdate()
    {
        if(!diagnostic||testFinished)return;
        if(Time.realtimeSinceStartup-testStart>60f) {
            testFinished=true;MelonLogger.Error("[journal-art-test] FAIL startup timeout");Application.Quit();return;
        }
        if(testPlugin==null||SceneManager.GetActiveScene().name!="Main Menu")return;
        testFinished=true;
        try {
            var show=AccessTools.Method(typeof(VRPlugin),"ShowJournalChoices");
            for(int i=0;i<2;i++) {
                Choice.SetValue(testPlugin,i);
                show.Invoke(testPlugin,new object[]{Vector3.zero,Quaternion.identity,true});
                var root=Pictures.GetValue(testPlugin) as GameObject;
                if(root==null||!root.activeSelf||Textures[0]==null||Textures[1]==null)throw new Exception("Chooser or textures missing");
                for(int n=0;n<2;n++) {
                    var card=root.transform.Find(n==0?"Notebook choice":"Camera choice");
                    var art=card.Find("Journal choice illustration");
                    if(art.GetComponent<Renderer>().material.mainTexture!=Textures[n])throw new Exception("Incorrect chooser texture");
                    if(Math.Abs(art.localScale.x-art.localScale.y)>0.001f)throw new Exception("Icon stretched");
                    if(Math.Abs(card.localScale.x-(n==i?1.06f:1f))>0.001f)throw new Exception("Selection cue incorrect");
                    foreach(var tr in card.GetComponentsInChildren<Transform>())tr.gameObject.layer=30;
                }
                CaptureTestPreview(i);
                MelonLogger.Msg("[journal-art-test] PASS textures, square aspect and selected-state styling: "+i);
            }
            show.Invoke(testPlugin,new object[]{Vector3.zero,Quaternion.identity,false});
            if(((GameObject)Pictures.GetValue(testPlugin)).activeSelf)throw new Exception("Chooser remains visible after close");
            MelonLogger.Msg("[journal-art-test] PASS chooser hides on close. No game scene or save was loaded.");
        } catch(Exception ex){MelonLogger.Error("[journal-art-test] FAIL "+ex);}
        finally {Application.Quit();}
    }

    private static void CaptureTestPreview(int selected)
    {
        var obj=new GameObject("BPVRJournalArtPreviewCamera");
        var cam=obj.AddComponent<Camera>();cam.enabled=false;
        var rt=new RenderTexture(1280,640,24);
        var image=new Texture2D(1280,640,TextureFormat.RGBA32,false);
        var previous=RenderTexture.active;
        try {
            cam.orthographic=true;cam.orthographicSize=0.18f;cam.aspect=2f;
            cam.nearClipPlane=0.05f;cam.farClipPlane=2f;cam.cullingMask=1<<30;
            cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(0.045f,0.055f,0.075f,1f);
            rt.Create();cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;
            image.ReadPixels(new Rect(0,0,1280,640),0,0);image.Apply();
            string directory=Path.Combine(Application.dataPath,"..","UserData","JournalArt-Test");
            Directory.CreateDirectory(directory);
            File.WriteAllBytes(Path.Combine(directory,"chooser-selected-"+selected+".png"),image.EncodeToPNG());
        } finally {
            RenderTexture.active=previous;cam.targetTexture=null;rt.Release();
            UnityEngine.Object.Destroy(rt);UnityEngine.Object.Destroy(image);UnityEngine.Object.Destroy(obj);
        }
    }

    private static bool ReplaceTexture(int choice,ref Texture2D __result)
    {
        if(choice<0||choice>1)return true;
        if(!Attempted[choice]) {
            Attempted[choice]=true;
            string path=Path.Combine(Application.dataPath,"..","Mods","BluePrinceVR-Codex","JournalArt",choice==0?"notebook-choice-flat.png":"camera-choice-flat.png");
            Texture2D texture=null;
            try {
                if(!File.Exists(path))throw new FileNotFoundException("Chooser icon missing",path);
                texture=new Texture2D(2,2,TextureFormat.RGBA32,true);
                if(!texture.LoadImage((Il2CppStructArray<byte>)File.ReadAllBytes(path)))throw new InvalidDataException("Cannot decode chooser icon");
                if(texture.width!=texture.height||texture.width>2048||texture.width<256)throw new InvalidDataException("Chooser icon must be square, 256-2048 pixels");
                texture.name=choice==0?"BPVR_NotebookChoiceIllustrated":"BPVR_CameraChoiceIllustrated";
                texture.filterMode=FilterMode.Trilinear;
                texture.wrapMode=TextureWrapMode.Clamp;
                texture.anisoLevel=2;
                texture.Apply(true,true);
                UnityEngine.Object.DontDestroyOnLoad(texture);
                Textures[choice]=texture;
                MelonLogger.Msg("[journal-art] Loaded "+texture.name+" "+texture.width+"x"+texture.height);
            } catch(Exception ex) {
                if(texture!=null)UnityEngine.Object.Destroy(texture);
                MelonLogger.Warning("[journal-art] "+ex.Message+"; retaining original icon.");
            }
        }
        if(Textures[choice]==null)return true;
        __result=Textures[choice];return false;
    }

    private static void StyleChoices(VRPlugin __instance,bool show)
    {
        if(!show)return;
        try {
            var root=Pictures.GetValue(__instance) as GameObject;
            if(root==null)return;
            int selected=(int)Choice.GetValue(__instance);
            if(transparent==null)transparent=Shader.Find("Unlit/Transparent");
            for(int i=0;i<2;i++) {
                if(Textures[i]==null)continue;
                var card=root.transform.Find(i==0?"Notebook choice":"Camera choice");
                var illustration=card?.Find("Journal choice illustration");
                if(illustration==null)continue;
                var renderer=illustration.GetComponent<Renderer>();
                var material=renderer.material;
                if(transparent!=null&&material.shader!=transparent)material.shader=transparent;
                material.color=i==selected?Color.white:new Color(0.82f,0.86f,0.9f,1f);
                renderer.shadowCastingMode=ShadowCastingMode.Off;
                renderer.receiveShadows=false;
                illustration.localScale=new Vector3(0.258f,0.258f,1f);
                // Keep native selected-text cue; use a slight size change for
                // visual focus without a solid block behind transparent corners.
                card.localScale=Vector3.one*(i==selected?1.06f:1f);
                var border=card.Find("Selection frame")?.GetComponent<Renderer>();
                if(border!=null)border.enabled=false;
            }
        } catch(Exception ex) {
            if(!warned){warned=true;MelonLogger.Warning("[journal-art] Styling skipped: "+ex.Message);}
        }
    }
}
