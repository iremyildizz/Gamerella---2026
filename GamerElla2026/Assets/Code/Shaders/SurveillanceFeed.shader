// CCTV look for the surveillance screens: scanlines, animated grain, a
// rolling bar, line jitter, colour fringing, slight CRT curvature and
// flicker. Unlit (the screen emits its own light).
//
// Uses _BaseMap like URP/Lit, so existing materials keep their image when
// switched to this shader. _StaticBurst (0..1) can be driven from a script
// with a MaterialPropertyBlock to fill the screen with static
// (SecurityScreen does this when the image changes).
Shader "Custom/SurveillanceFeed"
{
    Properties
    {
        [MainTexture] _BaseMap ("Feed Image", 2D) = "white" {}
        [MainColor] _BaseColor ("Image Tint", Color) = (1, 1, 1, 1)
        _Brightness ("Brightness", Range(0, 3)) = 1.2

        [Header(Colour)]
        _Desaturate ("Desaturate", Range(0, 1)) = 0.65
        _FeedTint ("Monochrome Tint", Color) = (0.82, 1, 0.88, 1)
        _ChromaOffset ("Colour Fringing", Range(0, 0.02)) = 0.00125

        [Header(Scanlines)]
        _ScanlineCount ("Scanline Count", Float) = 220
        _ScanlineStrength ("Scanline Strength", Range(0, 1)) = 0.15

        [Header(Noise)]
        _NoiseStrength ("Grain Strength", Range(0, 1)) = 0.07
        _NoiseResolution ("Grain Resolution", Vector) = (320, 240, 0, 0)
        _NoiseFps ("Grain FPS", Float) = 24

        [Header(Rolling Bar and Jitter)]
        _RollSpeed ("Roll Speed", Float) = 0.12
        _RollStrength ("Roll Strength", Range(0, 1)) = 0.09
        _JitterStrength ("Line Jitter", Range(0, 0.05)) = 0.0015

        [Header(CRT)]
        _Curvature ("Curvature", Range(0, 0.3)) = 0.04
        _Vignette ("Vignette", Range(0, 2)) = 0.35
        _Flicker ("Flicker", Range(0, 0.5)) = 0.03

        [Header(Static)]
        _StaticBurst ("Static Burst (script)", Range(0, 1)) = 0
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            half4 _BaseColor;
            half _Brightness;
            half _Desaturate;
            half4 _FeedTint;
            half _ChromaOffset;
            float _ScanlineCount;
            half _ScanlineStrength;
            half _NoiseStrength;
            float4 _NoiseResolution;
            float _NoiseFps;
            float _RollSpeed;
            half _RollStrength;
            half _JitterStrength;
            half _Curvature;
            half _Vignette;
            half _Flicker;
            half _StaticBurst;
        CBUFFER_END
        ENDHLSL

        Pass
        {
            Name "Unlit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float fogFactor : TEXCOORD1;
            };

            float Hash12(float2 p)
            {
                float3 p3 = frac(float3(p.xyx) * 0.1031);
                p3 += dot(p3, p3.yzx + 33.33);
                return frac((p3.x + p3.y) * p3.z);
            }

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.uv = v.uv;
                o.fogFactor = ComputeFogFactor(o.positionCS.z);
                return o;
            }

            half3 SampleFeed(float2 uv)
            {
                return SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv * _BaseMap_ST.xy + _BaseMap_ST.zw).rgb;
            }

            half4 frag(Varyings i) : SV_Target
            {
                // Different seed per screen (from its position) so the four
                // monitors don't flicker in sync.
                float3 objPos = float3(unity_ObjectToWorld._m03, unity_ObjectToWorld._m13, unity_ObjectToWorld._m23);
                float seed = Hash12(objPos.xz * 7.13 + objPos.y) * 100.0;
                float t = _Time.y + seed;

                // Slight CRT barrel curve; black outside the curved image.
                float2 cc = i.uv * 2.0 - 1.0;
                cc *= 1.0 + _Curvature * dot(cc, cc);
                float2 uv = cc * 0.5 + 0.5;
                float inside = step(0.0, uv.x) * step(uv.x, 1.0) * step(0.0, uv.y) * step(uv.y, 1.0);

                float burst = saturate(_StaticBurst);

                // Rolling bright bar travelling down the screen.
                float rollY = 1.0 - frac(t * _RollSpeed);
                float band = saturate(1.0 - abs(uv.y - rollY) * 10.0);

                // Horizontal jitter, in bands of a few lines.
                float frame = floor(t * _NoiseFps);
                float lineBand = floor(uv.y * _ScanlineCount * 0.25);
                float jitter = (Hash12(float2(lineBand, frame)) - 0.5) * (_JitterStrength + burst * 0.06);
                uv.x += jitter + band * _RollStrength * 0.008;

                // Colour fringing (stronger during static).
                float ca = _ChromaOffset * (1.0 + burst * 5.0);
                half3 col;
                col.r = SampleFeed(uv + float2(ca, 0)).r;
                col.g = SampleFeed(uv).g;
                col.b = SampleFeed(uv - float2(ca, 0)).b;
                col *= _BaseColor.rgb;

                // Washed-out, slightly tinted security-cam colour.
                half lum = dot(col, half3(0.299, 0.587, 0.114));
                col = lerp(col, lum * _FeedTint.rgb, _Desaturate);

                // Scanlines.
                half scan = sin(uv.y * _ScanlineCount * PI * 2.0) * 0.5 + 0.5;
                col *= 1.0 - _ScanlineStrength * scan;

                col *= 1.0 + band * _RollStrength;

                // Animated grain, and full static during bursts.
                float2 grainCell = floor(uv * _NoiseResolution.xy);
                half n = Hash12(grainCell + float2(frame * 17.31, frame * 91.7));
                col += (n - 0.5) * _NoiseStrength;
                col = lerp(col, n.xxx * _FeedTint.rgb, burst);

                // Brightness flicker.
                col *= 1.0 - _Flicker * Hash12(float2(floor(t * 30.0), seed));

                // Vignette.
                float2 v = uv - 0.5;
                col *= saturate(1.0 - dot(v, v) * _Vignette * 2.0);

                col = max(col, 0) * _Brightness * inside;
                col = MixFog(col, i.fogFactor);
                return half4(col, 1);
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On
            ColorMask R

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            float4 vert(float4 positionOS : POSITION) : SV_POSITION
            {
                return TransformObjectToHClip(positionOS.xyz);
            }

            half frag() : SV_Target { return 0; }
            ENDHLSL
        }

        Pass
        {
            Name "DepthNormalsOnly"
            Tags { "LightMode" = "DepthNormalsOnly" }
            ZWrite On

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 normalWS : TEXCOORD0; };

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                return half4(NormalizeNormalPerPixel(i.normalWS), 0.0);
            }
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
