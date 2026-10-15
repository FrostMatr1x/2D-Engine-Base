#version 450

layout(set = 0, binding = 0) uniform sampler uSampler;
layout(set = 1, binding = 0) uniform texture2D uTexture;

layout(location = 0) in vec2 fragUV;
layout(location = 1) in vec4 fragColor;

layout(location = 0) out vec4 outColor;

void main()
{
    outColor = texture(sampler2D(uTexture, uSampler), fragUV) * fragColor;
}
