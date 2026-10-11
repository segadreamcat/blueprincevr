using UnityEngine;
namespace BluePrinceVR;
public partial class VRPlugin
{
    private GameObject _journalPencil,_journalFlash;
    private Transform _journalPencilTip,_journalPencilEraser;
    private Material _journalFlashMaterial;
    // Small faceted solids give the pencil a real hexagonal barrel and a
    // sharpened point without textures, asset bundles or per-frame mesh work.
    private static void PencilSection(Transform root,string name,float bottom,float top,float r0,float r1,int sides,Color color)
    {
        var vertices=new System.Collections.Generic.List<Vector3>();
        var triangles=new System.Collections.Generic.List<int>();
        var colors=new System.Collections.Generic.List<Color>();
        void Triangle(Vector3 a,Vector3 b,Vector3 c,Color shade) {
            int start=vertices.Count;vertices.Add(a);vertices.Add(b);vertices.Add(c);
            triangles.Add(start);triangles.Add(start+1);triangles.Add(start+2);
            colors.Add(shade);colors.Add(shade);colors.Add(shade);
        }
        for(int i=0;i<sides;i++) {
            float a=i*Mathf.PI*2/sides,b=(i+1)*Mathf.PI*2/sides;
            var p=new Vector3(Mathf.Cos(a)*r0,bottom,Mathf.Sin(a)*r0);
            var q=new Vector3(Mathf.Cos(b)*r0,bottom,Mathf.Sin(b)*r0);
            var s=new Vector3(Mathf.Cos(a)*r1,top,Mathf.Sin(a)*r1);
            var t=new Vector3(Mathf.Cos(b)*r1,top,Mathf.Sin(b)*r1);
            Color shade=color*(.78f+.22f*(.5f+.5f*Mathf.Cos(a)));shade.a=1;
            Triangle(p,s,q,shade);Triangle(q,s,t,shade);
            Triangle(new Vector3(0,bottom,0),p,q,color*.8f);
            Triangle(new Vector3(0,top,0),t,s,color);
        }
        var part=new GameObject(name);part.transform.SetParent(root,false);
        var mesh=new Mesh();mesh.name=name;mesh.vertices=vertices.ToArray();mesh.triangles=triangles.ToArray();mesh.colors=colors.ToArray();mesh.RecalculateNormals();mesh.RecalculateBounds();
        part.AddComponent<MeshFilter>().sharedMesh=mesh;
        var renderer=part.AddComponent<MeshRenderer>();
        var shader=Shader.Find("Sprites/Default")??Shader.Find("Unlit/Color");
        if(shader!=null)renderer.material=new Material(shader){color=Color.white};
        renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
    }
    private static GameObject ToolPart(Transform parent,string name,PrimitiveType shape,Vector3 position,Vector3 scale,Color color)
    {
        var part=GameObject.CreatePrimitive(shape);part.name=name;
        UnityEngine.Object.Destroy(part.GetComponent<Collider>());
        part.transform.SetParent(parent,false);part.transform.localPosition=position;part.transform.localScale=scale;
        var shader=Shader.Find("Unlit/Color");
        if(shader!=null)part.GetComponent<Renderer>().material=new Material(shader){color=color};
        part.GetComponent<Renderer>().shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
        return part;
    }
    private void UpdateJournalPencil()
    {
        if(_journalPencil==null) {
            _journalPencil=new GameObject("BPVR_JournalPencil");UnityEngine.Object.DontDestroyOnLoad(_journalPencil);
            var root=_journalPencil.transform;
            PencilSection(root,"Hexagonal blue lacquer",-.067f,.067f,.0045f,.0045f,6,new Color(.08f,.28f,.65f));
            PencilSection(root,"Sharpened cedar",-.082f,-.067f,.0012f,.0045f,12,new Color(.83f,.65f,.42f));
            PencilSection(root,"Tapered blue core",-.086f,-.082f,.00015f,.0012f,12,JournalInk);
            _journalPencilTip=new GameObject("BPVR_LeadContact").transform;
            _journalPencilTip.SetParent(root,false);_journalPencilTip.localPosition=new Vector3(0,-.086f,0);
            PencilSection(root,"Brushed metal ferrule",.067f,.080f,.0047f,.0047f,16,new Color(.64f,.66f,.69f));
            for(int i=0;i<4;i++)PencilSection(root,"Ferrule crimp "+i,.068f+i*.003f,.0687f+i*.003f,.0049f,.0049f,16,new Color(.40f,.43f,.48f));
            PencilSection(root,"Rubber eraser",.080f,.100f,.0048f,.0048f,16,new Color(.77f,.40f,.43f));
            PencilSection(root,"Rounded eraser end",.100f,.103f,.0048f,.0038f,16,new Color(.83f,.47f,.49f));
            _journalPencilEraser=new GameObject("BPVR_EraserContact").transform;
            _journalPencilEraser.SetParent(root,false);_journalPencilEraser.localPosition=new Vector3(0,.103f,0);
        }
        bool show=_journalOpen&&!_journalFraming&&_handR!=null&&_handR.Valid;
        _journalPencil.SetActive(show);if(!show)return;
        var wrist=_celHands?.ToolAnchors[_comfort.LeftHanded?0:1]??HandRig.Model?.HandR;
        if(wrist==null)return;
        // Fixed grip-relative tool: turning the controller turns the actual eraser
        // toward the paper; stick click provides a convenient end-for-end flip.
        var rotation=wrist.rotation*Quaternion.Euler(25,0,25)*(_journalErase?Quaternion.identity:Quaternion.Euler(180,0,0));
        _journalPencil.transform.SetPositionAndRotation(wrist.TransformPoint(new Vector3(0,.065f,.015f)),rotation);
    }
    private void JournalFlashFrame(Vector3 eye,Quaternion rotation)
    {
        float fade=Mathf.Clamp01((_journalFlashUntil-Time.unscaledTime)/JournalFlashDuration);
        if(fade<=0 || !_journalOpen) {if(_journalFlash!=null)_journalFlash.SetActive(false);return;}
        if(_journalFlash==null) {
            var shader=Shader.Find("Sprites/Default");if(shader==null)return;
            _journalFlash=GameObject.CreatePrimitive(PrimitiveType.Quad);_journalFlash.name="BPVR_JournalFlash";
            UnityEngine.Object.Destroy(_journalFlash.GetComponent<Collider>());UnityEngine.Object.DontDestroyOnLoad(_journalFlash);
            _journalFlashMaterial=new Material(shader);_journalFlashMaterial.renderQueue=4100;
            _journalFlash.GetComponent<Renderer>().material=_journalFlashMaterial;
        }
        _journalFlash.SetActive(true);
        _journalFlash.transform.SetPositionAndRotation(eye+rotation*Vector3.forward*.4f,rotation);
        // A single translucent flash localized to the camera rectangle, never strobing.
        _journalFlash.transform.localScale=new Vector3(1.02f,.765f,1);
        _journalFlashMaterial.color=new Color(1,.96f,.85f,.48f*fade);
    }
}
