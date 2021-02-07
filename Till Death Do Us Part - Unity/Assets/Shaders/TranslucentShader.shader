Shader "Custom/SSSShader" {
	Properties {
		_Color ("Color", Color) = (1,1,1,1)
		_MainTex ("Albedo (RGB)", 2D) = "white" {}
		_Normal("Normal", 2D) = "bump" {}
		_Glossiness ("Smoothness", Range(0,1)) = 0.5
		_Metallic ("Metallic", Range(0,1)) = 0.0

		_Distortion("Distortion", float) = 0
		_Power("Power", float) = 0
		_Scale("Scale", float) = 0
		[HDR]_SSSColor("SSS color", Color) = (1,1,1,1)
	}
	SubShader {
		Tags { "RenderType"="Opaque" }
		LOD 200

		CGPROGRAM
		#pragma surface surf SSS addshadow fullforwardshadows
		#include "UnityPBSLighting.cginc"
		// Use shader model 3.0 target, to get nicer looking lighting
		#pragma target 3.0

		sampler2D _MainTex;

		struct Input {
			float2 uv_MainTex;
			float2 uv_Normal;
		};


		half _Glossiness;
		half _Metallic;
		fixed4 _Color;

		float _Distortion;
		float _Power;
		float _Scale;
		fixed4 _SSSColor;
		sampler2D _Normal;

		half4 LightingSSS(SurfaceOutputStandard s, half3 viewDir, UnityGI gi){
			float3 lightColor = gi.light.color;
			float3 lightDir = gi.light.dir + s.Normal * _Distortion;
			half transDot = pow(saturate(dot(normalize(viewDir), -normalize(lightDir))), _Power) * _Scale ;
		
			return LightingStandard(s, viewDir, gi) + transDot * _SSSColor * fixed4(lightColor, 1);
		}

		inline void LightingSSS_GI(SurfaceOutputStandard s, UnityGIInput data, inout UnityGI gi )
		{
			UNITY_GI(gi, s, data);
		}

		// Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
		// See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
		// #pragma instancing_options assumeuniformscaling
		UNITY_INSTANCING_BUFFER_START(Props)
			// put more per-instance properties here
		UNITY_INSTANCING_BUFFER_END(Props)


		void surf (Input IN, inout SurfaceOutputStandard o) {
			// Albedo comes from a texture tinted by color
			fixed4 c = tex2D (_MainTex, IN.uv_MainTex) * _Color;
			o.Albedo = c.rgb;
			o.Normal = UnpackNormal( tex2D( _Normal, IN.uv_Normal ) );
			// Metallic and smoothness come from slider variables
			o.Metallic = _Metallic;
			o.Smoothness = _Glossiness;
			o.Alpha = c.a;
		}
		ENDCG
	}
	FallBack "Diffuse"
}