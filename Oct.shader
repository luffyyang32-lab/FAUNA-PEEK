// Upgrade NOTE: commented out 'float4x4 _CameraToWorld', a built-in variable
// Upgrade NOTE: replaced '_CameraToWorld' with 'unity_CameraToWorld'

Shader "Custom/OctopusVision"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Exposure ("Low Light Exposure", Range(0.1, 2.0)) = 0.3  // 低光亮度系数
        _FocusDistance ("Focus Distance", Float) = 10.0          // 对焦距离
        _FocusRange ("Focus Range", Vector) = (0.5, 20.0, 0, 0)  // 对焦范围
        _WideAngleDistortion ("Wide Angle Distortion", Range(0, 0.5)) = 0.2 // 广角畸变
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Transparent" }
        LOD 100
        ZTest Always ZWrite Off Cull Off  // 后处理必须的设置

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            // 顶点输入结构
            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            // 顶点输出结构
            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float3 worldPos : TEXCOORD1;  // 世界坐标（用于计算对焦）
            };

            // Shader变量
            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _Exposure;
            float _FocusDistance;
            float2 _FocusRange;
            float _WideAngleDistortion;

            // 相机参数（由C#传递）
            // float4x4 _CameraToWorld;
            float4x4 _CameraProjection;
            float4x4 _InverseProjection;

            // 顶点着色器（处理广角畸变）
            v2f vert (appdata v)
            {
                v2f o;
                
                // 基础顶点变换
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);

                // 广角鱼眼畸变模拟（章鱼超广角视野）
                float2 uv = o.uv - 0.5;
                float dist = length(uv);
                uv = uv * (1 + dist * _WideAngleDistortion);
                o.uv = uv + 0.5;

                // 计算世界坐标（用于对焦模糊）
                float4 clipPos = float4(v.vertex.xy * 2 - 1, 0, 1);
                float4 viewPos = mul(_InverseProjection, clipPos);
                o.worldPos = mul(unity_CameraToWorld, float4(viewPos.xyz, 1)).xyz;

                return o;
            }

            // 片元着色器（核心效果：低光曝光+对焦模糊）
            fixed4 frag (v2f i) : SV_Target
            {
                // 1. 采样原纹理
                fixed4 col = tex2D(_MainTex, i.uv);

                // 2. 低光环境曝光调整（模拟章鱼夜视能力）
                col.rgb *= _Exposure * 3;  // 放大曝光系数，增强低光效果
                col.rgb = pow(col.rgb, 1/2.2);  // 伽马矫正，避免过曝

                // 3. 对焦模糊（章鱼视觉对焦特性）
                float pixelDistance = distance(i.worldPos, _WorldSpaceCameraPos);
                float blurFactor = smoothstep(_FocusRange.x, _FocusRange.y, abs(pixelDistance - _FocusDistance));
                
                // 简易高斯模糊（基于对焦距离）
                if (blurFactor > 0.1)
                {
                    float2 blurSize = blurFactor * 0.01f;
                    fixed4 blurCol = col;
                    blurCol += tex2D(_MainTex, i.uv + float2(-blurSize.x, -blurSize.y)) * 0.2;
                    blurCol += tex2D(_MainTex, i.uv + float2(blurSize.x, -blurSize.y)) * 0.2;
                    blurCol += tex2D(_MainTex, i.uv + float2(-blurSize.x, blurSize.y)) * 0.2;
                    blurCol += tex2D(_MainTex, i.uv + float2(blurSize.x, blurSize.y)) * 0.2;
                    col = lerp(col, blurCol, blurFactor);
                }

                // 4. 章鱼视觉偏蓝绿色调（可选）
                float luminance = 0.299 * col.r + 0.587 * col.g + 0.114 * col.b;
                col.rgb = lerp(col.rgb, fixed3(luminance*0.1, luminance*0.8, luminance*0.9), 0.2);

                return col;
            }
            ENDCG
        }
    }
    FallBack "Hidden/BlitCopy"  // 降级兼容
}