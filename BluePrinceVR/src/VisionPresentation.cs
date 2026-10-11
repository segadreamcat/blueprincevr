using System;
using UnityEngine;
namespace BluePrinceVR;
public partial class VRPlugin
{
    private Camera _visionCamera,_lastExplorationCamera;
    private bool _anteCinematic;
    private readonly GameObject[] _overlayHidden=new GameObject[12];
    private readonly bool[] _overlayWasActive=new bool[12];
    private Camera VisionAnchor()
    {
        _visionCamera=null;
        Camera ante=null,player=null;
        foreach(var candidate in Camera.allCameras) {
            if(candidate==null || !candidate.isActiveAndEnabled || candidate.targetTexture!=null)continue;
            if(candidate.name.Equals("VIDEO CAM",StringComparison.OrdinalIgnoreCase)&&
                (_visionCamera==null||ComparePresentationCameras(candidate,_visionCamera)<0))_visionCamera=candidate;
            if(candidate.name=="Ante Chamber Camera"&&(ante==null||candidate.GetInstanceID()<ante.GetInstanceID()))ante=candidate;
            if(candidate.name.Equals("FirstPersonCharacter",StringComparison.OrdinalIgnoreCase)&&(player==null||candidate.GetInstanceID()<player.GetInstanceID()))player=candidate;
        }
        if(player!=null)_lastExplorationCamera=player;
        _anteCinematic=ante!=null;
        if(_anteCinematic)_visionCamera=ante;
        // The vision switches OFF FirstPersonCharacter. Keep its unmodified
        // playspace for VR and render VIDEO CAM in its native projection on the UI.
        return _visionCamera!=null&&_lastExplorationCamera!=null?_lastExplorationCamera:null;
    }
    private static int ComparePresentationCameras(Camera a,Camera b)
    {
        int depth=a.depth.CompareTo(b.depth);
        return depth!=0?depth:a.GetInstanceID().CompareTo(b.GetInstanceID());
    }
    private void HideOverlayHands(out Renderer arms,out bool armsEnabled,out bool outlineEnabled)
    {
        arms=HandRig.Model?.Renderer;armsEnabled=arms!=null&&arms.enabled;
        outlineEnabled=_armOutline!=null&&_armOutline.enabled;
        if(arms!=null)arms.enabled=false;if(_armOutline!=null)_armOutline.enabled=false;
        _overlayHidden[0]=_uiQuad;_overlayHidden[1]=_journalBoard;_overlayHidden[2]=_journalPencil;
        _overlayHidden[3]=_journalFinder;_overlayHidden[4]=_journalFlash;_overlayHidden[5]=_journalLabel==null?null:_journalLabel.gameObject;
        _overlayHidden[6]=_heldShovel;_overlayHidden[7]=_heldParlorKey;
        _overlayHidden[8]=_celHands?.Root;
        _overlayHidden[9]=_comfortText==null?null:_comfortText.gameObject;
        _overlayHidden[10]=_memoRecorder;
        _overlayHidden[11]=_quickMemoLabel==null?null:_quickMemoLabel.gameObject;
        for(int i=0;i<_overlayHidden.Length;i++) {
            var go=_overlayHidden[i];_overlayWasActive[i]=go!=null&&go.activeSelf;
            if(_overlayWasActive[i])go.SetActive(false);
        }
    }
    private void RestoreOverlayHands(Renderer arms,bool armsEnabled,bool outlineEnabled)
    {
        if(arms!=null)arms.enabled=armsEnabled;
        if(_armOutline!=null)_armOutline.enabled=outlineEnabled;
        for(int i=0;i<_overlayHidden.Length;i++)if(_overlayHidden[i]!=null)_overlayHidden[i].SetActive(_overlayWasActive[i]);
    }
}
