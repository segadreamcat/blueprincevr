using System;
using System.IO;
using MelonLoader;
using UnityEngine;
using UnityEngine.Rendering;

namespace BluePrinceVR;

public partial class VRPlugin
{
    private bool _codexBootScene = true;
    private bool _mainMenuScene;
    private bool _importedArms;
    public override void OnGUI()
    {
        if (_computerKeyboardMode && _activeComputer!=null)
            GUI.Label(new Rect(12,45,900,65),TypingInstruction());
        GUI.Label(new Rect(12, 12, 1100, 30), "CODEX VR 2.10.57 TEST | " + (_codexBootScene ? "Game startup — waiting for next scene" : "Y: floor plan | Hold Y: pause | Tap X: journal | Hold X: recorder"));
    }
    private string _lastStartupTrace;
    private void TraceStartupFrame(string stage)
    {
        if (_sceneGrace > 0 || _framesRendered >= 30) return;
        string message = $"[Codex frame {_framesRendered}] {stage}";
        if (message == _lastStartupTrace) return;
        _lastStartupTrace = message;
        Log(message);
    }
    private MelonPreferences_Entry<bool> _pPanelAdjust, _pGpuProbe, _pFoveated, _pRoundedArms;
    private MelonPreferences_Entry<float> _pFocusSize, _pPeripheralScale;
    private readonly RenderTexture[] _peripheral = new RenderTexture[2], _focus = new RenderTexture[2];
    private Material _foveatedMaterial;
    private AssetBundle _codexBundle;
    private bool _ffrFailed, _panelAdjusting;
    private float _panelDistance = 1.2f;
    private string CodexAssetDirectory => Path.Combine(Path.GetDirectoryName(typeof(VRPlugin).Assembly.Location), "BluePrinceVR-Codex");

    private void InitCodex()
    {
        _pShowCursor.Value = false; // User requested no floating aim ball in gameplay.
        _pHideStepHud.Value = false;
        _pPanelAdjust = _cfg.CreateEntry("GripStickPanelDistance", true, null, "While a panel is open: hold RIGHT grip and pull right stick down to bring it closer; push up to move away. Release grip to select normally.");
        _pGpuProbe = _cfg.CreateEntry("PeriodicGpuReadback", false, null, "Diagnostic pixel readback; can stall the GPU. Leave off for performance testing.");
        _pFoveated = _cfg.CreateEntry("ExperimentalFixedFoveated", false, null, "Two-pass fixed foveation. F7 toggles for A/B testing. Can help fill-rate-bound GPUs but can cost CPU time. Menus always use full resolution. Not eye tracking or NVIDIA VRS.");
        _pFocusSize = _cfg.CreateEntry("FoveatedCenterFraction", 0.65f, null, "Full-detail central fraction of each dimension, 0.4 to 0.9.");
        _pPeripheralScale = _cfg.CreateEntry("FoveatedPeripheralScale", 0.5f, null, "Resolution multiplier for peripheral image, 0.3 to 0.8.");
        _pRoundedArms = _cfg.CreateEntry("RoundedToonArms", true, null, "Reshape boxy sections, smooth twice, use matte scene-lit three-tone skin/ivory and a thin outline. Overrides legacy arm brightness/unlit settings while enabled.");
        _panelDistance = Mathf.Clamp(_pUIDistance.Value, 0.35f, 2.5f);
        var bundlePath = Path.Combine(CodexAssetDirectory, "codex-shaders.bundle");
        if (File.Exists(bundlePath))
        {
            try
            {
                _codexBundle = AssetBundle.LoadFromFile(bundlePath);
                var shader = _codexBundle?.LoadAsset<Shader>("Assets/FoveatedComposite.shader");
                if (shader != null && shader.isSupported) _foveatedMaterial = new Material(shader);
            }
            catch (Exception ex) { LogWarn("[Codex] Optional shaders unavailable: " + ex.Message); }
        }
        Log("[Codex] Native 90 target = 11.11 ms/frame; set headset refresh to 90 Hz. No refresh/reprojection settings are changed automatically.");
    }

