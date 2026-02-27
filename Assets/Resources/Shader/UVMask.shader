Shader "PositionBasedHighlight/UVMask"
{
    Properties
    {
        _TargetDepthTex ("Target Depth", 2D) = "black" {}
        _OccluderDepthTex ("Occluder Depth", 2D) = "black" {}
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
                float4 posUV   : SV_POSITION; // UV座標を頂点位置とする
                float4 screenPos : TEXCOORD0; // 深度画像参照のためのスクリーン座標
            };

            sampler2D _TargetDepthTex;
            sampler2D _OccluderDepthTex;

            v2f vert (appdata v)
            {
                v2f o;

                // RenderTextureに描画する時、上下反転が起きるので、あらかじめ反転しておく
                // プラットフォームによる違いがあるらしいので要検証
                // 参考：https://docs.unity3d.com/ja/2017.4/Manual/SL-PlatformDifferences.html
                float2 flippedUV = float2(v.uv.x, 1.0 - v.uv.y);
                float2 uvNDC = flippedUV * 2.0 - 1.0;

                // 頂点のUV座標を正規化デバイス座標とする
                o.posUV = float4(uvNDC, 0.0, 1.0);


                o.screenPos = ComputeScreenPos(UnityObjectToClipPos(v.vertex));
                
                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
                // スクリーン座標をUV座標に変換
                float2 sUV = i.screenPos.xy / i.screenPos.w;

                // 深度画像をサンプリング
                float targetDepth = tex2D(_TargetDepthTex, sUV).r;
                float occluderDepth = tex2D(_OccluderDepthTex, sUV).r;

                // 遮蔽物と重なっているかどうかを判定する
                bool occluded = occluderDepth > targetDepth;

                return (occluded) ? float4(1, 0, 0, 1) : float4(0, 0, 0, 1);
            }
            ENDCG
        }
    }
}
