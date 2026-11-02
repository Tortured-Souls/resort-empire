// Stylized animated ocean for a SpriteRenderer: two samples of the caustic tile (the sprite texture) drift
// in different directions and are blended between a deep and a shallow tint, plus soft sun glints.
// UVs come from world space, so the sprite can be any size and the pattern never stretches.
Shader "ResortEmpire/Ocean"
{
    Properties
    {
        [PerRendererData] _MainTex ("Caustic Tile", 2D) = "white" {}
        _DeepColor ("Deep Color", Color) = (0.09, 0.45, 0.66, 1)
        _ShallowColor ("Shallow Color", Color) = (0.25, 0.75, 0.86, 1)
        _TileSize ("Tile Size (world units)", Float) = 8
        _Speed ("Drift Speed", Float) = 0.02
        _Glint ("Glint Strength", Float) = 0.35
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" "PreviewType" = "Plane" }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            Name "Ocean"

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                half4 _DeepColor;
                half4 _ShallowColor;
                float _TileSize;
                float _Speed;
                half _Glint;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                half4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(positionWS);
                output.uv = positionWS.xy / max(_TileSize, 0.01);
                output.color = input.color;
                return output;
            }

            half Luma(half3 c)
            {
                return dot(c, half3(0.299, 0.587, 0.114));
            }

            half4 frag(Varyings input) : SV_Target
            {
                // Wrap time so the UV offset stays small and precise on mobile GPUs however long the game runs.
                float t = frac(_Time.y * _Speed / 64.0) * 64.0;
                half a = Luma(SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv + float2(t, t * 0.6)).rgb);
                half b = Luma(SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv * 1.37 + float2(-t * 0.8, t * 0.35)).rgb);
                half caustic = saturate((a + b) * 0.5);
                half shade = smoothstep(0.45, 0.95, caustic);
                half3 color = lerp(_DeepColor.rgb, _ShallowColor.rgb, shade * 0.8);
                color += pow(caustic, 6.0) * _Glint;
                return half4(color, 1.0) * input.color;
            }
            ENDHLSL
        }
    }
}
