#define WIN32_LEAN_AND_MEAN
#define NOMINMAX
#include <windows.h>
#include <d3d11.h>
#include <wrl/client.h>
#include <nvapi.h>
#include <atomic>
#include <mutex>
#include <vector>
#include <cmath>
#include <algorithm>
#include <cstdio>
using Microsoft::WRL::ComPtr;
namespace {
struct Eye {
 ComPtr<ID3D11Texture2D> target, pattern;
 ComPtr<ID3D11NvShadingRateResourceView> view;
 float cx=0.5f,cy=0.5f,inner=0.6f,outer=0.9f;
};
Eye eyes[2];
ComPtr<ID3D11Device> device;
ComPtr<ID3D11DeviceContext> context;
std::mutex gate;
std::atomic<int> status{0},passes{0};
bool initialized=false;
void Disable() {
 if(!initialized || !context) return;
 NV_D3D11_VIEWPORT_SHADING_RATE_DESC rate{};
 rate.enableVariablePixelShadingRate=false;
 NV_D3D11_VIEWPORTS_SHADING_RATE_DESC desc{};
 desc.version=NV_D3D11_VIEWPORTS_SHADING_RATE_DESC_VER; desc.numViewports=1; desc.pViewports=&rate;
 NvAPI_D3D11_RSSetViewportsPixelShadingRates(context.Get(),&desc);
 NvAPI_D3D11_RSSetShadingRateResourceView(context.Get(),nullptr);
}
void Fail(int code) { Disable(); status.store(code ? code : -1000); }
bool Initialize(ID3D11Texture2D* target) {
 if(initialized) return true;
 auto result=NvAPI_Initialize(); if(result!=NVAPI_OK) { Fail(result); return false; }
 target->GetDevice(device.GetAddressOf()); device->GetImmediateContext(context.GetAddressOf());
 NV_D3D1x_GRAPHICS_CAPS caps{};
 result=NvAPI_D3D1x_GetGraphicsCapabilities(device.Get(),NV_D3D1x_GRAPHICS_CAPS_VER,&caps);
 if(result!=NVAPI_OK || !caps.bVariablePixelRateShadingSupported) { Fail(result==NVAPI_OK ? -1001 : result); return false; }
 initialized=true; return true;
}
bool MakePattern(Eye& eye,const D3D11_TEXTURE2D_DESC& target) {
 const UINT tw=NV_VARIABLE_PIXEL_SHADING_TILE_WIDTH,th=NV_VARIABLE_PIXEL_SHADING_TILE_HEIGHT;
 UINT w=(target.Width+tw-1)/tw,h=(target.Height+th-1)/th;
 std::vector<unsigned char> bytes(w*h);
 for(UINT y=0;y<h;y++) for(UINT x=0;x<w;x++) {
  float dx=(x+0.5f)*tw/target.Width-eye.cx,dy=(y+0.5f)*th/target.Height-eye.cy;
  float radius=2*std::sqrt(dx*dx+dy*dy);
  bytes[y*w+x]=radius<eye.inner ? 0 : radius<eye.outer ? 1 : 2;
 }
 D3D11_TEXTURE2D_DESC desc{};
 desc.Width=w;desc.Height=h;desc.MipLevels=1;desc.ArraySize=1;desc.Format=DXGI_FORMAT_R8_UINT;
 desc.SampleDesc.Count=1;desc.Usage=D3D11_USAGE_IMMUTABLE;desc.BindFlags=D3D11_BIND_SHADER_RESOURCE;
 D3D11_SUBRESOURCE_DATA data{};data.pSysMem=bytes.data();data.SysMemPitch=w;
 if(FAILED(device->CreateTexture2D(&desc,&data,eye.pattern.ReleaseAndGetAddressOf()))) { Fail(-1002); return false; }
 NV_D3D11_SHADING_RATE_RESOURCE_VIEW_DESC view{};
 view.version=NV_D3D11_SHADING_RATE_RESOURCE_VIEW_DESC_VER;view.Format=desc.Format;view.ViewDimension=NV_SRRV_DIMENSION_TEXTURE2D;
 auto result=NvAPI_D3D11_CreateShadingRateResourceView(device.Get(),eye.pattern.Get(),&view,eye.view.ReleaseAndGetAddressOf());
 if(result!=NVAPI_OK) { Fail(result); return false; } return true;
}
void __stdcall RenderEvent(int event) {
 std::lock_guard<std::mutex> lock(gate);
 if(event==0) { Disable(); return; }
 if(event<1 || event>2 || status.load()<0) return;
 Eye& eye=eyes[event-1]; if(!eye.target || !Initialize(eye.target.Get())) return;
 ComPtr<ID3D11RenderTargetView> current;context->OMGetRenderTargets(1,current.GetAddressOf(),nullptr);
 if(!current) { Disable(); return; }
 ComPtr<ID3D11Resource> resource;current->GetResource(resource.GetAddressOf());
 ComPtr<ID3D11Texture2D> texture; if(FAILED(resource.As(&texture))) { Disable(); return; }
 D3D11_TEXTURE2D_DESC actual{},expected{};texture->GetDesc(&actual);eye.target->GetDesc(&expected);
 // Opaque passes only; exclude shadow maps and differently sized post-process buffers.
 if(actual.Width!=expected.Width || actual.Height!=expected.Height) { Disable(); return; }
 if(!eye.view && !MakePattern(eye,expected)) return;
 auto result=NvAPI_D3D11_RSSetShadingRateResourceView(context.Get(),eye.view.Get());
 if(result!=NVAPI_OK) { Fail(result); return; }
 NV_D3D11_VIEWPORT_SHADING_RATE_DESC rate{}; rate.enableVariablePixelShadingRate=true;
 std::fill(std::begin(rate.shadingRateTable),std::end(rate.shadingRateTable),NV_PIXEL_X1_PER_RASTER_PIXEL);
 rate.shadingRateTable[0]=NV_PIXEL_X1_PER_RASTER_PIXEL;
 rate.shadingRateTable[1]=NV_PIXEL_X1_PER_2X1_RASTER_PIXELS;
 rate.shadingRateTable[2]=NV_PIXEL_X1_PER_2X2_RASTER_PIXELS;
 NV_D3D11_VIEWPORTS_SHADING_RATE_DESC desc{};
 desc.version=NV_D3D11_VIEWPORTS_SHADING_RATE_DESC_VER;desc.numViewports=1;desc.pViewports=&rate;
 result=NvAPI_D3D11_RSSetViewportsPixelShadingRates(context.Get(),&desc);
 if(result!=NVAPI_OK) { Fail(result); return; }
 status.store(1);passes.fetch_add(1);
}
}
extern "C" __declspec(dllexport) void* __cdecl CodexVrsEvent() { return reinterpret_cast<void*>(&RenderEvent); }
extern "C" __declspec(dllexport) int __cdecl CodexVrsStatus() { return status.load(); }
extern "C" __declspec(dllexport) int __cdecl CodexVrsPasses() { return passes.load(); }
extern "C" __declspec(dllexport) void __cdecl CodexVrsConfigure(int index,void* texture,float cx,float cy,float inner,float outer) {
 if(index<0 || index>1) return;
 std::lock_guard<std::mutex> lock(gate);Eye& eye=eyes[index];
 auto target=static_cast<ID3D11Texture2D*>(texture);
 if(eye.target.Get()==target && eye.cx==cx && eye.cy==cy && eye.inner==inner && eye.outer==outer) return;
 eye.target=target;eye.cx=std::clamp(cx,0.f,1.f);eye.cy=std::clamp(cy,0.f,1.f);
 eye.inner=std::clamp(inner,0.35f,0.9f);eye.outer=std::clamp(outer,eye.inner,1.4f);
 eye.pattern.Reset();eye.view.Reset();
}
#ifdef CODEX_VRS_TEST
int main() {
 ComPtr<ID3D11Device> testDevice;ComPtr<ID3D11DeviceContext> testContext;
 HRESULT hr=D3D11CreateDevice(nullptr,D3D_DRIVER_TYPE_HARDWARE,nullptr,0,nullptr,0,D3D11_SDK_VERSION,testDevice.GetAddressOf(),nullptr,testContext.GetAddressOf());
 if(FAILED(hr)) { std::printf("D3D11 device failed: %lx\n",hr); return 1; }
 D3D11_TEXTURE2D_DESC desc{};desc.Width=2496;desc.Height=2688;desc.MipLevels=desc.ArraySize=desc.SampleDesc.Count=1;
 desc.Format=DXGI_FORMAT_R8G8B8A8_UNORM;desc.BindFlags=D3D11_BIND_RENDER_TARGET;
 ComPtr<ID3D11Texture2D> target;testDevice->CreateTexture2D(&desc,nullptr,target.GetAddressOf());
 ComPtr<ID3D11RenderTargetView> view;testDevice->CreateRenderTargetView(target.Get(),nullptr,view.GetAddressOf());
 auto rtv=view.Get();testContext->OMSetRenderTargets(1,&rtv,nullptr);
 CodexVrsConfigure(0,target.Get(),0.45f,0.5f,0.6f,0.9f);RenderEvent(1);RenderEvent(0);
 std::printf("Native NVIDIA VRS: status=%d, successful enabled passes=%d (1 means hardware API accepted).\n",status.load(),passes.load());
 return status.load()==1 ? 0 : 1;
}
#endif
