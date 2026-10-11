using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
namespace BluePrinceVR;
public partial class VRPlugin
{
    private void BatchMemoShell(Transform root)
    {
        // Once per construction, merge stationary primitive details by color.
        // Reels, REC lamp, moving button and text retain independent renderers.
        var groups=new Dictionary<Color,List<MeshFilter>>();
        foreach(var filter in root.GetComponentsInChildren<MeshFilter>()) {
            if(filter.transform.parent!=root || filter.transform==_memoRecordButton || filter.gameObject==_memoLamp.gameObject)continue;
            var renderer=filter.GetComponent<MeshRenderer>();
            if(renderer==null || filter.sharedMesh==null || renderer.sharedMaterial==null || renderer.sharedMaterial.shader.name!="Unlit/Color")continue;
            Color color=renderer.sharedMaterial.color;
            if(!groups.TryGetValue(color,out var list))groups[color]=list=new List<MeshFilter>();
            list.Add(filter);
        }
        int removed=0;
        foreach(var pair in groups) {
            if(pair.Value.Count<2)continue;
            var combine=new CombineInstance[pair.Value.Count];
            for(int i=0;i<combine.Length;i++)combine[i]=new CombineInstance{mesh=pair.Value[i].sharedMesh,transform=root.worldToLocalMatrix*pair.Value[i].transform.localToWorldMatrix};
            var mesh=new Mesh{name="Memo static shell batch",indexFormat=IndexFormat.UInt32};
            mesh.CombineMeshes(combine,true,true);
            var go=new GameObject("Memo shell color batch");go.transform.SetParent(root,false);
            go.AddComponent<MeshFilter>().sharedMesh=mesh;
            var renderer=go.AddComponent<MeshRenderer>();renderer.sharedMaterial=pair.Value[0].GetComponent<MeshRenderer>().sharedMaterial;
            renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
            foreach(var filter in pair.Value) {
                var old=filter.GetComponent<MeshRenderer>();old.enabled=false;
                if(old.sharedMaterial!=renderer.sharedMaterial)UnityEngine.Object.Destroy(old.sharedMaterial);
                UnityEngine.Object.Destroy(filter.gameObject);
            }
            removed+=combine.Length-1;
        }
        Log("[memo] Static recorder batching removed "+removed+" renderers; animated parts preserved.");
    }
}
