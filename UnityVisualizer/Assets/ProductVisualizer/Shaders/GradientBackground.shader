Shader "Custom/GradientBackground"
{
    Properties
    {
        _TopColor      ("Top Color",          Color)        = (0.06, 0.06, 0.18, 1)
        _BottomColor   ("Bottom Color",       Color)        = (0.01, 0.01, 0.04, 1)
        _VignetteAmount("Vignette Strength",  Range(0,4))   = 1.2
        _VignetteOffsetY("Vignette Center Y", Range(0,1))   = 0.45
    }

    HLSLINCLUDE
    #pragma target 4.5
    #pragma only_renderers d3d11 playstation xboxone xboxseries vulkan metal switch
    ENDHLSL

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "HDRenderPipeline"
            "RenderType"     = "Opaque"
            "Queue"          = "Geometry-100"
        }

        // ---- Forward pass -----------------------------------------------
        Pass
        {
            Name "ForwardOnly"
            Tags { "LightMode" = "ForwardOnly" }

            ZWrite On
            ZTest  LEqual
            Cull   Back

            HLSLPROGRAM
            #pragma vertex   Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _TopColor;
                float4 _BottomColor;
                float  _VignetteAmount;
                float  _VignetteOffsetY;
            CBUFFER_END

            struct Attributes
            {
                float3 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS = TransformObjectToHClip(input.positionOS);
                output.uv = input.uv;
                return output;
            }

            float4 Frag(Varyings input) : SV_Target
            {
                float3 color = lerp(_BottomColor.rgb, _TopColor.rgb, input.uv.y);

                // Radial vignette
                float2 d = input.uv - float2(0.5, _VignetteOffsetY);
                d.x *= 0.55; // compensate for wide aspect
                float vignette = 1.0 - saturate(dot(d, d) * _VignetteAmount * 3.5);
                color *= lerp(0.25, 1.0, vignette);

                return float4(color, 1.0);
            }
            ENDHLSL
        }

        // ---- Depth prepass -----------------------------------------------
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }

            ZWrite On
            ZTest  LEqual
            Cull   Back
            ColorMask 0

            HLSLPROGRAM
            #pragma vertex   Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"

            struct Attributes { float3 positionOS : POSITION; };
            struct Varyings   { float4 positionCS : SV_POSITION; };

            Varyings Vert(Attributes i)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(i.positionOS);
                return o;
            }

            void Frag(Varyings i) {}
            ENDHLSL
        }
    }
}
