Shader "AvatarStudio/Cartoon"
{
    Properties { _Color("Color",Color)=(1,1,1,1) _Glossiness("Gloss",Range(0,1))=.2 _Fiber("Hair fiber",Range(0,1))=0 }
    SubShader {
        Tags { "RenderType"="Opaque" }
        Pass {
            Tags { "LightMode"="ForwardBase" }
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct a2v {float4 vertex:POSITION;float3 normal:NORMAL;float4 color:COLOR;float2 uv:TEXCOORD0;};
            struct v2f {float4 pos:SV_POSITION;float3 normal:TEXCOORD0;float3 view:TEXCOORD1;float4 color:COLOR;float2 uv:TEXCOORD2;};
            float4 _Color;float _Glossiness,_Fiber;
            v2f vert(a2v v){v2f o;o.pos=UnityObjectToClipPos(v.vertex);float3 world=mul(unity_ObjectToWorld,v.vertex).xyz;o.normal=UnityObjectToWorldNormal(v.normal);o.view=_WorldSpaceCameraPos-world;o.color=v.color;o.uv=v.uv;return o;}
            fixed4 frag(v2f i):SV_Target {
                float3 n=normalize(i.normal),view=normalize(i.view),light=normalize(float3(-.45,.7,-1));
                float diffuse=saturate(dot(n,light)*.5+.5);
                float rim=pow(1-saturate(dot(n,view)),3)*.07;
                float spec=pow(saturate(dot(n,normalize(light+view))),32)*_Glossiness*.25;
                float fiber=1-_Fiber*(.045+.028*sin(i.uv.x*6.28318*7+i.uv.y*4));
                return float4(_Color.rgb*i.color.rgb*(.60+diffuse*.40)*fiber+float3(.50,.48,.68)*rim+spec,_Color.a);
            }
            ENDCG
        }
    }
}
