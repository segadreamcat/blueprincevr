using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace BluePrinceVR;

public partial class VRPlugin
{
    private readonly HashSet<int> _safeEffectCameras = new();
    private SkinnedMeshRenderer _armOutline;
    private static bool IsInspectionSceneCamera(Camera camera) => camera!=null &&
        (camera.name=="Coat Check Camera" || camera.name=="Slot Cam" || camera.name=="FirstPersonCharacter" ||
         camera.name.StartsWith("Laundry Room Camera",StringComparison.OrdinalIgnoreCase));
    private readonly Dictionary<int,Behaviour> _casinoPostLayers=new();

    private void RenderWithoutBeautify(Camera camera)
    {
        // Commissary log: Beautify.OnRenderImage repeatedly requests oversized RTs
        // when its camera is manually rendered to the VR eye targets.
        var effect = camera.GetComponent<Il2CppBeautifyEffect.Beautify>();
        bool enabled = effect != null && effect.enabled;
        Behaviour postLayer=null;
        if(camera.name=="Slot Cam") {
            int id=camera.GetInstanceID();
            if(!_casinoPostLayers.TryGetValue(id,out postLayer)) {
                foreach(var component in camera.GetComponents<Component>())
                    if(component!=null && component.GetIl2CppType().Name=="PostProcessLayer") {
                        postLayer=component.TryCast<Behaviour>();break;
                    }
                _casinoPostLayers[id]=postLayer;
            }
        }
        bool postEnabled=postLayer!=null && postLayer.enabled;
        try {
            // Casino's desktop post stack smears tracked hands/world across eyes.
            // Bypass only for our render; preserve the native monitor effect.
            if(postEnabled)postLayer.enabled=false;
            if(enabled) {
                effect.enabled=false;
                if(_safeEffectCameras.Add(camera.GetInstanceID())) Log("[Codex] Bypassing Beautify during VR render: " + camera.name);
            }
            camera.Render();
        }
        finally {
            if(enabled && effect!=null) effect.enabled=true;
            if(postEnabled && postLayer!=null) postLayer.enabled=true;
        }
    }

    private void ReshapeArmVolume(ArmsModel model)
    {
        var mesh=model.Renderer.sharedMesh;
        var vertices=mesh.vertices; var weights=mesh.boneWeights; var bind=mesh.bindposes;
        var owner=new int[vertices.Length];
        var bounds=new Dictionary<int,Bounds>();
        for(int i=0;i<vertices.Length;i++) {
            var w=weights[i]; int b=w.boneIndex0; float max=w.weight0;
            if(w.weight1>max){b=w.boneIndex1;max=w.weight1;}
            if(w.weight2>max){b=w.boneIndex2;max=w.weight2;}
            if(w.weight3>max){b=w.boneIndex3;}
            owner[i]=b;
            var p=bind[b].MultiplyPoint3x4(vertices[i]);
            if(bounds.TryGetValue(b,out var box)) box.Encapsulate(p); else box=new Bounds(p,Vector3.zero);
            bounds[b]=box;
        }
        int changed=0;
        var seamPositions=new Dictionary<(int,int,int),Vector3>();
        for(int i=0;i<vertices.Length;i++) {
            int b=owner[i]; string name=model.BoneNames[b];
            var v=vertices[i]; var key=(Mathf.RoundToInt(v.x*100000),Mathf.RoundToInt(v.y*100000),Mathf.RoundToInt(v.z*100000));
            if(seamPositions.TryGetValue(key,out var previous)) { vertices[i]=previous; continue; }
            if(!(name.StartsWith("hand.") || name.StartsWith("f_") || name.StartsWith("thumb.") ||
                name.StartsWith("forearm.") || name.StartsWith("upper_arm."))) continue;
            var box=bounds[b]; var local=bind[b].MultiplyPoint3x4(vertices[i]);
            var rounded=ArmRefinement.RoundCrossSection(local,box.center,box.extents.x,box.extents.z,0.55f);
            vertices[i]=bind[b].inverse.MultiplyPoint3x4(rounded); seamPositions[key]=vertices[i]; changed++;
        }
        mesh.vertices=vertices; mesh.RecalculateBounds();
        Log("[Codex] Rounded palm/finger/arm cross-sections on " + changed + " source vertices before subdivision.");
    }

    private void AddArmOutline(ArmsModel model)
    {
        var shader=Shader.Find("Unlit/Color");
        if(shader==null) return;
        var source=model.Renderer.sharedMesh;
        var mesh=UnityEngine.Object.Instantiate(source);
        var vertices=mesh.vertices; var normals=mesh.normals; var triangles=mesh.triangles;
        for(int i=0;i<vertices.Length;i++) { vertices[i]+=normals[i]*0.0008f; normals[i]=-normals[i]; }
        for(int i=0;i<triangles.Length;i+=3) { int t=triangles[i+1];triangles[i+1]=triangles[i+2];triangles[i+2]=t; }
        mesh.vertices=vertices;mesh.normals=normals;mesh.triangles=triangles;
        var root=new GameObject("Codex_ArmInkOutline");
        root.transform.SetParent(model.Renderer.transform,false);
        _armOutline=root.AddComponent<SkinnedMeshRenderer>();
        _armOutline.sharedMesh=mesh;_armOutline.bones=model.Renderer.bones;_armOutline.rootBone=model.Renderer.rootBone;
        _armOutline.updateWhenOffscreen=true;_armOutline.localBounds=model.Renderer.localBounds;
        _armOutline.shadowCastingMode=ShadowCastingMode.Off;_armOutline.receiveShadows=false;
        _armOutline.material=new Material(shader){color=new Color(0.035f,0.03f,0.025f,1)};
    }
}
