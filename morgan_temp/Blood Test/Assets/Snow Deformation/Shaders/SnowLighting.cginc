#if !defined(SNOWLIGHTING_INCLUDED)
#define SNOWLIGHTING_INCLUDED

#include "UnityPBSLighting.cginc"
#include "AutoLight.cginc"

#if defined(FOG_LINEAR) || defined(FOG_EXP) || defined(FOG_EXP2)
	#if !defined(FOG_DISTANCE)
		#define FOG_DEPTH 1
	#endif
	#define FOG_ON 1
#endif

struct appdata {
	float4 vertex : POSITION;
	float3 normal : NORMAL;
	float4 tangent : TANGENT;
	float4 uv : TEXCOORD0;
};

struct v2f {
	float4 pos : SV_POSITION;
	float4 uv : TEXCOORD0;
	float2 uvDetail : TEXCOORD1;
	float3 normal : TEXCOORD2;
	#if defined(BINORMAL_PER_FRAGMENT)
		float4 tangent : TEXCOORD3;
	#else
		float3 tangent : TEXCOORD3;
		float3 binormal : TEXCOORD4;
	#endif
		float3 worldPos : TEXCOORD5;

	SHADOW_COORDS(6)

	#if defined(VERTEXLIGHT_ON)
		float3 vertexLightColor : TEXCOORD7;
	#endif
};

sampler2D _DispTex;
sampler2D _DispMap;
float _Displacement;
half4 _SnowColor;
sampler _SnowTex;
float4 _SnowTex_ST;
half4 _GroundColor;
sampler _GroundTex;
sampler2D _NormalMap;
float _BumpScale;
float _Metallic;
float _Smoothness;
sampler2D _DetailTex;
float4 _DetailTex_ST;
sampler2D _DetailNormalMap;
float _DetailBumpScale;

void ComputeVertexLightColor(inout v2f i) {
#if defined(VERTEXLIGHT_ON)
	i.vertexLightColor = Shade4PointLights(
		unity_4LightPosX0, unity_4LightPosY0, unity_4LightPosZ0,
		unity_LightColor[0].rgb, unity_LightColor[1].rgb,
		unity_LightColor[2].rgb, unity_LightColor[3].rgb,
		unity_4LightAtten0, i.worldPos, i.normal);
#endif
}

float3 CreateBinormal(float3 normal, float3 tangent, float binormalSign) {
	return cross(normal, tangent.xyz) * (binormalSign * unity_WorldTransformParams.w);
}

v2f VertexProgram(appdata v) {
	v2f i;
	UNITY_INITIALIZE_OUTPUT(v2f, i);

	float d = (tex2Dlod(_DispTex, float4(v.uv.zw, 0, 0)).r +
			  tex2Dlod(_DispMap, float4(v.uv.xy, 0, 0)).r) * 
			  _Displacement;
	v.vertex.y -= v.normal.y * d;
	v.vertex.y += v.normal.y * _Displacement;

	i.pos = UnityObjectToClipPos(v.vertex);
	i.worldPos = mul(unity_ObjectToWorld, v.vertex);
	#if FOG_DEPTH
		i.worldPos.w = i.pos.z;
	#endif
	i.normal = UnityObjectToWorldNormal(v.normal);

	#if defined(BINORMAL_PER_FRAGMENT)
		i.tangent = float4(UnityObjectToWorldDir(v.tangent.xyz), v.tangent.w);
	#else
		i.tangent = UnityObjectToWorldDir(v.tangent.xyz);
		i.binormal = CreateBinormal(i.normal, i.tangent, v.tangent.w);
	#endif

	TRANSFER_SHADOW(i);

	i.uv = float4(TRANSFORM_TEX(v.uv.xy, _SnowTex), v.uv.zw);
	i.uvDetail = TRANSFORM_TEX(v.uv.xy, _DetailTex);
	ComputeVertexLightColor(i);
	return i;
}

UnityLight CreateLight(v2f i) {
	UnityLight light;

#if defined(POINT) || defined(POINT_COOKIE) || defined(SPOT)
	light.dir = normalize(_WorldSpaceLightPos0.xyz - i.worldPos);
#else
	light.dir = _WorldSpaceLightPos0.xyz;
#endif

	UNITY_LIGHT_ATTENUATION(attenuation, i, i.worldPos);

	light.color = _LightColor0.rgb * attenuation;
	light.ndotl = DotClamped(i.normal, light.dir);
	return light;
}

UnityIndirect CreateIndirectLight(v2f i) {
	UnityIndirect indirectLight;
	indirectLight.diffuse = 0;
	indirectLight.specular = 0;

#if defined(VERTEXLIGHT_ON)
	indirectLight.diffuse = i.vertexLightColor;
#endif

#if defined(FORWARD_BASE_PASS)
	indirectLight.diffuse += max(0, ShadeSH9(float4(i.normal, 1)));
#endif

	return indirectLight;
}

void InitializeFragmentNormal(inout v2f i) {
	float3 mainNormal = UnpackScaleNormal(tex2D(_NormalMap, i.uv.xy), _BumpScale);
	float3 detailNormal = UnpackScaleNormal(tex2D(_DetailNormalMap, i.uvDetail), _DetailBumpScale);
	float3 tangentSpaceNormal = BlendNormals(mainNormal, detailNormal);

	#if defined(BINORMAL_PER_FRAGMENT)
		float3 binormal = CreateBinormal(i.normal, i.tangent.xyz, i.tangent.w);
	#else
		float3 binormal = i.binormal;
	#endif

	i.normal = normalize(tangentSpaceNormal.x * i.tangent +
		tangentSpaceNormal.y * binormal +
		tangentSpaceNormal.z * i.normal);
}

float4 ApplyFog(float4 color, v2f i) {
	#if FOG_ON
		float viewDistance = length(_WorldSpaceCameraPos - i.worldPos.xyz);
		#if FOG_DEPTH
			viewDistance = UNITY_Z_0_FAR_FROM_CLIPSPACE(i.worldPos.w);
		#endif
		UNITY_CALC_FOG_FACTOR_RAW(viewDistance);
		float3 fogColor = 0;
		#if defined(FORWARD_BASE_PASS)
			fogColor = unity_FogColor.rgb;
		#endif
		color.rgb = lerp(fogColor, color.rgb, saturate(unityFogFactor));
	#endif
	return color;
}

float4 FragmentProgram(v2f i) : SV_TARGET{
	InitializeFragmentNormal(i);

	float3 viewDir = normalize(_WorldSpaceCameraPos - i.worldPos);

	float2 SnowUV = i.uv.xy;
	float2 GroundUV = i.uv.zw;
	half amount = tex2Dlod(_DispTex, float4(GroundUV, 0, 0)).r + tex2Dlod(_DispMap, float4(SnowUV, 0, 0)).r;
	float3 albedo = lerp(tex2D(_SnowTex, SnowUV) * _SnowColor, tex2D(_GroundTex, SnowUV) * _GroundColor, amount);

	float3 specularTint = albedo * _Metallic;
	float oneMinusReflectivity = 1 - _Metallic;
	albedo = DiffuseAndSpecularFromMetallic(albedo, _Metallic, specularTint, oneMinusReflectivity);
	albedo *= tex2D(_DetailTex, i.uvDetail) * unity_ColorSpaceDouble;

	float4 color = UNITY_BRDF_PBS(albedo, specularTint,
						  oneMinusReflectivity, _Smoothness,
						  i.normal, viewDir,
						  CreateLight(i), CreateIndirectLight(i));

	return ApplyFog(color, i);
}

#endif