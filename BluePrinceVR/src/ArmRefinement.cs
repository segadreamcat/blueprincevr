using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

namespace BluePrinceVR;

// Bounded subdivision at load time, not a per-frame mesh rebuild.
internal static class ArmRefinement
{
    private sealed class Edge
    {
        public int A, B;
        public readonly List<int> Opposite = new();
    }
    private static (int,int) Key(int a,int b) => a<b ? (a,b) : (b,a);
    private static (int,int,int) PositionKey(Vector3 p) => (Mathf.RoundToInt(p.x*100000),Mathf.RoundToInt(p.y*100000),Mathf.RoundToInt(p.z*100000));

    internal static void Apply(SkinnedMeshRenderer renderer)
    {
        var mesh = renderer.sharedMesh;
        var source = mesh.vertices; var sourceUv = mesh.uv; var sourceWeights=mesh.boneWeights; var triangles=mesh.triangles;
        if(source.Length>50000 || source.Length==0 || triangles.Length%3!=0) throw new InvalidOperationException("Unexpected mesh size or topology");
        var welded = new Dictionary<(int,int,int),int>();
        var positions = new List<Vector3>(); var map = new int[source.Length];
        for(int i=0;i<source.Length;i++)
        {
            var key=PositionKey(source[i]);
            if(!welded.TryGetValue(key,out int index)) { index=positions.Count; welded.Add(key,index); positions.Add(source[i]); }
            map[i]=index;
        }
        var edges = new Dictionary<(int,int),Edge>();
        var neighbours = positions.Select(_=>new HashSet<int>()).ToArray();
        void EdgeFor(int a,int b,int opposite)
        {
            if(a==b) return;
            var key=Key(a,b);
            if(!edges.TryGetValue(key,out var edge)) { edge=new Edge{A=a,B=b}; edges.Add(key,edge); }
            edge.Opposite.Add(opposite); neighbours[a].Add(b); neighbours[b].Add(a);
        }
        for(int i=0;i<triangles.Length;i+=3)
        {
            int a=map[triangles[i]], b=map[triangles[i+1]], c=map[triangles[i+2]];
            EdgeFor(a,b,c); EdgeFor(b,c,a); EdgeFor(c,a,b);
        }
        var boundary=positions.Select(_=>new List<int>()).ToArray();
        foreach(var edge in edges.Values) if(edge.Opposite.Count==1) { boundary[edge.A].Add(edge.B); boundary[edge.B].Add(edge.A); }
        var smooth=new Vector3[positions.Count];
        for(int i=0;i<positions.Count;i++)
        {
            if(boundary[i].Count==2) smooth[i]=positions[i]*0.75f+(positions[boundary[i][0]]+positions[boundary[i][1]])*0.125f;
            else if(boundary[i].Count!=0 || neighbours[i].Count<3) smooth[i]=positions[i];
            else
            {
                float beta=neighbours[i].Count==3 ? 3f/16f : 3f/(8f*neighbours[i].Count);
                smooth[i]=positions[i]*(1-beta*neighbours[i].Count);
                foreach(int n in neighbours[i]) smooth[i]+=positions[n]*beta;
            }
        }
        var verts=new List<Vector3>(); var uvs=new List<Vector2>(); var weights=new List<BoneWeight>();
        for(int i=0;i<source.Length;i++) { verts.Add(smooth[map[i]]); uvs.Add(sourceUv[i]); weights.Add(sourceWeights[i]); }
        var split=new Dictionary<(int,int),int>();
        int Mid(int a,int b)
        {
            var key=Key(a,b);
            if(split.TryGetValue(key,out int index)) return index;
            var pa=positions[map[a]]; var pb=positions[map[b]];
            Vector3 point=(pa+pb)*0.5f;
            if(edges.TryGetValue(Key(map[a],map[b]),out var e) && e.Opposite.Count==2)
                point=(pa+pb)*0.375f+(positions[e.Opposite[0]]+positions[e.Opposite[1]])*0.125f;
            index=verts.Count; split.Add(key,index); verts.Add(point); uvs.Add((sourceUv[a]+sourceUv[b])*0.5f);
            weights.Add(Blend(sourceWeights[a],sourceWeights[b])); return index;
        }
        var tris=new List<int>(triangles.Length*4);
        for(int i=0;i<triangles.Length;i+=3)
        {
            int a=triangles[i],b=triangles[i+1],c=triangles[i+2],ab=Mid(a,b),bc=Mid(b,c),ca=Mid(c,a);
            tris.AddRange(new[]{a,ab,ca,ab,b,bc,ca,bc,c,ab,bc,ca});
        }
        var normalSums=new Dictionary<(int,int,int),Vector3>();
        for(int i=0;i<tris.Count;i+=3)
        {
            var n=Vector3.Cross(verts[tris[i+1]]-verts[tris[i]],verts[tris[i+2]]-verts[tris[i]]);
            for(int j=0;j<3;j++) { var key=PositionKey(verts[tris[i+j]]); normalSums.TryGetValue(key,out var old); normalSums[key]=old+n; }
        }
        var result=new Mesh { name="Codex_RoundedArms", indexFormat=IndexFormat.UInt32 };
        result.vertices=verts.ToArray(); result.uv=uvs.ToArray(); result.triangles=tris.ToArray();
        result.normals=verts.Select(v=>normalSums.TryGetValue(PositionKey(v),out var n) ? n.normalized : Vector3.up).ToArray();
        result.boneWeights=weights.ToArray(); result.bindposes=mesh.bindposes; result.RecalculateBounds();
        renderer.sharedMesh=result;
    }

