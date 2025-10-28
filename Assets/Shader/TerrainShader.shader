Shader "Unlit/TerrainShader"
{
    Properties
    {
        _MainTex("Base Texture", 2D) = "white" {}
        _TerrainGradient("Terrain Gradient", 2D) = "white" {}
        _MinTerrainHeight("Min Terrain Height", Float) = 0
        _MaxTerrainHeight("Max Terrain Height", Float) = 10
        _TextureScale("Texture Scale", Float) = 1
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float3 worldPos : TEXCOORD0;
                float2 uv : TEXCOORD1;
                UNITY_FOG_COORDS(2)
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            sampler2D _TerrainGradient;
            float4 _TerrainGradient_ST;
            float _MinTerrainHeight;
            float _MaxTerrainHeight;
            float _TextureScale;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                
                // 월드 좌표 기반 UV (XZ 평면)
                o.uv = o.worldPos.xz * _TextureScale;
                
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // 기본 텍스처 샘플링
                fixed4 baseColor = tex2D(_MainTex, i.uv);
                
                // world Y값에서 heightValue 계산
                float heightValue = saturate((i.worldPos.y - _MinTerrainHeight) / (_MaxTerrainHeight - _MinTerrainHeight));

                // 그라디언트 텍스처에서 V축을 heightValue로 샘플링
                fixed4 gradientColor = tex2D(_TerrainGradient, float2(0, heightValue));
                
                // 기본 텍스처와 그라디언트 컬러 혼합
                fixed4 col = baseColor * gradientColor;

                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }
    }
}