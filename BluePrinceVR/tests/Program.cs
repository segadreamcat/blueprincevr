using BluePrinceVR;
using UnityEngine;
NotebookChecks.Run();
if(!FingerControls.PointReady(true,false,false,0,0))throw new Exception("Trigger-ready point missing");
if(FingerControls.PointReady(false,false,false,0,0)||FingerControls.PointReady(true,true,false,0,0)||FingerControls.PointReady(true,false,true,0,0))throw new Exception("Point cue ignored eligibility/pickup/modal priority");
if(FingerControls.PointReady(true,false,false,1,0)||FingerControls.PointReady(true,false,false,0,1))throw new Exception("Point cue overrides deliberate manual gestures");
Console.WriteLine("PASS trigger-ready point eligibility, pickup/tool priority and manual override");
var crouch=new CrouchToggle();
if(crouch.Update(true,false,false))throw new Exception("Crouch toggled before release/chord decision");
if(!crouch.Update(false,false,false)||!crouch.Update(false,false,false))throw new Exception("Crouch did not latch");
crouch.Update(true,false,false);
if(crouch.Update(false,false,false))throw new Exception("Second click did not stand");
crouch.Update(true,false,false);crouch.Update(true,true,false);
if(crouch.Update(false,false,false))throw new Exception("Comfort chord crouched");
crouch.Update(true,true,false);
if(crouch.Update(false,false,false))throw new Exception("Pencil/menu click leaked into crouch");
crouch.Update(true,false,false);crouch.Update(false,false,false);
if(crouch.Update(false,false,true))throw new Exception("Scene/tracking reset retained crouch");
Console.WriteLine("PASS toggle crouch, persistence, second click, comfort/pencil suppression and reset");
var movieGate=new VideoPresentationGate();
if(!VideoPresentationGate.ReachedEnd(false,299,300,9.96,10)||!VideoPresentationGate.ReachedEnd(false,-1,0,10,10))throw new Exception("Video end not detected");
if(VideoPresentationGate.ReachedEnd(true,299,300,10,10)||VideoPresentationGate.ReachedEnd(false,150,300,5,10))throw new Exception("Active video incorrectly ended");
if(!movieGate.Show(1,0,0,0,true,true) || !movieGate.Show(1,1,.033, .033f,true,true))throw new Exception("Advancing movie not presented");
if(movieGate.Show(1,1,.033,2,true,true))throw new Exception("Stalled video retained its panel");
if(movieGate.Show(1,2,.066,2.1f,false,true))throw new Exception("Paused/stopped video retained its panel");
if(movieGate.Show(1,3,.1,2.2f,true,false))throw new Exception("Walking retained cinematic panel");
if(!movieGate.Show(2,0,0,3,true,true))throw new Exception("New movie failed to start");
Console.WriteLine("PASS video advancement, stale-frame timeout, stopped playback and presentation-context gating");
var noteTestPath=Path.Combine(Path.GetTempPath(),"bpvr-notes-test-"+Guid.NewGuid().ToString("N")+".json");
try {
 var notes=new List<List<float[]>> { new() {new[]{0f,0f},new[]{1024f,900f}},new(){new[]{12.5f,50.25f}} };
 JournalNotes.Save(noteTestPath,notes);
 var loaded=JournalNotes.Load(noteTestPath);
 if(loaded.Count!=2||loaded[1][0][0]!=12.5f)throw new Exception("Note round-trip failed");
 loaded.RemoveAt(1);JournalNotes.Save(noteTestPath,loaded);
 if(JournalNotes.Load(noteTestPath).Count!=1||File.Exists(noteTestPath+".tmp"))throw new Exception("Undo persistence/atomic save failed");
 File.WriteAllText(noteTestPath,"[[[-1,2]]]");
 bool rejected=false;try{JournalNotes.Load(noteTestPath);}catch(InvalidDataException){rejected=true;}
 if(!rejected)throw new Exception("Invalid note point accepted");
 Console.WriteLine("PASS journal note round-trip, undo persistence, temp-file promotion and invalid-coordinate rejection");
}finally{if(File.Exists(noteTestPath))File.Delete(noteTestPath);if(File.Exists(noteTestPath+".tmp"))File.Delete(noteTestPath+".tmp");}
var keyGate=new KeyTouchGate();
var pumpGate=new KeyTouchGate();
if(pumpGate.Tick(.03f,true,.035f,.06f))throw new Exception("pump must start lifted");
pumpGate.Tick(.07f,true,.035f,.06f);
if(!pumpGate.Tick(.03f,true,.035f,.06f))throw new Exception("expanded pump contact missed");
for(int i=0;i<50;i++)if(pumpGate.Tick(.03f,true,.035f,.06f))throw new Exception("held pump contact repeated");
pumpGate.Tick(.05f,true,.035f,.06f);
if(pumpGate.Tick(.03f,true,.035f,.06f))throw new Exception("pump rearmed before full release");
pumpGate.Tick(.07f,true,.035f,.06f);
if(!pumpGate.Tick(.03f,true,.035f,.06f))throw new Exception("pump failed to rearm");
Console.WriteLine("PASS wider pump touch with release hysteresis; keyboard defaults unchanged");
var eraseTest=new List<List<float[]>> {new(){new[]{0f,100f},new[]{100f,100f}}};
if(!JournalNotes.Erase(eraseTest,50,100,10)||eraseTest.Count!=2)throw new Exception("Eraser must split sparse line");
if(eraseTest.SelectMany(s=>s).Any(p=>Math.Abs(p[0]-50)<=10))throw new Exception("Eraser left ink in disk");
if(JournalNotes.Erase(eraseTest,500,500,10))throw new Exception("Eraser changed distant ink");
Console.WriteLine("PASS eraser splits sparse strokes without reconnecting ink across erased gap");
if(keyGate.Tick(0,true)) throw new Exception("key must start lifted");
if(keyGate.Tick(.04f,true) || !keyGate.Tick(.01f,true)) throw new Exception("lift then touch presses");
for(int i=0;i<50;i++) if(keyGate.Tick(.01f,true)) throw new Exception("stationary finger repeated key");
keyGate.Tick(.04f,true);
if(keyGate.Tick(.01f,false) || !keyGate.Tick(.01f,true)) throw new Exception("only eligible nearest key presses");
keyGate.Reset();if(keyGate.Tick(.01f,true)) throw new Exception("reset retained contact");
Console.WriteLine("PASS fingertip lift-to-rearm, no repeat, nearest-key gating and reset");
void Check(bool condition,string message) { if(!condition) throw new Exception(message); }
for(int joint=0;joint<3;joint++) {
 Check(FingerControls.Angle(0,joint,1,0)==0,"grip leaves index extended");
 Check(FingerControls.Angle(0,joint,0,1)>0,"trigger curls index");
 for(int finger=1;finger<4;finger++) {
  Check(FingerControls.Angle(finger,joint,1,0)>0,"grip curls middle/ring/pinky");
  Check(FingerControls.Angle(finger,joint,0,1)==0,"trigger leaves other fingers alone");
 }
}
Console.WriteLine("PASS independent grip/index control");
for(int finger=0;finger<5;finger++) for(int joint=0;joint<3;joint++) {
 Check(FingerControls.PoseAngle(finger,joint,.1f,.1f,true,true)>0,"pickup cue survives resting pressure and open touch state");
 Check(FingerControls.PoseAngle(finger,joint,1,1,true,false)==FingerControls.Angle(finger,joint,1,1),"deliberate squeeze overrides hover");
 Check(FingerControls.PoseAngle(finger,joint,0,0,false,true)==0,"leaving pickup restores open hand");
}
Console.WriteLine("PASS pickup pose priority and release");
Check(FingerControls.IdleTogether(0,0,false)==1,"idle fingers together");
Check(FingerControls.IdleTogether(0,0,true)==0,"pickup releases idle adduction");
Check(FingerControls.IdleTogether(1,0,false)==0 && FingerControls.IdleTogether(0,1,false)==0,"manual gestures preserve original spread");
for(int finger=0;finger<4;finger++) for(int joint=0;joint<3;joint++) {
 Check(FingerControls.PoseAngle(finger,joint,0,0,false,false)<=3,"resting fingers nearly straight");
 Check(FingerControls.PoseAngle(finger,joint,0,0,true,false)<=10,"reaching fingers stay extended, not claw curled");
}
Console.WriteLine("PASS quiet idle pose and independent pickup/manual gestures");
using var reader=new BinaryReader(File.OpenRead(args[0]));
Check(System.Text.Encoding.ASCII.GetString(reader.ReadBytes(8))=="BPVRARM1","magic");
int n=reader.ReadInt32(); var mesh=new Mesh{vertices=new Vector3[n],normals=new Vector3[n],uv=new Vector2[n],boneWeights=new BoneWeight[n]};
Vector3 Vec()=>new(reader.ReadSingle(),reader.ReadSingle(),reader.ReadSingle());
for(int i=0;i<n;i++) {
 mesh.vertices[i]=Vec(); mesh.normals[i]=Vec(); mesh.uv[i]=new(reader.ReadSingle(),reader.ReadSingle());
 mesh.boneWeights[i]=new BoneWeight{boneIndex0=reader.ReadInt32(),boneIndex1=reader.ReadInt32(),boneIndex2=reader.ReadInt32(),boneIndex3=reader.ReadInt32(),weight0=reader.ReadSingle(),weight1=reader.ReadSingle(),weight2=reader.ReadSingle(),weight3=reader.ReadSingle()};
}
int indices=reader.ReadInt32(); mesh.triangles=Enumerable.Range(0,indices).Select(_=>reader.ReadInt32()).ToArray();
int bones=reader.ReadInt32();
if(args.Length>1 && args[1]=="--imported") {
 var names=new HashSet<string>();
 for(int i=0;i<bones;i++) {
  string name=System.Text.Encoding.UTF8.GetString(reader.ReadBytes(reader.ReadUInt16()));
  Check(names.Add(name),"duplicate bone name");int parent=reader.ReadInt32();Check(parent>=-1 && parent<i,"bone parent order");
  var pos=Vec();float x=reader.ReadSingle(),y=reader.ReadSingle(),z=reader.ReadSingle(),w=reader.ReadSingle();var scale=Vec();
  Check(Math.Abs(x*x+y*y+z*z+w*w-1)<.001,"bone rotation normalization");
  Check(float.IsFinite(pos.x+pos.y+pos.z+scale.x+scale.y+scale.z),"finite bone transform");
 }
 foreach(var side in new[]{"L","R"}) {
  foreach(var name in new[]{"shoulder","upper_arm","forearm","hand"}) Check(names.Contains(name+"."+side),"IK bone missing");
  foreach(var finger in new[]{"f_index","f_middle","f_ring","f_pinky","thumb"}) for(int j=1;j<=3;j++) Check(names.Contains($"{finger}.{j:00}.{side}"),"finger bone missing");
 }
 Check(names.Contains("camera"),"head anchor missing");
 foreach(var weight in mesh.boneWeights) {
  Check(Math.Abs(weight.weight0+weight.weight1+weight.weight2+weight.weight3-1)<.001,"normalized imported weights");
  Check(new[]{weight.boneIndex0,weight.boneIndex1,weight.boneIndex2,weight.boneIndex3}.All(b=>b>=0&&b<bones),"imported bone indices");
 }
 Check(mesh.triangles.All(i=>i>=0&&i<n),"imported mesh indices");
 Check(mesh.vertices.All(v=>float.IsFinite(v.x+v.y+v.z)),"finite imported vertices");
 Check(mesh.uv.All(v=>v.x>=0&&v.x<=1&&v.y>=0&&v.y<=1),"imported palette UV");
 int pngLength=reader.ReadInt32();var png=reader.ReadBytes(pngLength);
 Check(png.Length==pngLength && png[0]==137 && png[1]==80 && png[2]==78 && png[3]==71,"embedded palette PNG");
 Check(reader.BaseStream.Position==reader.BaseStream.Length,"unexpected trailing bytes");
 Console.WriteLine($"PASS imported runtime asset: {n} vertices, {indices/3} triangles, {bones} bones; IK/finger names, hierarchy, normalized weights/quaternions, indices, UVs and embedded PNG verified.");
 var importedRenderer=new SkinnedMeshRenderer{sharedMesh=mesh};
 ArmRefinement.Apply(importedRenderer);
 var refined=importedRenderer.sharedMesh;
 Check(refined.triangles.Length==indices*4,"imported single-pass triangle budget");
 Check(refined.vertices.All(v=>float.IsFinite(v.x+v.y+v.z)),"imported smoothing finite");
 Check(refined.triangles.All(i=>i>=0&&i<refined.vertices.Length),"refined indices");
 foreach(var w in refined.boneWeights)Check(Math.Abs(w.weight0+w.weight1+w.weight2+w.weight3-1)<.001,"smoothed weight normalization");
 var seamNormals=new Dictionary<(int,int,int),Vector3>();
 for(int i=0;i<refined.vertices.Length;i++) {
  var v=refined.vertices[i];var normal=refined.normals[i];
  Check(float.IsFinite(normal.x+normal.y+normal.z),"finite smooth normal");
  var key=((int)MathF.Round(v.x*100000),(int)MathF.Round(v.y*100000),(int)MathF.Round(v.z*100000));
  if(seamNormals.TryGetValue(key,out var previous))Check(Vector3.Dot(normal-previous,normal-previous)<1e-8,"split corners must share smooth normals");
  else seamNormals[key]=normal;
 }
 importedRenderer.sharedMaterial=new Material(new Shader()){mainTexture=new Texture2D()};
 ArmRefinement.ApplyBakedPalette(importedRenderer);
 var shaded=importedRenderer.sharedMesh;
 Check(shaded.triangles.Length==indices*4,"cel pass preserves triangle budget");
 for(int i=0;i<shaded.vertices.Length;i++) {
  var delta=shaded.vertices[i]-refined.vertices[refined.triangles[i]];
  Check(Vector3.Dot(delta,delta)<1e-10,"cel remap preserves refined mesh");
 }
 Console.WriteLine("PASS imported one-pass subdivision, shared seam normals, skin weights and shape-preserving cel remap (53,296 triangles)");
 return;
}
var renderer=new SkinnedMeshRenderer{sharedMesh=mesh};
ArmRefinement.Apply(renderer); var result=renderer.sharedMesh;
Check(result.triangles.Length==indices*4,"triangle subdivision count");
Check(result.vertices.Length>n,"new subdivision vertices");
Check(result.triangles.All(i=>i>=0 && i<result.vertices.Length),"index bounds");
Check(result.vertices.All(p=>float.IsFinite(p.x)&&float.IsFinite(p.y)&&float.IsFinite(p.z)),"finite vertices");
Check(result.normals.All(p=>float.IsFinite(p.x)&&float.IsFinite(p.y)&&float.IsFinite(p.z)),"finite normals");
foreach(var w in result.boneWeights) {
 Check(Math.Abs(w.weight0+w.weight1+w.weight2+w.weight3-1)<0.001,"normalized skin weights");
 Check(new[]{w.boneIndex0,w.boneIndex1,w.boneIndex2,w.boneIndex3}.All(b=>b>=0&&b<bones),"bone indices");
}
// UV seam duplicates must remain welded in position after subdivision.
var groups=mesh.vertices.Select((p,i)=>(p,i)).GroupBy(v=>(v.p.x,v.p.y,v.p.z)).Where(g=>g.Count()>1);
foreach(var group in groups) { var first=result.vertices[group.First().i]; foreach(var v in group) Check(Vector3.Dot(result.vertices[v.i]-first,result.vertices[v.i]-first)<1e-10,"UV seam crack"); }
Console.WriteLine($"PASS: actual arm asset {n} -> {result.vertices.Length} vertices; {indices/3} -> {result.triangles.Length/3} triangles; {bones} bones. Finite geometry, UV seams, indices and normalized skin weights verified.");
// Verify cropped projection maps the selected rectangle's edges to [-1,1].
foreach(int width in new[]{1280,2496,2701}) foreach(float fraction in new[]{0.4f,0.65f,0.9f}) foreach(float offset in new[]{-0.3f,0f,0.3f}) {
 int cropW=(int)MathF.Round(width*fraction)/2*2;
 int x=Math.Clamp((int)MathF.Round((1-offset)*0.5f*width-cropW*0.5f),0,width-cropW);
 float scale=(float)cropW/width, center=(2f*x+cropW)/width-1;
 float left=((2f*x/width-1)-center)/scale, right=((2f*(x+cropW)/width-1)-center)/scale;
 Check(Math.Abs(left+1)<1e-5 && Math.Abs(right-1)<1e-5,"foveated crop alignment");
}
Console.WriteLine("PASS: asymmetric foveated crop alignment at 27 resolution/focus/eye-offset combinations. GPU orientation and performance still require headset testing.");
ArmRefinement.Apply(renderer);
Check(renderer.sharedMesh.triangles.Length==indices*16,"second subdivision triangle count");
var rounded=renderer.sharedMesh;
renderer.sharedMaterial=new Material(new Shader()){mainTexture=new Texture2D()};
ArmRefinement.ApplyBakedPalette(renderer);
var cel=renderer.sharedMesh;
Check(cel.vertices.Length==indices*16,"cel vertex count");
for(int i=0;i<cel.vertices.Length;i++) {
 var source=rounded.triangles[i];
 var delta=cel.vertices[i]-rounded.vertices[source];
 Check(Vector3.Dot(delta,delta)<1e-10,"cel remap changes shape");
 Check(cel.uv[i].x>0 && cel.uv[i].x<1 && cel.uv[i].y>0 && cel.uv[i].y<1,"cel UV bounds");
 var w=cel.boneWeights[i];
 Check(Math.Abs(w.weight0+w.weight1+w.weight2+w.weight3-1)<0.001,"cel skin weights");
 Check(w.boneIndex0==rounded.boneWeights[source].boneIndex0,"cel remap bone mismatch");
}
Check(ArmRefinement.PaletteRow(new Color(.9f,.72f,.63f))==0,"skin palette");
Check(ArmRefinement.PaletteRow(new Color(.9f,.9f,.85f))==1,"sleeve palette");
Check(ArmRefinement.PaletteRow(new Color(.1f,.1f,.1f))==2,"wristband palette");
var ramp=(Texture2D)renderer.material.mainTexture;
Check(ramp.pixels.All(c=>c.a==1 && c.r>0 && c.g>0 && c.b>0),"opaque nonblack cel ramp");
Check(ramp.pixels.Take(96).Select(c=>c.r).Distinct().Count()==3,"three shading bands");
Console.WriteLine($"PASS: two subdivisions and cel UV remap ({cel.vertices.Length} vertices), skin weights, opaque three-band ramp and palette classification. Unity rendering is not simulated.");
var roundedCorner=ArmRefinement.RoundCrossSection(new Vector3(1,2,1),new Vector3(),1,1,.55f);
Check(roundedCorner.x<.9f && roundedCorner.z<.9f && roundedCorner.y==2,"rounded cross-section must change shape without shortening bone");
var axis=ArmRefinement.RoundCrossSection(new Vector3(1,2,0),new Vector3(),1,1,.55f);
Check(axis.x==1 && axis.z==0,"preserve width at section center");
var pointer=new PanelStickPointer();
Check(pointer.Tick(true,false,new Vector2(.1f,0),new Vector2(500,500),1000,1000,.02f),"panel owns pointer immediately");
Check(pointer.Position.x==500,"stick deadzone stays centered");
Check(pointer.Tick(true,false,new Vector2(1,0),new Vector2(500,500),1000,1000,.02f),"stick takeover");
Check(pointer.Position.x>500 && pointer.Position.y==500,"stick moves right");
Check(pointer.Position.x<=504.5f,"full stick speed limited to 22 percent height per second");
var held=pointer.Position;
pointer.Tick(true,false,new Vector2(),new Vector2(),1000,1000,.02f);
Check(pointer.Position.x==held.x,"magnifier remains where released");
for(int i=0;i<1000;i++) pointer.Tick(true,false,new Vector2(1,1),new Vector2(),1000,1000,.05f);
Check(pointer.Position.x==998 && pointer.Position.y==998,"clamped page edges");
Check(!pointer.Tick(false,false,new Vector2(1,0),new Vector2(),1000,1000,.02f),"close resets pointer");
pointer.Tick(true,false,new Vector2(1,0),new Vector2(),1000,1000,.02f);
Check(!pointer.Tick(true,true,new Vector2(1,0),new Vector2(),1000,1000,.02f),"grip/cancel resets pointer");
Console.WriteLine("PASS: mesh corner rounding and pointer deadzone/takeover/hold/edges/close/cancel. Live magnifier and Commissary must be tested in game.");
