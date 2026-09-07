// Machine-vision building shader (URP).
//
// This is a fork of Universal Render Pipeline/Lit (same Properties, same ForwardLit /
// ShadowCaster / DepthOnly code paths via URP's own LitInput.hlsl + LitForwardPass.hlsl),
// so swapping a material onto this shader keeps its full PBR look — specular, smoothness,
// environment reflections, shadows, lightmaps, fog — byte-for-byte from the artist's tuned
// values. Only two things are added on top, both driven per-building from CameraFocus via a
// MaterialPropertyBlock (no material instancing):
//   D: a horizontal row-band UV tear, applied before sampling / alpha-cutout
//   C: a vertical scan-in reveal (world-height clip) with an emissive band at the front
//
// Not carried over from stock Lit: decal (DBUFFER) projection, rendering-layer masking and
// Adaptive Probe Volumes — niche URP 17 features this project doesn't use. Everything else
// (metallic/specular workflow, normal/occlusion/detail maps, reflection probes, all light
// types + shadows, lightmaps, fog) is the same code URP/Lit uses.
Shader "Custom/BuildingScanTear"
{
    Properties
    {
        // Specular vs Metallic workflow
        _WorkflowMode("WorkflowMode", Float) = 1.0

        [MainTexture] _BaseMap("Albedo", 2D) = "white" {}
        [MainColor] _BaseColor("Color", Color) = (1,1,1,1)

        _Cutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5

        _Smoothness("Smoothness", Range(0.0, 1.0)) = 0.5
        _SmoothnessTextureChannel("Smoothness texture channel", Float) = 0

        _Metallic("Metallic", Range(0.0, 1.0)) = 0.0
        _MetallicGlossMap("Metallic", 2D) = "white" {}

        _SpecColor("Specular", Color) = (0.2, 0.2, 0.2)
        _SpecGlossMap("Specular", 2D) = "white" {}

        [ToggleOff] _SpecularHighlights("Specular Highlights", Float) = 1.0
        [ToggleOff] _EnvironmentReflections("Environment Reflections", Float) = 1.0

        _BumpScale("Scale", Float) = 1.0
        _BumpMap("Normal Map", 2D) = "bump" {}

        _Parallax("Scale", Range(0.005, 0.08)) = 0.005
        _ParallaxMap("Height Map", 2D) = "black" {}

        _OcclusionStrength("Strength", Range(0.0, 1.0)) = 1.0
        _OcclusionMap("Occlusion", 2D) = "white" {}

        [HDR] _EmissionColor("Color", Color) = (0,0,0)
        _EmissionMap("Emission", 2D) = "white" {}

        _DetailMask("Detail Mask", 2D) = "white" {}
        _DetailAlbedoMapScale("Scale", Range(0.0, 2.0)) = 1.0
        _DetailAlbedoMap("Detail Albedo x2", 2D) = "linearGrey" {}
        _DetailNormalMapScale("Scale", Range(0.0, 2.0)) = 1.0
        [Normal] _DetailNormalMap("Normal Map", 2D) = "bump" {}

        [HideInInspector] _ClearCoatMask("_ClearCoatMask", Float) = 0.0
        [HideInInspector] _ClearCoatSmoothness("_ClearCoatSmoothness", Float) = 0.0

        // Blending state
        _Surface("__surface", Float) = 0.0
        _Blend("__blend", Float) = 0.0
        _Cull("__cull", Float) = 2.0
        [ToggleUI] _AlphaClip("__clip", Float) = 0.0
        [HideInInspector] _SrcBlend("__src", Float) = 1.0
        [HideInInspector] _DstBlend("__dst", Float) = 0.0
        [HideInInspector] _SrcBlendAlpha("__srcA", Float) = 1.0
        [HideInInspector] _DstBlendAlpha("__dstA", Float) = 0.0
        [HideInInspector] _ZWrite("__zw", Float) = 1.0
        [HideInInspector] _BlendModePreserveSpecular("_BlendModePreserveSpecular", Float) = 1.0
        [HideInInspector] _AlphaToMask("__alphaToMask", Float) = 0.0
        [HideInInspector] _AddPrecomputedVelocity("_AddPrecomputedVelocity", Float) = 0.0

        [ToggleUI] _ReceiveShadows("Receive Shadows", Float) = 1.0
        _QueueOffset("Queue offset", Float) = 0.0

        // ObsoleteProperties
        [HideInInspector] _MainTex("BaseMap", 2D) = "white" {}
        [HideInInspector] _Color("Base Color", Color) = (1, 1, 1, 1)
        [HideInInspector] _GlossMapScale("Smoothness", Float) = 0.0
        [HideInInspector] _Glossiness("Smoothness", Float) = 0.0
        [HideInInspector] _GlossyReflections("EnvironmentReflections", Float) = 0.0

        [HideInInspector][NoScaleOffset] unity_Lightmaps("unity_Lightmaps", 2DArray) = "" {}
        [HideInInspector][NoScaleOffset] unity_LightmapsInd("unity_LightmapsInd", 2DArray) = "" {}
        [HideInInspector][NoScaleOffset] unity_ShadowMasks("unity_ShadowMasks", 2DArray) = "" {}

        // ---- Detection additions (C + D), driven per-building via MaterialPropertyBlock ----
        _Scan ("Scan (0..1)", Range(0,1)) = 1
        _BoundsMinY ("Bounds Min Y (world)", Float) = 0
        _BoundsHeight ("Bounds Height (world)", Float) = 1
        _ScanRows ("Scan Rows (quantize front, 0 = smooth)", Float) = 24
        _ScanBandHeight ("Scan Band Height (world units)", Float) = 0.3
        _AccentColor ("Accent Color", Color) = (1, 0.16470589, 0.10196079, 1)
        _TearRows ("Tear Rows", Float) = 32
        _TearInterval ("Tear Interval (s)", Float) = 0.12
        _TearAmount ("Tear Amount (UV)", Float) = 0.08
        _TearCount ("Tear Bands Per Interval", Float) = 3
        _TearIntensity ("Tear Intensity (0..1)", Range(0,1)) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "UniversalMaterialType" = "Lit"
            "IgnoreProjector" = "True"
        }
        LOD 300

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"

        float _Scan;
        float _BoundsMinY;
        float _BoundsHeight;
        float _ScanRows;
        float _ScanBandHeight;
        half4 _AccentColor;
        float _TearRows;
        float _TearInterval;
        float _TearAmount;
        float _TearCount;
        float _TearIntensity;

        float TearHash(float2 p)
        {
            return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453123);
        }

        // D: shift whole horizontal rows of UV. Same shape as the inset UI shader (InsetTear.shader).
        float2 ApplyTear(float2 uv)
        {
            float intensity = saturate(_TearIntensity);
            if (intensity <= 0.0001 || _TearAmount <= 0.0 || _TearRows < 1.0)
                return uv;

            float band = floor(uv.y * _TearRows);
            float bucket = floor(_Time.y / max(_TearInterval, 0.0001));

            // Per (band, bucket): selected with probability TearCount / TearRows (~K bands per interval).
            if (TearHash(float2(band, bucket)) >= saturate(_TearCount / _TearRows))
                return uv;

            float r = TearHash(float2(band + 17.13, bucket + 3.71));
            uv.x += (r * 2.0 - 1.0) * _TearAmount * intensity;
            return uv;
        }

        // C: quantized scan front as a 0..1 height fraction. Clip where heightFrac > this.
        float ScanFront()
        {
            float s = saturate(_Scan);
            if (_ScanRows >= 1.0)
                s = floor(s * _ScanRows) / _ScanRows;
            return s;
        }

        float HeightFrac(float worldY)
        {
            return (worldY - _BoundsMinY) / max(_BoundsHeight, 0.0001);
        }
        ENDHLSL

        // ------------------------------------------------------------------
        // Forward pass: URP's own LitForwardPass.hlsl (LitPassVertex + InitializeInputData +
        // InitializeBakedGIData + UniversalFragmentPBR) for full parity; only the fragment
        // entry point is ours, wrapping LitPassFragment with the tear + scan additions.
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Blend [_SrcBlend] [_DstBlend], [_SrcBlendAlpha] [_DstBlendAlpha]
            ZWrite [_ZWrite]
            Cull [_Cull]
            AlphaToMask [_AlphaToMask]

            HLSLPROGRAM
            #pragma target 2.0

            #pragma vertex LitPassVertex
            #pragma fragment ScanTearLitFragment

            #pragma shader_feature_local _NORMALMAP
            #pragma shader_feature_local _PARALLAXMAP
            #pragma shader_feature_local _RECEIVE_SHADOWS_OFF
            #pragma shader_feature_local _ _DETAIL_MULX2 _DETAIL_SCALED
            #pragma shader_feature_local_fragment _SURFACE_TYPE_TRANSPARENT
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma shader_feature_local_fragment _ _ALPHAPREMULTIPLY_ON _ALPHAMODULATE_ON
            #pragma shader_feature_local_fragment _EMISSION
            #pragma shader_feature_local_fragment _METALLICSPECGLOSSMAP
            #pragma shader_feature_local_fragment _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
            #pragma shader_feature_local_fragment _OCCLUSIONMAP
            #pragma shader_feature_local_fragment _SPECULARHIGHLIGHTS_OFF
            #pragma shader_feature_local_fragment _ENVIRONMENTREFLECTIONS_OFF
            #pragma shader_feature_local_fragment _SPECULAR_SETUP

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ EVALUATE_SH_MIXED EVALUATE_SH_VERTEX
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BLENDING
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BOX_PROJECTION
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_ATLAS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile_fragment _ _LIGHT_COOKIES
            #pragma multi_compile _ _LIGHT_LAYERS
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP

            #pragma multi_compile _ LIGHTMAP_SHADOW_MIXING
            #pragma multi_compile _ SHADOWS_SHADOWMASK
            #pragma multi_compile _ DIRLIGHTMAP_COMBINED
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile_fragment _ LIGHTMAP_BICUBIC_SAMPLING
            #pragma multi_compile_fragment _ REFLECTION_PROBE_ROTATION
            #pragma multi_compile _ DYNAMICLIGHTMAP_ON
            #pragma multi_compile _ USE_LEGACY_LIGHTMAPS
            #pragma multi_compile _ LOD_FADE_CROSSFADE
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Fog.hlsl"

            #pragma multi_compile_instancing
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"

            // Force the world-position interpolator on so the scan clip always has worldY,
            // regardless of which lighting keywords happen to be active.
            #define REQUIRES_WORLD_SPACE_POS_INTERPOLATOR 1

            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitForwardPass.hlsl"

            half4 ScanTearLitFragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float2 uv = ApplyTear(input.uv); // D: before sampling / alpha-cutout

            #if defined(_PARALLAXMAP)
                #if defined(REQUIRES_TANGENT_SPACE_VIEW_DIR_INTERPOLATOR)
                    half3 viewDirTS = input.viewDirTS;
                #else
                    half3 viewDirWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
                    half3 viewDirTS = GetViewDirectionTangentSpace(input.tangentWS, input.normalWS, viewDirWS);
                #endif
                ApplyPerPixelDisplacement(viewDirTS, uv);
            #endif

                SurfaceData surfaceData;
                InitializeStandardLitSurfaceData(uv, surfaceData); // includes the alpha-cutout clip

            #ifdef LOD_FADE_CROSSFADE
                LODFadeCrossFade(input.positionCS);
            #endif

                InputData inputData;
                InitializeInputData(input, surfaceData.normalTS, inputData);
                SETUP_DEBUG_TEXTURE_DATA(inputData, UNDO_TRANSFORM_TEX(input.uv, _BaseMap));

                InitializeBakedGIData(input, inputData);

                // C: scan-in reveal + emissive band riding just under the front.
                float heightFrac = HeightFrac(inputData.positionWS.y);
                float front = ScanFront();
                clip(front - heightFrac);

                float bandFrac = _ScanBandHeight / max(_BoundsHeight, 0.0001);
                float bandMask = saturate((heightFrac - (front - bandFrac)) / max(bandFrac, 0.0001)) * step(heightFrac, front);
                surfaceData.emission += _AccentColor.rgb * bandMask;

                half4 color = UniversalFragmentPBR(inputData, surfaceData);
                color.rgb = MixFog(color.rgb, inputData.fogCoord);
                color.a = OutputAlpha(color.a, IsSurfaceTypeTransparent(_Surface));
                return color;
            }
            ENDHLSL
        }

        // ------------------------------------------------------------------
        // ShadowCaster: fork of URP's ShadowCasterPass.hlsl (directional + punctual bias,
        // shadow clamping) with the tear + scan clip added before the alpha-cutout.
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull [_Cull]

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex ScanTearShadowVertex
            #pragma fragment ScanTearShadowFragment

            #pragma shader_feature_local _ALPHATEST_ON
            #pragma shader_feature_local_fragment _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A

            #pragma multi_compile_instancing
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"

            #pragma multi_compile _ LOD_FADE_CROSSFADE
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
            #if defined(LOD_FADE_CROSSFADE)
                #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/LODCrossFade.hlsl"
            #endif

            float3 _LightDirection;
            float3 _LightPosition;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 texcoord   : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float2 uv          : TEXCOORD0;
                float  worldY      : TEXCOORD1;
                float4 positionCS  : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings ScanTearShadowVertex(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);

                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);

            #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                float3 lightDirectionWS = normalize(_LightPosition - positionWS);
            #else
                float3 lightDirectionWS = _LightDirection;
            #endif

                float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDirectionWS));
                output.positionCS = ApplyShadowClamping(positionCS);
                output.uv = TRANSFORM_TEX(input.texcoord, _BaseMap);
                output.worldY = positionWS.y;
                return output;
            }

            half4 ScanTearShadowFragment(Varyings input) : SV_TARGET
            {
                UNITY_SETUP_INSTANCE_ID(input);

                float2 uv = ApplyTear(input.uv);

            #if defined(_ALPHATEST_ON)
                Alpha(SampleAlbedoAlpha(uv, TEXTURE2D_ARGS(_BaseMap, sampler_BaseMap)).a, _BaseColor, _Cutoff);
            #endif

                clip(ScanFront() - HeightFrac(input.worldY));

            #if defined(LOD_FADE_CROSSFADE)
                LODFadeCrossFade(input.positionCS);
            #endif

                return 0;
            }
            ENDHLSL
        }

        // ------------------------------------------------------------------
        // DepthOnly: fork of URP's DepthOnlyPass.hlsl with the same tear + scan clip.
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }

            ZWrite On
            ColorMask R
            Cull [_Cull]

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex ScanTearDepthVertex
            #pragma fragment ScanTearDepthFragment

            #pragma shader_feature_local _ALPHATEST_ON
            #pragma shader_feature_local_fragment _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A

            #pragma multi_compile _ LOD_FADE_CROSSFADE
            #pragma multi_compile_instancing
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"

            #if defined(LOD_FADE_CROSSFADE)
                #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/LODCrossFade.hlsl"
            #endif

            struct Attributes
            {
                float4 position  : POSITION;
                float2 texcoord  : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float2 uv          : TEXCOORD0;
                float  worldY      : TEXCOORD1;
                float4 positionCS  : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings ScanTearDepthVertex(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                output.uv = TRANSFORM_TEX(input.texcoord, _BaseMap);
                output.worldY = TransformObjectToWorld(input.position.xyz).y;
                output.positionCS = TransformObjectToHClip(input.position.xyz);
                return output;
            }

            half ScanTearDepthFragment(Varyings input) : SV_TARGET
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float2 uv = ApplyTear(input.uv);

            #if defined(_ALPHATEST_ON)
                Alpha(SampleAlbedoAlpha(uv, TEXTURE2D_ARGS(_BaseMap, sampler_BaseMap)).a, _BaseColor, _Cutoff);
            #endif

                clip(ScanFront() - HeightFrac(input.worldY));

            #if defined(LOD_FADE_CROSSFADE)
                LODFadeCrossFade(input.positionCS);
            #endif

                return input.positionCS.z;
            }
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
    CustomEditor "UnityEditor.Rendering.Universal.ShaderGUI.LitShader"
}
