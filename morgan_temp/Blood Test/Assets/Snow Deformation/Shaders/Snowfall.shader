Shader "Hidden/Snowfall"
{
	Properties
	{
		_MyDepthTex("-", 2D) = "black" {}
	}

	CGINCLUDE

	#include "UnityCG.cginc"

	sampler2D _MyDepthTex;
	float4 _MyDepthTex_ST;
	half _FlakeAmount;
	half _FlakeStrength;

	struct appdata
	{
		float4 vertex : POSITION;
		float2 uv : TEXCOORD0;
	};

	float rand(float3 co)
	{
		return frac(sin(dot(co.xyz, float3(12.9898, 78.233, 45.5432))) * 43758.5453);
	}

	half4 frag(v2f_img i, out float outDepth : SV_Depth) : SV_Target
	{
		float depth = SAMPLE_DEPTH_TEXTURE(_MyDepthTex, i.uv);
		float rValue = ceil(rand(float3(i.uv.x, i.uv.y, 0)*_Time.x) - (1 - _FlakeAmount));
		outDepth = saturate(depth - (rValue * _FlakeStrength));
		return 0;
	}

	half4 CopyDepthBufferFragmentShader(v2f_img i, out float outDepth : SV_Depth) : SV_Target
	{
		float depth = SAMPLE_DEPTH_TEXTURE(_MyDepthTex, i.uv);
		outDepth = depth;
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
			#pragma fragment frag
			ENDCG
		}
		Pass
		{
			ZTest Always Cull Off ZWrite On
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment CopyDepthBufferFragmentShader
			ENDCG
		}
	}
}
