#if OPENGL
    #define VS_SHADERMODEL vs_3_0
    #define PS_SHADERMODEL ps_3_0
    #define POSITION_SEMANTIC POSITION0
    #define SV_POSITION_SEMANTIC POSITION0
#else
    #define VS_SHADERMODEL vs_4_0_level_9_1
    #define PS_SHADERMODEL ps_4_0_level_9_1
    #define POSITION_SEMANTIC POSITION0
    #define SV_POSITION_SEMANTIC SV_POSITION
#endif

float4 GradientColorLeftTop : register(c0);
float4 GradientColorRightTop : register(c1);
float4 GradientColorLeftBottom : register(c2);
float4 GradientColorRightBottom : register(c3);

float4x4 ProjectionMatrix : register(c4);

struct VertexShaderInput {
    float4 Position : POSITION0;
    float4 Color : COLOR0;
    float2 TexCoord : TEXCOORD0;
};

struct VertexShaderOutput {
    float4 Position : SV_POSITION_SEMANTIC;
    float4 Color : COLOR0;
    float2 TexCoord : TEXCOORD0;
};

VertexShaderOutput VertexShaderFunction(VertexShaderInput input) {
    VertexShaderOutput output;
    
    output.Position = mul(input.Position, ProjectionMatrix);
    
    output.Color = input.Color;
    output.TexCoord = input.TexCoord;
    return output;
}

float4 PixelShaderFunction(VertexShaderOutput input) : COLOR0 {
    float2 uv = input.TexCoord;
    
    float4 top = lerp(GradientColorLeftTop, GradientColorRightTop, uv.x);
    float4 bottom = lerp(GradientColorLeftBottom, GradientColorRightBottom, uv.x);
    float4 result = lerp(top, bottom, uv.y);
    
    result *= input.Color;
    
    return result;
}

technique Technique1 {
    pass Pass1 {
        VertexShader = compile VS_SHADERMODEL VertexShaderFunction();
        PixelShader = compile PS_SHADERMODEL PixelShaderFunction();
    }
}