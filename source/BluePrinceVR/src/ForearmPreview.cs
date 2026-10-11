using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
namespace BluePrinceVR;
public partial class VRPlugin
{
    private readonly GameObject[] _forearmPreview=new GameObject[2];
    private readonly List<UnityEngine.Object> _forearmAssets=new();
    private bool _forearmPreviewFailed;
    private void ForearmPreviewFrame()
    {
        if(_celHands==null || _handL==null || _handR==null || !_handL.Valid || !_handR.Valid)return;
        try {
            for(int side=0;side<2;side++) {
                if(_comfort.Forearms && !_forearmPreviewFailed && _forearmPreview[side]==null)BuildForearmPreview(side);
                if(_forearmPreview[side]!=null)_forearmPreview[side].SetActive(_comfort.Forearms&&!_forearmPreviewFailed);
            }
        }catch(Exception e){
            _forearmPreviewFailed=true;
            foreach(var go in _forearmPreview)if(go!=null)go.SetActive(false);
            LogWarn("[forearms] Preview disabled; hands retained: "+e.Message);
        }
    }
    private void BuildForearmPreview(int side)
    {
        var anchor=_celHands.Forearms[side];var renderer=_celHands.Renderer;
        var skin=renderer.sharedMesh.boneWeights;var bones=renderer.bones;
        var uv=renderer.sharedMesh.uv;
        var points=new List<Vector3>();var colors=new List<Color>();
        var baked=new Mesh();
        var sourceTexture=renderer.sharedMaterials[0].mainTexture.TryCast<Texture2D>();
        string suffix=side==0?"_l":"_r";
        try {
            renderer.BakeMesh(baked);var vertices=baked.vertices;
            // First half is the base surface; second half is its outline shell.
            for(int i=0;i<vertices.Length/2;i++) {
                var w=skin[i];int bone=w.boneIndex0;float weight=w.weight0;
                if(w.weight1>weight){bone=w.boneIndex1;weight=w.weight1;}
                if(w.weight2>weight){bone=w.boneIndex2;weight=w.weight2;}
                if(w.weight3>weight)bone=w.boneIndex3;
                if(!bones[bone].name.EndsWith(suffix))continue;
                points.Add(anchor.InverseTransformPoint(renderer.transform.TransformPoint(vertices[i])));
                colors.Add(sourceTexture==null?new Color(.78f,.59f,.48f):sourceTexture.GetPixelBilinear(uv[i].x,uv[i].y));
            }
        }finally{UnityEngine.Object.Destroy(baked);}
        if(points.Count<20)throw new InvalidOperationException("Wrist surface not found");
        float cut=float.PositiveInfinity;foreach(var p in points)cut=Mathf.Min(cut,p.z);
        float minX=float.PositiveInfinity,maxX=float.NegativeInfinity,minY=float.PositiveInfinity,maxY=float.NegativeInfinity;
        Color color=new Color(0,0,0,0);int samples=0;
        for(int i=0;i<points.Count;i++)if(points[i].z<=cut+.006f) {
            var p=points[i];minX=Mathf.Min(minX,p.x);maxX=Mathf.Max(maxX,p.x);minY=Mathf.Min(minY,p.y);maxY=Mathf.Max(maxY,p.y);
            color+=colors[i];samples++;
        }
        if(samples<6)throw new InvalidOperationException("Incomplete wrist cross section");
        color/=samples;color.a=1;
        float rx=Mathf.Clamp((maxX-minX)*.5f,.014f,.045f),ry=Mathf.Clamp((maxY-minY)*.5f,.010f,.035f);
        _forearmPreview[side]=new GameObject(side==0?"BPVR_ForearmPreviewL":"BPVR_ForearmPreviewR");
        var root=_forearmPreview[side].transform;root.SetParent(anchor,false);
        root.localPosition=new Vector3((minX+maxX)*.5f,(minY+maxY)*.5f,cut+.003f);
        Material MakeSurface(Color tint) {
            var texture=new Texture2D(1,1,TextureFormat.RGBA32,false);texture.SetPixel(0,0,tint);texture.Apply(false);
            var material=new Material(renderer.sharedMaterials[0]);material.mainTexture=texture;
            _forearmAssets.Add(texture);_forearmAssets.Add(material);return material;
        }
        var skinMaterial=MakeSurface(color);
        var cuffMaterial=MakeSurface(new Color(.86f,.85f,.79f));
        var outline=new Material(Shader.Find("Unlit/Color")){color=new Color(.035f,.033f,.038f)};_forearmAssets.Add(outline);
        // Wrist-attached visual preview only: no elbow joints, shoulder solver or colliders.
        MakeForearmSurface(root,"Tapered forearm",rx,ry,
            new[]{0f,-.025f,-.065f,-.105f,-.145f,-.17f},new[]{1f,1.04f,1.18f,1.34f,1.43f,1.39f},skinMaterial,outline);
        MakeForearmSurface(root,"Rolled ivory cuff",rx,ry,
            new[]{-.15f,-.154f,-.164f,-.176f,-.185f},new[]{1.43f,1.56f,1.60f,1.57f,1.46f},cuffMaterial,outline);
        Log("[forearms] Built short wrist-fitted preview for "+suffix+"; hands unchanged.");
    }
    private void MakeForearmSurface(Transform parent,string name,float rx,float ry,float[] z,float[] radii,Material material,Material outline)
    {
        const int segments=32;int rings=z.Length;
        var vertices=new Vector3[rings*segments+2];var triangles=new List<int>();
        for(int ring=0;ring<rings;ring++)for(int i=0;i<segments;i++) {
            float a=i*Mathf.PI*2/segments;
            vertices[ring*segments+i]=new Vector3(Mathf.Cos(a)*rx*radii[ring],Mathf.Sin(a)*ry*radii[ring],z[ring]);
            if(ring>0) {
                int p=(ring-1)*segments+i,q=(ring-1)*segments+(i+1)%segments,r=ring*segments+i,s=ring*segments+(i+1)%segments;
                triangles.AddRange(new[]{p,r,s,p,s,q});
            }
        }
        int front=rings*segments,back=front+1;
        vertices[front]=new Vector3(0,0,z[0]);vertices[back]=new Vector3(0,0,z[rings-1]);
        for(int i=0;i<segments;i++){int next=(i+1)%segments;triangles.AddRange(new[]{front,i,next,back,(rings-1)*segments+next,(rings-1)*segments+i});}
        var mesh=new Mesh{name=name,vertices=vertices,triangles=triangles.ToArray()};mesh.RecalculateNormals();mesh.RecalculateBounds();
        Add(mesh,material,name);
        var normal=mesh.normals;var shell=new Vector3[vertices.Length];for(int i=0;i<shell.Length;i++)shell[i]=vertices[i]+normal[i]*.0008f;
        for(int i=0;i<triangles.Count;i+=3){int tmp=triangles[i];triangles[i]=triangles[i+2];triangles[i+2]=tmp;}
        var shellMesh=new Mesh{name=name+" outline",vertices=shell,triangles=triangles.ToArray()};shellMesh.RecalculateBounds();Add(shellMesh,outline,name+" outline");
        void Add(Mesh mesh,Material mat,string label) {
            _forearmAssets.Add(mesh);var go=new GameObject(label);go.transform.SetParent(parent,false);
            go.AddComponent<MeshFilter>().sharedMesh=mesh;var r=go.AddComponent<MeshRenderer>();r.sharedMaterial=mat;
            r.shadowCastingMode=ShadowCastingMode.Off;r.receiveShadows=false;
        }
    }
    private void ReleaseForearmPreview()
    {
        foreach(var asset in _forearmAssets)if(asset!=null)UnityEngine.Object.Destroy(asset);
        _forearmAssets.Clear();
    }
}
