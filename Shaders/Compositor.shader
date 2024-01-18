Shader "BackgroundSegmentation/Compositor"
{
    Properties
    {
        _MainTex("Webcam", 2D) = "white" {}
        _MattingTex("Matting", 2D) = "white" {}
        _DepthTex("Depth", 2D) = "white" {}
        _BackgroundTex("Background", 2D) = "white" {}
    }
    SubShader
    {
        Tags {"RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert 
            #pragma fragment frag
            #pragma target 2.0

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            sampler2D _MattingTex;
            sampler2D _DepthTex;
            sampler2D _BackgroundTex;
            float4 _MainTex_ST;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float3 fg = tex2D(_MainTex, i.uv).rgb;
                float matting = tex2D(_MattingTex, i.uv).r;
                // float depth = tex2D(_DepthTex, i.uv).r;
                // float3 bg = tex2D(_BackgroundTex, i.uv);

                //float3 blending = fg < 0.5 ? 2 * bg * fg : 1 - 2 * (1 - bg) * (1 - fg);
                // float3 col = lerp(fg, bg, 1 - matting);

                // return float4(col, 1);
                return float4(fg, matting);
            }
            ENDCG
        }
    }
}
