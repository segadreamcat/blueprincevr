using UnityEngine;
namespace BluePrinceVR;
public partial class VRPlugin
{
    private Camera _roomPanelCamera,_lastPanelRoomCamera;
    private Camera CoatCheckView()
    {
        _roomPanelCamera=null;
        Camera coat=null,player=null;
        // Both cameras stay enabled here. Camera.allCameras order is not a
        // priority contract; selecting its first entry alternated every frame.
        foreach(var candidate in Camera.allCameras) {
            if(candidate==null || !candidate.isActiveAndEnabled || candidate.targetTexture!=null)continue;
            if((candidate.name=="Coat Check Camera" || candidate.name.StartsWith("Laundry Room Camera",System.StringComparison.OrdinalIgnoreCase)) && (coat==null || candidate.GetInstanceID()<coat.GetInstanceID()))coat=candidate;
            if(candidate.name=="Slot Cam" && (_roomPanelCamera==null || candidate.GetInstanceID()<_roomPanelCamera.GetInstanceID()))_roomPanelCamera=candidate;
            if(candidate.name=="FirstPersonCharacter" && (player==null || candidate.GetInstanceID()<player.GetInstanceID()))player=candidate;
        }
        if(coat==null)coat=_roomPanelCamera;
        if(coat==null)return null;
        // Stay at the player's physical location; the native Coat Check menu
        // remains on the UI panel. Fall back to the inspection view only if
        // the game has actually disabled the player camera.
        var selected=player!=null?player:coat;
        if(_roomPanelCamera!=null&&player==null&&_lastExplorationCamera!=null)selected=_lastExplorationCamera;
        if(_lastCamName!=selected.name) {
            _lastCamName=selected.name;
            Log("[coat-check] Stable VR camera: "+selected.name);
        }
        return selected;
    }
}
