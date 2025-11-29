Shader "Custom/FrogVision"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        
        // 视觉参数（和脚本对应）
        _HorizontalFOV ("Horizontal FOV (Rad)", Float) = 3.14159 // 180°
        _VerticalFOV ("Vertical FOV (Rad)", Float) = 1.5708 // 90°
        _VisionDistance ("Vision Distance", Float) = 8.0
        _StaticBlurIntensity ("Static Blur Intensity", Float) = 2.0
        _ColorTintIntensity ("Color Tint Intensity", Float) = 0.8
        _OutOfFOVDarken ("Out Of FOV Darken", Float) = 0.9
        
        // 内部参数（由脚本赋值）
        _MotionTexture ("Motion Texture", 2D) = "white" {}
        _CameraForward ("Camera Forward", Vector) = (0,0,1,0)
        _CameraPosition ("Camera Position", Vector) = (0,0,0,0)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Transparent" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            #include "UnityShaderVariables.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float3 worldPos : TEXCOORD1;
                float4 screenPos : TEXCOORD2;
                float3 viewDir : TEXCOORD3;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            
            float _HorizontalFOV;
            float _VerticalFOV;
            float _VisionDistance;
            float _StaticBlurIntensity;
            float _ColorTintIntensity;
            float _OutOfFOVDarken;
            
            sampler2D _MotionTexture;
            float4 _MotionTexture_ST;
            float3 _CameraForward;
            float3 _CameraPosition;

            // 运动向量纹理（Unity内置）
            sampler2D _CameraMotionVectorsTexture;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex);
                o.screenPos = ComputeScreenPos(o.vertex);
                o.viewDir = normalize(o.worldPos - _WorldSpaceCameraPos);
                return o;
            }

            // 高斯模糊（针对静态物体）
            float4 GaussianBlur(sampler2D tex, float2 uv, float blurAmount)
            {
                float4 col = 0;
                float2 offset = float2(blurAmount, blurAmount) / _ScreenParams.xy;
                
                // 9x9高斯核（简化版）
                float weights[9] = {1,2,1,2,4,2,1,2,1};
                float2 offsets[9] = {
                    float2(-1,-1), float2(0,-1), float2(1,-1),
                    float2(-1,0),  float2(0,0),  float2(1,0),
                    float2(-1,1),  float2(0,1),  float2(1,1)
                };
                
                float totalWeight = 16;
                for(int i=0; i<9; i++)
                {
                    col += tex2D(tex, uv + offsets[i] * offset) * weights[i];
                }
                return col / totalWeight;
            }

            // 检查是否在青蛙视野范围内
            bool IsInFOV(float3 viewDir)
            {
                // 归一化视角方向
                float3 forward = normalize(_CameraForward);
                float3 dir = normalize(viewDir);
                
                // 计算水平/垂直角度
                float horizontalAngle = acos(dot(float3(dir.x, 0, dir.z), float3(forward.x, 0, forward.z)));
                float verticalAngle = acos(dot(float3(0, dir.y, dir.z), float3(0, forward.y, forward.z)));
                
                // 检查是否在视野角度内
                return horizontalAngle <= _HorizontalFOV * 0.5 && verticalAngle <= _VerticalFOV * 0.5;
            }

            // 青蛙视觉色偏（黄绿色调）
            float3 FrogColorTint(float3 col)
            {
                // 青蛙对黄绿色敏感，弱化红蓝
                float3 tint = float3(0.2, 0.8, 0.1); // 黄绿色调
                return lerp(col, col * tint, _ColorTintIntensity);
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // 1. 获取基础颜色
                float4 baseCol = tex2D(_MainTex, i.uv);
                
                // 2. 检测物体运动（通过运动向量）
                float2 motionVec = tex2D(_CameraMotionVectorsTexture, i.uv).xy;
                float motionAmount = length(motionVec);
                bool isMoving = motionAmount > 0.001;
                
                // 3. 静态物体应用模糊
                if(!isMoving)
                {
                    baseCol = GaussianBlur(_MainTex, i.uv, _StaticBlurIntensity);
                }
                
                // 4. 应用青蛙色偏
                baseCol.rgb = FrogColorTint(baseCol.rgb);
                
                // 5. 检查视野范围
                bool inFOV = IsInFOV(i.viewDir);
                float distance = length(i.worldPos - _CameraPosition);
                bool inDistance = distance <= _VisionDistance;
                
                // 6. 视野外/超距离物体暗化
                if(!inFOV || !inDistance)
                {
                    baseCol.rgb *= (1 - _OutOfFOVDarken);
                }
                
                // 7. 深度雾效（模拟视觉距离衰减）
                float fogFactor = saturate(distance / _VisionDistance);
                baseCol.rgb = lerp(baseCol.rgb, float3(0.1, 0.2, 0.05), fogFactor * 0.5);

                return baseCol;
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}