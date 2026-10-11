using System;
using UnityEngine;
using Il2CppTMPro;
namespace BluePrinceVR;
public partial class VRPlugin
{
    private readonly TapHoldButton _quickMemoX=new();
    private bool _quickMemo,_quickMemoA,_quickMemoB;
    private TextMeshPro _quickMemoLabel;
    private void ToggleQuickMemo()
    {
        if(_quickMemo){CloseQuickMemo();return;}
        if(_computerKeyboardMode||_draftMenu||NativePanelMenu)return;
        if(_journalDirty&&!SaveJournal())return;
        try{
            _book=NotebookData.Load(NotebookPath);_notebook=true;LoadNotebookPage();
            _quickMemo=true;_memoMode=true;_memoTrigger=true;_memoX=false;
            _quickMemoA=_quickMemoB=true;
            _memoStatus="Hold trigger, wait for REC, then speak. Release to transcribe.";
            ClearClickState();
        }catch(Exception e){LogWarn("[quick memo] Cannot open notebook: "+e.Message);}
    }
    private void CloseQuickMemo()
    {
        CancelMemo();_quickMemo=false;_memoMode=false;
        if(_quickMemoLabel!=null)_quickMemoLabel.gameObject.SetActive(false);
    }
    private void QuickMemoInput(bool toolValid,VRControllerState_t tool,bool faceValid,VRControllerState_t face)
    {
        if(!_inHouse||!Application.isFocused||!toolValid){CloseQuickMemo();return;}
        bool trigger=(tool.ulButtonPressed&0x200000000UL)!=0||tool.rAxis1.x>.6f;
        bool a=faceValid&&(face.ulButtonPressed&0x80UL)!=0,b=faceValid&&(face.ulButtonPressed&2UL)!=0;
        MemoInput(false,trigger,a&&!_quickMemoA,b&&!_quickMemoB,0);
        _quickMemoA=a;_quickMemoB=b;
        // Keep movement, turning and sprint intact; reserve confirmation and
        // recorder trigger so a note cannot also activate the world.
        VRInput.Select=VRInput.SelectEdge=VRInput.Interact=VRInput.Cancel=VRInput.CancelEdge=false;
        VRInput.InteractDownFrames=0;
    }
    private void QuickMemoFrame(Vector3 eye,Quaternion rotation)
    {
        if(!_quickMemo)return;
        try{
            PollMemo();MemoRecorderFrame();
            if(_quickMemoLabel==null){
                var go=new GameObject("BPVR_QuickMemoStatus");UnityEngine.Object.DontDestroyOnLoad(go);
                _quickMemoLabel=go.AddComponent(Il2CppInterop.Runtime.Il2CppType.Of<TextMeshPro>()).Cast<TextMeshPro>();
                _quickMemoLabel.fontSize=1.3f;_quickMemoLabel.richText=false;_quickMemoLabel.enableWordWrapping=true;
                _quickMemoLabel.alignment=TextAlignmentOptions.Center;_quickMemoLabel.rectTransform.sizeDelta=new Vector2(6,2.6f);
                go.transform.localScale=Vector3.one*.1f;
            }
            _quickMemoLabel.gameObject.SetActive(true);
            _quickMemoLabel.transform.SetPositionAndRotation(eye+rotation*new Vector3(0,-.28f,.85f),rotation);
            string preview=_memoPreview;
            if(preview!=null&&preview.Length>400)preview=preview.Substring(0,400)+"… (full note saved with A)";
            _quickMemoLabel.text="QUICK NOTE — Hold X: put away\n"+(_memoPreview!=null?"A: save to notebook | B: discard\n"+preview:_memoStatus);
        }catch(Exception e){CloseQuickMemo();LogWarn("[quick memo] Closed after error: "+e.Message);}
    }
    private void DetailMemoRecorder(Transform root)
    {
        // Graphic, stepped tones and dark seams; all geometry is built once.
        var ink=new Color(.018f,.023f,.029f);var edge=new Color(.22f,.26f,.28f);
        ToolPart(root,"Back shell dark edge",PrimitiveType.Cube,new Vector3(0,0,.008f),new Vector3(.056f,.118f,.010f),ink);
        ToolPart(root,"Face highlight band",PrimitiveType.Cube,new Vector3(.020f,0,-.012f),new Vector3(.004f,.11f,.002f),edge);
        ToolPart(root,"Cassette hinge",PrimitiveType.Cube,new Vector3(-.024f,.013f,-.014f),new Vector3(.003f,.046f,.003f),edge);
        ToolPart(root,"Door latch",PrimitiveType.Cube,new Vector3(.018f,.015f,-.015f),new Vector3(.004f,.010f,.004f),ink);
        ToolPart(root,"Tape counter bezel",PrimitiveType.Cube,new Vector3(-.010f,-.019f,-.014f),new Vector3(.023f,.007f,.002f),ink);
        for(int i=0;i<3;i++){
            ToolPart(root,"Counter digit",PrimitiveType.Cube,new Vector3(-.017f+i*.006f,-.019f,-.0155f),new Vector3(.003f,.004f,.0006f),new Color(.64f,.69f,.62f));
            ToolPart(root,"Counter ink",PrimitiveType.Cube,new Vector3(-.017f+i*.006f,-.019f,-.016f),new Vector3(.0014f,.0024f,.0005f),ink);
        }
        for(int i=0;i<4;i++){
            float x=i%2==0?-.023f:.023f,y=i<2?.054f:-.054f;
            ToolPart(root,"Recessed screw",PrimitiveType.Sphere,new Vector3(x,y,-.0125f),new Vector3(.003f,.003f,.001f),edge);
            ToolPart(root,"Screw slot",PrimitiveType.Cube,new Vector3(x,y,-.0132f),new Vector3(.002f,.0005f,.0004f),ink);
        }
        var wheel=ToolPart(root,"Volume thumb wheel",PrimitiveType.Cylinder,new Vector3(.016f,.060f,0),new Vector3(.013f,.003f,.013f),ink);
        for(int i=0;i<6;i++)ToolPart(root,"Grip rib",PrimitiveType.Cube,new Vector3(-.0275f,-.028f+i*.004f,.003f),new Vector3(.002f,.001f,.013f),edge);
    }
}
