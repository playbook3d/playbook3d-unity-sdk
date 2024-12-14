Shader "Playbook Shaders/OutlinePassShader"
{
    Properties
    {
        [HideInInspector][NoScaleOffset]unity_Lightmaps("unity_Lightmaps", 2DArray) = "" {}
        [HideInInspector][NoScaleOffset]unity_LightmapsInd("unity_LightmapsInd", 2DArray) = "" {}
        [HideInInspector][NoScaleOffset]unity_ShadowMasks("unity_ShadowMasks", 2DArray) = "" {}
    }
    SubShader
    {
        Tags
        {
            "RenderPipeline"="UniversalPipeline"
            // RenderType: <None>
            // Queue: <None>
            // DisableBatching: <None>
            "ShaderGraphShader"="true"
            "ShaderGraphTargetId"="UniversalFullscreenSubTarget"
        }
        Pass
        {
            Name "DrawProcedural"
        
        // Render State
        Cull Off
        Blend Off
        ZTest Off
        ZWrite Off
        
        // Debug
        // <None>
        
        // --------------------------------------------------
        // Pass
        
        HLSLPROGRAM
        
        // Pragmas
        #pragma target 3.0
        #pragma vertex vert
        #pragma fragment frag
        // #pragma enable_d3d11_debug_symbols
        
        /* WARNING: $splice Could not find named fragment 'DotsInstancingOptions' */
        /* WARNING: $splice Could not find named fragment 'HybridV1InjectedBuiltinProperties' */
        
        // Keywords
        // PassKeywords: <None>
        // GraphKeywords: <None>
        
        #define FULLSCREEN_SHADERGRAPH
        
        // Defines
        #define ATTRIBUTES_NEED_TEXCOORD0
        #define ATTRIBUTES_NEED_TEXCOORD1
        #define ATTRIBUTES_NEED_VERTEXID
        #define VARYINGS_NEED_POSITION_WS
        #define VARYINGS_NEED_TEXCOORD0
        #define VARYINGS_NEED_TEXCOORD1
        
        // Force depth texture because we need it for almost every nodes
        // TODO: dependency system that triggers this define from position or view direction usage
        #define REQUIRE_DEPTH_TEXTURE
        #define REQUIRE_NORMAL_TEXTURE
        
        /* WARNING: $splice Could not find named fragment 'PassInstancing' */
        #define SHADERPASS SHADERPASS_DRAWPROCEDURAL
        #define REQUIRE_DEPTH_TEXTURE
        
        // custom interpolator pre-include
        /* WARNING: $splice Could not find named fragment 'sgci_CustomInterpolatorPreInclude' */
        
        // Includes
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
        #include "Packages/com.unity.shadergraph/Editor/Generation/Targets/Fullscreen/Includes/FullscreenShaderPass.cs.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        #include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/SpaceTransforms.hlsl"
        #include "Packages/com.unity.shadergraph/ShaderGraphLibrary/Functions.hlsl"
        
        // --------------------------------------------------
        // Structs and Packing
        
        // custom interpolators pre packing
        /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPrePacking' */
        
        struct Attributes
        {
            #if UNITY_ANY_INSTANCING_ENABLED
             uint instanceID : INSTANCEID_SEMANTIC;
            #endif
             uint vertexID : VERTEXID_SEMANTIC;
        };
        struct SurfaceDescriptionInputs
        {
             float3 WorldSpacePosition;
             float4 ScreenPosition;
             float2 NDCPosition;
             float2 PixelPosition;
        };
        struct Varyings
        {
             float4 positionCS : SV_POSITION;
             float4 texCoord0;
             float4 texCoord1;
            #if UNITY_ANY_INSTANCING_ENABLED
             uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
             uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
             uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
            #endif
        };
        struct VertexDescriptionInputs
        {
        };
        struct PackedVaryings
        {
             float4 positionCS : SV_POSITION;
             float4 texCoord0 : INTERP0;
             float4 texCoord1 : INTERP1;
            #if UNITY_ANY_INSTANCING_ENABLED
             uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
             uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
             uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
            #endif
        };
        
        PackedVaryings PackVaryings (Varyings input)
        {
            PackedVaryings output;
            ZERO_INITIALIZE(PackedVaryings, output);
            output.positionCS = input.positionCS;
            output.texCoord0.xyzw = input.texCoord0;
            output.texCoord1.xyzw = input.texCoord1;
            #if UNITY_ANY_INSTANCING_ENABLED
            output.instanceID = input.instanceID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
            #endif
            return output;
        }
        
        Varyings UnpackVaryings (PackedVaryings input)
        {
            Varyings output;
            output.positionCS = input.positionCS;
            output.texCoord0 = input.texCoord0.xyzw;
            output.texCoord1 = input.texCoord1.xyzw;
            #if UNITY_ANY_INSTANCING_ENABLED
            output.instanceID = input.instanceID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
            #endif
            return output;
        }
        
        
        // --------------------------------------------------
        // Graph
        
        // Graph Properties
        CBUFFER_START(UnityPerMaterial)
        CBUFFER_END
        
        
        // Object and Global properties
        float _FlipY;
        
        // Graph Includes
        // GraphIncludes: <None>
        
        // Graph Functions
        
        void Unity_Add_float(float A, float B, out float Out)
        {
            Out = A + B;
        }
        
        void Unity_Multiply_float2_float2(float2 A, float2 B, out float2 Out)
        {
            Out = A * B;
        }
        
        void Unity_Divide_float2(float2 A, float2 B, out float2 Out)
        {
            Out = A / B;
        }
        
        void Unity_Add_float2(float2 A, float2 B, out float2 Out)
        {
            Out = A + B;
        }
        
        float3 Unity_Universal_SampleBuffer_NormalWorldSpace_float(float2 uv)
        {
            return SHADERGRAPH_SAMPLE_SCENE_NORMAL(uv);
        }
        
        void Unity_Multiply_float3_float3(float3 A, float3 B, out float3 Out)
        {
            Out = A * B;
        }
        
        void Unity_Add_float3(float3 A, float3 B, out float3 Out)
        {
            Out = A + B;
        }
        
        void Unity_Subtract_float3(float3 A, float3 B, out float3 Out)
        {
            Out = A - B;
        }
        
        void Unity_Absolute_float3(float3 In, out float3 Out)
        {
            Out = abs(In);
        }
        
        void Unity_Maximum_float(float A, float B, out float Out)
        {
            Out = max(A, B);
        }
        
        void Unity_Smoothstep_float(float Edge1, float Edge2, float In, out float Out)
        {
            Out = smoothstep(Edge1, Edge2, In);
        }
        
        void Unity_SceneDepth_Linear01_float(float4 UV, out float Out)
        {
            Out = Linear01Depth(SHADERGRAPH_SAMPLE_SCENE_DEPTH(UV.xy), _ZBufferParams);
        }
        
        void Unity_Subtract_float(float A, float B, out float Out)
        {
            Out = A - B;
        }
        
        void Unity_Absolute_float(float In, out float Out)
        {
            Out = abs(In);
        }
        
        void Unity_Divide_float(float A, float B, out float Out)
        {
            Out = A / B;
        }
        
        void Unity_Saturate_float(float In, out float Out)
        {
            Out = saturate(In);
        }
        
        // Custom interpolators pre vertex
        /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPreVertex' */
        
        // Graph Vertex
        // GraphVertex: <None>
        
        // Custom interpolators, pre surface
        /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPreSurface' */
        
        // Graph Pixel
        struct SurfaceDescription
        {
            float3 BaseColor;
            float Alpha;
        };
        
        SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
        {
            SurfaceDescription surface = (SurfaceDescription)0;
            float _Float_513d9acd4d3943bfa229d715b707cc2d_Out_0_Float = float(0.2);
            float _Float_4dda8083dd7b4dd0a31d95750ec2456a_Out_0_Float = float(0.5);
            float _Add_2f9b8113be2b49958d22bf18da0f0b83_Out_2_Float;
            Unity_Add_float(_Float_513d9acd4d3943bfa229d715b707cc2d_Out_0_Float, _Float_4dda8083dd7b4dd0a31d95750ec2456a_Out_0_Float, _Add_2f9b8113be2b49958d22bf18da0f0b83_Out_2_Float);
            float2 _Vector2_7d915c4ef0b74d5c859f07ddccda529e_Out_0_Vector2 = float2(float(-1), float(1));
            float _Float_6bba64a861a442b98280dc6b4dd860eb_Out_0_Float = float(1);
            float2 _Multiply_b7cba998566f44a9b2080f7f6e399086_Out_2_Vector2;
            Unity_Multiply_float2_float2(_Vector2_7d915c4ef0b74d5c859f07ddccda529e_Out_0_Vector2, (_Float_6bba64a861a442b98280dc6b4dd860eb_Out_0_Float.xx), _Multiply_b7cba998566f44a9b2080f7f6e399086_Out_2_Vector2);
            float2 _Vector2_aa209240a3724e15bc8c6278c206d8b0_Out_0_Vector2 = float2(_ScreenParams.x, _ScreenParams.y);
            float2 _Divide_00b23a1e3a7e4f29ac8238e2a0b5a6b5_Out_2_Vector2;
            Unity_Divide_float2(_Multiply_b7cba998566f44a9b2080f7f6e399086_Out_2_Vector2, _Vector2_aa209240a3724e15bc8c6278c206d8b0_Out_0_Vector2, _Divide_00b23a1e3a7e4f29ac8238e2a0b5a6b5_Out_2_Vector2);
            float4 _ScreenPosition_7c21c5c3f39f4e549505ff367402679b_Out_0_Vector4 = float4(IN.NDCPosition.xy, 0, 0);
            float2 _Add_88d0c71986a04617a4e9655f90c51868_Out_2_Vector2;
            Unity_Add_float2(_Divide_00b23a1e3a7e4f29ac8238e2a0b5a6b5_Out_2_Vector2, (_ScreenPosition_7c21c5c3f39f4e549505ff367402679b_Out_0_Vector4.xy), _Add_88d0c71986a04617a4e9655f90c51868_Out_2_Vector2);
            float3 _URPSampleBuffer_8105220b7bde4ff9a5686493e5919eff_Output_2_Vector3 = Unity_Universal_SampleBuffer_NormalWorldSpace_float((float4(_Add_88d0c71986a04617a4e9655f90c51868_Out_2_Vector2, 0.0, 1.0)).xy);
            float3 _Multiply_8af9b23d5d9148c4940ebd84b8668915_Out_2_Vector3;
            Unity_Multiply_float3_float3(_URPSampleBuffer_8105220b7bde4ff9a5686493e5919eff_Output_2_Vector3, float3(0.5, 0.5, 0.5), _Multiply_8af9b23d5d9148c4940ebd84b8668915_Out_2_Vector3);
            float3 _Add_eaf59e23dc3f4ba895d11501e35dc248_Out_2_Vector3;
            Unity_Add_float3(_Multiply_8af9b23d5d9148c4940ebd84b8668915_Out_2_Vector3, float3(0.5, 0.5, 0.5), _Add_eaf59e23dc3f4ba895d11501e35dc248_Out_2_Vector3);
            float2 _Vector2_030e6f8a65c94413af6443c3f204c8be_Out_0_Vector2 = float2(float(1), float(-1));
            float2 _Multiply_ed10c2dd88e64c0b8925760988e4e4c8_Out_2_Vector2;
            Unity_Multiply_float2_float2(_Vector2_030e6f8a65c94413af6443c3f204c8be_Out_0_Vector2, (_Float_6bba64a861a442b98280dc6b4dd860eb_Out_0_Float.xx), _Multiply_ed10c2dd88e64c0b8925760988e4e4c8_Out_2_Vector2);
            float2 _Divide_211c76f69178482298ec44ba49888895_Out_2_Vector2;
            Unity_Divide_float2(_Multiply_ed10c2dd88e64c0b8925760988e4e4c8_Out_2_Vector2, _Vector2_aa209240a3724e15bc8c6278c206d8b0_Out_0_Vector2, _Divide_211c76f69178482298ec44ba49888895_Out_2_Vector2);
            float2 _Add_4a442d7e0929482eaa3b956648350aad_Out_2_Vector2;
            Unity_Add_float2(_Divide_211c76f69178482298ec44ba49888895_Out_2_Vector2, (_ScreenPosition_7c21c5c3f39f4e549505ff367402679b_Out_0_Vector4.xy), _Add_4a442d7e0929482eaa3b956648350aad_Out_2_Vector2);
            float3 _URPSampleBuffer_0afa8cf6764644868442439d07be5cdf_Output_2_Vector3 = Unity_Universal_SampleBuffer_NormalWorldSpace_float((float4(_Add_4a442d7e0929482eaa3b956648350aad_Out_2_Vector2, 0.0, 1.0)).xy);
            float3 _Multiply_795e0dd0b8404dc295294574cf9119f5_Out_2_Vector3;
            Unity_Multiply_float3_float3(_URPSampleBuffer_0afa8cf6764644868442439d07be5cdf_Output_2_Vector3, float3(0.5, 0.5, 0.5), _Multiply_795e0dd0b8404dc295294574cf9119f5_Out_2_Vector3);
            float3 _Add_a9d1f57d111d429194c10cfd520726d6_Out_2_Vector3;
            Unity_Add_float3(_Multiply_795e0dd0b8404dc295294574cf9119f5_Out_2_Vector3, float3(0.5, 0.5, 0.5), _Add_a9d1f57d111d429194c10cfd520726d6_Out_2_Vector3);
            float3 _Subtract_ccb18e241774456abd5c757d1d516bf1_Out_2_Vector3;
            Unity_Subtract_float3(_Add_eaf59e23dc3f4ba895d11501e35dc248_Out_2_Vector3, _Add_a9d1f57d111d429194c10cfd520726d6_Out_2_Vector3, _Subtract_ccb18e241774456abd5c757d1d516bf1_Out_2_Vector3);
            float3 _Absolute_c57e68ae36b440b1b554e4ba7314f0bf_Out_1_Vector3;
            Unity_Absolute_float3(_Subtract_ccb18e241774456abd5c757d1d516bf1_Out_2_Vector3, _Absolute_c57e68ae36b440b1b554e4ba7314f0bf_Out_1_Vector3);
            float2 _Vector2_695180491a2e48ccb8f4143bfac4da0d_Out_0_Vector2 = float2(float(1), float(1));
            float2 _Multiply_b359b21071ae427398ac04871e09cc8a_Out_2_Vector2;
            Unity_Multiply_float2_float2(_Vector2_695180491a2e48ccb8f4143bfac4da0d_Out_0_Vector2, (_Float_6bba64a861a442b98280dc6b4dd860eb_Out_0_Float.xx), _Multiply_b359b21071ae427398ac04871e09cc8a_Out_2_Vector2);
            float2 _Divide_34c50f2c4351427783401e2e8b7091ba_Out_2_Vector2;
            Unity_Divide_float2(_Multiply_b359b21071ae427398ac04871e09cc8a_Out_2_Vector2, _Vector2_aa209240a3724e15bc8c6278c206d8b0_Out_0_Vector2, _Divide_34c50f2c4351427783401e2e8b7091ba_Out_2_Vector2);
            float2 _Add_c9a9cc5f3b5f4bd8b7bab8ca2bcffac5_Out_2_Vector2;
            Unity_Add_float2(_Divide_34c50f2c4351427783401e2e8b7091ba_Out_2_Vector2, (_ScreenPosition_7c21c5c3f39f4e549505ff367402679b_Out_0_Vector4.xy), _Add_c9a9cc5f3b5f4bd8b7bab8ca2bcffac5_Out_2_Vector2);
            float3 _URPSampleBuffer_d0b7b65d029642ef812c79ce3c42503c_Output_2_Vector3 = Unity_Universal_SampleBuffer_NormalWorldSpace_float((float4(_Add_c9a9cc5f3b5f4bd8b7bab8ca2bcffac5_Out_2_Vector2, 0.0, 1.0)).xy);
            float3 _Multiply_c4debf4377ed440b889cd25c121b68e7_Out_2_Vector3;
            Unity_Multiply_float3_float3(_URPSampleBuffer_d0b7b65d029642ef812c79ce3c42503c_Output_2_Vector3, float3(0.5, 0.5, 0.5), _Multiply_c4debf4377ed440b889cd25c121b68e7_Out_2_Vector3);
            float3 _Add_920c7577271340f49431941d67136f56_Out_2_Vector3;
            Unity_Add_float3(_Multiply_c4debf4377ed440b889cd25c121b68e7_Out_2_Vector3, float3(0.5, 0.5, 0.5), _Add_920c7577271340f49431941d67136f56_Out_2_Vector3);
            float2 _Vector2_6cfdcd8e4e6844f58f0e8be145f7eb7a_Out_0_Vector2 = float2(float(-1), float(-1));
            float2 _Multiply_e199b37899734f9d94394b118a251833_Out_2_Vector2;
            Unity_Multiply_float2_float2(_Vector2_6cfdcd8e4e6844f58f0e8be145f7eb7a_Out_0_Vector2, (_Float_6bba64a861a442b98280dc6b4dd860eb_Out_0_Float.xx), _Multiply_e199b37899734f9d94394b118a251833_Out_2_Vector2);
            float2 _Divide_431b4ce228184f79b58c640dc267ee99_Out_2_Vector2;
            Unity_Divide_float2(_Multiply_e199b37899734f9d94394b118a251833_Out_2_Vector2, _Vector2_aa209240a3724e15bc8c6278c206d8b0_Out_0_Vector2, _Divide_431b4ce228184f79b58c640dc267ee99_Out_2_Vector2);
            float2 _Add_c5349c473b314c0085b203a1f526be81_Out_2_Vector2;
            Unity_Add_float2(_Divide_431b4ce228184f79b58c640dc267ee99_Out_2_Vector2, (_ScreenPosition_7c21c5c3f39f4e549505ff367402679b_Out_0_Vector4.xy), _Add_c5349c473b314c0085b203a1f526be81_Out_2_Vector2);
            float3 _URPSampleBuffer_09967472878746aebfd18d66ca5fff1d_Output_2_Vector3 = Unity_Universal_SampleBuffer_NormalWorldSpace_float((float4(_Add_c5349c473b314c0085b203a1f526be81_Out_2_Vector2, 0.0, 1.0)).xy);
            float3 _Multiply_080a3a0bdf2a4841bc887bb84b967f02_Out_2_Vector3;
            Unity_Multiply_float3_float3(_URPSampleBuffer_09967472878746aebfd18d66ca5fff1d_Output_2_Vector3, float3(0.5, 0.5, 0.5), _Multiply_080a3a0bdf2a4841bc887bb84b967f02_Out_2_Vector3);
            float3 _Add_b2c09bb07755420eacc67e96f5803a24_Out_2_Vector3;
            Unity_Add_float3(_Multiply_080a3a0bdf2a4841bc887bb84b967f02_Out_2_Vector3, float3(0.5, 0.5, 0.5), _Add_b2c09bb07755420eacc67e96f5803a24_Out_2_Vector3);
            float3 _Subtract_4593b26e81cc4f899ea85f0b896c2055_Out_2_Vector3;
            Unity_Subtract_float3(_Add_920c7577271340f49431941d67136f56_Out_2_Vector3, _Add_b2c09bb07755420eacc67e96f5803a24_Out_2_Vector3, _Subtract_4593b26e81cc4f899ea85f0b896c2055_Out_2_Vector3);
            float3 _Absolute_33e5e2ff998f4d698dae849337fd893e_Out_1_Vector3;
            Unity_Absolute_float3(_Subtract_4593b26e81cc4f899ea85f0b896c2055_Out_2_Vector3, _Absolute_33e5e2ff998f4d698dae849337fd893e_Out_1_Vector3);
            float3 _Add_873ec0b3e4cc49d086fef7985784846c_Out_2_Vector3;
            Unity_Add_float3(_Absolute_c57e68ae36b440b1b554e4ba7314f0bf_Out_1_Vector3, _Absolute_33e5e2ff998f4d698dae849337fd893e_Out_1_Vector3, _Add_873ec0b3e4cc49d086fef7985784846c_Out_2_Vector3);
            float _Split_4d27e0d9ec5843b7916bb0797a35e490_R_1_Float = _Add_873ec0b3e4cc49d086fef7985784846c_Out_2_Vector3[0];
            float _Split_4d27e0d9ec5843b7916bb0797a35e490_G_2_Float = _Add_873ec0b3e4cc49d086fef7985784846c_Out_2_Vector3[1];
            float _Split_4d27e0d9ec5843b7916bb0797a35e490_B_3_Float = _Add_873ec0b3e4cc49d086fef7985784846c_Out_2_Vector3[2];
            float _Split_4d27e0d9ec5843b7916bb0797a35e490_A_4_Float = 0;
            float _Maximum_00594f09ac234ed6a7b3e6c2035c85a6_Out_2_Float;
            Unity_Maximum_float(_Split_4d27e0d9ec5843b7916bb0797a35e490_R_1_Float, _Split_4d27e0d9ec5843b7916bb0797a35e490_G_2_Float, _Maximum_00594f09ac234ed6a7b3e6c2035c85a6_Out_2_Float);
            float _Maximum_141fbbf7913d492bb0fcb16064ea1f56_Out_2_Float;
            Unity_Maximum_float(_Maximum_00594f09ac234ed6a7b3e6c2035c85a6_Out_2_Float, _Split_4d27e0d9ec5843b7916bb0797a35e490_B_3_Float, _Maximum_141fbbf7913d492bb0fcb16064ea1f56_Out_2_Float);
            float _Smoothstep_0f1ecd64af784be08b343a17695d120f_Out_3_Float;
            Unity_Smoothstep_float(_Float_513d9acd4d3943bfa229d715b707cc2d_Out_0_Float, _Add_2f9b8113be2b49958d22bf18da0f0b83_Out_2_Float, _Maximum_141fbbf7913d492bb0fcb16064ea1f56_Out_2_Float, _Smoothstep_0f1ecd64af784be08b343a17695d120f_Out_3_Float);
            float _Float_4fdcdbf063064861b9c21ed399ab98e8_Out_0_Float = float(0.1);
            float _Float_158e2451fa2941e69a9b695cc5da68b9_Out_0_Float = float(0.1);
            float _Add_5bca4ba17f86438f90bc1b1717ea5d8c_Out_2_Float;
            Unity_Add_float(_Float_4fdcdbf063064861b9c21ed399ab98e8_Out_0_Float, _Float_158e2451fa2941e69a9b695cc5da68b9_Out_0_Float, _Add_5bca4ba17f86438f90bc1b1717ea5d8c_Out_2_Float);
            float _SceneDepth_8a63456898c6499bb47b26ec0c6bf7aa_Out_1_Float;
            Unity_SceneDepth_Linear01_float((float4(_Add_88d0c71986a04617a4e9655f90c51868_Out_2_Vector2, 0.0, 1.0)), _SceneDepth_8a63456898c6499bb47b26ec0c6bf7aa_Out_1_Float);
            float _SceneDepth_c4f84c27fc7b46978919fc57f007c7c5_Out_1_Float;
            Unity_SceneDepth_Linear01_float((float4(_Add_4a442d7e0929482eaa3b956648350aad_Out_2_Vector2, 0.0, 1.0)), _SceneDepth_c4f84c27fc7b46978919fc57f007c7c5_Out_1_Float);
            float _Subtract_f4a3da17f5604bd5902ed285ecd6ab6c_Out_2_Float;
            Unity_Subtract_float(_SceneDepth_8a63456898c6499bb47b26ec0c6bf7aa_Out_1_Float, _SceneDepth_c4f84c27fc7b46978919fc57f007c7c5_Out_1_Float, _Subtract_f4a3da17f5604bd5902ed285ecd6ab6c_Out_2_Float);
            float _Absolute_c40499d03f454cd29b544f8c1625c5b6_Out_1_Float;
            Unity_Absolute_float(_Subtract_f4a3da17f5604bd5902ed285ecd6ab6c_Out_2_Float, _Absolute_c40499d03f454cd29b544f8c1625c5b6_Out_1_Float);
            float _SceneDepth_2d79f47875224f189299ff65666d202e_Out_1_Float;
            Unity_SceneDepth_Linear01_float((float4(_Add_c9a9cc5f3b5f4bd8b7bab8ca2bcffac5_Out_2_Vector2, 0.0, 1.0)), _SceneDepth_2d79f47875224f189299ff65666d202e_Out_1_Float);
            float _SceneDepth_54bea42965b746a79723f61e4ef8ccfe_Out_1_Float;
            Unity_SceneDepth_Linear01_float((float4(_Add_c5349c473b314c0085b203a1f526be81_Out_2_Vector2, 0.0, 1.0)), _SceneDepth_54bea42965b746a79723f61e4ef8ccfe_Out_1_Float);
            float _Subtract_10b8a4e8abc14ece872ac6c21003da4b_Out_2_Float;
            Unity_Subtract_float(_SceneDepth_2d79f47875224f189299ff65666d202e_Out_1_Float, _SceneDepth_54bea42965b746a79723f61e4ef8ccfe_Out_1_Float, _Subtract_10b8a4e8abc14ece872ac6c21003da4b_Out_2_Float);
            float _Absolute_fc95386b1ee04897a2447d340fac1593_Out_1_Float;
            Unity_Absolute_float(_Subtract_10b8a4e8abc14ece872ac6c21003da4b_Out_2_Float, _Absolute_fc95386b1ee04897a2447d340fac1593_Out_1_Float);
            float _Add_b5de6643ed6143eea65eedc82905563d_Out_2_Float;
            Unity_Add_float(_Absolute_c40499d03f454cd29b544f8c1625c5b6_Out_1_Float, _Absolute_fc95386b1ee04897a2447d340fac1593_Out_1_Float, _Add_b5de6643ed6143eea65eedc82905563d_Out_2_Float);
            float _Maximum_6fdd774bf0c041b585d25a7904641534_Out_2_Float;
            Unity_Maximum_float(_SceneDepth_8a63456898c6499bb47b26ec0c6bf7aa_Out_1_Float, _SceneDepth_c4f84c27fc7b46978919fc57f007c7c5_Out_1_Float, _Maximum_6fdd774bf0c041b585d25a7904641534_Out_2_Float);
            float _Maximum_66432db0b4254cd5bf6d3fbdccfbfc77_Out_2_Float;
            Unity_Maximum_float(_Maximum_6fdd774bf0c041b585d25a7904641534_Out_2_Float, _SceneDepth_2d79f47875224f189299ff65666d202e_Out_1_Float, _Maximum_66432db0b4254cd5bf6d3fbdccfbfc77_Out_2_Float);
            float _Maximum_019190efe9cb4afa93c955b3999c6041_Out_2_Float;
            Unity_Maximum_float(_Maximum_66432db0b4254cd5bf6d3fbdccfbfc77_Out_2_Float, _SceneDepth_54bea42965b746a79723f61e4ef8ccfe_Out_1_Float, _Maximum_019190efe9cb4afa93c955b3999c6041_Out_2_Float);
            float _Divide_e5779cb5ea4b40dba83543f1570d923e_Out_2_Float;
            Unity_Divide_float(_Add_b5de6643ed6143eea65eedc82905563d_Out_2_Float, _Maximum_019190efe9cb4afa93c955b3999c6041_Out_2_Float, _Divide_e5779cb5ea4b40dba83543f1570d923e_Out_2_Float);
            float _Saturate_afffac401c0b4f5da76b7a21e603cbca_Out_1_Float;
            Unity_Saturate_float(_Divide_e5779cb5ea4b40dba83543f1570d923e_Out_2_Float, _Saturate_afffac401c0b4f5da76b7a21e603cbca_Out_1_Float);
            float _Smoothstep_dff86a1ba4d248eda435bf978f36d3a7_Out_3_Float;
            Unity_Smoothstep_float(_Float_4fdcdbf063064861b9c21ed399ab98e8_Out_0_Float, _Add_5bca4ba17f86438f90bc1b1717ea5d8c_Out_2_Float, _Saturate_afffac401c0b4f5da76b7a21e603cbca_Out_1_Float, _Smoothstep_dff86a1ba4d248eda435bf978f36d3a7_Out_3_Float);
            float _Maximum_21676ba74b3440b596f29284645e9193_Out_2_Float;
            Unity_Maximum_float(_Smoothstep_0f1ecd64af784be08b343a17695d120f_Out_3_Float, _Smoothstep_dff86a1ba4d248eda435bf978f36d3a7_Out_3_Float, _Maximum_21676ba74b3440b596f29284645e9193_Out_2_Float);
            surface.BaseColor = (_Maximum_21676ba74b3440b596f29284645e9193_Out_2_Float.xxx);
            surface.Alpha = float(1);
            return surface;
        }
        
        // --------------------------------------------------
        // Build Graph Inputs
        
        SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
        {
            SurfaceDescriptionInputs output;
            ZERO_INITIALIZE(SurfaceDescriptionInputs, output);
        
            float3 normalWS = SHADERGRAPH_SAMPLE_SCENE_NORMAL(input.texCoord0.xy);
            float4 tangentWS = float4(0, 1, 0, 0); // We can't access the tangent in screen space
        
        
        
        
            float3 viewDirWS = normalize(input.texCoord1.xyz);
            float linearDepth = LinearEyeDepth(SHADERGRAPH_SAMPLE_SCENE_DEPTH(input.texCoord0.xy), _ZBufferParams);
            float3 cameraForward = -UNITY_MATRIX_V[2].xyz;
            float camearDistance = linearDepth / dot(viewDirWS, cameraForward);
            float3 positionWS = viewDirWS * camearDistance + GetCameraPositionWS();
        
        
            output.WorldSpacePosition = positionWS;
            output.ScreenPosition = float4(input.texCoord0.xy, 0, 1);
            output.NDCPosition = input.texCoord0.xy;
        
        #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN output.FaceSign =                    IS_FRONT_VFACE(input.cullFace, true, false);
        #else
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
        #endif
        #undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
        
                return output;
        }
        
        // --------------------------------------------------
        // Main
        
        #include "Packages/com.unity.shadergraph/Editor/Generation/Targets/Fullscreen/Includes/FullscreenCommon.hlsl"
        #include "Packages/com.unity.shadergraph/Editor/Generation/Targets/Fullscreen/Includes/FullscreenDrawProcedural.hlsl"
        
        ENDHLSL
        }
        Pass
        {
            Name "Blit"
        
        // Render State
        Cull Off
        Blend Off
        ZTest Off
        ZWrite Off
        
        // Debug
        // <None>
        
        // --------------------------------------------------
        // Pass
        
        HLSLPROGRAM
        
        // Pragmas
        #pragma target 3.0
        #pragma vertex vert
        #pragma fragment frag
        // #pragma enable_d3d11_debug_symbols
        
        /* WARNING: $splice Could not find named fragment 'DotsInstancingOptions' */
        /* WARNING: $splice Could not find named fragment 'HybridV1InjectedBuiltinProperties' */
        
        // Keywords
        // PassKeywords: <None>
        // GraphKeywords: <None>
        
        #define FULLSCREEN_SHADERGRAPH
        
        // Defines
        #define ATTRIBUTES_NEED_TEXCOORD0
        #define ATTRIBUTES_NEED_TEXCOORD1
        #define ATTRIBUTES_NEED_VERTEXID
        #define VARYINGS_NEED_POSITION_WS
        #define VARYINGS_NEED_TEXCOORD0
        #define VARYINGS_NEED_TEXCOORD1
        
        // Force depth texture because we need it for almost every nodes
        // TODO: dependency system that triggers this define from position or view direction usage
        #define REQUIRE_DEPTH_TEXTURE
        #define REQUIRE_NORMAL_TEXTURE
        
        /* WARNING: $splice Could not find named fragment 'PassInstancing' */
        #define SHADERPASS SHADERPASS_BLIT
        #define REQUIRE_DEPTH_TEXTURE
        
        // custom interpolator pre-include
        /* WARNING: $splice Could not find named fragment 'sgci_CustomInterpolatorPreInclude' */
        
        // Includes
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
        #include "Packages/com.unity.shadergraph/Editor/Generation/Targets/Fullscreen/Includes/FullscreenShaderPass.cs.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        #include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/SpaceTransforms.hlsl"
        #include "Packages/com.unity.shadergraph/ShaderGraphLibrary/Functions.hlsl"
        
        // --------------------------------------------------
        // Structs and Packing
        
        // custom interpolators pre packing
        /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPrePacking' */
        
        struct Attributes
        {
            #if UNITY_ANY_INSTANCING_ENABLED
             uint instanceID : INSTANCEID_SEMANTIC;
            #endif
             uint vertexID : VERTEXID_SEMANTIC;
             float3 positionOS : POSITION;
        };
        struct SurfaceDescriptionInputs
        {
             float3 WorldSpacePosition;
             float4 ScreenPosition;
             float2 NDCPosition;
             float2 PixelPosition;
        };
        struct Varyings
        {
             float4 positionCS : SV_POSITION;
             float4 texCoord0;
             float4 texCoord1;
            #if UNITY_ANY_INSTANCING_ENABLED
             uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
             uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
             uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
            #endif
        };
        struct VertexDescriptionInputs
        {
        };
        struct PackedVaryings
        {
             float4 positionCS : SV_POSITION;
             float4 texCoord0 : INTERP0;
             float4 texCoord1 : INTERP1;
            #if UNITY_ANY_INSTANCING_ENABLED
             uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
             uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
             uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
            #endif
        };
        
        PackedVaryings PackVaryings (Varyings input)
        {
            PackedVaryings output;
            ZERO_INITIALIZE(PackedVaryings, output);
            output.positionCS = input.positionCS;
            output.texCoord0.xyzw = input.texCoord0;
            output.texCoord1.xyzw = input.texCoord1;
            #if UNITY_ANY_INSTANCING_ENABLED
            output.instanceID = input.instanceID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
            #endif
            return output;
        }
        
        Varyings UnpackVaryings (PackedVaryings input)
        {
            Varyings output;
            output.positionCS = input.positionCS;
            output.texCoord0 = input.texCoord0.xyzw;
            output.texCoord1 = input.texCoord1.xyzw;
            #if UNITY_ANY_INSTANCING_ENABLED
            output.instanceID = input.instanceID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
            #endif
            return output;
        }
        
        
        // --------------------------------------------------
        // Graph
        
        // Graph Properties
        CBUFFER_START(UnityPerMaterial)
        CBUFFER_END
        
        
        // Object and Global properties
        float _FlipY;
        
        // Graph Includes
        // GraphIncludes: <None>
        
        // Graph Functions
        
        void Unity_Add_float(float A, float B, out float Out)
        {
            Out = A + B;
        }
        
        void Unity_Multiply_float2_float2(float2 A, float2 B, out float2 Out)
        {
            Out = A * B;
        }
        
        void Unity_Divide_float2(float2 A, float2 B, out float2 Out)
        {
            Out = A / B;
        }
        
        void Unity_Add_float2(float2 A, float2 B, out float2 Out)
        {
            Out = A + B;
        }
        
        float3 Unity_Universal_SampleBuffer_NormalWorldSpace_float(float2 uv)
        {
            return SHADERGRAPH_SAMPLE_SCENE_NORMAL(uv);
        }
        
        void Unity_Multiply_float3_float3(float3 A, float3 B, out float3 Out)
        {
            Out = A * B;
        }
        
        void Unity_Add_float3(float3 A, float3 B, out float3 Out)
        {
            Out = A + B;
        }
        
        void Unity_Subtract_float3(float3 A, float3 B, out float3 Out)
        {
            Out = A - B;
        }
        
        void Unity_Absolute_float3(float3 In, out float3 Out)
        {
            Out = abs(In);
        }
        
        void Unity_Maximum_float(float A, float B, out float Out)
        {
            Out = max(A, B);
        }
        
        void Unity_Smoothstep_float(float Edge1, float Edge2, float In, out float Out)
        {
            Out = smoothstep(Edge1, Edge2, In);
        }
        
        void Unity_SceneDepth_Linear01_float(float4 UV, out float Out)
        {
            Out = Linear01Depth(SHADERGRAPH_SAMPLE_SCENE_DEPTH(UV.xy), _ZBufferParams);
        }
        
        void Unity_Subtract_float(float A, float B, out float Out)
        {
            Out = A - B;
        }
        
        void Unity_Absolute_float(float In, out float Out)
        {
            Out = abs(In);
        }
        
        void Unity_Divide_float(float A, float B, out float Out)
        {
            Out = A / B;
        }
        
        void Unity_Saturate_float(float In, out float Out)
        {
            Out = saturate(In);
        }
        
        // Custom interpolators pre vertex
        /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPreVertex' */
        
        // Graph Vertex
        // GraphVertex: <None>
        
        // Custom interpolators, pre surface
        /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPreSurface' */
        
        // Graph Pixel
        struct SurfaceDescription
        {
            float3 BaseColor;
            float Alpha;
        };
        
        SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
        {
            SurfaceDescription surface = (SurfaceDescription)0;
            float _Float_513d9acd4d3943bfa229d715b707cc2d_Out_0_Float = float(0.2);
            float _Float_4dda8083dd7b4dd0a31d95750ec2456a_Out_0_Float = float(0.5);
            float _Add_2f9b8113be2b49958d22bf18da0f0b83_Out_2_Float;
            Unity_Add_float(_Float_513d9acd4d3943bfa229d715b707cc2d_Out_0_Float, _Float_4dda8083dd7b4dd0a31d95750ec2456a_Out_0_Float, _Add_2f9b8113be2b49958d22bf18da0f0b83_Out_2_Float);
            float2 _Vector2_7d915c4ef0b74d5c859f07ddccda529e_Out_0_Vector2 = float2(float(-1), float(1));
            float _Float_6bba64a861a442b98280dc6b4dd860eb_Out_0_Float = float(1);
            float2 _Multiply_b7cba998566f44a9b2080f7f6e399086_Out_2_Vector2;
            Unity_Multiply_float2_float2(_Vector2_7d915c4ef0b74d5c859f07ddccda529e_Out_0_Vector2, (_Float_6bba64a861a442b98280dc6b4dd860eb_Out_0_Float.xx), _Multiply_b7cba998566f44a9b2080f7f6e399086_Out_2_Vector2);
            float2 _Vector2_aa209240a3724e15bc8c6278c206d8b0_Out_0_Vector2 = float2(_ScreenParams.x, _ScreenParams.y);
            float2 _Divide_00b23a1e3a7e4f29ac8238e2a0b5a6b5_Out_2_Vector2;
            Unity_Divide_float2(_Multiply_b7cba998566f44a9b2080f7f6e399086_Out_2_Vector2, _Vector2_aa209240a3724e15bc8c6278c206d8b0_Out_0_Vector2, _Divide_00b23a1e3a7e4f29ac8238e2a0b5a6b5_Out_2_Vector2);
            float4 _ScreenPosition_7c21c5c3f39f4e549505ff367402679b_Out_0_Vector4 = float4(IN.NDCPosition.xy, 0, 0);
            float2 _Add_88d0c71986a04617a4e9655f90c51868_Out_2_Vector2;
            Unity_Add_float2(_Divide_00b23a1e3a7e4f29ac8238e2a0b5a6b5_Out_2_Vector2, (_ScreenPosition_7c21c5c3f39f4e549505ff367402679b_Out_0_Vector4.xy), _Add_88d0c71986a04617a4e9655f90c51868_Out_2_Vector2);
            float3 _URPSampleBuffer_8105220b7bde4ff9a5686493e5919eff_Output_2_Vector3 = Unity_Universal_SampleBuffer_NormalWorldSpace_float((float4(_Add_88d0c71986a04617a4e9655f90c51868_Out_2_Vector2, 0.0, 1.0)).xy);
            float3 _Multiply_8af9b23d5d9148c4940ebd84b8668915_Out_2_Vector3;
            Unity_Multiply_float3_float3(_URPSampleBuffer_8105220b7bde4ff9a5686493e5919eff_Output_2_Vector3, float3(0.5, 0.5, 0.5), _Multiply_8af9b23d5d9148c4940ebd84b8668915_Out_2_Vector3);
            float3 _Add_eaf59e23dc3f4ba895d11501e35dc248_Out_2_Vector3;
            Unity_Add_float3(_Multiply_8af9b23d5d9148c4940ebd84b8668915_Out_2_Vector3, float3(0.5, 0.5, 0.5), _Add_eaf59e23dc3f4ba895d11501e35dc248_Out_2_Vector3);
            float2 _Vector2_030e6f8a65c94413af6443c3f204c8be_Out_0_Vector2 = float2(float(1), float(-1));
            float2 _Multiply_ed10c2dd88e64c0b8925760988e4e4c8_Out_2_Vector2;
            Unity_Multiply_float2_float2(_Vector2_030e6f8a65c94413af6443c3f204c8be_Out_0_Vector2, (_Float_6bba64a861a442b98280dc6b4dd860eb_Out_0_Float.xx), _Multiply_ed10c2dd88e64c0b8925760988e4e4c8_Out_2_Vector2);
            float2 _Divide_211c76f69178482298ec44ba49888895_Out_2_Vector2;
            Unity_Divide_float2(_Multiply_ed10c2dd88e64c0b8925760988e4e4c8_Out_2_Vector2, _Vector2_aa209240a3724e15bc8c6278c206d8b0_Out_0_Vector2, _Divide_211c76f69178482298ec44ba49888895_Out_2_Vector2);
            float2 _Add_4a442d7e0929482eaa3b956648350aad_Out_2_Vector2;
            Unity_Add_float2(_Divide_211c76f69178482298ec44ba49888895_Out_2_Vector2, (_ScreenPosition_7c21c5c3f39f4e549505ff367402679b_Out_0_Vector4.xy), _Add_4a442d7e0929482eaa3b956648350aad_Out_2_Vector2);
            float3 _URPSampleBuffer_0afa8cf6764644868442439d07be5cdf_Output_2_Vector3 = Unity_Universal_SampleBuffer_NormalWorldSpace_float((float4(_Add_4a442d7e0929482eaa3b956648350aad_Out_2_Vector2, 0.0, 1.0)).xy);
            float3 _Multiply_795e0dd0b8404dc295294574cf9119f5_Out_2_Vector3;
            Unity_Multiply_float3_float3(_URPSampleBuffer_0afa8cf6764644868442439d07be5cdf_Output_2_Vector3, float3(0.5, 0.5, 0.5), _Multiply_795e0dd0b8404dc295294574cf9119f5_Out_2_Vector3);
            float3 _Add_a9d1f57d111d429194c10cfd520726d6_Out_2_Vector3;
            Unity_Add_float3(_Multiply_795e0dd0b8404dc295294574cf9119f5_Out_2_Vector3, float3(0.5, 0.5, 0.5), _Add_a9d1f57d111d429194c10cfd520726d6_Out_2_Vector3);
            float3 _Subtract_ccb18e241774456abd5c757d1d516bf1_Out_2_Vector3;
            Unity_Subtract_float3(_Add_eaf59e23dc3f4ba895d11501e35dc248_Out_2_Vector3, _Add_a9d1f57d111d429194c10cfd520726d6_Out_2_Vector3, _Subtract_ccb18e241774456abd5c757d1d516bf1_Out_2_Vector3);
            float3 _Absolute_c57e68ae36b440b1b554e4ba7314f0bf_Out_1_Vector3;
            Unity_Absolute_float3(_Subtract_ccb18e241774456abd5c757d1d516bf1_Out_2_Vector3, _Absolute_c57e68ae36b440b1b554e4ba7314f0bf_Out_1_Vector3);
            float2 _Vector2_695180491a2e48ccb8f4143bfac4da0d_Out_0_Vector2 = float2(float(1), float(1));
            float2 _Multiply_b359b21071ae427398ac04871e09cc8a_Out_2_Vector2;
            Unity_Multiply_float2_float2(_Vector2_695180491a2e48ccb8f4143bfac4da0d_Out_0_Vector2, (_Float_6bba64a861a442b98280dc6b4dd860eb_Out_0_Float.xx), _Multiply_b359b21071ae427398ac04871e09cc8a_Out_2_Vector2);
            float2 _Divide_34c50f2c4351427783401e2e8b7091ba_Out_2_Vector2;
            Unity_Divide_float2(_Multiply_b359b21071ae427398ac04871e09cc8a_Out_2_Vector2, _Vector2_aa209240a3724e15bc8c6278c206d8b0_Out_0_Vector2, _Divide_34c50f2c4351427783401e2e8b7091ba_Out_2_Vector2);
            float2 _Add_c9a9cc5f3b5f4bd8b7bab8ca2bcffac5_Out_2_Vector2;
            Unity_Add_float2(_Divide_34c50f2c4351427783401e2e8b7091ba_Out_2_Vector2, (_ScreenPosition_7c21c5c3f39f4e549505ff367402679b_Out_0_Vector4.xy), _Add_c9a9cc5f3b5f4bd8b7bab8ca2bcffac5_Out_2_Vector2);
            float3 _URPSampleBuffer_d0b7b65d029642ef812c79ce3c42503c_Output_2_Vector3 = Unity_Universal_SampleBuffer_NormalWorldSpace_float((float4(_Add_c9a9cc5f3b5f4bd8b7bab8ca2bcffac5_Out_2_Vector2, 0.0, 1.0)).xy);
            float3 _Multiply_c4debf4377ed440b889cd25c121b68e7_Out_2_Vector3;
            Unity_Multiply_float3_float3(_URPSampleBuffer_d0b7b65d029642ef812c79ce3c42503c_Output_2_Vector3, float3(0.5, 0.5, 0.5), _Multiply_c4debf4377ed440b889cd25c121b68e7_Out_2_Vector3);
            float3 _Add_920c7577271340f49431941d67136f56_Out_2_Vector3;
            Unity_Add_float3(_Multiply_c4debf4377ed440b889cd25c121b68e7_Out_2_Vector3, float3(0.5, 0.5, 0.5), _Add_920c7577271340f49431941d67136f56_Out_2_Vector3);
            float2 _Vector2_6cfdcd8e4e6844f58f0e8be145f7eb7a_Out_0_Vector2 = float2(float(-1), float(-1));
            float2 _Multiply_e199b37899734f9d94394b118a251833_Out_2_Vector2;
            Unity_Multiply_float2_float2(_Vector2_6cfdcd8e4e6844f58f0e8be145f7eb7a_Out_0_Vector2, (_Float_6bba64a861a442b98280dc6b4dd860eb_Out_0_Float.xx), _Multiply_e199b37899734f9d94394b118a251833_Out_2_Vector2);
            float2 _Divide_431b4ce228184f79b58c640dc267ee99_Out_2_Vector2;
            Unity_Divide_float2(_Multiply_e199b37899734f9d94394b118a251833_Out_2_Vector2, _Vector2_aa209240a3724e15bc8c6278c206d8b0_Out_0_Vector2, _Divide_431b4ce228184f79b58c640dc267ee99_Out_2_Vector2);
            float2 _Add_c5349c473b314c0085b203a1f526be81_Out_2_Vector2;
            Unity_Add_float2(_Divide_431b4ce228184f79b58c640dc267ee99_Out_2_Vector2, (_ScreenPosition_7c21c5c3f39f4e549505ff367402679b_Out_0_Vector4.xy), _Add_c5349c473b314c0085b203a1f526be81_Out_2_Vector2);
            float3 _URPSampleBuffer_09967472878746aebfd18d66ca5fff1d_Output_2_Vector3 = Unity_Universal_SampleBuffer_NormalWorldSpace_float((float4(_Add_c5349c473b314c0085b203a1f526be81_Out_2_Vector2, 0.0, 1.0)).xy);
            float3 _Multiply_080a3a0bdf2a4841bc887bb84b967f02_Out_2_Vector3;
            Unity_Multiply_float3_float3(_URPSampleBuffer_09967472878746aebfd18d66ca5fff1d_Output_2_Vector3, float3(0.5, 0.5, 0.5), _Multiply_080a3a0bdf2a4841bc887bb84b967f02_Out_2_Vector3);
            float3 _Add_b2c09bb07755420eacc67e96f5803a24_Out_2_Vector3;
            Unity_Add_float3(_Multiply_080a3a0bdf2a4841bc887bb84b967f02_Out_2_Vector3, float3(0.5, 0.5, 0.5), _Add_b2c09bb07755420eacc67e96f5803a24_Out_2_Vector3);
            float3 _Subtract_4593b26e81cc4f899ea85f0b896c2055_Out_2_Vector3;
            Unity_Subtract_float3(_Add_920c7577271340f49431941d67136f56_Out_2_Vector3, _Add_b2c09bb07755420eacc67e96f5803a24_Out_2_Vector3, _Subtract_4593b26e81cc4f899ea85f0b896c2055_Out_2_Vector3);
            float3 _Absolute_33e5e2ff998f4d698dae849337fd893e_Out_1_Vector3;
            Unity_Absolute_float3(_Subtract_4593b26e81cc4f899ea85f0b896c2055_Out_2_Vector3, _Absolute_33e5e2ff998f4d698dae849337fd893e_Out_1_Vector3);
            float3 _Add_873ec0b3e4cc49d086fef7985784846c_Out_2_Vector3;
            Unity_Add_float3(_Absolute_c57e68ae36b440b1b554e4ba7314f0bf_Out_1_Vector3, _Absolute_33e5e2ff998f4d698dae849337fd893e_Out_1_Vector3, _Add_873ec0b3e4cc49d086fef7985784846c_Out_2_Vector3);
            float _Split_4d27e0d9ec5843b7916bb0797a35e490_R_1_Float = _Add_873ec0b3e4cc49d086fef7985784846c_Out_2_Vector3[0];
            float _Split_4d27e0d9ec5843b7916bb0797a35e490_G_2_Float = _Add_873ec0b3e4cc49d086fef7985784846c_Out_2_Vector3[1];
            float _Split_4d27e0d9ec5843b7916bb0797a35e490_B_3_Float = _Add_873ec0b3e4cc49d086fef7985784846c_Out_2_Vector3[2];
            float _Split_4d27e0d9ec5843b7916bb0797a35e490_A_4_Float = 0;
            float _Maximum_00594f09ac234ed6a7b3e6c2035c85a6_Out_2_Float;
            Unity_Maximum_float(_Split_4d27e0d9ec5843b7916bb0797a35e490_R_1_Float, _Split_4d27e0d9ec5843b7916bb0797a35e490_G_2_Float, _Maximum_00594f09ac234ed6a7b3e6c2035c85a6_Out_2_Float);
            float _Maximum_141fbbf7913d492bb0fcb16064ea1f56_Out_2_Float;
            Unity_Maximum_float(_Maximum_00594f09ac234ed6a7b3e6c2035c85a6_Out_2_Float, _Split_4d27e0d9ec5843b7916bb0797a35e490_B_3_Float, _Maximum_141fbbf7913d492bb0fcb16064ea1f56_Out_2_Float);
            float _Smoothstep_0f1ecd64af784be08b343a17695d120f_Out_3_Float;
            Unity_Smoothstep_float(_Float_513d9acd4d3943bfa229d715b707cc2d_Out_0_Float, _Add_2f9b8113be2b49958d22bf18da0f0b83_Out_2_Float, _Maximum_141fbbf7913d492bb0fcb16064ea1f56_Out_2_Float, _Smoothstep_0f1ecd64af784be08b343a17695d120f_Out_3_Float);
            float _Float_4fdcdbf063064861b9c21ed399ab98e8_Out_0_Float = float(0.1);
            float _Float_158e2451fa2941e69a9b695cc5da68b9_Out_0_Float = float(0.1);
            float _Add_5bca4ba17f86438f90bc1b1717ea5d8c_Out_2_Float;
            Unity_Add_float(_Float_4fdcdbf063064861b9c21ed399ab98e8_Out_0_Float, _Float_158e2451fa2941e69a9b695cc5da68b9_Out_0_Float, _Add_5bca4ba17f86438f90bc1b1717ea5d8c_Out_2_Float);
            float _SceneDepth_8a63456898c6499bb47b26ec0c6bf7aa_Out_1_Float;
            Unity_SceneDepth_Linear01_float((float4(_Add_88d0c71986a04617a4e9655f90c51868_Out_2_Vector2, 0.0, 1.0)), _SceneDepth_8a63456898c6499bb47b26ec0c6bf7aa_Out_1_Float);
            float _SceneDepth_c4f84c27fc7b46978919fc57f007c7c5_Out_1_Float;
            Unity_SceneDepth_Linear01_float((float4(_Add_4a442d7e0929482eaa3b956648350aad_Out_2_Vector2, 0.0, 1.0)), _SceneDepth_c4f84c27fc7b46978919fc57f007c7c5_Out_1_Float);
            float _Subtract_f4a3da17f5604bd5902ed285ecd6ab6c_Out_2_Float;
            Unity_Subtract_float(_SceneDepth_8a63456898c6499bb47b26ec0c6bf7aa_Out_1_Float, _SceneDepth_c4f84c27fc7b46978919fc57f007c7c5_Out_1_Float, _Subtract_f4a3da17f5604bd5902ed285ecd6ab6c_Out_2_Float);
            float _Absolute_c40499d03f454cd29b544f8c1625c5b6_Out_1_Float;
            Unity_Absolute_float(_Subtract_f4a3da17f5604bd5902ed285ecd6ab6c_Out_2_Float, _Absolute_c40499d03f454cd29b544f8c1625c5b6_Out_1_Float);
            float _SceneDepth_2d79f47875224f189299ff65666d202e_Out_1_Float;
            Unity_SceneDepth_Linear01_float((float4(_Add_c9a9cc5f3b5f4bd8b7bab8ca2bcffac5_Out_2_Vector2, 0.0, 1.0)), _SceneDepth_2d79f47875224f189299ff65666d202e_Out_1_Float);
            float _SceneDepth_54bea42965b746a79723f61e4ef8ccfe_Out_1_Float;
            Unity_SceneDepth_Linear01_float((float4(_Add_c5349c473b314c0085b203a1f526be81_Out_2_Vector2, 0.0, 1.0)), _SceneDepth_54bea42965b746a79723f61e4ef8ccfe_Out_1_Float);
            float _Subtract_10b8a4e8abc14ece872ac6c21003da4b_Out_2_Float;
            Unity_Subtract_float(_SceneDepth_2d79f47875224f189299ff65666d202e_Out_1_Float, _SceneDepth_54bea42965b746a79723f61e4ef8ccfe_Out_1_Float, _Subtract_10b8a4e8abc14ece872ac6c21003da4b_Out_2_Float);
            float _Absolute_fc95386b1ee04897a2447d340fac1593_Out_1_Float;
            Unity_Absolute_float(_Subtract_10b8a4e8abc14ece872ac6c21003da4b_Out_2_Float, _Absolute_fc95386b1ee04897a2447d340fac1593_Out_1_Float);
            float _Add_b5de6643ed6143eea65eedc82905563d_Out_2_Float;
            Unity_Add_float(_Absolute_c40499d03f454cd29b544f8c1625c5b6_Out_1_Float, _Absolute_fc95386b1ee04897a2447d340fac1593_Out_1_Float, _Add_b5de6643ed6143eea65eedc82905563d_Out_2_Float);
            float _Maximum_6fdd774bf0c041b585d25a7904641534_Out_2_Float;
            Unity_Maximum_float(_SceneDepth_8a63456898c6499bb47b26ec0c6bf7aa_Out_1_Float, _SceneDepth_c4f84c27fc7b46978919fc57f007c7c5_Out_1_Float, _Maximum_6fdd774bf0c041b585d25a7904641534_Out_2_Float);
            float _Maximum_66432db0b4254cd5bf6d3fbdccfbfc77_Out_2_Float;
            Unity_Maximum_float(_Maximum_6fdd774bf0c041b585d25a7904641534_Out_2_Float, _SceneDepth_2d79f47875224f189299ff65666d202e_Out_1_Float, _Maximum_66432db0b4254cd5bf6d3fbdccfbfc77_Out_2_Float);
            float _Maximum_019190efe9cb4afa93c955b3999c6041_Out_2_Float;
            Unity_Maximum_float(_Maximum_66432db0b4254cd5bf6d3fbdccfbfc77_Out_2_Float, _SceneDepth_54bea42965b746a79723f61e4ef8ccfe_Out_1_Float, _Maximum_019190efe9cb4afa93c955b3999c6041_Out_2_Float);
            float _Divide_e5779cb5ea4b40dba83543f1570d923e_Out_2_Float;
            Unity_Divide_float(_Add_b5de6643ed6143eea65eedc82905563d_Out_2_Float, _Maximum_019190efe9cb4afa93c955b3999c6041_Out_2_Float, _Divide_e5779cb5ea4b40dba83543f1570d923e_Out_2_Float);
            float _Saturate_afffac401c0b4f5da76b7a21e603cbca_Out_1_Float;
            Unity_Saturate_float(_Divide_e5779cb5ea4b40dba83543f1570d923e_Out_2_Float, _Saturate_afffac401c0b4f5da76b7a21e603cbca_Out_1_Float);
            float _Smoothstep_dff86a1ba4d248eda435bf978f36d3a7_Out_3_Float;
            Unity_Smoothstep_float(_Float_4fdcdbf063064861b9c21ed399ab98e8_Out_0_Float, _Add_5bca4ba17f86438f90bc1b1717ea5d8c_Out_2_Float, _Saturate_afffac401c0b4f5da76b7a21e603cbca_Out_1_Float, _Smoothstep_dff86a1ba4d248eda435bf978f36d3a7_Out_3_Float);
            float _Maximum_21676ba74b3440b596f29284645e9193_Out_2_Float;
            Unity_Maximum_float(_Smoothstep_0f1ecd64af784be08b343a17695d120f_Out_3_Float, _Smoothstep_dff86a1ba4d248eda435bf978f36d3a7_Out_3_Float, _Maximum_21676ba74b3440b596f29284645e9193_Out_2_Float);
            surface.BaseColor = (_Maximum_21676ba74b3440b596f29284645e9193_Out_2_Float.xxx);
            surface.Alpha = float(1);
            return surface;
        }
        
        // --------------------------------------------------
        // Build Graph Inputs
        
        SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
        {
            SurfaceDescriptionInputs output;
            ZERO_INITIALIZE(SurfaceDescriptionInputs, output);
        
            float3 normalWS = SHADERGRAPH_SAMPLE_SCENE_NORMAL(input.texCoord0.xy);
            float4 tangentWS = float4(0, 1, 0, 0); // We can't access the tangent in screen space
        
        
        
        
            float3 viewDirWS = normalize(input.texCoord1.xyz);
            float linearDepth = LinearEyeDepth(SHADERGRAPH_SAMPLE_SCENE_DEPTH(input.texCoord0.xy), _ZBufferParams);
            float3 cameraForward = -UNITY_MATRIX_V[2].xyz;
            float camearDistance = linearDepth / dot(viewDirWS, cameraForward);
            float3 positionWS = viewDirWS * camearDistance + GetCameraPositionWS();
        
        
            output.WorldSpacePosition = positionWS;
            output.ScreenPosition = float4(input.texCoord0.xy, 0, 1);
            output.NDCPosition = input.texCoord0.xy;
        
        #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN output.FaceSign =                    IS_FRONT_VFACE(input.cullFace, true, false);
        #else
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
        #endif
        #undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
        
                return output;
        }
        
        // --------------------------------------------------
        // Main
        
        #include "Packages/com.unity.shadergraph/Editor/Generation/Targets/Fullscreen/Includes/FullscreenCommon.hlsl"
        #include "Packages/com.unity.shadergraph/Editor/Generation/Targets/Fullscreen/Includes/FullscreenBlit.hlsl"
        
        ENDHLSL
        }
    }
    CustomEditor "UnityEditor.Rendering.Fullscreen.ShaderGraph.FullscreenShaderGUI"
    FallBack "Hidden/Shader Graph/FallbackError"
}