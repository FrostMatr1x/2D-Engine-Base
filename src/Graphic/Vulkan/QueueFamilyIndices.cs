namespace Engine.Graphic.Vulkan;

/// <summary>
/// Индексы семейств очередей
/// </summary>
public struct QueueFamilyIndices
{
    public uint? GraphicsFamily;
    public uint? PresentFamily;

    public readonly bool IsComplete => GraphicsFamily.HasValue && PresentFamily.HasValue;
}