    private void CodexHotkeys()
    {
        if (Input.GetKeyDown(KeyCode.F6)) _manualVisualCapture = true;
        if (Input.GetKeyDown(KeyCode.F8))
        {
            if (_pNativeVrs == null) InitNativeFoveation();
            _pNativeVrs.Value = !_pNativeVrs.Value;
            _pFoveated.Value = false;
            Log("[Codex VRS] Hardware foveation requested " + (_pNativeVrs.Value ? "ON" : "OFF"));
        }
        if (Input.GetKeyDown(KeyCode.F7))
        {
            _pFoveated.Value = !_pFoveated.Value;
            _ffrFailed = false;
            Log("[Codex] Experimental fixed foveation " + (_pFoveated.Value ? "ON" : "OFF") + "; compare SteamVR GPU and CPU frametimes.");
        }
    }

    private void RenderCodexEye(Camera cam, RenderTexture destination, int eye)
    {
        if (_inHouse && _pNativeVrs == null) InitNativeFoveation();
        bool panel = _uiQuad != null && _uiQuad.activeInHierarchy && _uiQuadRenderer != null;
        bool wasPanelEnabled = panel && _uiQuadRenderer.enabled;
        var savedTarget = cam.targetTexture;
        var savedProjection = cam.projectionMatrix;
        var savedRect = cam.rect;
        var savedClear = cam.clearFlags;
        var savedMask = cam.cullingMask;
        var savedCulling = cam.cullingMatrix;
        bool savedOcclusion = cam.useOcclusionCulling;
        bool savedSrgb = GL.sRGBWrite;
        // RenderTexture.active is also changed by Blit; restore it for other mods/game cameras.
        var savedActive = RenderTexture.active;
        bool nativeAttached = false;
        try
        {
            // A tracked eye can see through doors outside the desktop view.
            cam.useOcclusionCulling = false;
            cam.cullingMatrix = cam.projectionMatrix * cam.worldToCameraMatrix;
            if (panel) _uiQuadRenderer.enabled = false;
            nativeAttached = BeginNativeFoveation(cam, destination, eye, panel || _computerKeyboardMode || VRInput.MenuMode || Time.timeScale <= 0f);
            bool foveate = _pFoveated.Value && !_ffrFailed && _inHouse && !panel && !_computerKeyboardMode && !VRInput.MenuMode && Time.timeScale > 0f;
            if (foveate)
            {
                try { RenderFoveated(cam, destination, eye); }
                catch (Exception ex)
                {
                    _ffrFailed = true;
                    LogWarn("[Codex] Foveation failed; using ordinary stereo: " + ex.Message);
                    cam.projectionMatrix = savedProjection;
                    cam.targetTexture = destination;
                    cam.rect = savedRect;
                    cam.clearFlags = savedClear;
                    RenderWithoutBeautify(cam);
                }
            }
            else RenderWithoutBeautify(cam);

            if (panel && wasPanelEnabled)
            {
                RenderCardOverlay(cam, destination);
            }
        }
        finally
        {
            if (nativeAttached) EndNativeFoveation(cam, eye);
            if (panel) _uiQuadRenderer.enabled = wasPanelEnabled;
            cam.targetTexture = savedTarget;
            cam.projectionMatrix = savedProjection;
            cam.rect = savedRect;
            cam.clearFlags = savedClear;
            cam.cullingMask = savedMask;
            cam.cullingMatrix = savedCulling;
            cam.useOcclusionCulling = savedOcclusion;
            GL.sRGBWrite = savedSrgb;
            RenderTexture.active = savedActive;
        }
    }

    private Camera _cardOverlayCamera;
    private int _cardOverlayLayer = -1;
    private bool _manualVisualCapture;
    private int _captureCount;

