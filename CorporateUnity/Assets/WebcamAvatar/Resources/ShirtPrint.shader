Shader "CorporateAvatar/ShirtPrint" {
 Properties { _MainTex("Fabric",2D)="white" {} _Color("Color",Color)=(1,1,1,1) _Logo("Logo",2D)="white" {} _HasLogo("Has logo",Float)=0 _Pattern("Pattern",Float)=0 _LogoRect("Logo rectangle",Vector)=(0,1,.2,.2) }
 SubShader { Tags {"RenderType"="Opaque"} Cull Off
 CGPROGRAM
 #pragma surface surf Standard vertex:vert
 #pragma target 4.0
 sampler2D _MainTex,_Logo; float4 _Color,_LogoRect; float _HasLogo,_Pattern;
 struct Input {float2 uv_MainTex;float3 garmentPosition;float3 garmentNormal;float3 worldPos;};
 void vert(inout appdata_full v,out Input o){UNITY_INITIALIZE_OUTPUT(Input,o);o.garmentPosition=v.vertex.xyz;o.garmentNormal=v.normal;}
 void surf(Input i,inout SurfaceOutputStandard o){
  fixed4 fabric=tex2D(_MainTex,i.uv_MainTex)*_Color;
  float stripe=step(.5,frac(i.garmentPosition.y*28));
  if(_Pattern>.5&&_Pattern<1.5)fabric.rgb=lerp(fabric.rgb,fabric.rgb*.45,stripe*.7);
  if(_Pattern>1.5&&_Pattern<2.5)fabric.rgb*=i.garmentPosition.y<_LogoRect.y?.5:1;
  if(_Pattern>2.5)fabric.rgb*=lerp(.5,1,step(.08,abs(i.garmentPosition.x-_LogoRect.x)));
  float2 uv=(i.worldPos.xy-_LogoRect.xy)/_LogoRect.zw+.5;
  uv.x=1-uv.x;
  if(_HasLogo>.5&&i.garmentNormal.z>.25&&all(uv>=0)&&all(uv<=1)){fixed4 logo=tex2D(_Logo,uv);fabric.rgb=lerp(fabric.rgb,logo.rgb,logo.a);}
  o.Albedo=fabric.rgb;o.Alpha=1;o.Smoothness=.15;o.Metallic=0;
 }
 ENDCG
 } Fallback "Diffuse"
}

