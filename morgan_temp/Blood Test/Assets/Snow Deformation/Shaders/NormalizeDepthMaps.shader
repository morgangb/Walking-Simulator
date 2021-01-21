Shader "Hidden/NormalizeDepthMaps"
{
    Properties
    {
        _GroundTex ("-", 2D) = "white" {}
		_ObjectTex("-", 2D) = "white" {}
		_Displacement("-", Range(0, 1)) = 0.3
		_NearClip("-", Float) = 0.0
		_FarClip("-", Float) = 0.0
	}

	CGINCLUDE

	#include "UnityCG.cginc"

	sampler2D_float _GroundTex;
	sampler2D_float _ObjectTex;
	float _Displacement;
	float _NearClip;
	float _FarClip;

	half4 NormalizeDepth(v2f_img i, out float outDepth : SV_Depth) : SV_Target
	{
		float objectDepth = SAMPLE_DEPTH_TEXTURE(_ObjectTex, i.uv);
		float groundDepth = 1-SAMPLE_DEPTH_TEXTURE(_GroundTex, float2(1-i.uv.x,i.uv.y));

		outDepth = clamp((_FarClip - _NearClip)*(objectDepth - groundDepth) / _Displacement, 0, 1);
		return 0;
	}

	ENDCG

    SubShader
    {
        Pass
        {
			ZTest Always Cull Off ZWrite On
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment NormalizeDepth
            ENDCG
        }
    }
}
