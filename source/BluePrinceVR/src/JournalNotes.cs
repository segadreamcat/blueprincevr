using System;
using System.IO;
using System.Collections.Generic;
using System.Text.Json;
namespace BluePrinceVR;
internal static class JournalNotes
{
    // Split strokes around the eraser disk. Resample segments so a sparse stroke
    // cannot bridge an erased area when reloaded. Originals/photos are untouched.
    public static bool Erase(List<List<float[]>> strokes,float x,float y,float radius)
    {
        var result=new List<List<float[]>>();bool changed=false;
        foreach(var stroke in strokes) {
            bool intersects=false;
            for(int i=0;i<stroke.Count&&!intersects;i++) {
                var a=stroke[Math.Max(0,i-1)];var b=stroke[i];
                float dx=b[0]-a[0],dy=b[1]-a[1],length=dx*dx+dy*dy;
                float t=length>0?Math.Clamp(((x-a[0])*dx+(y-a[1])*dy)/length,0,1):0;
                float ex=a[0]+t*dx-x,ey=a[1]+t*dy-y;
                intersects=ex*ex+ey*ey<=radius*radius;
            }
            if(!intersects){result.Add(stroke);continue;}
            var run=new List<float[]>();
            for(int i=0;i<stroke.Count;i++) {
                var a=stroke[Math.Max(0,i-1)];var b=stroke[i];
                int steps=Math.Max(1,(int)Math.Ceiling(Math.Sqrt(Math.Pow(b[0]-a[0],2)+Math.Pow(b[1]-a[1],2))/2));
                for(int k=(i==0?0:1);k<=steps;k++) {
                    float px=a[0]+(b[0]-a[0])*k/steps,py=a[1]+(b[1]-a[1])*k/steps;
                    if((px-x)*(px-x)+(py-y)*(py-y)<=radius*radius) {
                        changed=true;if(run.Count>0){result.Add(run);run=new();}
                    } else {
                        run.Add(new[]{px,py});
                        if(run.Count==20000){result.Add(run);run=new();}
                    }
                }
            }
            if(run.Count>0)result.Add(run);
        }
        if(!changed || result.Count>1000)return false;
        strokes.Clear();strokes.AddRange(result);return true;
    }
    public static List<List<float[]>> Load(string path)
    {
        if(!File.Exists(path))return new();
        if(new FileInfo(path).Length>8*1024*1024)throw new InvalidDataException("Notes file exceeds 8MB limit");
        var strokes=JsonSerializer.Deserialize<List<List<float[]>>>(File.ReadAllText(path))??new();
        if(strokes.Count>1000)throw new InvalidDataException("Too many strokes");
        foreach(var s in strokes) {
            if(s==null||s.Count>20000)throw new InvalidDataException("Invalid stroke");
            foreach(var p in s)if(p==null||p.Length!=2||!float.IsFinite(p[0]+p[1])||p[0]<0||p[0]>1024||p[1]<0||p[1]>900)
                throw new InvalidDataException("Invalid note point");
        }
        return strokes;
    }
    public static void Save(string path,List<List<float[]>> strokes)
    {
        string json=JsonSerializer.Serialize(strokes);
        if(System.Text.Encoding.UTF8.GetByteCount(json)>8*1024*1024)throw new InvalidDataException("Notes exceed 8MB limit");
        File.WriteAllText(path+".tmp",json);File.Move(path+".tmp",path,true);
    }
}
