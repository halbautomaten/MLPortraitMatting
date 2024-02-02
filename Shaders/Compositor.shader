Shader "BackgroundSegmentation/Compositor"
{
    Properties
    {
        _MainTex("Webcam", 2D) = "white" {}
        _MattingTex("Matting", 2D) = "white" {}
        _Matting2Tex("Matting 2", 2D) = "white" {}
        _DepthTex("Depth", 2D) = "white" {}

        _DepthEdge0("Depth Edge 0", Float) = 0.6
        _DepthEdge1("Depth Edge 1", Float) = 0.6
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
            sampler2D _Matting2Tex;
            sampler2D _DepthTex;
            float4 _MainTex_ST;
            float _DepthEdge0;
            float _DepthEdge1;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // input pixel
                float3 fg = tex2D(_MainTex, i.uv).rgb;
                float matting = tex2D(_MattingTex, i.uv).r;
                float matting2 = tex2D(_Matting2Tex, i.uv).r;
                float depth = tex2D(_DepthTex, i.uv).r;
                
                // blending
                float mask = matting * matting2 * smoothstep(_DepthEdge0, _DepthEdge1, depth);

                // output pixel
                return float4(fg, mask);
            }
            ENDCG
        }
    }
}
