using UnityEngine;
using UnityEngine.Rendering;
namespace BluePrinceVR;
public static class Watch
{
    private static GameObject _root;
    private static Renderer _display;
    private static float _bandWidth=.022f, _bandDepth=.016f;
    private static Vector3 _bandCenter=new Vector3(0,0,.017f);
    public static float Size=1f;
    public static bool Exists=>_root!=null;
    private static SkinnedMeshRenderer _fitRenderer;
    public static void Build(Transform wrist,Vector3 palmLocal,Shader unused,Transform forearmOverride=null,SkinnedMeshRenderer meshOverride=null,float wristInset=-.026f)
    {
        if(Exists||wrist==null)return;
        var forearm=forearmOverride??HandRig.Model?.ForearmL;
        _fitRenderer=meshOverride??HandRig.Model?.Renderer;
        var attachment=forearm!=null?forearm:wrist;
        _root=new GameObject("BPVR_WristWatch");_root.transform.SetParent(attachment,false);
        Vector3 outward=-(wrist.rotation*palmLocal).normalized;if(outward.sqrMagnitude<.5f)outward=wrist.forward;
        Vector3 along=forearm!=null?(wrist.position-forearm.position).normalized:wrist.up;
        // Keep the strap's axis on the forearm, even when the hand is flexed.
        // Project the dorsal reference, not the arm axis, to avoid a tilted hoop.
        outward=Vector3.ProjectOnPlane(outward,along).normalized;
        if(outward.sqrMagnitude<.5f)outward=Vector3.ProjectOnPlane(attachment.forward,along).normalized;
        // Dial's 3 o'clock points toward the hand; strap runs across, not along,
        // the forearm. Use world-space anatomical directions, not bone-local Y.
        Vector3 dialUp=Vector3.Cross(-outward,along).normalized;
        var center=wrist.position+along*wristInset;
        center=FitBand(center,along,dialUp,outward);
        _root.transform.SetPositionAndRotation(center+outward*(_bandDepth+.001f),Quaternion.LookRotation(-outward,dialUp));
        // Geometry is measured in metres. Old WatchSize=1.3 inflated the strap
        // away from the wrist; never apply a cosmetic size preference to its fit.
        var s=attachment.lossyScale;float size=1f;
        _root.transform.localScale=new Vector3(size/Mathf.Max(.001f,Mathf.Abs(s.x)),size/Mathf.Max(.001f,Mathf.Abs(s.y)),size/Mathf.Max(.001f,Mathf.Abs(s.z)));
        Part("Case",PrimitiveType.Cylinder,Vector3.zero,new Vector3(.044f,.003f,.044f),new Color(.30f,.28f,.23f));
        Part("Bezel",PrimitiveType.Cylinder,new Vector3(0,0,-.0032f),new Vector3(.041f,.001f,.041f),new Color(.49f,.45f,.35f));
        Part("Dial",PrimitiveType.Cylinder,new Vector3(0,0,-.0043f),new Vector3(.037f,.0006f,.037f),Color.black);
        BuildBand();
        for(int side=-1;side<=1;side+=2){
            Part("Lug",PrimitiveType.Cube,new Vector3(0,side*.021f,.001f),new Vector3(.024f,.005f,.005f),new Color(.34f,.31f,.25f));
        }
        Part("Crown",PrimitiveType.Sphere,new Vector3(.023f,0,0),new Vector3(.006f,.006f,.005f),new Color(.35f,.32f,.26f));
        var face=GameObject.CreatePrimitive(PrimitiveType.Quad);face.name="BPVR_WatchNativeSteps";
        Object.Destroy(face.GetComponent<Collider>());face.transform.SetParent(_root.transform,false);
        face.transform.localPosition=new Vector3(0,0,-.0051f);face.transform.localScale=new Vector3(.034f,.014f,1);
        _display=face.GetComponent<Renderer>();var shader=Shader.Find("Unlit/Texture")??Shader.Find("Sprites/Default");
        if(shader!=null)_display.material=new Material(shader);
        _display.shadowCastingMode=ShadowCastingMode.Off;_display.receiveShadows=false;_display.enabled=false;
        // A 34 mm case suits the slender wrist; keep the fitted band unchanged.
        for(int i=0;i<_root.transform.childCount;i++) {
            var child=_root.transform.GetChild(i);
            if(child.name=="Watch closed leather band" || child.name=="Watch Buckle")continue;
            child.localPosition*=.77f;child.localScale*=.77f;
        }
    }
    private static Vector3 FitBand(Vector3 center,Vector3 along,Vector3 across,Vector3 outward)
    {
        var renderer=_fitRenderer;
        if(renderer==null)return center;
        var baked=new Mesh();
        try {
            renderer.BakeMesh(baked);
            float minY=float.PositiveInfinity,maxY=float.NegativeInfinity;
            float minZ=float.PositiveInfinity,maxZ=float.NegativeInfinity;int count=0;
            foreach(var vertex in baked.vertices) {
                var p=renderer.transform.TransformPoint(vertex)-center;
                if(Mathf.Abs(Vector3.Dot(p,along))>.009f || p.sqrMagnitude>.0064f)continue;
                float y=Vector3.Dot(p,across),z=Vector3.Dot(p,outward);
                minY=Mathf.Min(minY,y);maxY=Mathf.Max(maxY,y);
                minZ=Mathf.Min(minZ,z);maxZ=Mathf.Max(maxZ,z);count++;
            }
            float width=(maxY-minY)*.5f,depth=(maxZ-minZ)*.5f;
            if(count>=8 && width>.008f && depth>.006f) {
                _bandWidth=Mathf.Clamp(width+.001f,.012f,.035f);
                _bandDepth=Mathf.Clamp(depth+.001f,.009f,.028f);
                center+=across*((maxY+minY)*.5f)+outward*((maxZ+minZ)*.5f);
            }
        }
        finally { Object.Destroy(baked); }
        _bandCenter=new Vector3(0,0,_bandDepth+.001f);
        return center;
    }
    private static void BuildBand()
    {
        // Closed elliptical leather band: outside, inside and both edges.
        const int segments=64;
        var vertices=new System.Collections.Generic.List<Vector3>();
        var triangles=new System.Collections.Generic.List<int>();
        Vector3 Point(int i,int corner){
            float angle=i*Mathf.PI*2/segments;
            bool outer=corner<2;float thickness=outer?.0018f:0;
            return new Vector3((corner==0||corner==3)?-.0095f:.0095f,
                Mathf.Sin(angle)*(_bandWidth+thickness),_bandCenter.z-Mathf.Cos(angle)*(_bandDepth+thickness));
        }
        for(int i=0;i<segments;i++)for(int edge=0;edge<4;edge++){
            int next=(edge+1)%4,n=vertices.Count;
            vertices.Add(Point(i,edge));vertices.Add(Point(i+1,edge));
            vertices.Add(Point(i+1,next));vertices.Add(Point(i,next));
            triangles.AddRange(new[]{n,n+1,n+2,n,n+2,n+3,n+2,n+1,n,n+3,n+2,n});
        }
        var go=new GameObject("Watch closed leather band");go.transform.SetParent(_root.transform,false);
        var mesh=new Mesh();mesh.name="Closed wrist band";mesh.vertices=vertices.ToArray();mesh.triangles=triangles.ToArray();mesh.RecalculateNormals();mesh.RecalculateBounds();
        go.AddComponent<MeshFilter>().sharedMesh=mesh;
        var renderer=go.AddComponent<MeshRenderer>();var shader=Shader.Find("Unlit/Color");
        if(shader!=null)renderer.material=new Material(shader){color=new Color(.15f,.085f,.045f)};
        renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
        Part("Buckle",PrimitiveType.Cube,new Vector3(0,0,_bandCenter.z+_bandDepth+.002f),new Vector3(.021f,.010f,.002f),new Color(.34f,.31f,.25f));
    }
    private static void Part(string name,PrimitiveType type,Vector3 position,Vector3 scale,Color color)
    {
        var go=GameObject.CreatePrimitive(type);go.name="Watch "+name;Object.Destroy(go.GetComponent<Collider>());
        go.transform.SetParent(_root.transform,false);go.transform.localPosition=position;go.transform.localScale=scale;
        if(type==PrimitiveType.Cylinder)go.transform.localRotation=Quaternion.Euler(90,0,0);
        var renderer=go.GetComponent<Renderer>();var shader=Shader.Find("Unlit/Color");
        if(shader!=null)renderer.material=new Material(shader){color=color};
        renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
    }
    // Reuse the native HUD texture directly; no readbacks, screenshots or extra cameras.
    public static void SetSource(RenderTexture ui)
    {
        if(_display==null)return;_display.enabled=ui!=null;if(ui==null)return;
        var mat=_display.material;
        if(mat.mainTexture!=ui){
            mat.mainTexture=ui;
            // Upper-left native HUD cluster, padded for a three-digit count.
            mat.mainTextureScale=new Vector2(.10f,.07f);mat.mainTextureOffset=new Vector2(.01f,.917f);
        }
    }
    public static void Set(int steps) { } // Native game draws the live count.
    public static void FaceViewer(Vector3 eye) { } // Fixed to the wrist, not billboarded.
    public static void SetVisible(bool show){if(_root!=null)_root.SetActive(show);}
}
