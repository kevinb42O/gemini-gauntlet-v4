Shader "Custom/XPTextWallhack"
{
    Properties
    {
        _MainTex ("Font Texture", 2D) = "white" {}
        _Color ("Text Color", Color) = (1,1,1,1)
        
        [Header(Visible Part - Normal)]
        _FaceColor ("Face Color", Color) = (1,1,1,1)
        _OutlineColor ("Outline Color", Color) = (0,0,0,1)
        _OutlineWidth ("Outline Width", Range(0, 1)) = 0.3
        
        [Header(Occluded Part - Through Walls)]
        _OccludedColor ("Occluded Color", Color) = (0.5, 0.5, 0.5, 0.5)
        _OccludedOutlineColor ("Occluded Outline", Color) = (0.3, 0.3, 0.3, 0.5)
        _OccludedAlpha ("Occluded Alpha", Range(0, 1)) = 0.4
        
        [Header(Glow Settings)]
        _GlowColor ("Glow Color", Color) = (0, 1, 1, 1)
        _GlowPower ("Glow Power", Range(0, 5)) = 2.0
        
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
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]
        
        // PASS 1: Occluded (Behind Walls) - Renders FIRST with ZTest Greater
        Pass
        {
            Name "OCCLUDED"
            ZTest Greater // Only render when BEHIND something
            
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
            };
            
            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _OccludedColor;
            float4 _OccludedOutlineColor;
            float _OccludedAlpha;
            float _OutlineWidth;
            
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color;
                return o;
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
                // Sample texture
                fixed4 texColor = tex2D(_MainTex, i.uv);
                
                // Smooth edges
                float alpha = smoothstep(0.5 - 0.15, 0.5 + 0.15, texColor.a);
                
                // Occluded appearance (desaturated, transparent)
                fixed4 finalColor = _OccludedColor;
                finalColor.a = alpha * _OccludedAlpha;
                
                // Add subtle outline
                float outlineFactor = smoothstep(0.5 - _OutlineWidth, 0.5, texColor.a);
                finalColor.rgb = lerp(_OccludedOutlineColor.rgb, finalColor.rgb, outlineFactor);
                
                return finalColor;
            }
            ENDCG
        }
        
        // PASS 2: Visible (Normal) - Renders SECOND with ZTest LEqual
        Pass
        {
            Name "VISIBLE"
            ZTest LEqual // Only render when IN FRONT or equal
            
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
            };
            
            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _Color;
            float4 _FaceColor;
            float4 _OutlineColor;
            float4 _GlowColor;
            float _OutlineWidth;
            float _GlowPower;
            
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color * _Color;
                return o;
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
                // Sample texture
                fixed4 texColor = tex2D(_MainTex, i.uv);
                
                // Smooth edges
                float alpha = smoothstep(0.5 - 0.15, 0.5 + 0.15, texColor.a);
                
                // Base color
                fixed4 finalColor = i.color * _FaceColor;
                finalColor.a *= alpha;
                
                // Outline
                float outlineFactor = smoothstep(0.5 - _OutlineWidth, 0.5, texColor.a);
                finalColor.rgb = lerp(_OutlineColor.rgb, finalColor.rgb, outlineFactor);
                
                // Glow effect
                float glowFactor = smoothstep(0.5 - 0.25, 0.5, texColor.a);
                glowFactor = pow(glowFactor, 2.0);
                fixed4 glow = _GlowColor * glowFactor * _GlowPower;
                
                finalColor.rgb += glow.rgb * alpha;
                
                // Clamp
                finalColor.rgb = saturate(finalColor.rgb);
                
                return finalColor;
            }
            ENDCG
        }
    }
    
    FallBack "TextMeshPro/Mobile/Distance Field"
}
