Shader "Tutorial/VolumetricFog"
{
    Properties
    {
        _Color("Color", Color) = (1, 1, 1, 1)
        _GrayColor("Gray Color", Color) = (0.5, 0.5, 0.5, 1)
        _ColorBlend("Color Blend", Range(0, 1)) = 0
        _MaxDistance("Max distance", float) = 100
        _StepSize("Step size", Range(0.1, 20)) = 1
        _DensityMultiplier("Density multiplier", Range(0, 10)) = 1
        _NoiseOffset("Noise offset", float) = 0
        
        _FogNoise("Fog noise", 3D) = "white" {}
        _NoiseTiling("Noise tiling", float) = 1
        _DensityThreshold("Density threshold", Range(0, 1)) = 0.1
        
        [HDR]_LightContribution("Light contribution", Color) = (1, 1, 1, 1)
        _LightScattering("Light scattering", Range(0, 1)) = 0.2
    }

    SubShader
    {
        // ✅ FullScreenPass는 Tags가 필요없음 (렌더러가 제어)
        Tags { "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Name "VolumetricFog"
            
            // ✅ FullScreenPass는 이 설정들이 필요없음
            // ZWrite Off
            // ZTest Always
            // Blend SrcAlpha OneMinusSrcAlpha
            
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            float4 _Color;
            float4 _GrayColor;
            float _ColorBlend;
            float _MaxDistance;
            float _DensityMultiplier;
            float _StepSize;
            float _NoiseOffset;
            TEXTURE3D(_FogNoise);
            SAMPLER(sampler_FogNoise); // ✅ 명시적 sampler 추가
            float _DensityThreshold;
            float _NoiseTiling;
            float4 _LightContribution;
            float _LightScattering;

            float henyey_greenstein(float angle, float scattering)
            {
                return (1.0 - angle * angle) / (4.0 * PI * pow(1.0 + scattering * scattering - (2.0 * scattering) * angle, 1.5f));
            }
            
            float get_density(float3 worldPos)
            {
                float4 noise = SAMPLE_TEXTURE3D_LOD(_FogNoise, sampler_FogNoise, worldPos * 0.01 * _NoiseTiling, 0);
                float density = dot(noise, noise);
                density = saturate(density - _DensityThreshold) * _DensityMultiplier;
                return density;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float4 col = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, IN.texcoord);
                float depth = SampleSceneDepth(IN.texcoord);
                float3 worldPos = ComputeWorldSpacePosition(IN.texcoord, depth, UNITY_MATRIX_I_VP);

                float3 entryPoint = _WorldSpaceCameraPos;
                float3 viewDir = worldPos - _WorldSpaceCameraPos;
                float viewLength = length(viewDir);
                float3 rayDir = normalize(viewDir);

                float2 pixelCoords = IN.texcoord * _BlitTexture_TexelSize.zw;
                float distLimit = min(viewLength, _MaxDistance);
                float distTravelled = InterleavedGradientNoise(pixelCoords, (int)(_Time.y / max(HALF_EPS, unity_DeltaTime.x))) * _NoiseOffset;
                float transmittance = 1;
                
                // ✅ 원래 색상과 회색 보간
                float4 currentColor = lerp(_Color, _GrayColor, _ColorBlend);
                float4 fogCol = currentColor;

                while(distTravelled < distLimit)
                {
                    float3 rayPos = entryPoint + rayDir * distTravelled;
                    float density = get_density(rayPos);
                    if (density > 0)
                    {
                        Light mainLight = GetMainLight(TransformWorldToShadowCoord(rayPos));
                        fogCol.rgb += mainLight.color.rgb * _LightContribution.rgb * henyey_greenstein(dot(rayDir, mainLight.direction), _LightScattering) * density * mainLight.shadowAttenuation * _StepSize;
                        transmittance *= exp(-density * _StepSize);
                    }
                    distTravelled += _StepSize;
                }
                
                return lerp(col, fogCol, 1.0 - saturate(transmittance));
            }
            ENDHLSL
        }
    }
}