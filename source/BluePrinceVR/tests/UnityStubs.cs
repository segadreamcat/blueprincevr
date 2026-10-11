// Numeric test doubles only: these tests do not claim to exercise Unity rendering.
namespace UnityEngine {
 public struct Vector3 {
  public float x,y,z; public Vector3(float a,float b,float c){x=a;y=b;z=c;}
  public static Vector3 up=>new(0,1,0);
  public Vector3 normalized { get { float m=MathF.Sqrt(x*x+y*y+z*z); return m>1e-10 ? this*(1/m) : new(); } }
  public static Vector3 operator +(Vector3 a,Vector3 b)=>new(a.x+b.x,a.y+b.y,a.z+b.z);
  public static Vector3 operator -(Vector3 a,Vector3 b)=>new(a.x-b.x,a.y-b.y,a.z-b.z);
  public static Vector3 operator *(Vector3 a,float b)=>new(a.x*b,a.y*b,a.z*b);
  public static Vector3 Cross(Vector3 a,Vector3 b)=>new(a.y*b.z-a.z*b.y,a.z*b.x-a.x*b.z,a.x*b.y-a.y*b.x);
  public static float Dot(Vector3 a,Vector3 b)=>a.x*b.x+a.y*b.y+a.z*b.z;
 }
 public struct Vector2 { public float x,y; public Vector2(float a,float b){x=a;y=b;} public static Vector2 operator +(Vector2 a,Vector2 b)=>new(a.x+b.x,a.y+b.y); public static Vector2 operator *(Vector2 a,float b)=>new(a.x*b,a.y*b); }
 public struct BoneWeight { public int boneIndex0,boneIndex1,boneIndex2,boneIndex3; public float weight0,weight1,weight2,weight3; }
 public struct Color { public float r,g,b,a; public Color(float r,float g,float b,float a=1){this.r=r;this.g=g;this.b=b;this.a=a;} public static Color white=>new(1,1,1); }
 public static class Mathf { public static int RoundToInt(float x)=>(int)MathF.Round(x); public static float Max(float a,float b)=>MathF.Max(a,b); }
 public class Mesh { public string name; public Rendering.IndexFormat indexFormat; public Vector3[] vertices,normals; public Vector2[] uv; public BoneWeight[] boneWeights; public int[] triangles; public object bindposes; public Color[] colors; public void RecalculateBounds(){} }
 public class Shader { public static Shader Find(string n)=>new(); }
 public class Texture { public T TryCast<T>() where T:class=>this as T; }
 public enum TextureFormat { RGBA32 }
 public enum TextureWrapMode { Clamp }
 public enum FilterMode { Point }
 public class Texture2D:Texture {
  public string name; public TextureWrapMode wrapMode; public FilterMode filterMode;
  public int width,height; public Color[] pixels;
  public Texture2D(){} public Texture2D(int w,int h,TextureFormat f,bool mip){width=w;height=h;pixels=new Color[w*h];}
  public static Texture2D whiteTexture=new();
  public Color GetPixelBilinear(float u,float v)=>new(0.6f,0.4f,0.3f);
  public void SetPixel(int x,int y,Color c){pixels[y*width+x]=c;} public void Apply(bool mip){}
 }
 public class Material { public Material(Shader s){} public Texture mainTexture; public Color color; public int renderQueue; public bool HasProperty(string p)=>true; public void SetFloat(string p,float v){} public void SetColor(string p,Color c){} public void DisableKeyword(string k){} public void EnableKeyword(string k){} }
 public class SkinnedMeshRenderer { public Mesh sharedMesh; public Material sharedMaterial,material; }
}
namespace UnityEngine.Rendering { public enum IndexFormat { UInt16,UInt32 } }
