Shader "Hidden/Custom/FullSunshaft"
{
    Properties
    {
        _MainTex ("MainTex", 2D) = "white" {}
        _SunPos ("Sun Position (Viewport)", Vector) = (0.5, 0.5, 0, 0)
        _BlurRadius ("Blur Radius", Float) = 0.85
        _Threshold ("Brightness Threshold", Float) = 0.7
        _Intensity ("Sunshaft Intensity", Float) = 1.0
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Cull Off ZWrite Off ZTest Always

        // 1. Occlusion Mask Pass
        Pass
        {
            Name "Occlusion"
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag_occlusion
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float _Threshold;

            fixed4 frag_occlusion(v2f_img i) : SV_Target
            {
                float3 color = tex2D(_MainTex, i.uv).rgb;
                float brightness = dot(color, float3(0.299, 0.587, 0.114)); // Luminance
                return (brightness > _Threshold) ? fixed4(1,1,1,1) : fixed4(0,0,0,1);
            }
            ENDCG
        }

        // 2. Radial Blur Pass
        Pass
        {
            Name "RadialBlur"
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag_blur
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _SunPos;
            float _BlurRadius;

            fixed4 frag_blur(v2f_img i) : SV_Target
            {
                float2 dir = (i.uv - _SunPos.xy) * _BlurRadius;
                fixed4 col = 0;
                const int SAMPLE_COUNT = 64;
                float step = 1.0 / SAMPLE_COUNT;

                for (int j = 0; j < SAMPLE_COUNT; j++)
                {
                    col += tex2D(_MainTex, i.uv - dir * j * step);
                }

                return col / SAMPLE_COUNT;
            }
            ENDCG
        }

        // 3. Final Composite Pass
        Pass
        {
            Name "Composite"
            Blend One One
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag_composite
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float _Intensity;

            fixed4 frag_composite(v2f_img i) : SV_Target
            {
                return tex2D(_MainTex, i.uv) * _Intensity;
            }
            ENDCG
        }
    }
}
