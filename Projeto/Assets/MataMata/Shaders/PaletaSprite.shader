Shader "MataMata/PaletaSprite"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1, 1, 1, 1)
        _DarkColor ("Shadow Color", Color) = (0.03, 0.08, 0.18, 1)
        _MidColor ("Midtone Color", Color) = (0.08, 0.56, 0.82, 1)
        _LightColor ("Highlight Color", Color) = (0.87, 0.95, 1, 1)
        _Strength ("Palette Strength", Range(0, 1)) = 1

        [HideInInspector] _RendererColor ("Renderer Color", Color) = (1, 1, 1, 1)
        [HideInInspector] _Flip ("Flip", Vector) = (1, 1, 1, 1)
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "UniversalMaterialType" = "Unlit"
            "CanUseSpriteAtlas" = "True"
            "IgnoreProjector" = "True"
            "PreviewType" = "Plane"
        }

        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"

        TEXTURE2D(_MainTex);
        SAMPLER(sampler_MainTex);

        CBUFFER_START(UnityPerMaterial)
            half4 _Color;
            half4 _DarkColor;
            half4 _MidColor;
            half4 _LightColor;
            half _Strength;
        CBUFFER_END

        struct Attributes
        {
            float3 positionOS : POSITION;
            half4 color : COLOR;
            float2 uv : TEXCOORD0;
            UNITY_VERTEX_INPUT_INSTANCE_ID
        };

        struct Varyings
        {
            float4 positionCS : SV_POSITION;
            half4 color : COLOR;
            float2 uv : TEXCOORD0;
            UNITY_VERTEX_OUTPUT_STEREO
        };

        Varyings PaletteVertex(Attributes input)
        {
            Varyings output = (Varyings)0;
            UNITY_SETUP_INSTANCE_ID(input);
            UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

            // Use URP's per-sprite state, including renderer tint and flip.
            SetUpSpriteInstanceProperties();
            input.positionOS = UnityFlipSprite(input.positionOS, unity_SpriteProps.xy);

            output.positionCS = TransformObjectToHClip(input.positionOS);
            output.color = input.color * _Color * unity_SpriteColor;
            output.uv = input.uv;
            return output;
        }

        half4 PaletteFragment(Varyings input) : SV_Target
        {
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
            half4 source = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
            half luminance = saturate(dot(source.rgb, half3(0.2126h, 0.7152h, 0.0722h)));

            // A continuous ramp retains shading in the original sprite art.
            half3 shadowsToMidtones = lerp(
                _DarkColor.rgb, _MidColor.rgb, saturate(luminance * 2.0h));
            half3 palette = lerp(
                shadowsToMidtones, _LightColor.rgb,
                saturate(luminance * 2.0h - 1.0h));
            half3 color = lerp(source.rgb, palette, saturate(_Strength));

            // Palette colors never replace or enlarge the source alpha mask.
            return half4(color * input.color.rgb, source.a * input.color.a);
        }
        ENDHLSL

        Pass
        {
            Name "SpritePalette2D"
            Tags { "LightMode" = "Universal2D" }

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex PaletteVertex
            #pragma fragment PaletteFragment
            #pragma multi_compile_instancing
            ENDHLSL
        }

        // Also supports the standard URP material preview and forward renderer.
        Pass
        {
            Name "SpritePaletteForward"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex PaletteVertex
            #pragma fragment PaletteFragment
            #pragma multi_compile_instancing
            ENDHLSL
        }
    }

    Fallback "Hidden/Universal Render Pipeline/FallbackError"
}
