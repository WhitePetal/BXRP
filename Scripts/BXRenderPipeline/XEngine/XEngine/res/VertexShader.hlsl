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

struct ModelViewProjection
{
	matrix MVP;
};

ConstantBuffer<ModelViewProjection> ModelViewProjectionCB : register(b0);


VertexShaderOutput main(VertexPosColor IN)
{
	VertexShaderOutput Out;
	Out.Position = mul(ModelViewProjectionCB.MVP, float4(IN.Position, 1.0f));
	Out.Color = float4(IN.Color, 1.0f);
	return Out;
}