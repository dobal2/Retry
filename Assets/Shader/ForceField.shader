Shader "Custom/ForceField_Complete_URP"
{
    Properties
    {
        [Header(Colors)]
        [HDR] _MainColor ("Main Color", Color) = (0, 0.5, 1, 1)
        [HDR] _RimColor ("Rim Color", Color) = (0, 1, 1, 1)
        _FresnelPower ("Fresnel Power", Range(0.1, 10)) = 3
        _RimIntensity ("Rim Intensity", Range(0, 5)) = 2
        _Alpha ("Overall Alpha", Range(0, 1)) = 0.5
        
        [Header(Intersection)]
        [HDR] _IntersectColor ("Intersection Color", Color) = (1, 1, 1, 1)
        _IntersectPower ("Intersection Power", Range(0.1, 10)) = 2
        _IntersectWidth ("Intersection Width", Range(0, 5)) = 0.5
        
        [Header(Hexagon Pattern)]
        _HexTex ("Hexagon Texture", 2D) = "white" {}
        [HDR] _HexEdgeColor ("Hex Edge Color", Color) = (0, 2, 2, 1)
        [HDR] _HexFillColor ("Hex Fill Color", Color) = (0, 0.3, 0.5, 1)
        _HexScaleX ("Hex Scale X", Range(0.1, 20)) = 5
        _HexScaleY ("Hex Scale Y", Range(0.1, 20)) = 5
        _HexSpeed ("Hex Scroll Speed", Vector) = (0, 0.1, 0, 0)
        _HexEdgeOnly ("Hex Edge Only", Range(0, 1)) = 1
        _HexEdgeThickness ("Hex Edge Thickness", Range(0.01, 0.5)) = 0.1
        _HexTopFade ("Hex Top Fade", Range(0, 1)) = 0.3
    }
    
    SubShader
    {
        Tags 
        { 
            "RenderType"="Transparent" 
            "Queue"="Transparent"
            "RenderPipeline"="UniversalPipeline"
        }
        
        Pass
        {
            Name "ForceField"
            
            Cull Off
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            
            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };
            
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float3 viewDirWS : TEXCOORD2;
                float2 uv : TEXCOORD3;
                float4 screenPos : TEXCOORD4;
                float3 positionOS : TEXCOORD5;
            };
            
            TEXTURE2D(_HexTex);
            SAMPLER(sampler_HexTex);
            
            CBUFFER_START(UnityPerMaterial)
                half4 _MainColor;
                half4 _RimColor;
                half4 _IntersectColor;
                half4 _HexEdgeColor;
                half4 _HexFillColor;
                half _FresnelPower;
                half _RimIntensity;
                half _Alpha;
                half _IntersectPower;
                half _IntersectWidth;
                
                float4 _HexTex_ST;
                half _HexScaleX;
                half _HexScaleY;
                float4 _HexSpeed;
                half _HexEdgeOnly;
                half _HexEdgeThickness;
                half _HexTopFade;
            CBUFFER_END
            
            Varyings vert(Attributes input)
            {
                Varyings output;
                
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS);
                
                output.positionCS = vertexInput.positionCS;
                output.positionWS = vertexInput.positionWS;
                output.normalWS = normalInput.normalWS;
                output.viewDirWS = GetWorldSpaceViewDir(vertexInput.positionWS);
                output.uv = input.uv;
                output.screenPos = ComputeScreenPos(vertexInput.positionCS);
                output.positionOS = input.positionOS.xyz;
                
                return output;
            }
            
            half4 frag(Varyings input) : SV_Target
            {
                // ★ 양면 Fresnel 계산
                half3 normal = normalize(input.normalWS);
                half3 viewDir = normalize(input.viewDirWS);
                
                half NdotV = abs(dot(normal, viewDir));
                half fresnel = pow(1.0 - NdotV, _FresnelPower);
                
                // ★ 높이 기반 페이드 (꼭대기 = 텍스처 안 보임)
                // normalOS.y가 1에 가까우면 꼭대기
                half3 normalOS = normalize(input.positionOS);
                half topMask = saturate((normalOS.y - (1.0 - _HexTopFade)) / _HexTopFade);
                topMask = 1.0 - topMask; // 반전 (꼭대기 = 0)
                
                // ★ Hexagon 패턴
                float2 hexUV = input.uv * float2(_HexScaleX, _HexScaleY) + _HexSpeed.xy * _Time.y;
                half4 hexPattern = SAMPLE_TEXTURE2D(_HexTex, sampler_HexTex, hexUV);
                
                half hexValue = hexPattern.r;
                
                // ★ 테두리 두께 (0~1 범위 유지)
                half hexEdgeMask = pow(hexValue, 1.0 / _HexEdgeThickness);
                hexEdgeMask = saturate(hexEdgeMask);
                
                // 안쪽 영역
                half hexFillMask = 1.0 - hexEdgeMask;
                hexFillMask = saturate(hexFillMask * hexValue);
                
                // ★ 높이 마스크 적용 (꼭대기에서 텍스처 페이드)
                hexEdgeMask *= topMask;
                hexFillMask *= topMask;
                
                // ★ Depth Intersection
                float2 screenUV = input.screenPos.xy / input.screenPos.w;
                float sceneDepth = LinearEyeDepth(SampleSceneDepth(screenUV), _ZBufferParams);
                float surfaceDepth = LinearEyeDepth(input.positionCS.z, _ZBufferParams);
                float depthDiff = abs(sceneDepth - surfaceDepth);
                
                half intersection = 1.0 - saturate(depthDiff / _IntersectWidth);
                intersection = pow(intersection, _IntersectPower);
                
                // ★ 최종 컬러
                half4 col = _MainColor;
                
                half hexMask = lerp(1.0, fresnel, _HexEdgeOnly);
                
                // ★★ 마스크(두께)와 색상(밝기)을 분리
                col.rgb += _HexEdgeColor.rgb * hexEdgeMask * hexMask;
                col.rgb += _HexFillColor.rgb * hexFillMask * hexMask;
                
                col.rgb += _RimColor.rgb * fresnel * _RimIntensity;
                col.rgb += _IntersectColor.rgb * intersection * 3.0;
                
                // 교차점 Hex
                col.rgb += _HexEdgeColor.rgb * hexEdgeMask * intersection * 0.5;
                col.rgb += _HexFillColor.rgb * hexFillMask * intersection * 0.5;
                
                half patternAlpha = (hexEdgeMask * 0.1 + hexFillMask * 0.05) * fresnel * 0.2;
                col.a = saturate((fresnel + patternAlpha) * _Alpha + intersection);
                col.a = max(col.a, 0.1);
                
                return col;
            }
            ENDHLSL
        }
    }
    
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}