Shader "Pipe/NeonLiquid"
{
    Properties
    {
        _MainColor ("Main Color", Color) = (1, 1, 1, 1)
        _NoiseTex ("Noise Texture", 2D) = "white" {}
        _FlowSpeed ("Flow Speed", Float) = 1.0
        _NoiseScale ("Noise Scale", Float) = 1.0
        _CoreIntensity ("Core Intensity", Range(0.1, 10.0)) = 3.0
        _TrailDensity ("Trail Density", Range(0.0, 1.0)) = 0.7
        _TrailSpeed ("Trail Speed", Float) = 2.0
        _TrailScale ("Trail Scale", Float) = 0.2
        _IsComplete ("Is Complete", Float) = 0.0 // 0 = Incomplete, 1 = Complete
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline" = "UniversalPipeline" }
        LOD 100
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            Name "NeonLiquid"
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct Varyings
            {
                float2 uv : TEXCOORD0;
                float4 positionCS : SV_POSITION; // Clip Space
                float4 color : COLOR;
            };

            sampler2D _NoiseTex; // Samplers in HLSL are often separate or combined
            // use macros for textures if strictly following URP, but sampler2D works usually.
            // Better: TEXTURE2D(_NoiseTex); SAMPLER(sampler_NoiseTex);
            
            CBUFFER_START(UnityPerMaterial)
                float4 _MainColor;
                float4 _NoiseTex_ST;
                float _FlowSpeed;
                float _NoiseScale;
                float _CoreIntensity;
                float _TrailDensity;
                float _TrailSpeed;
                float _TrailScale;
                float _IsComplete;
            CBUFFER_END

            Varyings vert (Attributes v)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.uv = v.uv * _NoiseTex_ST.xy + _NoiseTex_ST.zw;
                o.color = v.color * _MainColor;
                return o;
            }

            half4 frag (Varyings i) : SV_Target
            {
                // UV.y is 0..1. Center is 0.5.
                // dist 0 = center, 1 = edge
                float dist = abs(i.uv.y - 0.5) * 2.0; 
                
                // --- LAYERING STRATEGY ---
                // [Center --(Neon Glow)---> 0.8] --(Dark Glass Space)---> [0.95 Specular] -> Edge
                
                // 1. NEON GLOW (The "Rim Light")
                float glowLimit = 0.85;
                float glowDist = saturate(dist / glowLimit); // 0 to 1 within the limit
                
                // Sharp Outer Rim of Neon (Non-Blooming)
                // Keep intensity below Threshold (3.0). Core is usually 3.0.
                // 3.0 * 0.9 = 2.7 (No Bloom)
                float innerRim = pow(glowDist, 10.0) * step(dist, glowLimit); 
                
                // Inner Blur (Blooming)
                // Making the inside brighter so it blooms
                // 3.0 * 1.8 = 5.4 (Bloom!)
                float innerBlur = pow(glowDist, 2.0) * step(dist, glowLimit);
                
                // Flow Noise
                float2 flowUV = i.uv;
                flowUV.x *= _NoiseScale;
                flowUV.x -= _Time.y * _FlowSpeed;
                float noise = tex2D(_NoiseTex, flowUV).r;
                float flowPulse = noise * 0.5 + 0.5;
                
                // Neon Color Composition
                // Rim is sharp but constrained brightness. Inner body is bright and blooming.
                float3 neonColor = i.color.rgb * _CoreIntensity * (innerRim * 0.9 + innerBlur * 1.8) * flowPulse;

                // 2. OUTER GLASS SHELL (Specular)
                // Place it OUTSIDE the glow limit
                // distFromCenter and highlightOffset logic
                float distFromCenter = abs(i.uv.y - 0.5);
                float highlightOffset = 0.46; // At dist 0.92
                float highlightWidth = 0.05; // Sharp
                float highlightDist = abs(distFromCenter - highlightOffset);
                float specular = smoothstep(highlightWidth, 0.0, highlightDist) * 1.5; // Intensity 1.5 (No Bloom)
                
                // Specular Tint (Strong Color Tint)
                float3 specColor = lerp(float3(1,1,1), i.color.rgb, 0.7);
                
                // --- 3. FROSTED CENTER ---
                // Frost area: inverse of dist, fading out at the glow limit
                float frostShape = 1.0 - smoothstep(0.1, 1.0, dist); 
                
                // Static Grain (Scattering)
                float2 grainUV = i.uv * 8.0; 
                float grain = tex2D(_NoiseTex, grainUV).r;
                
                // Frost Color: Mix of Line Color and White
                float3 frostColor = lerp(i.color.rgb, float3(1,1,1), 0.5) * 0.3; // 30% brightness
                frostColor += grain * 0.05; // Subtle grain
                
                // --- 3. FROSTED CENTER ---
                // ... (Calculation code remains)
                float3 frostLayer = frostColor * frostShape * _IsComplete; // Mask by Complete
                float frostAlpha = frostShape * 0.4 * _IsComplete;

                // --- 4. LIQUID TRAIL EFFECT (ORGANIC DUAL LAYER) ---
                float2 trailUV1 = i.uv;
                trailUV1.x *= _TrailScale;
                trailUV1.x -= _Time.y * _FlowSpeed * _TrailSpeed;
                
                // Second layer: Slightly slower and offset, creates relative motion (morphing)
                float2 trailUV2 = i.uv;
                trailUV2.x *= _TrailScale * 1.2; // Slightly different scale
                trailUV2.x -= _Time.y * _FlowSpeed * (_TrailSpeed * 0.7) + 0.5; 
                
                float n1 = tex2D(_NoiseTex, trailUV1).r;
                float n2 = tex2D(_NoiseTex, trailUV2).r;
                
                // Metaball blending: Combine noises.
                float combinedNoise = n1 * 0.6 + n2 * 0.6; 
                
                // Trail mask: liquidVal
                float liquidVal = smoothstep(_TrailDensity, _TrailDensity + 0.2, combinedNoise);
                
                // Trail Shape
                float trailShape = innerBlur * liquidVal * _IsComplete; // Mask by Complete
                
                // Trail Color
                float3 trailColor = i.color.rgb * _CoreIntensity * 3.0 * trailShape;

                // --- Combine ---
                float3 finalColor = neonColor + (specColor * specular) + frostLayer + trailColor;
                
                // Alpha
                // Frost and Trail only add alpha if Complete
                float trailAlpha = liquidVal * _IsComplete;
                
                float glassEdge = pow(dist, 10.0) * 0.3; 
                
                float finalAlpha = saturate(innerBlur + (specular * 2.0) + glassEdge + (innerRim * 0.5) + frostAlpha + trailAlpha);
                
                return half4(finalColor, finalAlpha);
            }
            ENDHLSL
        }
    }
}
