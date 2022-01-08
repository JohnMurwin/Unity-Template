Shader "Impostors/ImpostorsShader"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" { }
    }

    // UniversalPipeline shader must be at the top...
    SubShader
    {
        Name "UniversalPipeline"
        Tags
        {
            "RenderType" = "TransparentCutout"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "ImpostorsUnlitCutout"
            Tags
            {
                "LightMode" = "UniversalForward"
            }

            ZWrite On
            ZTest LEqual
            Cull Back
            AlphaToMask On

            CGPROGRAM
            #include "Impostors.cginc"

            #pragma multi_compile_fog
            #pragma multi_compile _ IMPOSTORS_DEBUG_FADING
            #pragma vertex impostors_vert
            #pragma fragment impostors_frag
            ENDCG

        }

        Pass
        {
            Name "ImpostorsDepthOnly"
            Tags
            {
                "LightMode" = "DepthOnly"
            }

            ZWrite On
            ZTest LEqual
            Cull Back

            CGPROGRAM
            #include "Impostors.cginc"

            #pragma vertex impostors_vert
            #pragma fragment impostors_frag
            ENDCG

        }
    }

    SubShader
    {
        Name "StandardPipeline"
        Tags
        {
            "Queue" = "AlphaTest" // actual queue number sets from ImpostorsChunk
            "IgnoreProjector" = "True"
            "RenderType" = "TransparentCutout"
        }

        Lighting Off

        Pass
        {
            Name "ImpostorsUnlitCutout"
            Tags
            {
                "LightMode" = "ForwardBase"
                "ForceNoShadowCasting" = "True"
            }
            ZWrite On
            ZTest LEqual
            Cull Back
            AlphaToMask On

            CGPROGRAM
            #include "Impostors.cginc"

            #pragma multi_compile_fog
            #pragma multi_compile _ IMPOSTORS_DEBUG_FADING
            #pragma vertex impostors_vert
            #pragma fragment impostors_frag
            ENDCG
        }

        Pass
        {
            Name "ImpostorsDepthOnly"
            Tags
            {
                "LightMode" = "ShadowCaster"
                "ForceNoShadowCasting" = "True"
            }
            ZWrite On
            ZTest LEqual
            Cull Back

            CGPROGRAM
            #include "Impostors.cginc"

            #pragma vertex impostors_vert
            #pragma fragment impostors_frag
            ENDCG
        }

        Pass
        {
            Tags{ "LightMode" = "Deferred"  }
            ZWrite On
            ZTest Less
            
            CGPROGRAM
            
            #include "Impostors.cginc"
            
            #pragma vertex impostors_vert
            #pragma fragment impostors_deferred_frag
            #pragma multi_compile _ UNITY_HDR_ON
            #pragma multi_compile _ IMPOSTORS_DEBUG_FADING
            
            void impostors_deferred_frag(
                v2f i,
                out half4 outDiffuse : SV_Target0,           // RT0: diffuse color (rgb), occlusion (a)
                out half4 outSpecSmoothness : SV_Target1,    // RT1: spec color (rgb), smoothness (a)
                out half4 outNormal : SV_Target2,            // RT2: normal (rgb), --unused, very low precision-- (a) 
                out half4 outEmission : SV_Target3 
            ) 
            {
                half4 color = impostors_frag(i);

                // diffuse should be black, because it is affected by lights and then combined with emissions, which we don't want to. 
                outDiffuse = half4(0,0,0,0);

                // idk, black seems to work here
                outSpecSmoothness = half4(0,0,0,0);
                
                // set normal to zero so that lights do not affect impostors
                outNormal = half4(0,0,0,1); 

                // using emission buffer to output true colors of the impostor texture without lighting
                outEmission = color;
                #ifndef UNITY_HDR_ON
					outEmission.rgb = exp2(-outEmission.rgb);
				#endif
            }
                        
            ENDCG
            
        }

    }


}