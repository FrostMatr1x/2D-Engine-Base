#version 450

layout(location = 0) in vec2 inPosition;
layout(location = 1) in vec2 inInstancePosition;
layout(location = 2) in vec2 inInstanceScale;
layout(location = 3) in float inInstanceRotation;
layout(location = 4) in uint inInstanceColor;

layout(push_constant) uniform PushConstants
{
    mat4 proj;
} pc;

layout(location = 0) out vec4 fragColor;

void main()
{
    vec2 scaled = inPosition * inInstanceScale;
    float c = cos(inInstanceRotation);
    float s = sin(inInstanceRotation);
    vec2 rotated = vec2(scaled.x * c - scaled.y * s, scaled.x * s + scaled.y * c);
    vec2 world = inInstancePosition + rotated;
    gl_Position = pc.proj * vec4(world, 0.0, 1.0);
    fragColor = unpackUnorm4x8(inInstanceColor);
}
