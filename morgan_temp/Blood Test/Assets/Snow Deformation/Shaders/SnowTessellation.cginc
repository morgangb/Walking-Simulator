#if !defined(TESSELLATION_INCLUDED)
#define TESSELLATION_INCLUDED

#include "UnityCG.cginc"
#include "AutoLight.cginc"

float _TessellationEdgeLength;

struct TessellationControlPoint {
	float4 vertex : INTERNALTESSPOS;
	float3 normal : NORMAL;
	#if TESSELLATION_TANGENT
		float4 tangent : TANGENT;
	#endif
	float4 uv : TEXCOORD0;
};

struct TessellationFactors {
	float edge[3] : SV_TessFactor;
	float inside : SV_InsideTessFactor;
};

//bool TriangleIsBelowClipPlane(
//	float3 p0, float3 p1, float3 p2, int planeIndex, float bias) {
//	float4 plane = unity_CameraWorldClipPlanes[planeIndex];
//	return dot(float4(p0, 1), plane) < bias &&
//		   dot(float4(p1, 1), plane) < bias &&
//		   dot(float4(p2, 1), plane) < bias;
//}
//
//bool TriangleIsCulled(float3 p0, float3 p1, float3 p2, float bias) {
//	return TriangleIsBelowClipPlane(p0, p1, p2, 0, bias) ||
//		   TriangleIsBelowClipPlane(p0, p1, p2, 1, bias) ||
//		   TriangleIsBelowClipPlane(p0, p1, p2, 2, bias) ||
//		   TriangleIsBelowClipPlane(p0, p1, p2, 3, bias);
//}

TessellationControlPoint TessellationVertexProgram(appdata v) {
	TessellationControlPoint p;
	p.vertex = v.vertex;
	p.normal = v.normal;
	#if TESSELLATION_TANGENT
		p.tangent = v.tangent;
	#endif
	p.uv = v.uv;
	return p;
}

float TessellationEdgeFactor(float3 p0, float3 p1) {
	float edgeLength = distance(p0, p1);

	float3 edgeCenter = (p0 + p1) * 0.5;
	float viewDistance = distance(edgeCenter, _WorldSpaceCameraPos);

	return edgeLength * _ScreenParams.y / (_TessellationEdgeLength * viewDistance);
}

TessellationFactors PatchConstantFunction(
	InputPatch<TessellationControlPoint, 3> patch
) {
	float3 p0 = mul(unity_ObjectToWorld, patch[0].vertex).xyz;
	float3 p1 = mul(unity_ObjectToWorld, patch[1].vertex).xyz;
	float3 p2 = mul(unity_ObjectToWorld, patch[2].vertex).xyz;

	half p0factor = tex2Dlod(_DispTex, float4(patch[0].uv.zw, 0, 0)).r;
	half p1factor = tex2Dlod(_DispTex, float4(patch[1].uv.zw, 0, 0)).r;
	half p2factor = tex2Dlod(_DispTex, float4(patch[2].uv.zw, 0, 0)).r;
	half p3factor = tex2Dlod(_DispTex, float4(((patch[1].uv.z) + (patch[2].uv.z)) / 2, (patch[1].uv.w + patch[2].uv.w) / 2, 0, 0)).r;
	half p4factor = tex2Dlod(_DispTex, float4(((patch[2].uv.z) + (patch[0].uv.z)) / 2, (patch[2].uv.w + patch[0].uv.w) / 2, 0, 0)).r;
	half p5factor = tex2Dlod(_DispTex, float4(((patch[0].uv.z) + (patch[1].uv.z)) / 2, (patch[0].uv.w + patch[1].uv.w) / 2, 0, 0)).r;
	half dispFactor = tex2Dlod(_DispMap, float4(patch[0].uv.xy, 0, 0)).r + 
					  tex2Dlod(_DispMap, float4(patch[1].uv.xy, 0, 0)).r +
					  tex2Dlod(_DispMap, float4(patch[2].uv.xy, 0, 0)).r;
	half factor = (p0factor + p1factor + p2factor + p3factor + p4factor + p5factor + dispFactor);

	TessellationFactors f;
	f.edge[0] = factor > 0.0 ? TessellationEdgeFactor(p1, p2) : 1.0;
	f.edge[1] = factor > 0.0 ? TessellationEdgeFactor(p2, p0) : 1.0;
	f.edge[2] = factor > 0.0 ? TessellationEdgeFactor(p0, p1) : 1.0;
	f.inside = factor > 0.0 ? (TessellationEdgeFactor(p1, p2) +
							   TessellationEdgeFactor(p2, p0) +
							   TessellationEdgeFactor(p0, p1)) * (1 / 3.0) : 1.0;

	/*if (TriangleIsCulled(p0, p1, p2, -_Displacement)) {
		f.edge[0] = f.edge[1] = f.edge[2] = f.inside = 0;
	}*/
	return f;
}

[UNITY_domain("tri")]
[UNITY_outputcontrolpoints(3)]
[UNITY_outputtopology("triangle_cw")]
[UNITY_partitioning("integer")]
[UNITY_patchconstantfunc("PatchConstantFunction")]
TessellationControlPoint HullProgram(
	InputPatch<TessellationControlPoint, 3> patch,
	uint id : SV_OutputControlPointID
) {
	return patch[id];
}

[UNITY_domain("tri")]
v2f DomainProgram(
	TessellationFactors factors,
	OutputPatch<TessellationControlPoint, 3> patch,
	float3 barycentricCoordinates : SV_DomainLocation
) {
	appdata data;

#define MY_DOMAIN_PROGRAM_INTERPOLATE(fieldName) data.fieldName = \
		patch[0].fieldName * barycentricCoordinates.x + \
		patch[1].fieldName * barycentricCoordinates.y + \
		patch[2].fieldName * barycentricCoordinates.z;

		MY_DOMAIN_PROGRAM_INTERPOLATE(vertex)
		MY_DOMAIN_PROGRAM_INTERPOLATE(normal)
		#if TESSELLATION_TANGENT
			MY_DOMAIN_PROGRAM_INTERPOLATE(tangent)
		#endif
		MY_DOMAIN_PROGRAM_INTERPOLATE(uv)

		return VertexProgram(data);
}

#endif