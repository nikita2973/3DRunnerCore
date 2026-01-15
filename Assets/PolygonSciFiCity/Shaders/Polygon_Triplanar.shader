Shader "SyntyStudios/URP/Triplanar"
{
    Properties
    {
        _Overlay("Overlay", 2D) = "white" {}
        _FallOff("FallOff", Float) = 5.01
        _Tiling("Tiling", Float) = 6.24
        _Emission("Emission", 2D) = "white" {}
        _Texture("Texture", 2D) = "white" {}
        _EmissionColor("EmissionColor", Color) = (0,0,0,0)
        _DirtAmount("DirtAmount", Range(0.5, 1.2)) = 1
        
        [HideInInspector] _Surface("__surface", Float) = 0.0
        [HideInInspector] _Blend("__blend", Float) = 0.0
        [HideInInspector] _AlphaClip("__clip", Float) = 0.0
        [HideInInspector] _SrcBlend("__src", Float) = 1.0
        [HideInInspector] _DstBlend("__dst", Float) = 0.0
        [HideInInspector] _ZWrite("__zw", Float) = 1.0
        [HideInInspector] _Cull("__cull", Float) = 2.0
    }

    SubShader
    {
        Tags 
        { 
            "RenderType" = "Opaque" 
            "RenderPipeline" = "UniversalPipeline" 
            "Queue" = "Geometry"
        }
        
        LOD 300

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Blend[_SrcBlend][_DstBlend]
            ZWrite[_ZWrite]
            Cull[_Cull]

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x
            #pragma target 3.0

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
                float fogCoord : TEXCOORD3;
            };

            TEXTURE2D(_Texture);
            SAMPLER(sampler_Texture);
            
            TEXTURE2D(_Overlay);
            SAMPLER(sampler_Overlay);
            
            TEXTURE2D(_Emission);
            SAMPLER(sampler_Emission);

            CBUFFER_START(UnityPerMaterial)
                float4 _Texture_ST;
                float4 _Emission_ST;
                float _Tiling;
                float _FallOff;
                float _DirtAmount;
                float4 _EmissionColor;
            CBUFFER_END

            float4 TriplanarSampling(TEXTURE2D(tex), SAMPLER(samp), float3 worldPos, float3 worldNormal, float falloff, float tiling)
            {
                float3 projNormal = pow(abs(worldNormal), falloff);
                projNormal /= (projNormal.x + projNormal.y + projNormal.z);
                float3 nsign = sign(worldNormal);
                
                float negProjNormalY = max(0, projNormal.y * -nsign.y);
                projNormal.y = max(0, projNormal.y * nsign.y);
                
                float4 xNorm = SAMPLE_TEXTURE2D(tex, samp, tiling * worldPos.zy * float2(nsign.x, 1.0));
                float4 yNorm = SAMPLE_TEXTURE2D(tex, samp, tiling * worldPos.xz * float2(nsign.y, 1.0));
                float4 yNormN = SAMPLE_TEXTURE2D(tex, samp, tiling * worldPos.xz * float2(nsign.y, 1.0));
                float4 zNorm = SAMPLE_TEXTURE2D(tex, samp, tiling * worldPos.xy * float2(-nsign.z, 1.0));
                
                return xNorm * projNormal.x + yNorm * projNormal.y + yNormN * negProjNormalY + zNorm * projNormal.z;
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS, input.tangentOS);
                
                output.positionCS = vertexInput.positionCS;
                output.positionWS = vertexInput.positionWS;
                output.normalWS = normalInput.normalWS;
                output.uv = TRANSFORM_TEX(input.uv, _Texture);
                output.fogCoord = ComputeFogFactor(vertexInput.positionCS.z);
                
                return output;
            }

            float4 frag(Varyings input) : SV_Target
            {
                // Sample base texture
                float4 baseColor = SAMPLE_TEXTURE2D(_Texture, sampler_Texture, input.uv);
                
                // Sample triplanar overlay
                float4 triplanar = TriplanarSampling(TEXTURE2D_ARGS(_Overlay, sampler_Overlay), 
                                                     input.positionWS, 
                                                     input.normalWS, 
                                                     _FallOff, 
                                                     _Tiling);
                
                // Clamp triplanar result
                float4 clampedTriplanar = clamp(triplanar, _DirtAmount, 1.0);
                
                // Calculate albedo
                float3 albedo = baseColor.rgb * clampedTriplanar.rgb;
                
                // Sample emission
                float4 emissionTex = SAMPLE_TEXTURE2D(_Emission, sampler_Emission, TRANSFORM_TEX(input.uv, _Emission));
                float3 emission = emissionTex.rgb * _EmissionColor.rgb;
                
                // Lighting
                InputData lightingInput = (InputData)0;
                lightingInput.positionWS = input.positionWS;
                lightingInput.normalWS = normalize(input.normalWS);
                lightingInput.viewDirectionWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
                lightingInput.shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                lightingInput.fogCoord = input.fogCoord;
                
                SurfaceData surfaceData = (SurfaceData)0;
                surfaceData.albedo = albedo;
                surfaceData.metallic = 0;
                surfaceData.specular = 0;
                surfaceData.smoothness = 0.5;
                surfaceData.normalTS = float3(0, 0, 1);
                surfaceData.emission = emission;
                surfaceData.occlusion = 1;
                surfaceData.alpha = 1;
                
                float4 color = UniversalFragmentPBR(lightingInput, surfaceData);
                color.rgb = MixFog(color.rgb, input.fogCoord);
                
                return color;
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull[_Cull]

            HLSLPROGRAM
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment

            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x
            #pragma target 3.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderVariablesFunctions.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/ShadowCasterPass.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }

            ZWrite On
            ColorMask 0
            Cull[_Cull]

            HLSLPROGRAM
            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment

            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x
            #pragma target 3.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/DepthOnlyPass.hlsl"
            ENDHLSL
        }
    }
    
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}