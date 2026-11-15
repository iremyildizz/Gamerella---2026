Shader "Custom/UI_HackingScreenCRT"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (0.1, 0.9, 1.0, 0.28)

        [Header(Scanlines)]
        _ScanlineDensity ("Scanline Count", Range(50, 800)) = 320
        _ScanlineStrength ("Scanline Darkness", Range(0, 1)) = 0.45
        _ScanlineSpeed ("Scanline Scroll Speed", Range(-5, 5)) = 0.8

        [Header(Rolling Refresh Bar)]
        _RollBarSpeed ("Refresh Bar Speed", Range(0, 3)) = 0.35
        _RollBarIntensity ("Refresh Bar Glow", Range(0, 0.5)) = 0.14

        [Header(Subpixel Grid and Vignette)]
        _PixelGridScale ("Subpixel Grid Density", Range(50, 600)) = 220
        _PixelGridStrength ("Subpixel Grid Strength", Range(0, 0.5)) = 0.15
        _VignetteStrength ("Edge Vignette", Range(0, 2)) = 0.85
        _FlickerAmount ("Subtle Screen Flicker", Range(0, 0.15)) = 0.03

        // Required Unity UI Stencil / Mask properties
        [HideInInspector] _StencilComp ("Stencil Comparison", Float) = 8
        [HideInInspector] _Stencil ("Stencil ID", Float) = 0
        [HideInInspector] _StencilOp ("Stencil Operation", Float) = 0
        [HideInInspector] _StencilWriteMask ("Stencil Write Mask", Float) = 255
        [HideInInspector] _StencilReadMask ("Stencil Read Mask", Float) = 255
        [HideInInspector] _ColorMask ("Color Mask", Float) = 15
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;

            float _ScanlineDensity;
            float _ScanlineStrength;
            float _ScanlineSpeed;
            float _RollBarSpeed;
            float _RollBarIntensity;
            float _PixelGridScale;
            float _PixelGridStrength;
            float _VignetteStrength;
            float _FlickerAmount;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color * _Color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 baseCol = tex2D(_MainTex, i.uv) * i.color;

                // 1. Horizontal CRT scanlines
                float scanPhase = (i.uv.y * _ScanlineDensity) + (_Time.y * _ScanlineSpeed);
                float scanline = 0.5 + 0.5 * sin(scanPhase * 6.2831853);
                float scanFactor = lerp(1.0, scanline, _ScanlineStrength);

                // 2. Vertical phosphor subpixel grid
                float grid = 0.5 + 0.5 * sin(i.uv.x * _PixelGridScale * 6.2831853);
                float gridFactor = lerp(1.0, grid, _PixelGridStrength);

                // 3. Slow-moving CRT refresh band
                float rollPos = frac(_Time.y * _RollBarSpeed);
                float distToRoll = abs(i.uv.y - (1.0 - rollPos));
                float rollGlow = smoothstep(0.18, 0.0, distToRoll) * _RollBarIntensity;

                // 4. Curved monitor corner vignette
                float2 centeredUV = i.uv * 2.0 - 1.0;
                float vignette = saturate(1.0 - dot(centeredUV, centeredUV) * 0.45 * _VignetteStrength);

                // 5. High-frequency subtle power flicker
                float flicker = 1.0 - _FlickerAmount * (0.5 + 0.5 * sin(_Time.y * 53.0));

                float3 finalRGB = baseCol.rgb * scanFactor * gridFactor * flicker + (baseCol.rgb * rollGlow);
                float finalAlpha = saturate(baseCol.a + (1.0 - scanFactor) * 0.25 + (1.0 - vignette) * 0.55 + rollGlow * 0.5);

                return fixed4(finalRGB * vignette, finalAlpha);
            }
            ENDCG
        }
    }
}