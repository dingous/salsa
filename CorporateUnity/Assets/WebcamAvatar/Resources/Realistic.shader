Shader "AvatarStudio/Realistic"
{
    Properties {
        _Color ("Color", Color) = (1,1,1,1)
        _Glossiness ("Smoothness", Range(0,1)) = 0.25
    }
    SubShader {
        Tags { "RenderType"="Opaque" }
        LOD 200
        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows vertex:vert
        #pragma target 3.0
        fixed4 _Color;
        half _Glossiness;
        struct Input { float4 vertexColor : COLOR; float3 worldPos; };
        void vert(inout appdata_full v, out Input o) {
            UNITY_INITIALIZE_OUTPUT(Input,o);
            o.vertexColor=v.color;
        }
        void surf(Input i, inout SurfaceOutputStandard o) {
            // Fine, low-contrast material grain; photo-derived colors remain editable.
            float grain=frac(sin(dot(i.worldPos,float3(127.1,311.7,74.7))*280)*43758.5453);
            o.Albedo=_Color.rgb*i.vertexColor.rgb*lerp(.60,.62,grain);
            o.Metallic=0;
            o.Smoothness=_Glossiness;
            o.Alpha=1;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
