Shader "Custom/WideAngleDistortion_NoChromatic"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _DistortionAmount ("Distortion", Float) = 0.03
        _FocusRadius ("Focus Radius", Float) = 0.3
        _EdgeBlur ("Edge Blur", Float) = 0.01
        _ChromaticAberration ("Chroma", Float) = 0.005
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Transparent" }
        LOD 100
        ZTest Always Cull Off ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float2 uv : TEXCOORD0; float4 vertex : SV_POSITION; };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _DistortionAmount, _FocusRadius, _EdgeBlur, _ChromaticAberration;

            // 高斯模糊（掩盖边缘色散）
            fixed4 Blur(sampler2D tex, float2 uv, float blur)
            {
                fixed4 col = 0;
                float2 off = float2(blur, 0);
                col += tex2D(tex, uv - off*1.4) * 0.07;
                col += tex2D(tex, uv - off*1.0) * 0.13;
                col += tex2D(tex, uv - off*0.7) * 0.19;
                col += tex2D(tex, uv - off*0.3) * 0.21;
                col += tex2D(tex, uv) * 0.21;
                col += tex2D(tex, uv + off*0.3) * 0.19;
                col += tex2D(tex, uv + off*0.7) * 0.13;
                col += tex2D(tex, uv + off*1.0) * 0.07;
                return col;
            }

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // 1. 非线性畸变（核心消色散）
                float2 uv = i.uv - 0.5;
                float dist = length(uv);
                float curve = 1 + dist * _DistortionAmount * (1 - dist * 0.6);
                float focus = smoothstep(_FocusRadius, 1.0, dist);
                float2 distortUV = uv * lerp(1.0, curve, focus) + 0.5;
                distortUV = clamp(distortUV, 0.001, 0.999);

                // 2. 分通道色差校正
                float2 rUV = distortUV + float2(_ChromaticAberration * dist, 0);
                float2 gUV = distortUV;
                float2 bUV = distortUV - float2(_ChromaticAberration * dist, 0);

                // 3. 采样并合并通道
                fixed r = tex2D(_MainTex, clamp(rUV, 0.001, 0.999)).r;
                fixed g = tex2D(_MainTex, clamp(gUV, 0.001, 0.999)).g;
                fixed b = tex2D(_MainTex, clamp(bUV, 0.001, 0.999)).b;
                fixed a = tex2D(_MainTex, distortUV).a;

                // 4. 边缘模糊消色散
                fixed4 col = fixed4(r, g, b, a);
                col = lerp(col, Blur(_MainTex, distortUV, _EdgeBlur * focus), focus);

                return col;
            }
            ENDCG
        }
    }
    FallBack "Hidden/Blit"
}