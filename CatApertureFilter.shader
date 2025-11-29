Shader "Custom/CatApertureFilter"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _FilterIntensity ("Filter Intensity", Range(0,1)) = 0.8
        _ApertureRadius ("Aperture Radius", Range(0,1)) = 0.7
        _ApertureSmooth ("Aperture Smooth", Range(0.01,0.5)) = 0.15
        _CenterBrightness ("Center Brightness", Range(1,2)) = 1.2
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Transparent" }
        LOD 100
        ZTest Always ZWrite Off Cull Off

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
            float _FilterIntensity;
            float _ApertureRadius;
            float _ApertureSmooth;
            float _CenterBrightness;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // 1. 采样原纹理
                fixed4 col = tex2D(_MainTex, i.uv);

                // 2. 蓝绿色滤镜（保留亮度，叠加蓝绿色调）
                float luminance = 0.299 * col.r + 0.587 * col.g + 0.114 * col.b;
                fixed3 filterColor = fixed3(0.2f, 0.8f, 0.9f); // 蓝绿色基调
                fixed3 filteredCol = lerp(col.rgb, luminance * filterColor, _FilterIntensity);

                // 3. 光圈效果（中心清晰亮，边缘暗）
                float2 center = float2(0.5, 0.5); // 屏幕中心
                float distance = length(i.uv - center); // 像素到中心的距离
                float aperture = smoothstep(_ApertureRadius + _ApertureSmooth, _ApertureRadius - _ApertureSmooth, distance);
                
                // 光圈亮度叠加
                filteredCol *= lerp(0.5f, _CenterBrightness, aperture);

                return fixed4(filteredCol, col.a);
            }
            ENDCG
        }
    }
}