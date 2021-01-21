Shader "Hidden/GaussianDepthBlur"
{
	Properties
	{
		_MainTex("-", 2D) = "white" {}
		_MyDepthTex("-", 2D) = "white" {}
	}

	CGINCLUDE

	#include "UnityCG.cginc"

	sampler2D_float _MyDepthTex;
	float4 _MyDepthTex_TexelSize;
	vector _DepthTransform;

	// 9-tap Gaussian filter with linear sampling
	// http://rastergrid.com/blog/2010/09/efficient-gaussian-blur-with-linear-sampling/
	half gaussian_filter(float2 uv, float2 stride)
	{
		half s = tex2D(_MyDepthTex, float4(uv, 0, 0)).r * 0.2270270270;

		float2 d1 = stride * 1.3846153846;
		s += tex2D(_MyDepthTex, uv + d1).r * 0.3162162162;
		s += tex2D(_MyDepthTex, uv - d1).r * 0.3162162162;

		float2 d2 = stride * 3.2307692308;
		s += tex2D(_MyDepthTex, uv + d2).r * 0.0702702703;
		s += tex2D(_MyDepthTex, uv - d2).r * 0.0702702703;

		return s;
	}

	// important part: outputs depth from _MyDepthTex to depth buffer
	half4 CopyDepthBufferFragmentShader(v2f_img i, out float outDepth : SV_Depth) : SV_Target
	{
		float depth = SAMPLE_DEPTH_TEXTURE(_MyDepthTex, i.uv);
		outDepth = depth;
		return 0;
	}

	// Quarter downsampler
	half4 frag_quarter(v2f_img i, out float outDepth : SV_Depth) : SV_Target
	{
		float depth = SAMPLE_DEPTH_TEXTURE(_MyDepthTex, i.uv);
		outDepth = depth;

		float4 d = _MyDepthTex_TexelSize.xyxy * float4(1, 1, -1, -1);
		half4 s;
		s = tex2D(_MyDepthTex, i.uv + d.xy);
		s += tex2D(_MyDepthTex, i.uv + d.xw);
		s += tex2D(_MyDepthTex, i.uv + d.zy);
		s += tex2D(_MyDepthTex, i.uv + d.zw);
		return s * 0.25;
	}

	// Separable Gaussian filters
	half4 frag_blur_h(v2f_img i, out float outDepth : SV_Depth) : SV_Target
	{
		outDepth = gaussian_filter(i.uv, float2(_MyDepthTex_TexelSize.x, 0));

		return 0;
	}

	half4 frag_blur_v(v2f_img i, out float outDepth : SV_Depth) : SV_Target
	{
		outDepth = gaussian_filter(i.uv, float2(0, _MyDepthTex_TexelSize.y));

		return 0;
	}

	ENDCG

	Subshader
	{
		// Depth Copy
		Pass
		{
			ZTest Always Cull Off ZWrite On
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment CopyDepthBufferFragmentShader
			ENDCG
		}

		// Quarter Downsample
		Pass
		{
			ZTest Always Cull Off ZWrite On
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment frag_quarter
			ENDCG
		}

		// Horizontal Blur
		Pass
		{
			ZTest Always Cull Off ZWrite On
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment frag_blur_h
			#pragma target 3.0
			ENDCG
		}

		// Vertical Blur
		Pass
		{
			ZTest Always Cull Off ZWrite On
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment frag_blur_v
			#pragma target 3.0
			ENDCG
		}
	}
}
