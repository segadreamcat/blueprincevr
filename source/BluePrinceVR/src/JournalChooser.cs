using UnityEngine;
namespace BluePrinceVR;
public partial class VRPlugin
{
 private GameObject _journalChoicePictures;
 private readonly Renderer[] _journalChoiceBorders=new Renderer[2];
 private void ShowJournalChoices(Vector3 eye,Quaternion rotation,bool show)
 {
  if(!show){if(_journalChoicePictures!=null)_journalChoicePictures.SetActive(false);return;}
  if(_journalChoicePictures==null){
   _journalChoicePictures=new GameObject("BPVR_JournalPictureChoices");UnityEngine.Object.DontDestroyOnLoad(_journalChoicePictures);
   for(int choice=0;choice<2;choice++){
    var root=new GameObject(choice==0?"Notebook choice":"Camera choice").transform;root.SetParent(_journalChoicePictures.transform,false);root.localPosition=new Vector3(choice==0?-.15f:.15f,0,0);
    var border=ToolPart(root,"Selection frame",PrimitiveType.Cube,new Vector3(0,0,.002f),new Vector3(.25f,.27f,.003f),Color.white);_journalChoiceBorders[choice]=border.GetComponent<Renderer>();
    var icon=GameObject.CreatePrimitive(PrimitiveType.Quad);icon.name="Journal choice illustration";UnityEngine.Object.Destroy(icon.GetComponent<Collider>());icon.transform.SetParent(root,false);icon.transform.localScale=new Vector3(.238f,.258f,1);
    var shader=Shader.Find("Unlit/Texture");if(shader!=null)icon.GetComponent<Renderer>().material=new Material(shader){mainTexture=JournalChoiceTexture(choice)};
   }
  }
  _journalChoicePictures.SetActive(true);
  _journalChoicePictures.transform.SetPositionAndRotation(eye+rotation*new Vector3(0,-.015f,.8f),rotation);
  for(int i=0;i<2;i++)_journalChoiceBorders[i].material.color=i==_journalChoice?new Color(.25f,.73f,1):new Color(.20f,.22f,.25f);
 }
 private static Texture2D JournalChoiceTexture(int choice)
 {
  const int n=256;var pixels=new Color[n*n];
  for(int i=0;i<pixels.Length;i++)pixels[i]=new Color(.12f,.15f,.18f);
  void Box(int x,int y,int w,int h,Color c){for(int j=Mathf.Max(0,y);j<Mathf.Min(n,y+h);j++)for(int i=Mathf.Max(0,x);i<Mathf.Min(n,x+w);i++)pixels[j*n+i]=c;}
  void Disc(int x,int y,int radius,Color c){for(int j=-radius;j<=radius;j++)for(int i=-radius;i<=radius;i++)if(i*i+j*j<=radius*radius&&x+i>=0&&x+i<n&&y+j>=0&&y+j<n)pixels[(y+j)*n+x+i]=c;}
  var dark=new Color(.025f,.03f,.04f);
  if(choice==0){
   Box(51,24,166,207,dark);Box(43,30,166,204,new Color(.91f,.84f,.43f));Box(43,214,166,21,dark);
   for(int y=46;y<209;y+=12)Box(44,y,164,1,new Color(.54f,.52f,.31f));
   Box(62,30,2,184,new Color(.68f,.22f,.13f));Box(67,30,1,184,new Color(.68f,.22f,.13f));
  }else{
   Box(37,32,191,70,dark);Box(44,40,177,50,new Color(.33f,.36f,.41f));
   Box(43,86,179,40,new Color(.77f,.78f,.72f));
   Box(60,113,148,101,dark);Box(67,120,134,85,new Color(.82f,.83f,.78f));
   Box(170,164,27,34,dark);Box(175,169,17,24,new Color(.85f,.64f,.38f));
   Box(51,48,163,22,dark);Box(58,56,147,3,new Color(.32f,.35f,.37f));
   Box(103,87,7,34,new Color(.83f,.12f,.09f));Box(110,87,7,34,new Color(.92f,.77f,.12f));Box(117,87,7,34,new Color(.15f,.65f,.39f));
   Disc(123,159,44,dark);Disc(123,159,36,new Color(.31f,.33f,.37f));Disc(123,159,30,dark);Disc(123,159,22,new Color(.055f,.10f,.19f));Disc(115,168,6,new Color(.65f,.80f,.9f));
   Disc(80,134,10,dark);Disc(80,134,7,new Color(.72f,.06f,.04f));
  }
  var texture=new Texture2D(n,n,TextureFormat.RGBA32,false);texture.SetPixels(pixels);texture.Apply(false);texture.filterMode=FilterMode.Bilinear;return texture;
 }
}
