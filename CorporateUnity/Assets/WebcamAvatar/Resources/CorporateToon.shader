Shader "CorporateAvatar/TexturedToon"
{
    Properties {
        _MainTex("Texture",2D)="white" {}
        _Color("Color",Color)=(1,1,1,1)
        _Cutoff("Alpha cutoff",Range(0,1))=.45
    }
    SubShader {
        Tags {"RenderType"="TransparentCutout" "Queue"="AlphaTest"}
        Cull Off
        Pass {
            Tags {"LightMode"="ForwardBase"}
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            #include "Lighting.cginc"
            sampler2D _MainTex; float4 _MainTex_ST,_Color; float _Cutoff;
            struct appdata {float4 vertex:POSITION;float3 normal:NORMAL;float2 uv:TEXCOORD0;};
            struct v2f {float4 position:SV_POSITION;float2 uv:TEXCOORD0;float3 normal:TEXCOORD1;};
            v2f vert(appdata v) {v2f o;o.position=UnityObjectToClipPos(v.vertex);o.uv=TRANSFORM_TEX(v.uv,_MainTex);o.normal=UnityObjectToWorldNormal(v.normal);return o;}
            fixed4 frag(v2f i):SV_Target {
                fixed4 color=tex2D(_MainTex,i.uv)*_Color;clip(color.a-_Cutoff);
                float light=saturate(dot(normalize(i.normal),normalize(_WorldSpaceLightPos0.xyz)));
                float shade=lerp(.72,1,smoothstep(.25,.40,light));
                return fixed4(color.rgb*shade,color.a);
            }
            ENDCG
        }
    }
    Fallback "Transparent/Cutout/Diffuse"
}
