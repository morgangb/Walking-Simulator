//SOURCE: https://github.com/keijiro/GaussianBlur
Shader "Hidden/GaussianTextureBlur"
{
	Properties
	{
		_MainTex("-", 2D) = "white" {}
	}

	CGINCLUDE

	#include "UnityCG.cginc"

	sampler2D_float _MainTex;
	float4 _MainTex_TexelSize;

	// 9-tap Gaussian filter with linear sampling
	// http://rastergrid.com/blog/2010/09/efficient-gaussian-blur-with-linear-sampling/
	half gaussian_filter(float2 uv, float2 stride)
	{
		half s = tex2D(_MainTex, float4(uv, 0, 0)).r * 0.2270270270;

		float2 d1 = stride * 1.3846153846;
		s += tex2D(_MainTex, uv + d1).r * 0.3162162162;
		s += tex2D(_MainTex, uv - d1).r * 0.3162162162;

		float2 d2 = stride * 3.2307692308;
		s += tex2D(_MainTex, uv + d2).r * 0.0702702703;
		s += tex2D(_MainTex, uv - d2).r * 0.0702702703;

		return s;
	}

	// Quarter downsampler
	half4 frag_quarter(v2f_img i) : SV_Target
	{
		float4 d = _MainTex_TexelSize.xyxy * float4(1, 1, -1, -1);
		half4 s;
		s = tex2D(_MainTex, i.uv + d.xy);
		s += tex2D(_MainTex, i.uv + d.xw);
		s += tex2D(_MainTex, i.uv + d.zy);
		s += tex2D(_MainTex, i.uv + d.zw);
		return s * 0.25;
	}

	// Separable Gaussian filters
    half4 frag_blur_h(v2f_img i) : SV_Target
    {
        return gaussian_filter(i.uv, float2(_MainTex_TexelSize.x, 0));
    }

    half4 frag_blur_v(v2f_img i) : SV_Target
    {
        return gaussian_filter(i.uv, float2(0, _MainTex_TexelSize.y));
    }

	ENDCG

	Subshader
	{
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