    private void RenderCardOverlay(Camera source, RenderTexture target)
    {
        if (_cardOverlayCamera == null)
        {
            int occupied = 0;
            foreach (var renderer in UnityEngine.Object.FindObjectsOfType<Renderer>())
                if (renderer != null) occupied |= 1 << renderer.gameObject.layer;
            for (int layer = 31; layer >= 8; layer--)
                if ((occupied & (1 << layer)) == 0) { _cardOverlayLayer = layer; break; }
            if (_cardOverlayLayer < 0) throw new InvalidOperationException("No unused renderer layer for card overlay");
            var root = new GameObject("Codex_CardOverlayCamera");
            UnityEngine.Object.DontDestroyOnLoad(root);
            _cardOverlayCamera = root.AddComponent<Camera>();
            _cardOverlayCamera.enabled = false;
            Log("[Codex] Isolated card overlay camera, layer " + _cardOverlayLayer);
        }
        int oldLayer = _uiQuad.layer;
        try
        {
            _uiQuad.layer = _cardOverlayLayer;
            _uiQuadRenderer.enabled = true;
            _cardOverlayCamera.CopyFrom(source);
            _cardOverlayCamera.enabled = false;
            _cardOverlayCamera.transform.SetPositionAndRotation(source.transform.position, source.transform.rotation);
            _cardOverlayCamera.targetTexture = target;
            _cardOverlayCamera.cullingMask = 1 << _cardOverlayLayer;
            _cardOverlayCamera.clearFlags = CameraClearFlags.Depth;
            _cardOverlayCamera.useOcclusionCulling = false;
            _cardOverlayCamera.worldToCameraMatrix = source.worldToCameraMatrix;
            _cardOverlayCamera.projectionMatrix = source.projectionMatrix;
            _cardOverlayCamera.cullingMatrix = source.projectionMatrix * source.worldToCameraMatrix;
            _cardOverlayCamera.Render();
        }
        finally { _uiQuad.layer = oldLayer; _cardOverlayCamera.targetTexture = null; }
    }

    private void CaptureVisualDiagnostics()
    {
        // PNG encoding/readback stalled menu opening. Only capture on explicit F6.
        if (!_manualVisualCapture || !_inHouse) return;
        _manualVisualCapture = false;
        _captureCount++;
        try
        {
            string folder = Path.Combine(Application.dataPath, "..", "UserData", "Codex-Visual-Diagnostics");
            Directory.CreateDirectory(folder);
            string prefix = Path.Combine(folder, DateTime.Now.ToString("yyyyMMdd-HHmmss") + "-" + _captureCount);
            SaveRT(_uiRT, prefix + "-cards.png");
            SaveRT(_eyeRT[0], prefix + "-left.png");
            SaveRT(_eyeRT[1], prefix + "-right.png");
            Log("[Codex] Visual diagnostics saved: " + prefix);
        }
        catch (Exception ex) { LogWarn("[Codex] Visual capture failed: " + ex.Message); }
    }

    private void RenderFoveated(Camera cam, RenderTexture target, int eye)
    {
        var projection = cam.projectionMatrix;
        float fraction = Mathf.Clamp(_pFocusSize.Value, 0.4f, 0.9f);
        float lowScale = Mathf.Clamp(_pPeripheralScale.Value, 0.3f, 0.8f);
        int width = Mathf.Max(2, Mathf.RoundToInt(target.width * fraction) / 2 * 2);
        int height = Mathf.Max(2, Mathf.RoundToInt(target.height * fraction) / 2 * 2);
        EnsureFoveatedTexture(ref _peripheral[eye], target, Mathf.RoundToInt(target.width * lowScale), Mathf.RoundToInt(target.height * lowScale));
        EnsureFoveatedTexture(ref _focus[eye], target, width, height);
        cam.targetTexture = _peripheral[eye];
        RenderWithoutBeautify(cam);
        // Place the high-detail region around the optical axis for each asymmetric eye.
        int x = Mathf.Clamp(Mathf.RoundToInt((1f - projection.m02) * 0.5f * target.width - width * 0.5f), 0, target.width-width);
        int y = Mathf.Clamp(Mathf.RoundToInt((1f - projection.m12) * 0.5f * target.height - height * 0.5f), 0, target.height-height);
        float sx = (float)width/target.width, sy = (float)height/target.height;
        float cx = (2f*x+width)/target.width-1f, cy=(2f*y+height)/target.height-1f;
        var crop = Matrix4x4.identity;
        crop.m00=1f/sx; crop.m11=1f/sy; crop.m03=-cx/sx; crop.m13=-cy/sy;
        cam.projectionMatrix = crop * projection;
        cam.targetTexture = _focus[eye];
        RenderWithoutBeautify(cam);
        cam.projectionMatrix = projection;
        cam.targetTexture = target;
        GL.sRGBWrite = target.sRGB && QualitySettings.activeColorSpace == ColorSpace.Linear;
        if (_foveatedMaterial != null)
        {
            _foveatedMaterial.SetTexture("_FocusTex", _focus[eye]);
            _foveatedMaterial.SetVector("_FocusRect", new Vector4((float)x/target.width,(float)y/target.height,sx,sy));
            Graphics.Blit(_peripheral[eye],target,_foveatedMaterial);
        }
        else
        {
            Graphics.Blit(_peripheral[eye],target);
            Graphics.CopyTexture(_focus[eye],0,0,0,0,width,height,target,0,0,x,y);
        }
    }

