Shader "Custom/HoverOutline"
{
    Properties
    {
        _OutlineColor ("Outline Color", Color) = (1.0, 1.0, 1.0, 1.0)
        _OutlineWidth ("Outline Width (meters)", Range(0.0, 0.1)) = 0.01
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry+1" }
        Cull Front

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
            };

            fixed4 _OutlineColor;
            float _OutlineWidth;

            v2f vert (appdata v)
            {
                v2f o;
                // Extrude in WORLD space so the width is in meters and does
                // not depend on the model's scale or import units.
                float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                float3 worldNormal = normalize(UnityObjectToWorldNormal(v.normal));
                worldPos += worldNormal * _OutlineWidth;
                o.pos = UnityWorldToClipPos(worldPos);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                return _OutlineColor;
            }
            ENDCG
        }
    }
}