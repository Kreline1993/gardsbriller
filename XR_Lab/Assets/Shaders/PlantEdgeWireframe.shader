Shader "Custom/Plant Edge Wireframe"
{
    Properties
    {
        _EdgeColor("Edge Color", Color) = (1,1,1,1)
        _EdgeWidth("Edge Width", Range(0.5, 6.0)) = 1.5
        [Enum(UnityEngine.Rendering.CompareFunction)] _ZTest("ZTest", Float) = 4
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent+120"
            "RenderType" = "Transparent"
            "DisableBatching" = "True"
        }

        Pass
        {
            Name "Wireframe"
            Cull Off
            ZWrite Off
            ZTest [_ZTest]
            Blend SrcAlpha OneMinusSrcAlpha

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 bary : TEXCOORD2;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 bary : TEXCOORD0;
            };

            fixed4 _EdgeColor;
            float _EdgeWidth;

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.bary = v.bary;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // Scalar edge distance is more stable than per-channel smoothing on some platforms.
                float minBary = min(i.bary.x, min(i.bary.y, i.bary.z));
                float pixelWidth = max(fwidth(minBary) * _EdgeWidth, 1e-4);
                float edge = 1.0 - smoothstep(0.0, pixelWidth, minBary);

                // Hard-clip interior fragments to avoid accidental full-triangle fills.
                clip(edge - 0.001);

                fixed4 col = _EdgeColor;
                col.a *= edge;
                return col;
            }
            ENDCG
        }
    }
}