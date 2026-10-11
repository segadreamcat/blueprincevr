using System;
using System.IO;
using System.Collections.Generic;
using System.Text.Json;
namespace BluePrinceVR;
public sealed class NotebookPage
{
 public string Id {get;set;}=Guid.NewGuid().ToString("N");
 public string Created {get;set;}=DateTimeOffset.Now.ToString("O");
 public string Updated {get;set;}=DateTimeOffset.Now.ToString("O");
 public List<string> Lines {get;set;}=new();
 public List<List<float[]>> Strokes {get;set;}=new();
}
public sealed class NotebookData
{
 public int Version {get;set;}=1;
 public int Current {get;set;}
 public List<NotebookPage> Pages {get;set;}=new();
 public static NotebookData Load(string path){
  if(!File.Exists(path))return new NotebookData{Pages=new(){new NotebookPage()}};
  var book=JsonSerializer.Deserialize<NotebookData>(File.ReadAllText(path));
  if(book==null||book.Version!=1||book.Pages==null||book.Pages.Count==0)throw new InvalidDataException("Invalid notebook; original file preserved.");
  foreach(var page in book.Pages)if(page==null||page.Lines==null||page.Strokes==null||page.Updated==null||page.Updated.Length<16||string.IsNullOrEmpty(page.Id))throw new InvalidDataException("Invalid notebook page.");
  book.Current=Math.Clamp(book.Current,0,book.Pages.Count-1);return book;
 }
 public void Save(string path){
  Directory.CreateDirectory(Path.GetDirectoryName(path));
  string temp=path+".tmp";File.WriteAllText(temp,JsonSerializer.Serialize(this));
  if(File.Exists(path))File.Copy(path,path+".bak",true);
  File.Move(temp,path,true);
 }
 public void Append(string text){
  var lines=new List<string>{"["+DateTimeOffset.Now.ToString("yyyy-MM-dd HH:mm zzz")+"]"};
  string line="";
  foreach(string word in text.Split((char[])null,StringSplitOptions.RemoveEmptyEntries)){
   string rest=word;
   if(line.Length>0&&line.Length+1+rest.Length>56){lines.Add(line);line="";}
   while(rest.Length>56){if(line.Length>0){lines.Add(line);line="";}lines.Add(rest.Substring(0,56));rest=rest.Substring(56);}
   if(rest.Length>0)line+=(line.Length>0?" ":"")+rest;
  }
  if(line.Length>0)lines.Add(line);
  foreach(string value in lines){
   if(Pages[Current].Lines.Count>=24){Pages.Add(new NotebookPage());Current=Pages.Count-1;}
   Pages[Current].Lines.Add(value);Pages[Current].Updated=DateTimeOffset.Now.ToString("O");
  }
 }
}
