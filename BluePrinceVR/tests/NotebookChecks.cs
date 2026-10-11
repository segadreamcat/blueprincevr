using BluePrinceVR;
public static class NotebookChecks {
 public static void Run(){
  string path=Path.Combine(Path.GetTempPath(),"bpvr-book-"+Guid.NewGuid().ToString("N")+".json");
  try{
   var book=NotebookData.Load(path);book.Pages[0].Strokes.Add(new(){new[]{20f,30f}});
   book.Append("Remember the laboratory password.");book.Save(path);
   string first=book.Pages[0].Lines[1];
   book.Append(string.Join(" ",Enumerable.Repeat("continuing",400)));book.Save(path);
   var loaded=NotebookData.Load(path);
   if(loaded.Pages.Count<2||loaded.Current==0||loaded.Pages[0].Lines[1]!=first||loaded.Pages[0].Strokes[0][0][0]!=20)throw new Exception("Notebook overflow/preservation failed");
   if(loaded.Pages.Any(p=>p.Lines.Count>24||p.Lines.Any(l=>l.Length>56)))throw new Exception("Notebook line bounds failed");
   if(!File.Exists(path+".bak")||File.Exists(path+".tmp"))throw new Exception("Atomic notebook save failed");
   File.WriteAllText(path,"invalid");bool rejected=false;try{NotebookData.Load(path);}catch{rejected=true;}
   if(!rejected||File.ReadAllText(path)!="invalid")throw new Exception("Corrupt notebook preservation failed");
   Console.WriteLine("PASS notebook dates, append, overflow, handwriting preservation, reopen position and atomic save");
  }finally{foreach(var suffix in new[]{"",".bak",".tmp"})if(File.Exists(path+suffix))File.Delete(path+suffix);}
 }
}
