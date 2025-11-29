Shader "Custom/CamelVisionShader"
{
    Properties
    {
        _MainTex ("Base (RGB)", 2D) = "white" {}
        _BlackBarWidth ("Black Bar Width", Float) = 0.015
        _BlackBarColor ("Black Bar Color", Color) = (0,0,0,1)
    }

    SubShader
    {
        // 修复：使用Unity合法队列，剔除所有多余逻辑
        Tags { "RenderType"="Opaque" "Queue"="Transparent" }
        LOD 100
        ZTest Always
        ZWrite Off
        Cull Off

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
            float _BlackBarWidth;
            float4 _BlackBarColor;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // 仅计算屏幕正中间的垂直黑条
                float centerX = 0.5; // 屏幕水平中心
                float halfWidth = _BlackBarWidth * 0.5; // 黑条半宽
                bool isBlackBar = abs(i.uv.x - centerX) <= halfWidth;

                // 仅绘制黑条/原画面，无其他效果
                return isBlackBar ? _BlackBarColor : tex2D(_MainTex, i.uv);
            }
            ENDCG
        }
    }
    FallBack "Unlit/Texture"
}