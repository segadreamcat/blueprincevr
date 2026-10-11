using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
public static class MemoCapture {
 [StructLayout(LayoutKind.Sequential)] struct Format { public ushort tag,channels; public uint rate,bytesPerSec; public ushort align,bits,extra; }
 [StructLayout(LayoutKind.Sequential)] struct Header { public IntPtr data; public uint length,recorded; public UIntPtr user; public uint flags,loops; public IntPtr next; public UIntPtr reserved; }
 [DllImport("winmm.dll")] static extern uint waveInOpen(out IntPtr h,uint device,ref Format format,IntPtr callback,IntPtr instance,uint flags);
 [DllImport("winmm.dll")] static extern uint waveInPrepareHeader(IntPtr h,IntPtr header,uint size);
 [DllImport("winmm.dll")] static extern uint waveInUnprepareHeader(IntPtr h,IntPtr header,uint size);
 [DllImport("winmm.dll")] static extern uint waveInAddBuffer(IntPtr h,IntPtr header,uint size);
 [DllImport("winmm.dll")] static extern uint waveInStart(IntPtr h);
 [DllImport("winmm.dll")] static extern uint waveInReset(IntPtr h);
 [DllImport("winmm.dll")] static extern uint waveInClose(IntPtr h);
 static void Check(uint result){if(result!=0)throw new Exception("Microphone capture failed (Windows audio error "+result+"). Check default input and microphone access.");}
 public static void Record(string job) {
  if(File.Exists(job+".stop"))throw new Exception("Record released too early. Hold and wait for REC.");
  var format=new Format{tag=1,channels=1,rate=16000,bytesPerSec=32000,align=2,bits=16};
  IntPtr handle=IntPtr.Zero;var data=new IntPtr[60];var headers=new IntPtr[60];var prepared=new bool[60];
  uint size=(uint)Marshal.SizeOf(typeof(Header));
  try {
   Check(waveInOpen(out handle,UInt32.MaxValue,ref format,IntPtr.Zero,IntPtr.Zero,0));
   for(int i=0;i<60;i++){
    data[i]=Marshal.AllocHGlobal(32000);headers[i]=Marshal.AllocHGlobal((int)size);
    Marshal.StructureToPtr(new Header{data=data[i],length=32000},headers[i],false);
    Check(waveInPrepareHeader(handle,headers[i],size));prepared[i]=true;Check(waveInAddBuffer(handle,headers[i],size));
   }
   if(File.Exists(job+".stop"))throw new Exception("Record released too early; wait for REC next time.");
   Check(waveInStart(handle));File.WriteAllText(job+".ready","ready");
   var end=DateTime.UtcNow.AddSeconds(60);
   while(DateTime.UtcNow<end&&!File.Exists(job+".stop"))Thread.Sleep(20);
   Check(waveInReset(handle));File.WriteAllText(job+".processing","transcribing");
   using(var pcm=new MemoryStream()){
    long energy=0,count=0;
    for(int i=0;i<60;i++){
     var header=(Header)Marshal.PtrToStructure(headers[i],typeof(Header));
     int length=(int)Math.Min(header.recorded,32000u);length-=length%2;
     var bytes=new byte[length];Marshal.Copy(data[i],bytes,0,length);pcm.Write(bytes,0,length);
     for(int n=0;n<length;n+=2){int sample=(short)(bytes[n]|(bytes[n+1]<<8));energy+=(long)sample*sample;count++;}
    }
    if(count<4000||Math.Sqrt((double)energy/Math.Max(1,count))<20)throw new Exception("No usable microphone audio. Check default input and speak after REC.");
    using(var wav=new BinaryWriter(File.Create(job+".wav"))){
     wav.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));wav.Write((int)pcm.Length+36);wav.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));
     wav.Write(16);wav.Write((ushort)1);wav.Write((ushort)1);wav.Write(16000);wav.Write(32000);wav.Write((ushort)2);wav.Write((ushort)16);
     wav.Write(System.Text.Encoding.ASCII.GetBytes("data"));wav.Write((int)pcm.Length);pcm.Position=0;pcm.CopyTo(wav.BaseStream);
    }
   }
  } finally {
   if(handle!=IntPtr.Zero)waveInReset(handle);
   for(int i=0;i<60;i++){
    if(prepared[i])waveInUnprepareHeader(handle,headers[i],size);
    if(headers[i]!=IntPtr.Zero)Marshal.FreeHGlobal(headers[i]);if(data[i]!=IntPtr.Zero)Marshal.FreeHGlobal(data[i]);
   }
   if(handle!=IntPtr.Zero)waveInClose(handle);
  }
 }
}
