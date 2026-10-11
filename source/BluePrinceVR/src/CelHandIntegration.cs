using System;
using System.IO;
using UnityEngine;
namespace BluePrinceVR;
public partial class VRPlugin
{
    private CelHandPack _celHands;
    private GameObject _rightWristband;
    private void BuildRightWristband()
    {
        if(_rightWristband!=null || _celHands==null)return;
        var anchor=_celHands.Forearms[1];
        var center=_celHands.Wrists[1].position+anchor.forward*.012f;
        float rx=.025f,ry=.016f;
        var mesh=new Mesh();
        try {
            _celHands.Renderer.BakeMesh(mesh);
            float minX=float.PositiveInfinity,maxX=float.NegativeInfinity,minY=float.PositiveInfinity,maxY=float.NegativeInfinity;int count=0;
            foreach(var v in mesh.vertices) {
                var d=_celHands.Renderer.transform.TransformPoint(v)-center;
                if(Mathf.Abs(Vector3.Dot(d,anchor.forward))>.008f || d.sqrMagnitude>.0064f)continue;
                float x=Vector3.Dot(d,anchor.right),y=Vector3.Dot(d,anchor.up);
                minX=Mathf.Min(minX,x);maxX=Mathf.Max(maxX,x);minY=Mathf.Min(minY,y);maxY=Mathf.Max(maxY,y);count++;
            }
            if(count>=8) {
                rx=Mathf.Clamp((maxX-minX)*.5f+.0008f,.012f,.04f);ry=Mathf.Clamp((maxY-minY)*.5f+.0008f,.009f,.03f);
                center+=anchor.right*((minX+maxX)*.5f)+anchor.up*((minY+maxY)*.5f);
            }
        }finally{UnityEngine.Object.Destroy(mesh);}
        const int segments=48;
        var vertices=new Vector3[segments*4];var indices=new int[segments*24];
        for(int i=0;i<segments;i++) {
            float angle=i*Mathf.PI*2/segments;
            for(int c=0;c<4;c++) {
                float thick=c<2?.0015f:0;
                vertices[i*4+c]=new Vector3(Mathf.Cos(angle)*(rx+thick),Mathf.Sin(angle)*(ry+thick),(c==0||c==3)?-.010f:.010f);
                int n=(c+1)%4,next=(i+1)%segments,k=i*24+c*6;
                indices[k]=i*4+c;indices[k+1]=next*4+c;indices[k+2]=next*4+n;
                indices[k+3]=i*4+c;indices[k+4]=next*4+n;indices[k+5]=i*4+n;
            }
        }
        _rightWristband=new GameObject("BPVR_DarkGreenWristband");_rightWristband.transform.SetParent(anchor,false);
        _rightWristband.transform.SetPositionAndRotation(center,anchor.rotation);
        var bandMesh=new Mesh{name="Fitted green wristband",vertices=vertices,triangles=indices};bandMesh.RecalculateNormals();bandMesh.RecalculateBounds();
        _rightWristband.AddComponent<MeshFilter>().sharedMesh=bandMesh;
        var renderer=_rightWristband.AddComponent<MeshRenderer>();renderer.material=new Material(Shader.Find("Unlit/Color")){color=new Color(.045f,.13f,.075f)};
        renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;renderer.receiveShadows=false;
    }
    private void LoadCelHands()
    {
        if(_celHands!=null)return;
        string path=Path.Combine(CodexAssetDirectory,"hands_fp_cel.glb");
        if(!File.Exists(path))return;
        try {
            var shader=_codexBundle?.LoadAsset<Shader>("Assets/CodexToon.shader")??Shader.Find("Unlit/Texture");
            _celHands=CelHandPack.Load(path,shader);
            Log("[hands] Supplied cel hand pack loaded: direct wrist tracking, no elbows, native pose clips.");
        }catch(Exception e){LogWarn("[hands] Supplied pack rejected; retaining fallback arms: "+e);}
    }
    private void ShowCelHands()
    {
        if(_celHands==null)return;
        // Hands-only again: do not construct the optional forearm preview.
        if(_handR!=null&&_handL!=null&&_handR.Valid&&_handL.Valid)BuildRightWristband();
        bool visible=_inHouse&&_handL!=null&&_handR!=null&&_handL.Valid&&_handR.Valid&&
            !_heldToolVisible&&_visionCamera==null&&_movieTexture==null&&
            !(_journalOpen&&!_journalFraming&&_journalBoard!=null);
        _celHands.Root.SetActive(visible);
        if(HandRig.Model?.Renderer!=null)HandRig.Model.Renderer.enabled=false;
        if(_armOutline!=null)_armOutline.enabled=false;
    }
}
