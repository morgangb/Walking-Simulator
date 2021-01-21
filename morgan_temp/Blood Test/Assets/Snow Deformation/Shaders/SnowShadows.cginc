#if !defined(MY_SHADOWS_INCLUDED)
#define MY_SHADOWS_INCLUDED

#include "UnityCG.cginc"

sampler _DispTex;
sampler _DispMap;
float _Displacement;
sampler _SnowTex;
float4 _SnowTex_ST;

struct appdata {
	float4 vertex : POSITION;
	float3 normal : NORMAL;
	float4 uv : TEXCOORD0;
};

struct v2f {
	float4 pos : SV_POSITION;
	float4 uv : TEXCOORD0;
	float3 lightVec : TEXCOORD1;
};

#if defined(SHADOWS_CUBE)

#define VertexProgram ShadowVertexProgram

v2f ShadowVertexProgram(appdata v) {
	v2f i;

	i.uv = float4(TRANSFORM_TEX(v.uv.xy, _SnowTex), v.uv.zw);

	i.pos = UnityObjectToClipPos(v.vertex);
	i.lightVec = mul(unity_ObjectToWorld, v.position).xyz - _LightPositionRange.xyz;
	return i;
}

float4 ShadowFragmentProgram(v2f i) : SV_TARGET{
	float depth = length(i.lightVec) + unity_LightShadowBias.x;
	depth *= _LightPositionRange.w;
	return UnityEncodeCubeShadowDepth(depth);
}

#else

#define VertexProgram ShadowVertexProgram

v2f ShadowVertexProgram(appdata v) : SV_POSITION {
	v2f i;

	i.uv = float4(TRANSFORM_TEX(v.uv.xy, _SnowTex), v.uv.zw);

	float d = (tex2Dlod(_DispTex, float4(v.uv.zw, 0, 0)).r +
			   tex2Dlod(_DispMap, float4(v.uv.xy, 0, 0)).r) *
		       _Displacement;
	v.vertex.y -= v.normal.y * d;
	v.vertex.y += v.normal.y * _Displacement;

	i.pos = UnityClipSpaceShadowCasterPos(v.vertex.xyz, v.normal);
	i.pos = UnityApplyLinearShadowBias(i.pos);
	return i;
}

half4 ShadowFragmentProgram(v2f i) : SV_TARGET {
	float depth = length(i.lightVec) + unity_LightShadowBias.x;
	depth *= _LightPositionRange.w;
	return UnityEncodeCubeShadowDepth(depth);
}

#endif

#endif