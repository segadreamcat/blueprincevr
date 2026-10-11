Shader "BluePrinceVR/FoveatedComposite" {
 Properties { _MainTex ("Peripheral",2D)="black" {} _FocusTex ("Focus",2D)="black" {} }
 SubShader { Cull Off ZWrite Off ZTest Always
  Pass {
   CGPROGRAM
   #pragma vertex vert_img
   #pragma fragment frag
   #include "UnityCG.cginc"
   sampler2D _MainTex, _FocusTex;
   float4 _FocusRect;
   fixed4 frag(v2f_img i):SV_Target {
    float2 uv=(i.uv-_FocusRect.xy)/_FocusRect.zw;
    float edge=min(min(uv.x,uv.y),min(1-uv.x,1-uv.y));
    float blend=smoothstep(0,0.06,edge);
    return lerp(tex2D(_MainTex,i.uv),tex2D(_FocusTex,saturate(uv)),blend);
   }
   ENDCG
  }
 }
}
