#!/usr/bin/env bash
# Source this file to expose a locally extracted Vulkan toolchain
# (validation layers, glslang, vulkaninfo) without system installation:
#   source tools/vulkan-env.sh

vt="$HOME/.local/opt/vulkantools"
if [ -d "$vt" ]; then
  export VK_LAYER_PATH="$vt/usr/share/vulkan/explicit_layer.d${VK_LAYER_PATH:+:$VK_LAYER_PATH}"
  export LD_LIBRARY_PATH="$vt/usr/lib64${LD_LIBRARY_PATH:+:$LD_LIBRARY_PATH}"
  export PATH="$vt/usr/bin:$PATH"
fi
