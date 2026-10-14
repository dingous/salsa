Shader "AvatarStudio/Background"
{
    Properties { _Top("Top",Color)=(.15,.17,.29,1) _Bottom("Bottom",Color)=(.04,.05,.09,1) _Glow("Glow",Color)=(.24,.20,.43,1) }
    SubShader {
        Tags { "RenderType"="Opaque" "Queue"="Background" }
        Cull Off ZWrite Off
        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata {float4 vertex:POSITION; float2 uv:TEXCOORD0;};
            struct v2f {float4 pos:SV_POSITION;float2 uv:TEXCOORD0;};
            float4 _Top,_Bottom,_Glow;
            v2f vert(appdata v) {v2f o;o.pos=UnityObjectToClipPos(v.vertex);o.uv=v.uv;return o;}
            fixed4 frag(v2f i):SV_Target {float glow=exp(-dot((i.uv-float2(.5,.57))*float2(2.3,1.7),(i.uv-float2(.5,.57))*float2(2.3,1.7))*3);return float4(lerp(_Bottom.rgb,_Top.rgb,i.uv.y)+_Glow.rgb*glow*.55,1);}
            ENDCG
        }
    }
}
