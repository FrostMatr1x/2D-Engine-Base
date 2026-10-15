using System;
using Silk.NET.Vulkan;

namespace Engine.Graphic.Vulkan;

public sealed unsafe class VulkanDescriptors : IDisposable
{
    private Vk _vk;
    private Device _device;

    public DescriptorSetLayout SamplerSetLayout;
    public DescriptorSetLayout TextureSetLayout;

    private DescriptorPool _pool;
    private Sampler _pointSampler;
    private Sampler _linearSampler;
    private DescriptorSet _pointSet;
    private DescriptorSet _linearSet;

    public void Create(VulkanContext context)
    {
        _vk = context.Vk;
        _device = context.Device;

        SamplerSetLayout = CreateSetLayout(DescriptorType.Sampler);
        TextureSetLayout = CreateSetLayout(DescriptorType.SampledImage);

        var poolSizes = stackalloc DescriptorPoolSize[2];
        poolSizes[0] = new DescriptorPoolSize { Type = DescriptorType.Sampler, DescriptorCount = 4 };
        poolSizes[1] = new DescriptorPoolSize { Type = DescriptorType.SampledImage, DescriptorCount = 512 };

        var poolInfo = new DescriptorPoolCreateInfo
        {
            SType = StructureType.DescriptorPoolCreateInfo,
            Flags = DescriptorPoolCreateFlags.FreeDescriptorSetBit,
            MaxSets = 512,
            PoolSizeCount = 2,
            PPoolSizes = poolSizes
        };

        Result poolResult = _vk.CreateDescriptorPool(_device, in poolInfo, null, out _pool);
        if (poolResult != Result.Success)
        {
            throw new Exception($"vkCreateDescriptorPool failed: {poolResult}");
        }

        _pointSampler = CreateSampler(Filter.Nearest, SamplerMipmapMode.Nearest);
        _linearSampler = CreateSampler(Filter.Linear, SamplerMipmapMode.Linear);

        _pointSet = CreateSamplerSet(_pointSampler);
        _linearSet = CreateSamplerSet(_linearSampler);

        SamplerState.PointClamp = new SamplerState { Set = _pointSet };
        SamplerState.LinearClamp = new SamplerState { Set = _linearSet };
    }

    public DescriptorSetLayout[] SetLayouts => new[] { SamplerSetLayout, TextureSetLayout };

    private DescriptorSetLayout CreateSetLayout(DescriptorType type)
    {
        var binding = new DescriptorSetLayoutBinding
        {
            Binding = 0,
            DescriptorType = type,
            DescriptorCount = 1,
            StageFlags = ShaderStageFlags.FragmentBit
        };

        var createInfo = new DescriptorSetLayoutCreateInfo
        {
            SType = StructureType.DescriptorSetLayoutCreateInfo,
            BindingCount = 1,
            PBindings = &binding
        };

        Result result = _vk.CreateDescriptorSetLayout(_device, in createInfo, null, out DescriptorSetLayout layout);
        if (result != Result.Success)
        {
            throw new Exception($"vkCreateDescriptorSetLayout failed: {result}");
        }
        return layout;
    }

    private Sampler CreateSampler(Filter filter, SamplerMipmapMode mipmapMode)
    {
        var createInfo = new SamplerCreateInfo
        {
            SType = StructureType.SamplerCreateInfo,
            MagFilter = filter,
            MinFilter = filter,
            MipmapMode = mipmapMode,
            AddressModeU = SamplerAddressMode.ClampToEdge,
            AddressModeV = SamplerAddressMode.ClampToEdge,
            AddressModeW = SamplerAddressMode.ClampToEdge,
            MipLodBias = 0f,
            MinLod = 0f,
            MaxLod = 0f,
            CompareEnable = false,
            AnisotropyEnable = false
        };

        Result result = _vk.CreateSampler(_device, in createInfo, null, out Sampler sampler);
        if (result != Result.Success)
        {
            throw new Exception($"vkCreateSampler failed: {result}");
        }
        return sampler;
    }

    private DescriptorSet Allocate(DescriptorSetLayout layout)
    {
        var layouts = stackalloc DescriptorSetLayout[1];
        layouts[0] = layout;

        var allocateInfo = new DescriptorSetAllocateInfo
        {
            SType = StructureType.DescriptorSetAllocateInfo,
            DescriptorPool = _pool,
            DescriptorSetCount = 1,
            PSetLayouts = layouts
        };

        Result result = _vk.AllocateDescriptorSets(_device, in allocateInfo, out DescriptorSet set);
        if (result != Result.Success)
        {
            throw new Exception($"vkAllocateDescriptorSets failed: {result}");
        }
        return set;
    }

    private DescriptorSet CreateSamplerSet(Sampler sampler)
    {
        DescriptorSet set = Allocate(SamplerSetLayout);

        var imageInfo = new DescriptorImageInfo
        {
            Sampler = sampler,
            ImageView = default,
            ImageLayout = ImageLayout.Undefined
        };

        var write = new WriteDescriptorSet
        {
            SType = StructureType.WriteDescriptorSet,
            DstSet = set,
            DstBinding = 0,
            DstArrayElement = 0,
            DescriptorCount = 1,
            DescriptorType = DescriptorType.Sampler,
            PImageInfo = &imageInfo
        };

        _vk.UpdateDescriptorSets(_device, 1, &write, 0, null);
        return set;
    }

    public DescriptorSet AllocateTextureSet(ImageView view)
    {
        DescriptorSet set = Allocate(TextureSetLayout);

        var imageInfo = new DescriptorImageInfo
        {
            Sampler = default,
            ImageView = view,
            ImageLayout = ImageLayout.ShaderReadOnlyOptimal
        };

        var write = new WriteDescriptorSet
        {
            SType = StructureType.WriteDescriptorSet,
            DstSet = set,
            DstBinding = 0,
            DstArrayElement = 0,
            DescriptorCount = 1,
            DescriptorType = DescriptorType.SampledImage,
            PImageInfo = &imageInfo
        };

        _vk.UpdateDescriptorSets(_device, 1, &write, 0, null);
        return set;
    }

    public void FreeTextureSet(DescriptorSet set)
    {
        if (set.Handle == 0)
        {
            return;
        }
        DescriptorSet local = set;
        _vk.FreeDescriptorSets(_device, _pool, 1, &local);
    }

    public void Dispose()
    {
        if (_pointSampler.Handle != 0)
        {
            _vk.DestroySampler(_device, _pointSampler, null);
            _pointSampler = default;
        }
        if (_linearSampler.Handle != 0)
        {
            _vk.DestroySampler(_device, _linearSampler, null);
            _linearSampler = default;
        }
        if (_pool.Handle != 0)
        {
            _vk.DestroyDescriptorPool(_device, _pool, null);
            _pool = default;
        }
        if (SamplerSetLayout.Handle != 0)
        {
            _vk.DestroyDescriptorSetLayout(_device, SamplerSetLayout, null);
            SamplerSetLayout = default;
        }
        if (TextureSetLayout.Handle != 0)
        {
            _vk.DestroyDescriptorSetLayout(_device, TextureSetLayout, null);
            TextureSetLayout = default;
        }
    }
}
