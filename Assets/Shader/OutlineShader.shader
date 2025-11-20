Shader "Custom/ConnectedOutline"
{
    Properties
    {
        _OutlineColor ("Outline Color", Color) = (0,0,0,1)
        _OutlineScale ("Outline Scale", Float) = 1.05
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        // tell Unity this shader uses transparency
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }

        Pass
        {
            Name "OUTLINE"
            Tags { "LightMode"="Always" }

            Cull Front
            ZWrite Off                   // disable depth writing so alpha blends correctly
            ZTest LEqual
            Blend SrcAlpha OneMinusSrcAlpha   // standard alpha blending

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
            };

            fixed4 _OutlineColor;
            float _OutlineScale;

            v2f vert(appdata v)
            {
                v2f o;
                v.vertex.xyz *= _OutlineScale; // scale mesh uniformly
                o.pos = UnityObjectToClipPos(v.vertex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                return _OutlineColor; // alpha now respected
            }
            ENDCG
        }
    }
}
