Shader "CorporateAvatar/AlphaCutout"
{
    Properties {
        _MainTex("Texture",2D)="white" {}
        _Color("Color",Color)=(1,1,1,1)
        _Cutoff("Alpha cutoff",Range(0,1))=.35
        _Glossiness("Smoothness",Range(0,1))=.15
    }
    SubShader {
        Tags {"RenderType"="TransparentCutout" "Queue"="AlphaTest"}
        Cull Off
        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows addshadow
        #pragma target 3.0
        sampler2D _MainTex;
        fixed4 _Color;
        half _Cutoff,_Glossiness;
        struct Input {float2 uv_MainTex;};
        void surf(Input i,inout SurfaceOutputStandard o) {
            fixed4 color=tex2D(_MainTex,i.uv_MainTex)*_Color;
            // Always clip: runtime-imported glTF has no editor material to
            // preserve Standard's optional alpha-test shader variant in builds.
            clip(color.a-_Cutoff);
            o.Albedo=color.rgb;o.Alpha=1;o.Metallic=0;o.Smoothness=_Glossiness;
        }
        ENDCG
    }
    Fallback "Transparent/Cutout/Diffuse"
}
