Shader "PositionBasedHighlight/TextureComposition"
{
    Properties
    {
        _MainTex ("Base", 2D) = "white" {}
        _OverlayTex ("Overlay", 2D) = "white" {}
        _Transform ("Transform", Vector) = (0.5, 0.5, 1, 0) // x,y: pos, z: scale, w: angle(rad)
    }

    SubShader
    {
        Pass
        {
            ZTest Always
            Cull Off
            ZWrite Off
            

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            sampler2D _MainTex;
            sampler2D _OverlayTex;
            float4 _Transform;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv  : TEXCOORD0;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv  = v.uv;
                return o;
            }

            float2 Rotate(float2 p, float a)
            {
                float s = sin(a);
                float c = cos(a);
                return float2(
                    c * p.x - s * p.y,
                    s * p.x + c * p.y
                );
            }

            float4 frag (v2f i) : SV_Target
            {
                float4 baseCol = tex2D(_MainTex, i.uv);

                // 合成先UV → 合成元ローカル空間
                float2 p = i.uv - _Transform.xy;
                p = Rotate(p, -_Transform.w);
                p /= _Transform.z;
                float2 localUV = p + 0.5;

                // 範囲内なら合成
                if (all(localUV >= 0) && all(localUV <= 1))
                {
                    float4 overCol = tex2D(_OverlayTex, localUV);
                    return lerp(baseCol, overCol, overCol.a);
                }

                return baseCol;
            }
            ENDCG
        }
    }
}
