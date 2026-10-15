#!/usr/bin/env bash
set -euo pipefail

root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"

glslang="${GLSLANG:-}"
if [ -z "$glslang" ]; then
  if command -v glslangValidator >/dev/null 2>&1; then
    glslang="glslangValidator"
  elif [ -x "$HOME/.local/opt/vulkantools/usr/bin/glslangValidator" ]; then
    glslang="$HOME/.local/opt/vulkantools/usr/bin/glslangValidator"
  else
    echo "glslangValidator not found. Install the 'glslang' package." >&2
    exit 1
  fi
fi

mkdir -p "$root/Content/Shaders"

shopt -s nullglob
for src in "$root"/shaders/*.vert "$root"/shaders/*.frag; do
  name="$(basename "$src")"
  out="$root/Content/Shaders/$name.spv"
  "$glslang" -V "$src" -o "$out"
  echo "compiled $name -> Content/Shaders/$name.spv"
done
