Shader "Hidden/AntEyeShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _GridSize ("Grid Size", Int) = 10
        _Blur ("Blur", Float) = 0.1
        _GridLine ("Grid Line", Float) = 0.05
        _ColorShift ("Color Shift", Float) = 0.03
        _ScreenSize ("Screen Size", Vector) = (1920,1080,0,0)
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
            float _Blur;
            float _GridLine;
            float _ColorShift;
            float2 _ScreenSize;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            // 简单模糊
            fixed4 Blur(float2 uv)
            {
                float2 texel = 1.0 / _ScreenSize * _Blur;
                fixed4 col = 0;
                col += tex2D(_MainTex, uv + float2(-texel.x, -texel.y)) * 0.2;
                col += tex2D(_MainTex, uv + float2(texel.x, -texel.y)) * 0.2;
                col += tex2D(_MainTex, uv + float2(-texel.x, texel.y)) * 0.2;
                col += tex2D(_MainTex, uv + float2(texel.x, texel.y)) * 0.2;
                col += tex2D(_MainTex, uv) * 0.2;
                return col;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // 1. 计算10×10网格UV
                float2 gridUV = i.uv * _GridSize;
                float2 cellUV = frac(gridUV);
                float2 cellIndex = floor(gridUV);

                // 2. 模拟每个小眼的轻微视角偏移
                float2 offset = (cellIndex / _GridSize - 0.5) * _ColorShift;
                float2 finalUV = i.uv + offset;

                // 3. 模糊+颜色偏移
                fixed4 col = Blur(finalUV);
                col.r = tex2D(_MainTex, finalUV + float2(_ColorShift, 0)).r;
                col.b = tex2D(_MainTex, finalUV - float2(0, _ColorShift)).b;

                // 4. 绘制100个切片的网格线
                float2 grid = step(1 - _GridLine, cellUV) + step(cellUV, _GridLine);
                float gridLine = saturate(grid.x + grid.y) * _GridLine;
                col.rgb = lerp(col.rgb, fixed3(0,0,0), gridLine);

                return col;
            }
            ENDCG
        }
    }
}