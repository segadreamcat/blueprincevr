using System;
using System.IO;
using System.Runtime.InteropServices;
using MelonLoader;
using UnityEngine;
using UnityEngine.Rendering;
using NativeLibrary = System.Runtime.InteropServices.NativeLibrary;

namespace BluePrinceVR;

public partial class VRPlugin
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate IntPtr EventFn();
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate int StatusFn();
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate void ConfigureFn(int eye,IntPtr texture,float cx,float cy,float inner,float outer);
    private IntPtr _vrsLibrary, _vrsEvent;
    private ConfigureFn _vrsConfigure;
    private StatusFn _vrsStatus, _vrsPasses;
    private MelonPreferences_Entry<bool> _pNativeVrs;
    private MelonPreferences_Entry<bool> _pExtraStrongVrs;
    private readonly bool[] _vrsStrongConfigured=new bool[2];
    private readonly CommandBuffer[] _vrsBegin = new CommandBuffer[2], _vrsEnd = new CommandBuffer[2];
    private readonly IntPtr[] _vrsTextures=new IntPtr[2];
    private readonly Vector2[] _vrsCenters=new Vector2[2];
    private int _lastVrsStatus=int.MinValue;

    private void InitNativeFoveation()
    {
        _pNativeVrs=_cfg.CreateEntry("NvidiaHardwareFoveation",true,null,"D3D11 NVIDIA VRS: sharp center, half-rate middle, quarter-rate outer shading. F8 toggles. Full quality in modal menus; does not enable reprojection or lower headset refresh.");
        _pExtraStrongVrs=_cfg.CreateEntry("ExtraStrongHardwareFoveation",false,null,"Smaller full-rate center and wider reduced-shading periphery. Trades peripheral clarity for potential GPU savings.");
        string file=Path.Combine(CodexAssetDirectory,"CodexVrs.dll");
        if(!File.Exists(file)) { LogWarn("[Codex VRS] Native module missing; ordinary rendering will be used."); return; }
        try {
            if(SystemInfo.graphicsDeviceType!=GraphicsDeviceType.Direct3D11) { LogWarn("[Codex VRS] Requires D3D11; ordinary rendering will be used."); return; }
            _vrsLibrary=NativeLibrary.Load(file);
            _vrsEvent=Marshal.GetDelegateForFunctionPointer<EventFn>(NativeLibrary.GetExport(_vrsLibrary,"CodexVrsEvent"))();
            _vrsConfigure=Marshal.GetDelegateForFunctionPointer<ConfigureFn>(NativeLibrary.GetExport(_vrsLibrary,"CodexVrsConfigure"));
            _vrsStatus=Marshal.GetDelegateForFunctionPointer<StatusFn>(NativeLibrary.GetExport(_vrsLibrary,"CodexVrsStatus"));
            _vrsPasses=Marshal.GetDelegateForFunctionPointer<StatusFn>(NativeLibrary.GetExport(_vrsLibrary,"CodexVrsPasses"));
            for(int i=0;i<2;i++) {
                _vrsBegin[i]=new CommandBuffer { name="Codex NVIDIA VRS begin eye "+i };
                _vrsEnd[i]=new CommandBuffer { name="Codex NVIDIA VRS end" };
                _vrsBegin[i].IssuePluginEvent(_vrsEvent,i+1); _vrsEnd[i].IssuePluginEvent(_vrsEvent,0);
            }
            Log("[Codex VRS] Native module loaded; waiting for first opaque eye pass to confirm activation.");
        } catch(Exception ex) { _vrsEvent=IntPtr.Zero; LogWarn("[Codex VRS] Unavailable: "+ex.Message); }
    }

    private bool BeginNativeFoveation(Camera cam,RenderTexture target,int eye,bool menu)
    {
        if(_vrsEvent==IntPtr.Zero || !_pNativeVrs.Value || _pFoveated.Value || menu || !_inHouse) return false;
        int status=_vrsStatus();
        if(status!=_lastVrsStatus) {
            _lastVrsStatus=status;
            Log(status==1 ? "[Codex VRS] Hardware foveation ACTIVE; driver accepted shading-rate passes." : $"[Codex VRS] status={status}; completed opaque passes={_vrsPasses()}");
        }
        if(status<0) return false;
        var p=cam.projectionMatrix;
        // D3D texture origin is at the top; use the actual GPU projection convention.
        var gpu=GL.GetGPUProjectionMatrix(p,true);
        var center=new Vector2(Mathf.Clamp01((1f-p.m02)*0.5f),Mathf.Clamp01((1f+gpu.m12)*0.5f));
        var pointer=_eyeTexPtr[eye];
        if(pointer==IntPtr.Zero) pointer=target.GetNativeTexturePtr();
        bool strong=_pExtraStrongVrs.Value;
        if(_vrsTextures[eye]!=pointer || _vrsCenters[eye]!=center || _vrsStrongConfigured[eye]!=strong) {
            _vrsConfigure(eye,pointer,center.x,center.y,strong?0.4f:0.6f,strong?0.65f:0.9f);
            _vrsTextures[eye]=pointer; _vrsCenters[eye]=center;
            _vrsStrongConfigured[eye]=strong;
        }
        cam.AddCommandBuffer(CameraEvent.BeforeForwardOpaque,_vrsBegin[eye]);
        cam.AddCommandBuffer(CameraEvent.AfterForwardOpaque,_vrsEnd[eye]);
        cam.AddCommandBuffer(CameraEvent.BeforeGBuffer,_vrsBegin[eye]);
        cam.AddCommandBuffer(CameraEvent.AfterGBuffer,_vrsEnd[eye]);
        return true;
    }

    private void EndNativeFoveation(Camera cam,int eye)
    {
        cam.RemoveCommandBuffer(CameraEvent.BeforeForwardOpaque,_vrsBegin[eye]);
        cam.RemoveCommandBuffer(CameraEvent.AfterForwardOpaque,_vrsEnd[eye]);
        cam.RemoveCommandBuffer(CameraEvent.BeforeGBuffer,_vrsBegin[eye]);
        cam.RemoveCommandBuffer(CameraEvent.AfterGBuffer,_vrsEnd[eye]);
        GL.IssuePluginEvent(_vrsEvent,0);
    }
}
