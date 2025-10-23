Shader "Custom/XPTextNeonGlow"
{
    Properties
    {
        _MainTex ("Font Texture", 2D) = "white" {}
        _Color ("Text Color", Color) = (1,1,1,1)
        
        [Header(Neon Glow Settings)]
        _GlowColor ("Glow Color", Color) = (0.5, 1, 1, 1)
        _GlowIntensity ("Glow Intensity", Range(0, 5)) = 2.0
        _GlowSize ("Glow Size", Range(0, 1)) = 0.3
        
        [Header(Emission Settings)]
        _EmissionStrength ("Emission Strength", Range(0, 10)) = 3.0
        _EmissionPulse ("Emission Pulse Speed", Range(0, 5)) = 1.0
        _EmissionPulseAmount ("Emission Pulse Amount", Range(0, 1)) = 0.3
        
        [Header(Edge Smoothing)]
        _EdgeSmoothness ("Edge Smoothness", Range(0, 1)) = 0.15
        
        [Header(Rendering)]
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
    }
    
    SubShader
    {
        Tags 
        { 
            "Queue"="Overlay+5000" 
            "IgnoreProjector"="True" 
            "RenderType"="Transparent" 
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
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
        ZTest Always // CRITICAL: Always render on top!
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]
        
        Pass
        {
            Name "NEON_GLOW_TEXT"
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            
            #include "UnityCG.cginc"
            
            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };
            
            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
                float4 worldPos : TEXCOORD1;
            };
            
            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _Color;
            float4 _GlowColor;
            float _GlowIntensity;
            float _GlowSize;
            float _EmissionStrength;
            float _EmissionPulse;
            float _EmissionPulseAmount;
            float _EdgeSmoothness;
            
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color * _Color;
                o.worldPos = mul(unity_ObjectToWorld, v.vertex);
                return o;
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
                // Sample the font texture
                fixed4 texColor = tex2D(_MainTex, i.uv);
                
                // Smooth the edges using smoothstep for anti-aliasing
                float alpha = smoothstep(0.5 - _EdgeSmoothness, 0.5 + _EdgeSmoothness, texColor.a);
                
                // Base text color
                fixed4 textColor = i.color;
                textColor.a *= alpha;
                
                // === NEON GLOW EFFECT ===
                // Create glow based on distance from edge
                float glowFactor = smoothstep(0.5 - _GlowSize, 0.5, texColor.a);
                glowFactor = pow(glowFactor, 2.0); // Sharper falloff
                
                // Glow color with intensity
                fixed4 glow = _GlowColor * glowFactor * _GlowIntensity;
                
                // === EMISSION PULSE ===
                // Subtle pulse effect using time
                float pulse = sin(_Time.y * _EmissionPulse) * 0.5 + 0.5; // 0 to 1
                float emissionMult = 1.0 + (pulse * _EmissionPulseAmount);
                
                // === COMBINE EFFECTS ===
                // Mix text color with glow
                fixed4 finalColor = textColor + glow;
                
                // Add emission (makes it "glow" in dark areas)
                finalColor.rgb += textColor.rgb * _EmissionStrength * emissionMult * alpha;
                
                // Ensure alpha is correct
                finalColor.a = textColor.a;
                
                // Clamp to prevent over-brightness
                finalColor.rgb = saturate(finalColor.rgb);
                
                return finalColor;
            }
            ENDCG
        }
    }
    
    FallBack "UI/Default"
}
