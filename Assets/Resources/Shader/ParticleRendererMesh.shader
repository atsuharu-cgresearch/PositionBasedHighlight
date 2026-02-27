Shader "PositionBasedHighlight/ParticleRendererMesh"
{
    Properties
    {
        
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Cull Off
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"
            #include "Assets/Resources/hlsl/Struct_ParticleData.hlsl"

            struct appdata
            {
                float4 vertex : POSITION;

                // Meshのuv2チャンネルに、この頂点が参照するパーティクルのインデックスを格納しておく
                float4 uv2 : TEXCOORD1;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
            };

            StructuredBuffer<ParticleData> _Particles;
            int _Start;

            v2f vert (appdata v)
            {
                v2f o;

                uint pID = _Start + (uint)(v.uv2.x);
                
                float2 particlePos = _Particles[pID].p;

                float4 worldPos = float4(particlePos.x, particlePos.y, v.vertex.z, 1.0);

                // o.vertex = UnityObjectToClipPos(v.vertex);
                // o.vertex = UnityObjectToClipPos(worldPos);
                o.vertex = UnityWorldToClipPos(worldPos);
                
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                return fixed4(1, 1, 1, 1);
            }
            ENDCG
        }
    }
}
