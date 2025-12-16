Shader "Pipe/FrostedGlass"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (0, 0.02, 0.06, 0.2) // Dark Blue, Low Alpha
        _BaseMap ("Base Map", 2D) = "white" {}
        _RimColor ("Rim Color", Color) = (0, 1, 1, 1) // Cyan Rim
        _RimPower ("Rim Power", Range(0.5, 8.0)) = 3.0
        _Smoothness ("Smoothness", Range(0, 1)) = 0.95
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline" = "UniversalPipeline" }
        LOD 100

        Pass
        {
            Name "FrostedGlass"
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float2 uv : TEXCOORD0;
                float4 positionCS : SV_POSITION;
                float3 normalWS : NORMAL;
                float3 viewDirWS : TEXCOORD1;
            };

            sampler2D _BaseMap;
            float4 _BaseMap_ST;

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _RimColor;
                float _RimPower;
            CBUFFER_END

            Varyings vert (Attributes v)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.uv = v.uv; // Simple UV pass
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                float3 positionWS = TransformObjectToWorld(v.positionOS.xyz);
                o.viewDirWS = GetWorldSpaceViewDir(positionWS);
                return o;
            }

            half4 frag (Varyings i) : SV_Target
            {
                float3 normal = normalize(i.normalWS);
                float3 viewDir = normalize(i.viewDirWS);
                
                // Sample Texture
                float4 texColor = tex2D(_BaseMap, i.uv);
                
                // Fresnel
                float NdotV = saturate(dot(normal, viewDir));
                float fresnel = pow(1.0 - NdotV, _RimPower);
                
                float3 emission = _RimColor.rgb * fresnel;
                
                // Base Color * Texture
                // Assume texture is white/grey pattern.
                float3 base = _BaseColor.rgb * texColor.rgb;
                
                // Alpha logic
                // Use BaseColor Alpha.
                // If using checkerboard for debug, we likely want Alpha ~ 1.0.
                float alpha = _BaseColor.a + (fresnel * 0.5);
                float3 finalColor = base + emission;
                
                return half4(finalColor, saturate(alpha));
            }
            ENDHLSL
        }
    }
}
