Shader "Custom/Snow"
{
    Properties
    {
        _DispTex ("Displacement Texture", 2D) = "white" {}
		_DispMap ("Displacement Map", 2D) = "black" {}
		_Displacement ("Displacement", Range(0,1)) = 0.2
		_TessellationEdgeLength ("Edge Length", Range(5, 64)) = 16
		_SnowColor("Snow Color", Color) = (1,1,1,1)
		_SnowTex("Snow (RGB)", 2D) = "white" {}
		_GroundColor("Ground Color", Color) = (1,1,1,1)
		_GroundTex("Ground (RGB)", 2D) = "white" {}
		[NoScaleOffset] _NormalMap("Normal Map", 2D) = "bump" {}
		_BumpScale("Bump Scale", Float) = 1
		[Gamma] _Metallic("Metallic", Range(0, 1)) = 0
		_Smoothness("Smoothness", Range(0, 1)) = 0.5
		_DetailTex("Detail Texture", 2D) = "gray" {}
		[NoScaleOffset] _DetailNormalMap("Detail Normals", 2D) = "bump" {}
		_DetailBumpScale("Detail Bump Scale", Float) = 1
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }

		//Blend[_SrcBlend][_DstBlend]
		//ZWrite[_ZWrite]

        Pass
		{
			Tags { "LightMode"="ForwardBase" }
			CGPROGRAM

			#pragma target 4.6

			#pragma multi_compile_fwdbase
			#pragma multi_compile_fog
			#pragma multi_compile EDGE_LENGTH

			#pragma vertex TessellationVertexProgram
			#pragma fragment FragmentProgram
			#pragma hull HullProgram
			#pragma domain DomainProgram

			#define FORWARD_BASE_PASS
			#define TESSELLATION_TANGENT 1
			#define FOG_DISTANCE

			#include "SnowLighting.cginc"
			#include "SnowTessellation.cginc"

			ENDCG
		}
		Pass
		{
			Tags { "LightMode"="ForwardAdd"}

			Blend One One

			CGPROGRAM

			#pragma target 4.6

			#pragma multi_compile_fwdadd
			#pragma multi_compile_fog

			#pragma vertex TessellationVertexProgram
			#pragma fragment FragmentProgram
			#pragma hull HullProgram
			#pragma domain DomainProgram

			#define TESSELLATION_TANGENT 1
			#define FOG_DISTANCE
			
			#include "SnowLighting.cginc"
			#include "SnowTessellation.cginc"
			
			ENDCG
		}
		Pass
		{
			Tags { "LightMode" = "ShadowCaster" }

			CGPROGRAM

			#pragma target 4.6

			#pragma multi_compile_shadowcaster

			#pragma vertex TessellationVertexProgram
			#pragma fragment ShadowFragmentProgram
			#pragma hull HullProgram
			#pragma domain DomainProgram

			#include "SnowShadows.cginc"
			#include "SnowTessellation.cginc"

			ENDCG
		}
    }
	Fallback "VertexLit"
}
