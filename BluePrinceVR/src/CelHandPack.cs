using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.Json;
using UnityEngine;
using UnityEngine.Rendering;
namespace BluePrinceVR;

// Narrow GLB reader for the user-supplied, embedded, uncompressed hand pack.
// Unsupported layouts fail closed to the existing hands; never fetch external URIs.
internal sealed class CelHandPack
{
    internal const float PalmLength=.1404f; // 30% larger than the previous .108 m reference.
    internal GameObject Root;
    internal SkinnedMeshRenderer Renderer;
    internal Transform[] Wrists=new Transform[2],Forearms=new Transform[2];
    internal Transform[] ToolAnchors=new Transform[2];
    private Transform[] _nodes;
    private string[] _names;
    private Quaternion[] _rest;
    private readonly Dictionary<string,Quaternion[]> _poses=new();
    private readonly Quaternion[] _alignment=new Quaternion[2];
    private float _scale;
    private readonly Transform[,] _thumbJoints=new Transform[2,3];
    private readonly Transform[,] _recorderFingerEnds=new Transform[2,4];
    private readonly Transform[,] _recorderOuterBases=new Transform[2,4];
    private readonly Vector3[] _thumbTips=new Vector3[2];
    internal static CelHandPack Load(string path,Shader shader)
    {
        var result=new CelHandPack();
        try {result.Read(path,shader);return result;}
        catch {if(result.Root!=null)UnityEngine.Object.Destroy(result.Root);throw;}
    }
    private void Read(string path,Shader shader)
    {
        using var f=new BinaryReader(File.OpenRead(path));
        if(f.ReadUInt32()!=0x46546C67 || f.ReadInt32()!=2)throw new InvalidDataException("GLB v2 required");
        int total=f.ReadInt32(),jsonLength=f.ReadInt32();
        if(total!=f.BaseStream.Length || jsonLength<2 || jsonLength>8000000 || f.ReadUInt32()!=0x4E4F534A)throw new InvalidDataException("Invalid GLB header");
        using var document=JsonDocument.Parse(f.ReadBytes(jsonLength));var doc=document.RootElement;
        int binLength=f.ReadInt32();if(binLength<0||binLength>32000000||f.ReadUInt32()!=0x004E4942)throw new InvalidDataException("Embedded binary required");
        var bytes=f.ReadBytes(binLength);if(bytes.Length!=binLength)throw new InvalidDataException("Truncated GLB");
        int Int(JsonElement e,string k,int fallback=0)=>e.TryGetProperty(k,out var v)?v.GetInt32():fallback;
        float[] Access(int index) {
            var a=doc.GetProperty("accessors")[index];if(a.TryGetProperty("sparse",out _))throw new InvalidDataException("Sparse accessor not supported");
            var view=doc.GetProperty("bufferViews")[a.GetProperty("bufferView").GetInt32()];
            int type=Int(a,"componentType"),count=Int(a,"count");
            int components=a.GetProperty("type").GetString() switch {"SCALAR"=>1,"VEC2"=>2,"VEC3"=>3,"VEC4"=>4,"MAT4"=>16,_=>throw new InvalidDataException("Accessor shape")};
            int size=type==5126||type==5125?4:type==5123?2:type==5121?1:throw new InvalidDataException("Accessor component");
            int start=Int(view,"byteOffset")+Int(a,"byteOffset"),stride=Int(view,"byteStride",size*components);
            if(count<0||count>1000000||start<0||stride<size*components||(long)start+(long)Math.Max(0,count-1)*stride+size*components>bytes.Length)throw new InvalidDataException("Accessor bounds");
            var data=new float[count*components];
            for(int i=0;i<count;i++)for(int c=0;c<components;c++) {
                int p=start+i*stride+c*size;
                data[i*components+c]=type switch {5126=>BitConverter.ToSingle(bytes,p),5125=>BitConverter.ToUInt32(bytes,p),5123=>BitConverter.ToUInt16(bytes,p),_=>bytes[p]};
                if(!float.IsFinite(data[i*components+c]))throw new InvalidDataException("Non-finite accessor");
            }
            return data;
        }
        float[] Values(JsonElement node,string name,float[] fallback)=>node.TryGetProperty(name,out var v)?v.EnumerateArray().Select(x=>x.GetSingle()).ToArray():fallback;
        Quaternion Rotation(float[] q,int offset=0)=>new Quaternion(-q[offset],-q[offset+1],q[offset+2],q[offset+3]);
        var nodes=doc.GetProperty("nodes");
        Root=new GameObject("BPVR_CelHandsPack");UnityEngine.Object.DontDestroyOnLoad(Root);
        _nodes=new Transform[nodes.GetArrayLength()];_names=new string[_nodes.Length];_rest=new Quaternion[_nodes.Length];
        for(int i=0;i<_nodes.Length;i++) {
            _names[i]=nodes[i].TryGetProperty("name",out var name)?name.GetString():"node"+i;
            _nodes[i]=new GameObject(_names[i]).transform;_nodes[i].SetParent(Root.transform,false);
        }
        for(int i=0;i<_nodes.Length;i++)if(nodes[i].TryGetProperty("children",out var children))foreach(var child in children.EnumerateArray())_nodes[child.GetInt32()].SetParent(_nodes[i],false);
        for(int i=0;i<_nodes.Length;i++) {
            if(nodes[i].TryGetProperty("matrix",out _))throw new InvalidDataException("Pack requires TRS nodes");
            var p=Values(nodes[i],"translation",new[]{0f,0f,0f});var q=Values(nodes[i],"rotation",new[]{0f,0f,0f,1f});var s=Values(nodes[i],"scale",new[]{1f,1f,1f});
            _nodes[i].localPosition=new Vector3(p[0],p[1],-p[2]);_nodes[i].localRotation=Rotation(q);_nodes[i].localScale=new Vector3(s[0],s[1],s[2]);_rest[i]=_nodes[i].localRotation;
        }
        Transform Find(string name)=>_nodes[Array.IndexOf(_names,name)];
        Wrists[0]=Find("hand_l");Wrists[1]=Find("hand_r");
        var recorderFingers=new[]{"index","middle","ring","pinky"};
        for(int side=0;side<2;side++) {
            _recorderOuterBases[side,0]=Find("index_01"+(side==0?"_l":"_r"));
            _recorderOuterBases[side,1]=Find("pinky_01"+(side==0?"_l":"_r"));
            _recorderOuterBases[side,2]=Find("middle_01"+(side==0?"_l":"_r"));
            _recorderOuterBases[side,3]=Find("ring_01"+(side==0?"_l":"_r"));
        }
        for(int side=0;side<2;side++)for(int finger=0;finger<4;finger++)
            _recorderFingerEnds[side,finger]=Find(recorderFingers[finger]+"_03"+(side==0?"_l":"_r"));
        for(int side=0;side<2;side++)for(int joint=0;joint<3;joint++)
            _thumbJoints[side,joint]=Find("thumb_0"+(joint+1)+(side==0?"_l":"_r"));
        for(int side=0;side<2;side++) {
            string suffix=side==0?"_l":"_r";var wrist=Wrists[side];
            var direction=(Find("middle_01"+suffix).position-wrist.position).normalized;
            var across=(Find("pinky_01"+suffix).position-Find("index_01"+suffix).position).normalized;
            // Anatomical dorsal normal: palm faces down when the controller is level.
            var normal=Vector3.Cross(direction,across).normalized*(side==0?-1f:1f);
            _alignment[side]=Quaternion.Inverse(Quaternion.LookRotation(direction,normal))*wrist.rotation;
            Forearms[side]=new GameObject("BPVR_HandPackWristAnchor").transform;Forearms[side].SetParent(Root.transform,false);
            ToolAnchors[side]=new GameObject("BPVR_HandPackToolAnchor").transform;ToolAnchors[side].SetParent(Root.transform,false);
        }
        // Compact wrist-to-knuckle length, rather than scaling to adult palms.
        _scale=Mathf.Clamp(PalmLength/Vector3.Distance(Find("middle_01_r").position,Wrists[1].position),.25f,1.4f);
        int meshNode=Enumerable.Range(0,_nodes.Length).First(i=>nodes[i].TryGetProperty("mesh",out _));
        var skin=doc.GetProperty("skins")[Int(nodes[meshNode],"skin")];var joints=skin.GetProperty("joints").EnumerateArray().Select(j=>_nodes[j.GetInt32()]).ToArray();
        var primitives=doc.GetProperty("meshes")[Int(nodes[meshNode],"mesh")].GetProperty("primitives");
        if(primitives.GetArrayLength()!=2)throw new InvalidDataException("Expected base and outline primitives");
        var vertices=new List<Vector3>();var normals=new List<Vector3>();var uvs=new List<Vector2>();var weights=new List<BoneWeight>();var triangles=new List<int[]>();
        foreach(var primitive in primitives.EnumerateArray()) {
            if(Int(primitive,"mode",4)!=4)throw new InvalidDataException("Triangle mesh required");
            var at=primitive.GetProperty("attributes");var p=Access(Int(at,"POSITION"));var n=Access(Int(at,"NORMAL"));var uv=Access(Int(at,"TEXCOORD_0"));var ji=Access(Int(at,"JOINTS_0"));var w=Access(Int(at,"WEIGHTS_0"));
            int first=vertices.Count,count=p.Length/3;
            if(n.Length!=p.Length||uv.Length!=count*2||ji.Length!=count*4||w.Length!=count*4)throw new InvalidDataException("Attribute count mismatch");
            for(int i=0;i<count;i++) {
                vertices.Add(new Vector3(p[i*3],p[i*3+1],-p[i*3+2]));normals.Add(new Vector3(n[i*3],n[i*3+1],-n[i*3+2]));uvs.Add(new Vector2(uv[i*2],1-uv[i*2+1]));
                int k=i*4;float sum=w[k]+w[k+1]+w[k+2]+w[k+3];
                if(sum<.001f||Enumerable.Range(k,4).Any(j=>ji[j]<0||ji[j]>=joints.Length||w[j]<0))throw new InvalidDataException("Skin weight/joint invalid");
                weights.Add(new BoneWeight{boneIndex0=(int)ji[k],boneIndex1=(int)ji[k+1],boneIndex2=(int)ji[k+2],boneIndex3=(int)ji[k+3],weight0=w[k]/sum,weight1=w[k+1]/sum,weight2=w[k+2]/sum,weight3=w[k+3]/sum});
            }
            var indices=Access(Int(primitive,"indices")).Select(x=>(int)x).ToArray();if(indices.Length%3!=0||indices.Any(x=>x<0||x>=count))throw new InvalidDataException("Indices invalid");
            for(int i=0;i<indices.Length;i+=3){int tmp=indices[i+1];indices[i+1]=indices[i+2];indices[i+2]=tmp;}
            triangles.Add(indices.Select(x=>x+first).ToArray());
        }
        // Derive actual thumb pad endpoints from skin weights, not the last
        // bone's axis (this pack's distal thumb axis is not the fingertip axis).
        for(int side=0;side<2;side++) {
            var end=_thumbJoints[side,2];int bone=Array.IndexOf(joints,end);
            var points=new List<Vector3>();float farthest=0;
            for(int i=0;i<vertices.Count;i++) {
                var w=weights[i];float influence=(w.boneIndex0==bone?w.weight0:0)+(w.boneIndex1==bone?w.weight1:0)+(w.boneIndex2==bone?w.weight2:0)+(w.boneIndex3==bone?w.weight3:0);
                if(influence<.6f)continue;
                var p=end.InverseTransformPoint(_nodes[meshNode].TransformPoint(vertices[i]));points.Add(p);farthest=Mathf.Max(farthest,p.magnitude);
            }
            int count=0;foreach(var p in points)if(p.magnitude>=farthest*.9f){_thumbTips[side]+=p;count++;}
            if(count>0)_thumbTips[side]/=count;
        }
        var mesh=new Mesh{name="Supplied cel hands",indexFormat=IndexFormat.UInt32};mesh.vertices=vertices.ToArray();mesh.normals=normals.ToArray();mesh.uv=uvs.ToArray();mesh.boneWeights=weights.ToArray();
        mesh.bindposes=joints.Select(j=>j.worldToLocalMatrix*_nodes[meshNode].localToWorldMatrix).ToArray();mesh.subMeshCount=2;
        for(int i=0;i<2;i++)mesh.SetTriangles(triangles[i],i);mesh.RecalculateBounds();
        Renderer=_nodes[meshNode].gameObject.AddComponent<SkinnedMeshRenderer>();Renderer.sharedMesh=mesh;Renderer.bones=joints;Renderer.rootBone=Root.transform;Renderer.updateWhenOffscreen=true;Renderer.localBounds=new Bounds(Vector3.zero,Vector3.one*10);
        var image=doc.GetProperty("images")[0];var viewImage=doc.GetProperty("bufferViews")[Int(image,"bufferView")];int offset=Int(viewImage,"byteOffset"),length=Int(viewImage,"byteLength");
        var texture=new Texture2D(2,2,TextureFormat.RGBA32,false);if(!ImageConversion.LoadImage(texture,bytes.Skip(offset).Take(length).ToArray()))throw new InvalidDataException("Texture decode failed");
        var body=new Material(shader){mainTexture=texture};ArmRefinement.MakeMatte(body);
        var outline=new Material(Shader.Find("Unlit/Color")){color=new Color(.025f,.022f,.03f,1)};
        Renderer.materials=new[]{body,outline};Renderer.shadowCastingMode=ShadowCastingMode.Off;Renderer.receiveShadows=false;
        foreach(var animation in doc.GetProperty("animations").EnumerateArray()) {
            var pose=(Quaternion[])_rest.Clone();var samplers=animation.GetProperty("samplers");
            foreach(var channel in animation.GetProperty("channels").EnumerateArray()) {
                var target=channel.GetProperty("target");if(target.GetProperty("path").GetString()!="rotation")continue;
                var sampler=samplers[Int(channel,"sampler")];var rotations=Access(Int(sampler,"output"));pose[Int(target,"node")]=Rotation(rotations,rotations.Length-4);
            }
            _poses[animation.GetProperty("name").GetString()]=pose;
        }
        foreach(string name in new[]{"PoseNeutral","PoseFist","PosePoint","PoseThumbsUp","PoseSpread"})if(!_poses.ContainsKey(name))throw new InvalidDataException("Required pose missing: "+name);
    }
    internal Vector3 IndexTip(bool left)
    {
        string suffix=left?"_l":"_r";
        var end=_nodes[Array.IndexOf(_names,"index_03"+suffix)];
        var previous=_nodes[Array.IndexOf(_names,"index_02"+suffix)];
        return end.position+(end.position-previous.position).normalized*.014f;
    }
    internal void Dispose()
    {
        if(Renderer!=null) {
            foreach(var material in Renderer.sharedMaterials)if(material!=null) {
                if(material.mainTexture!=null)UnityEngine.Object.Destroy(material.mainTexture);
                UnityEngine.Object.Destroy(material);
            }
            if(Renderer.sharedMesh!=null)UnityEngine.Object.Destroy(Renderer.sharedMesh);
        }
        if(Root!=null)UnityEngine.Object.Destroy(Root);
    }
    internal Vector3 PalmLocal(bool left)
    {
        int side=left?0:1;
        return Quaternion.Inverse(_alignment[side])*Vector3.down;
    }
    // Recorder back rests on the palm; microphone sits beside the thumb.
    internal void PlaceRecorder(Transform recorder,bool left)
    {
        int side=left?0:1;
        var wrist=Wrists[side];
        var frame=wrist.rotation*Quaternion.Inverse(_alignment[side]);
        // Follow the diagonal knuckle row (index higher than pinky), placing
        // the narrow casing INSIDE the curled fingers rather than below them.
        float angle=18f*Mathf.Deg2Rad;
        var longAxis=new Vector3((left?1f:-1f)*Mathf.Cos(angle),0,Mathf.Sin(angle));
        recorder.SetPositionAndRotation(wrist.position+frame*new Vector3(left?.010f:-.010f,-.037f,.122f),
            Quaternion.LookRotation(frame*Vector3.up,frame*longAxis));
        recorder.localScale=new Vector3(.85f,1.5f,1f);
    }
    // The recorder's record key is a short side-mounted cap, not a button
    // standing above the top edge.  Keep it below the casing's scaled top so
    // the thumb lands on the side switch shown in the reference photos.
    internal static Vector3 RecorderButtonPosition(bool left,float press) =>
        new Vector3((left?-1f:1f)*(.029f-.003f*Mathf.Clamp01(press)),.030f,-.006f);
    internal static Vector3 RecorderButtonNormal(bool left) => left?Vector3.left:Vector3.right;
    internal void RestThumbOnRecorder(Transform recorder,bool left,float press)
    {
        int side=left?0:1;
        // Opposing grip: four fingers wrap the far long edge; thumb folds
        // inward over the near long edge. Never use free distal-joint CCD.
        // LookRotation makes local X point toward the wrist on the right hand
        // (toward the fingers on the left). Button and contact share one layout.
        var target=recorder.TransformPoint(RecorderButtonPosition(left,press)+RecorderButtonNormal(left)*.004f);
        var end=_thumbJoints[side,2];
        var tipLocal=_thumbTips[side];
        for(int joint=0;joint<3;joint++) {
            var bone=_thumbJoints[side,joint];int index=Array.IndexOf(_nodes,bone);
            bone.localRotation=Quaternion.Slerp(_poses["PoseHoldFlashlight"][index],_poses["PoseFist"][index],.65f);
        }
        var anatomical=Wrists[side].rotation*Quaternion.Inverse(_alignment[side]);
        // Mild authored flexion, not the previous 104-degree extra fold.
        // Release slightly as the attached cap travels inward under the pad.
        var curl=Quaternion.AngleAxis(Mathf.Lerp(26f,23f,Mathf.Clamp01(press)),anatomical*Vector3.right);
        // Fixed, positive palmward flexion on a shared anatomical hinge axis;
        // not a target-derived rotation that can bend backward or add roll.
        for(int joint=1;joint<3;joint++)_thumbJoints[side,joint].rotation=curl*_thumbJoints[side,joint].rotation;
        var thumbBase=_thumbJoints[side,0];
        var from=end.TransformPoint(tipLocal)-thumbBase.position;var to=target-thumbBase.position;
        if(from.sqrMagnitude<.000001f || to.sqrMagnitude<.000001f)return;
        var delta=Quaternion.FromToRotation(from,to);
        thumbBase.rotation=Quaternion.RotateTowards(Quaternion.identity,delta,65f)*thumbBase.rotation;
    }
    internal void Pose(bool left,Vector3 controllerPosition,Quaternion controllerRotation,float grip,float trigger,float thumb,bool pickup,float size=1f,float wristOffset=.055f,bool pointReady=false,bool recorderHold=false,bool computerTyping=false)
    {
        int side=left?0:1;string suffix=left?"_l":"_r";
        for(int i=0;i<_nodes.Length;i++) {
            string name=_names[i];if(!name.EndsWith(suffix)||name=="hand"+suffix)continue;
            if(recorderHold && _poses.TryGetValue("PoseHoldFlashlight",out var hold)) {
                // Close the flashlight pose's hovering index around the casing.
                float wrap=name.StartsWith("index")?.25f:name.StartsWith("thumb")?.65f:0f;
                _nodes[i].localRotation=Quaternion.Slerp(hold[i],_poses["PoseFist"][i],wrap);
                continue; // Recording trigger must not open the supporting fingers.
            }
            var relaxed=_poses[pickup?"PoseSpread":pointReady?"PosePoint":"PoseNeutral"][i];
            if(computerTyping&&name.StartsWith("index_")) {
                // Computer mode needs a hooked, old-school typing finger,
                // not the fully extended interaction pointer. Bend the MCP
                // lightly and the two distal joints progressively more.
                float curl=name.StartsWith("index_01")?.10f:name.StartsWith("index_02")?.24f:.34f;
                relaxed=Quaternion.Slerp(relaxed,_poses["PoseFist"][i],curl);
            }
            float amount=name.StartsWith("index")?trigger:grip;
            var closed=_poses[name.StartsWith("thumb")&&thumb<.2f?"PoseThumbsUp":"PoseFist"][i];
            _nodes[i].localRotation=Quaternion.Slerp(relaxed,closed,Mathf.Clamp01(amount));
        }
        var wrist=Wrists[side];wrist.localScale=Vector3.one*(_scale*size);
        var handFrame=controllerRotation*Quaternion.AngleAxis(left?90f:-90f,Vector3.forward);
        wrist.SetPositionAndRotation(controllerPosition-controllerRotation*Vector3.forward*wristOffset,handFrame*_alignment[side]);
        if(recorderHold) {
            // Graduated lift across the finger row over the wider casing.
            // Pose above resets these joints every frame; rotations never accumulate.
            var axis=handFrame*Vector3.right;
            _recorderOuterBases[side,0].rotation=Quaternion.AngleAxis(-18f,axis)*_recorderOuterBases[side,0].rotation;
            _recorderOuterBases[side,1].rotation=Quaternion.AngleAxis(-24f,axis)*_recorderOuterBases[side,1].rotation;
            _recorderOuterBases[side,2].rotation=Quaternion.AngleAxis(-16f,axis)*_recorderOuterBases[side,2].rotation;
            _recorderOuterBases[side,3].rotation=Quaternion.AngleAxis(-20f,axis)*_recorderOuterBases[side,3].rotation;
            var wrap=Quaternion.AngleAxis(12f,handFrame*Vector3.right);
            for(int finger=0;finger<4;finger++) {
                var end=_recorderFingerEnds[side,finger];end.rotation=wrap*end.rotation;
            }
        }
        Forearms[side].SetPositionAndRotation(wrist.position-controllerRotation*Vector3.forward*.08f,handFrame);
        ToolAnchors[side].SetPositionAndRotation(wrist.position,controllerRotation*Quaternion.Euler(HandRig.HandEuler));
    }
}
