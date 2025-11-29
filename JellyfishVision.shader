Shader "Custom/JellyfishVision"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _LightContrast ("Light Contrast", Range(1,5)) = 2.5
        _BlurIntensity ("Blur Intensity", Range(0.001, 0.03)) = 0.018
        _LightSensitivity ("Light Sensitivity", Range(0.5,3)) = 1.5
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Transparent" }
        LOD 100
        ZTest Always ZWrite Off Cull Off // 后处理核心设置

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            // 顶点结构（极简）
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

            // Shader变量
            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _LightContrast;
            float _BlurIntensity;
            float _LightSensitivity;

            // 顶点着色器（仅基础变换）
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            // 片元着色器：核心水母感光逻辑
            fixed4 frag (v2f i) : SV_Target
            {
                // 1. 全局弥散模糊（模拟无清晰视觉）
                float2 blurDir = _BlurIntensity;
                fixed4 col = tex2D(_MainTex, i.uv) * 0.2;
                col += tex2D(_MainTex, i.uv + float2(-blurDir.x, -blurDir.y)) * 0.15;
                col += tex2D(_MainTex, i.uv + float2(blurDir.x, -blurDir.y)) * 0.15;
                col += tex2D(_MainTex, i.uv + float2(-blurDir.x, blurDir.y)) * 0.15;
                col += tex2D(_MainTex, i.uv + float2(blurDir.x, blurDir.y)) * 0.15;
                col += tex2D(_MainTex, i.uv + float2(-blurDir.x*2, 0)) * 0.08;
                col += tex2D(_MainTex, i.uv + float2(blurDir.x*2, 0)) * 0.08;
                col += tex2D(_MainTex, i.uv + float2(0, -blurDir.y*2)) * 0.08;
                col += tex2D(_MainTex, i.uv + float2(0, blurDir.y*2)) * 0.08;

                // 2. 转为纯明暗（移除所有色彩）
                float luminance = 0.299 * col.r + 0.587 * col.g + 0.114 * col.b;
                
                // 3. 增强明暗对比 + 感光灵敏度
                luminance = pow(luminance, 1/_LightContrast) * _LightSensitivity;
                luminance = clamp(luminance, 0, 1); // 限制范围，避免过曝

                // 4. 模拟水母感光细胞的均匀明暗（无细节，仅光感）
                luminance = smoothstep(0, 1, luminance);

                // 返回纯灰度明暗（无Alpha）
                return fixed4(luminance, luminance, luminance, 1);
            }
            ENDCG
        }
    }
    FallBack "Hidden/BlitCopy" // 降级兼容
}