    private static BoneWeight Blend(BoneWeight a,BoneWeight b)
    {
        var weights=new Dictionary<int,float>();
        void Add(int bone,float weight) { weights.TryGetValue(bone,out float old); weights[bone]=old+weight*0.5f; }
        Add(a.boneIndex0,a.weight0); Add(a.boneIndex1,a.weight1); Add(a.boneIndex2,a.weight2); Add(a.boneIndex3,a.weight3);
        Add(b.boneIndex0,b.weight0); Add(b.boneIndex1,b.weight1); Add(b.boneIndex2,b.weight2); Add(b.boneIndex3,b.weight3);
        var top=weights.OrderByDescending(p=>p.Value).Take(4).ToArray();
        float sum=top.Sum(p=>p.Value); var ids=new int[4]; var values=new float[4];
        for(int i=0;i<top.Length;i++) { ids[i]=top[i].Key; values[i]=top[i].Value/Mathf.Max(sum,0.00001f); }
        return new BoneWeight { boneIndex0=ids[0],boneIndex1=ids[1],boneIndex2=ids[2],boneIndex3=ids[3],weight0=values[0],weight1=values[1],weight2=values[2],weight3=values[3] };
    }

    internal static void ApplyBakedPalette(SkinnedMeshRenderer renderer)
    {
        // Use a regular mesh shader, not Sprites/Default (which needs sprite state).
        // A tiny three-band ramp produces cel shading with no custom shader bundle.
        // The ramp supplies art-directed bands; scene lighting now modulates them.
        var shader=Shader.Find("Standard") ?? Shader.Find("Diffuse");
        if(shader==null) throw new InvalidOperationException("Lit mesh shader unavailable for matte cel arms");
        var texture=renderer.sharedMaterial.mainTexture.TryCast<Texture2D>();
        if(texture==null) throw new InvalidOperationException("Arm source texture unavailable");
        var mesh=renderer.sharedMesh; var sourceUv=mesh.uv; var sourceNormals=mesh.normals;
        var sourceVertices=mesh.vertices; var sourceWeights=mesh.boneWeights; var sourceTriangles=mesh.triangles;
        var vertices=new Vector3[sourceTriangles.Length]; var normals=new Vector3[vertices.Length];
        var weights=new BoneWeight[vertices.Length]; var uv=new Vector2[vertices.Length];
        var triangles=new int[vertices.Length];
        var light=new Vector3(-0.35f,0.8f,0.45f).normalized;
        var ramp=new Texture2D(96,12,TextureFormat.RGBA32,false);
        ramp.name="Codex_Skin_Ivory_CelRamp";
        var baseColors=new[]{new Color(0.76f,0.57f,0.47f),new Color(0.78f,0.76f,0.68f),new Color(0.12f,0.13f,0.15f)};
        for(int row=0;row<12;row++) for(int x=0;x<96;x++)
        {
            float band=x<45 ? 0.66f : x<75 ? 0.84f : 1f;
            var c=baseColors[row/4];
            ramp.SetPixel(x,row,new Color(c.r*band,c.g*band,c.b*band,1));
        }
        ramp.wrapMode=TextureWrapMode.Clamp; ramp.filterMode=FilterMode.Point;
        ramp.Apply(false);
        for(int t=0;t<sourceTriangles.Length;t+=3)
        {
            var center=(sourceUv[sourceTriangles[t]]+sourceUv[sourceTriangles[t+1]]+sourceUv[sourceTriangles[t+2]])*(1f/3f);
            var original=texture.GetPixelBilinear(center.x,center.y);
            int row=PaletteRow(original);
            for(int j=0;j<3;j++)
            {
                int dst=t+j, src=sourceTriangles[dst];
                vertices[dst]=sourceVertices[src]; normals[dst]=sourceNormals[src]; weights[dst]=sourceWeights[src];
                float shade=Math.Clamp(Vector3.Dot(normals[dst],light)*0.5f+0.5f,0f,1f);
                uv[dst]=new Vector2((0.5f+shade*95f)/96f,(row*4+2f)/12f);
                triangles[dst]=dst;
            }
        }
        var result=new Mesh { name="Codex_CelArms", indexFormat=IndexFormat.UInt32 };
        result.vertices=vertices; result.normals=normals; result.uv=uv; result.triangles=triangles;
        result.boneWeights=weights; result.bindposes=mesh.bindposes; result.RecalculateBounds();
        var material=new Material(shader); material.mainTexture=ramp; material.renderQueue=2000;
        MakeMatte(material);
        renderer.sharedMesh=result; renderer.material=material;
    }

