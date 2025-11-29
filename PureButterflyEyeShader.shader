Shader "Hidden/PureButterflyEyeShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _GridSize ("Grid Size", Int) = 10
        _GridLineIntensity ("Grid Line Intensity", Float) = 0.01
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100
        ZTest Always Cull Off ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
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
            float4 _MainTex_ST;
            int _GridSize;
            float _GridLineIntensity;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            // 仅绘制复眼切片网格线，不修改任何色彩
            fixed DrawPureGrid(float2 uv)
            {
                if (_GridLineIntensity <= 0) return 0;
                
                float2 gridUV = uv * _GridSize;
                float2 gridFrac = frac(gridUV);
                // 极细的网格线，仅勾勒切片边界
                float line = step(1 - _GridLineIntensity * 5, gridFrac) + step(gridFrac, _GridLineIntensity * 5);
                return saturate(line.x + line.y) * _GridLineIntensity;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // 1. 获取原始画面（完全不修改色彩/亮度/畸变）
                fixed4 col = tex2D(_MainTex, i.uv);
                
                // 2. 仅绘制复眼切片网格线（黑色细线条）
                fixed gridLine = DrawPureGrid(i.uv);
                col.rgb = lerp(col.rgb, fixed3(0, 0, 0), gridLine);
                
                // 3. 完全保留原始Alpha和色彩
                col.a = 1;
                return col;
            }
            ENDCG
        }
    }
    FallBack "Unlit/Texture"
}