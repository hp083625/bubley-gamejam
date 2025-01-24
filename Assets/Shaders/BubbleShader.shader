Shader "Custom/BubbleShader"
{
    Properties
    {
        _MainTex ("Base (RGB)", 2D) = "white" {}
        _BubbleColor ("Bubble Color", Color) = (1,1,1,1)
        _Smoothness ("Smoothness", Range(0,1)) = 0.8
        _FresnelPower ("Fresnel Power", Range(0,10)) = 3.0
        _ReflectionTint ("Reflection Tint", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "RenderType"="Transparent"
        }

        // Blending for transparency
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off  // Don’t write to depth buffer so it looks “soft”
        LOD 300

        CGPROGRAM
        // Tell Unity we are writing a surface shader in the Standard lighting model with alpha fade
        #pragma surface surf Standard alpha:fade

        // Provide Shader Model (just to be safe)
        #pragma target 3.0

        sampler2D _MainTex;
        float4 _BubbleColor;
        float _Smoothness;
        float _FresnelPower;
        float4 _ReflectionTint;

        struct Input
        {
            float2 uv_MainTex;
            float3 viewDir;       // The view direction
            float3 worldNormal;   // The normal in world space
        };

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            // Sample base color
            fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _BubbleColor;

            // Fresnel term
            // Fresnel is stronger when viewDir is almost perpendicular to the normal.
            // The power value can intensify this highlight around the edges.
            float fresnelFactor = pow(1.0 - saturate(dot(normalize(IN.viewDir), normalize(IN.worldNormal))), _FresnelPower);

            // Base surface properties
            o.Albedo = c.rgb;         // Base color of the bubble
            o.Alpha = c.a * 0.5;      // Make it partially transparent (tweak as needed)
            o.Smoothness = _Smoothness; 
            o.Metallic = 0.0;

            // Use Fresnel to add a bright edge tint to the emission channel for a bubble-like rim
            float3 edgeTint = _ReflectionTint.rgb * fresnelFactor;
            o.Emission = edgeTint;    // Slight glow / reflection color on the edges
        }
        ENDCG
    }
    FallBack "Transparent/Cutout/VertexLit"
}
