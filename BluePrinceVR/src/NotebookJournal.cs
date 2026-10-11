using System;
using System.IO;
using UnityEngine;
using Il2CppTMPro;
namespace BluePrinceVR;
public partial class VRPlugin
{
 private bool _journalChooser,_notebook;
 private int _journalChoice;
 private NotebookData _book;
 private NotebookData _notebookPreview;
 private string _notebookPreviewText;
 private readonly TextMeshPro[] _bookLines=new TextMeshPro[25];
 private string NotebookPath=>Path.Combine(Application.dataPath,"..","UserData","Codex-Notebook","notebook.json");
 private void ChooseJournal()
 {
  if(_journalChoice==0){
   NotebookData loaded;
   try{loaded=NotebookData.Load(NotebookPath);}catch(Exception e){_book=null;_memoStatus=e.Message;return;}
   _book=loaded;_notebook=true;LoadNotebookPage();
  }
  else{
   _notebook=false;
   _journalFiles.Clear();Directory.CreateDirectory(JournalFolder);
   _journalFiles.AddRange(Directory.GetFiles(JournalFolder,"photo-*.png"));_journalFiles.Sort(StringComparer.Ordinal);_journalIndex=_journalFiles.Count-1;
   if(_journalIndex>=0)LoadJournalPhoto();_journalFraming=true;
  }
  _memoMode=false;_memoLoadedPhoto=null;_memoSaved="";_memoPreview=null;
  _journalChooser=false;
 }
 private void LoadNotebookPage()
 {
  _journalFraming=false;_journalStroke=null;_journalPointValid=false;
  _journalStrokes=_book.Pages[_book.Current].Strokes;
  if(_journalTexture!=null)UnityEngine.Object.Destroy(_journalTexture);
  _journalTexture=new Texture2D(JournalWidth,JournalHeight,TextureFormat.RGBA32,false);
  _journalBase=new Color[JournalWidth*JournalHeight];Array.Fill(_journalBase,new Color(.82f,.78f,.43f));
  for(int y=0;y<JournalHeight;y++)for(int x=0;x<JournalWidth;x++){
   if(y>850)_journalBase[y*JournalWidth+x]=new Color(.065f,.066f,.06f);
   else if(x==100||x==110)_journalBase[y*JournalWidth+x]=new Color(.55f,.19f,.13f);
  }
  for(int i=0;i<24;i++)for(int x=35;x<990;x++)_journalBase[(780-i*30)*JournalWidth+x]=new Color(.48f,.47f,.30f);
  RepaintJournal();
  if(_journalBoard==null){
   _journalBoard=GameObject.CreatePrimitive(PrimitiveType.Quad);_journalBoard.name="BPVR_NotebookJournal";
   UnityEngine.Object.Destroy(_journalBoard.GetComponent<Collider>());UnityEngine.Object.DontDestroyOnLoad(_journalBoard);
   _journalBoard.GetComponent<Renderer>().material=new Material(Shader.Find("Unlit/Texture"));
  }
  _journalBoard.GetComponent<Renderer>().material.mainTexture=_journalTexture;_journalBoard.SetActive(false);
  _journalDirty=false;
 }
 private bool SaveNotebook()
 {
  if(_book==null)return true;
  try{
   _book.Pages[_book.Current].Strokes=_journalStrokes;
   if(_journalDirty)_book.Pages[_book.Current].Updated=DateTimeOffset.Now.ToString("O");
   _book.Save(NotebookPath);_journalDirty=false;return true;
  }catch(Exception e){LogWarn("[notebook] Save failed: "+e.Message);return false;}
 }
 private bool KeepNotebookMemo()
 {
  if(!SaveNotebook())return false;
  // Work on a copy so a failed disk write cannot append the same note twice.
  var candidate=System.Text.Json.JsonSerializer.Deserialize<NotebookData>(System.Text.Json.JsonSerializer.Serialize(_book));
  try{candidate.Append(_memoPreview);candidate.Save(NotebookPath);}
  catch{_memoStatus="Notebook NOT saved. A: retry | B: discard";return false;}
  _book=candidate;_memoPreview=null;LoadNotebookPage();_memoStatus="Notebook saved. Continue dictating or off-hand trigger: pencil.";return true;
 }
 private void NotebookTextFrame()
 {
  // Render tentative words on the SAME ruled lines as accepted entries.
  // Preview uses a detached copy; B can discard it without touching saved notes.
  if(_notebook&&_memoPreview!=null&&_notebookPreviewText!=_memoPreview){
   var page=System.Text.Json.JsonSerializer.Deserialize<NotebookPage>(System.Text.Json.JsonSerializer.Serialize(_book.Pages[_book.Current]));
   _notebookPreview=new NotebookData{Pages=new(){page}};
   _notebookPreview.Append(_memoPreview);_notebookPreviewText=_memoPreview;
   _memoPage=page.Lines.Count>=24&&_book.Pages[_book.Current].Lines.Count>=24?1:0;
  }
  if(_memoPreview==null){_notebookPreview=null;_notebookPreviewText=null;}
  bool preview=_notebook&&_notebookPreview!=null;
  NotebookPage shown=_notebook?(preview?_notebookPreview.Pages[Math.Clamp(_memoPage,0,_notebookPreview.Pages.Count-1)]:_book.Pages[_book.Current]):null;
  if(preview)_memoPage=Math.Clamp(_memoPage,0,_notebookPreview.Pages.Count-1);
  for(int i=0;i<_bookLines.Length;i++){
   if(!_notebook){if(_bookLines[i]!=null)_bookLines[i].gameObject.SetActive(false);continue;}
   if(_bookLines[i]==null){
    var go=new GameObject("BPVR_NotebookLine"+i);go.transform.SetParent(_journalBoard.transform,false);go.transform.localScale=new Vector3(.01f,.01f,1);
    var label=go.AddComponent(Il2CppInterop.Runtime.Il2CppType.Of<TextMeshPro>()).Cast<TextMeshPro>();_bookLines[i]=label;
    if(_journalLabel?.font!=null)label.font=_journalLabel.font;
    label.fontSize=14;label.color=Color.black;label.richText=false;label.enableWordWrapping=false;label.alignment=TextAlignmentOptions.TopLeft;
    label.rectTransform.pivot=new Vector2(0,1);label.rectTransform.sizeDelta=new Vector2(86,4);
   }
   var text=_bookLines[i];text.gameObject.SetActive(true);
   string value=i==24?(preview?"PREVIEW  A: keep  B: discard  | "+(_memoPage+1)+"/"+_notebookPreview.Pages.Count:"Page "+(_book.Current+1)+" / "+_book.Pages.Count+"   Updated "+shown.Updated.Substring(0,16).Replace('T',' ')):i<shown.Lines.Count?shown.Lines[i]:"";
   if(text.text!=value){text.text=value;text.ForceMeshUpdate();}
   float baseline=text.textInfo.lineCount>0?text.textInfo.lineInfo[0].baseline:0;
   float y=i==24?825:780-i*30;
   text.transform.localPosition=new Vector3(-.37f,y/JournalHeight-.5f-baseline*.01f,-.004f);
  }
 }
}
