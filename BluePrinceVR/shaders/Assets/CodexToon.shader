Shader "BluePrinceVR/CodexToon" {
 Properties {
  _MainTex ("Original palette", 2D) = "white" {}
  _Color ("Tint", Color) = (1,1,1,1)
  _Skin ("Skin", Color) = (0.69,0.55,0.47,1)
  _Sleeve ("Rolled ivory sleeve", Color) = (0.78,0.78,0.70,1)
 }
 SubShader { Tags { "RenderType"="Opaque" }
  Pass {
   CGPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "UnityCG.cginc"
   struct appdata { float4 vertex:POSITION; float3 normal:NORMAL; float2 uv:TEXCOORD0; };
   struct v2f { float4 vertex:SV_POSITION; float3 normal:TEXCOORD0; float2 uv:TEXCOORD1; };
   sampler2D _MainTex; float4 _Color, _Skin, _Sleeve;
   v2f vert(appdata v) { v2f o; o.vertex=UnityObjectToClipPos(v.vertex); o.normal=UnityObjectToWorldNormal(v.normal); o.uv=v.uv; return o; }
   fixed4 frag(v2f i):SV_Target {
    float3 original=tex2D(_MainTex,i.uv).rgb;
    float warm=smoothstep(0.015,0.065,original.r-original.g);
    float3 base=lerp(_Sleeve.rgb,_Skin.rgb,warm);
    // Preserve dark wristband/ink, suppress photoreal wrinkles and noisy highlights.
    float ink=smoothstep(0.12,0.28,max(original.r,max(original.g,original.b)));
    base=lerp(original,lerp(base,original,0.12),ink);
    float light=dot(normalize(i.normal),normalize(float3(-0.35,0.8,0.45)));
    float shade=0.62+0.22*smoothstep(-0.08,-0.04,light)+0.16*smoothstep(0.55,0.59,light);
    return fixed4(base*shade*_Color.rgb,1);
   }
   ENDCG
  }
 }
 Fallback "Unlit/Texture"
}
