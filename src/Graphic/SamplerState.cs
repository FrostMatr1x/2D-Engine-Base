using Silk.NET.Vulkan;

namespace Engine.Graphic;

public sealed class SamplerState
{
    public DescriptorSet Set;

    public static SamplerState PointClamp { get; internal set; }

    public static SamplerState LinearClamp { get; internal set; }
}
