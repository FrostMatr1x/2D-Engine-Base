using System;
using System.IO;
using System.Runtime.InteropServices;
using Silk.NET.Vulkan;

namespace Engine.Graphic.Vulkan;

public sealed unsafe class VulkanPipeline : IDisposable
{
    private Vk _vk;
    private Device _device;

    public Pipeline Handle;
    public PipelineLayout Layout;

    public void Create(
        VulkanContext context,
        RenderPass renderPass,
        string vertexShaderPath,
        string fragmentShaderPath,
        VertexInputBindingDescription[] bindings,
        VertexInputAttributeDescription[] attributes,
        uint pushConstantSize,
        DescriptorSetLayout[] setLayouts = null)
    {
        _vk = context.Vk;
        _device = context.Device;

        ShaderModule vertexModule = CreateShaderModule(File.ReadAllBytes(vertexShaderPath));
        ShaderModule fragmentModule = CreateShaderModule(File.ReadAllBytes(fragmentShaderPath));

        IntPtr entryPoint = Marshal.StringToHGlobalAnsi("main");
        try
        {
            var stages = stackalloc PipelineShaderStageCreateInfo[2];
            stages[0] = new PipelineShaderStageCreateInfo
            {
                SType = StructureType.PipelineShaderStageCreateInfo,
                Stage = ShaderStageFlags.VertexBit,
                Module = vertexModule,
                PName = (byte*)entryPoint
            };
            stages[1] = new PipelineShaderStageCreateInfo
            {
                SType = StructureType.PipelineShaderStageCreateInfo,
                Stage = ShaderStageFlags.FragmentBit,
                Module = fragmentModule,
                PName = (byte*)entryPoint
            };

            fixed (VertexInputBindingDescription* pBindings = bindings)
            fixed (VertexInputAttributeDescription* pAttributes = attributes)
            {
                var vertexInput = new PipelineVertexInputStateCreateInfo
                {
                    SType = StructureType.PipelineVertexInputStateCreateInfo,
                    VertexBindingDescriptionCount = (uint)bindings.Length,
                    PVertexBindingDescriptions = pBindings,
                    VertexAttributeDescriptionCount = (uint)attributes.Length,
                    PVertexAttributeDescriptions = pAttributes
                };

                var inputAssembly = new PipelineInputAssemblyStateCreateInfo
                {
                    SType = StructureType.PipelineInputAssemblyStateCreateInfo,
                    Topology = PrimitiveTopology.TriangleList,
                    PrimitiveRestartEnable = false
                };

                var viewportState = new PipelineViewportStateCreateInfo
                {
                    SType = StructureType.PipelineViewportStateCreateInfo,
                    ViewportCount = 1,
                    ScissorCount = 1
                };

                var rasterization = new PipelineRasterizationStateCreateInfo
                {
                    SType = StructureType.PipelineRasterizationStateCreateInfo,
                    DepthClampEnable = false,
                    RasterizerDiscardEnable = false,
                    PolygonMode = PolygonMode.Fill,
                    CullMode = CullModeFlags.None,
                    FrontFace = FrontFace.Clockwise,
                    DepthBiasEnable = false,
                    LineWidth = 1.0f
                };

                var multisample = new PipelineMultisampleStateCreateInfo
                {
                    SType = StructureType.PipelineMultisampleStateCreateInfo,
                    RasterizationSamples = SampleCountFlags.Count1Bit,
                    SampleShadingEnable = false
                };

                var blendAttachment = new PipelineColorBlendAttachmentState
                {
                    BlendEnable = true,
                    SrcColorBlendFactor = BlendFactor.SrcAlpha,
                    DstColorBlendFactor = BlendFactor.OneMinusSrcAlpha,
                    ColorBlendOp = BlendOp.Add,
                    SrcAlphaBlendFactor = BlendFactor.One,
                    DstAlphaBlendFactor = BlendFactor.Zero,
                    AlphaBlendOp = BlendOp.Add,
                    ColorWriteMask = ColorComponentFlags.RBit | ColorComponentFlags.GBit | ColorComponentFlags.BBit | ColorComponentFlags.ABit
                };

                var colorBlend = new PipelineColorBlendStateCreateInfo
                {
                    SType = StructureType.PipelineColorBlendStateCreateInfo,
                    LogicOpEnable = false,
                    LogicOp = LogicOp.Copy,
                    AttachmentCount = 1,
                    PAttachments = &blendAttachment
                };

                var dynamicStates = stackalloc DynamicState[2];
                dynamicStates[0] = DynamicState.Viewport;
                dynamicStates[1] = DynamicState.Scissor;

                var dynamicState = new PipelineDynamicStateCreateInfo
                {
                    SType = StructureType.PipelineDynamicStateCreateInfo,
                    DynamicStateCount = 2,
                    PDynamicStates = dynamicStates
                };

                var pushConstantRange = new PushConstantRange
                {
                    StageFlags = ShaderStageFlags.VertexBit,
                    Offset = 0,
                    Size = pushConstantSize
                };

                DescriptorSetLayout[] layouts = setLayouts ?? Array.Empty<DescriptorSetLayout>();
                fixed (DescriptorSetLayout* pSetLayouts = layouts)
                {
                    var layoutInfo = new PipelineLayoutCreateInfo
                    {
                        SType = StructureType.PipelineLayoutCreateInfo,
                        SetLayoutCount = (uint)layouts.Length,
                        PSetLayouts = layouts.Length > 0 ? pSetLayouts : null,
                        PushConstantRangeCount = pushConstantSize > 0 ? 1u : 0u,
                        PPushConstantRanges = pushConstantSize > 0 ? &pushConstantRange : null
                    };

                    Result layoutResult = _vk.CreatePipelineLayout(_device, in layoutInfo, null, out PipelineLayout layout);
                    if (layoutResult != Result.Success)
                    {
                        throw new Exception($"vkCreatePipelineLayout failed: {layoutResult}");
                    }
                    Layout = layout;
                }

                var createInfo = new GraphicsPipelineCreateInfo
                {
                    SType = StructureType.GraphicsPipelineCreateInfo,
                    StageCount = 2,
                    PStages = stages,
                    PVertexInputState = &vertexInput,
                    PInputAssemblyState = &inputAssembly,
                    PViewportState = &viewportState,
                    PRasterizationState = &rasterization,
                    PMultisampleState = &multisample,
                    PDepthStencilState = null,
                    PColorBlendState = &colorBlend,
                    PDynamicState = &dynamicState,
                    Layout = Layout,
                    RenderPass = renderPass,
                    Subpass = 0,
                    BasePipelineHandle = default,
                    BasePipelineIndex = -1
                };

                Result result = _vk.CreateGraphicsPipelines(_device, default, 1, &createInfo, null, out Pipeline pipeline);
                if (result != Result.Success)
                {
                    throw new Exception($"vkCreateGraphicsPipelines failed: {result}");
                }
                Handle = pipeline;
            }
        }
        finally
        {
            Marshal.FreeHGlobal(entryPoint);
        }

        _vk.DestroyShaderModule(_device, vertexModule, null);
        _vk.DestroyShaderModule(_device, fragmentModule, null);
    }

    private ShaderModule CreateShaderModule(byte[] code)
    {
        fixed (byte* pCode = code)
        {
            var createInfo = new ShaderModuleCreateInfo
            {
                SType = StructureType.ShaderModuleCreateInfo,
                CodeSize = (nuint)code.Length,
                PCode = (uint*)pCode
            };

            Result result = _vk.CreateShaderModule(_device, in createInfo, null, out ShaderModule module);
            if (result != Result.Success)
            {
                throw new Exception($"vkCreateShaderModule failed: {result}");
            }
            return module;
        }
    }

    public void Dispose()
    {
        if (Handle.Handle != 0)
        {
            _vk.DestroyPipeline(_device, Handle, null);
            Handle = default;
        }
        if (Layout.Handle != 0)
        {
            _vk.DestroyPipelineLayout(_device, Layout, null);
            Layout = default;
        }
    }
}
