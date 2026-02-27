Shader "PositionBasedHighlight/UVMap"
{
    Properties
    {
        
    }

    SubShader
    {
        Pass
        {
            Tags { "LightMode"="Always" }

            ZWrite Off
            ZTest Always
            Cull Off

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float3 vertex : POSITION;
                float2 uv     : TEXCOORD0;
            };

            struct v2f
            {
                float4 posUV   : SV_POSITION;
            };

            v2f vert (appdata v)
            {
                v2f o;

                // RenderTextureに描画する時、上下反転が起きるので、あらかじめ反転しておく
                // プラットフォームによる違いがあるらしいので要検証
                // 参考：https://docs.unity3d.com/ja/2017.4/Manual/SL-PlatformDifferences.html
                float2 flippedUV = float2(v.uv.x, 1.0 - v.uv.y);
                float2 uvNDC = flippedUV * 2.0 - 1.0;

                o.posUV = float4(uvNDC, 0.0, 1.0);

                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
                return float4(1, 0, 0, 1);
            }
            ENDCG
        }
    }
}