    internal static void MakeMatte(Material material)
    {
        foreach(var property in new[]{"_Glossiness","_Metallic","_SpecularHighlights","_GlossyReflections"})
            if(material.HasProperty(property)) material.SetFloat(property,0f);
        if(material.HasProperty("_EmissionColor")) material.SetColor("_EmissionColor",new Color(0,0,0,1));
        material.DisableKeyword("_EMISSION");
        material.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");
        material.EnableKeyword("_GLOSSYREFLECTIONS_OFF");
    }

    internal static int PaletteRow(Color original)
    {
        if(Mathf.Max(original.r,Mathf.Max(original.g,original.b))<0.22f) return 2;
        return original.r-original.g>0.035f ? 0 : 1;
    }

    internal static Vector3 RoundCrossSection(Vector3 p,Vector3 center,float radiusX,float radiusZ,float strength)
    {
        if(radiusX<0.00001f || radiusZ<0.00001f) return p;
        float x=p.x-center.x,z=p.z-center.z;
        float nx=Math.Clamp(x/radiusX,-1,1),nz=Math.Clamp(z/radiusZ,-1,1);
        float sx=MathF.Sqrt(1f-0.5f*nz*nz),sz=MathF.Sqrt(1f-0.5f*nx*nx);
        // Round a rectangular section toward an ellipse without shortening bones.
        return new Vector3(center.x+x*(1-strength+strength*sx),p.y,center.z+z*(1-strength+strength*sz));
    }
}
