struct VertexPosColor
{
	float3 Position : POSITION;
	float3 Color : COLOR;
};

struct VertexShaderOutput
{
	float4 Position : SV_POSITION;
	float4 Color : COLOR;
};

cbuffer CameraBuffer : register(b0)
{
	float4x4 Matrix_V;
	float4x4 Matrix_P;
}


VertexShaderOutput main(VertexPosColor IN)
{
	VertexShaderOutput Out;
	float4 pos = float4(IN.Position, 1.0f);
	pos = mul(Matrix_V, pos);
	pos = mul(Matrix_P, pos);
	Out.Position = pos;
	Out.Color = float4(IN.Color, 1.0f);
	return Out;
}