    private static void EnsureFoveatedTexture(ref RenderTexture texture, RenderTexture target, int width, int height)
    {
        width = Mathf.Max(64,width); height=Mathf.Max(64,height);
        if(texture != null && texture.width==width && texture.height==height && texture.IsCreated()) return;
        if(texture != null) { texture.Release(); UnityEngine.Object.Destroy(texture); }
        texture = new RenderTexture(width,height,24,target.format,target.sRGB ? RenderTextureReadWrite.sRGB : RenderTextureReadWrite.Linear);
        texture.antiAliasing=1; texture.filterMode=FilterMode.Bilinear;
        texture.hideFlags=HideFlags.HideAndDontSave;
        texture.Create(); UnityEngine.Object.DontDestroyOnLoad(texture);
    }

    private void ApplyCodexArms(ArmsModel model)
    {
        if ((!_pRoundedArms.Value && !_importedArms) || model?.Renderer == null) return;
        try
        {
              if (!_importedArms)
            {
            ReshapeArmVolume(model);
            ArmRefinement.Apply(model.Renderer);
            ArmRefinement.Apply(model.Renderer);
              ArmRefinement.ApplyBakedPalette(model.Renderer);
              }
              else
              {
                  // The binary duplicates vertices per triangle. RecalculateNormals
                  // after the reach refit made every triangle visibly flat-shaded.
                  // Weld geometrically for one smoothing pass, keeping UV/skin seams.
                  ArmRefinement.Apply(model.Renderer);
                  ArmRefinement.ApplyBakedPalette(model.Renderer);
              }
            // The replacement mesh's rest-pose bounds do not describe tracked hands.
            model.Renderer.updateWhenOffscreen = true;
            model.Renderer.localBounds = new Bounds(Vector3.zero, new Vector3(8f, 8f, 8f));
            model.Renderer.receiveShadows = true;
            foreach(var renderer in model.Root.GetComponentsInChildren<Renderer>())
                if(renderer != null && renderer.sharedMaterial != null) ArmRefinement.MakeMatte(renderer.sharedMaterial);
            AddArmOutline(model);
            Log((_importedArms ? "[Codex] Imported FPS Arms rig with rolled sleeves and supplied palette: " : "[Codex] Refined legacy arms: ") +
                model.Renderer.sharedMesh.vertexCount + " vertices; emission/specular disabled.");
        }
        catch(Exception ex) { LogWarn("[Codex] Arm refinement unavailable: " + ex.Message); }
    }

    private void ReleaseCodex()
    {
        ReleaseComputerKeys();
        ReleaseForearmPreview();
        _celHands?.Dispose();_celHands=null;
        if(_comfortText!=null)UnityEngine.Object.Destroy(_comfortText.gameObject);
        if (_cardOverlayCamera != null) UnityEngine.Object.Destroy(_cardOverlayCamera.gameObject);
        foreach(var rt in _peripheral) if(rt!=null) { rt.Release(); UnityEngine.Object.Destroy(rt); }
        foreach(var rt in _focus) if(rt!=null) { rt.Release(); UnityEngine.Object.Destroy(rt); }
        if(_foveatedMaterial!=null) UnityEngine.Object.Destroy(_foveatedMaterial);
        if(_codexBundle!=null) _codexBundle.Unload(false);
    }
}
