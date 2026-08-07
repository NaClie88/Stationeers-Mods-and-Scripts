#!/bin/bash
# Rebuilds AirlockCardMod and copies the DLL into the BepInEx plugins
# folder in one shot. Stationeers must be closed first -- the plugin
# DLL is locked while the game process is running.
set -e

MSBUILD="/c/Program Files (x86)/Microsoft Visual Studio/2022/BuildTools/MSBuild/Current/Bin/MSBuild.exe"
PROJECT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/AirlockCardMod"
DLL_NAME="AirlockCardMod.dll"
PLUGIN_DIR="/c/Program Files (x86)/Steam/steamapps/common/Stationeers/BepInEx/plugins/AirlockCardMod"

if powershell -NoProfile -Command "Get-Process rocketstation -ErrorAction SilentlyContinue" | grep -q rocketstation; then
    echo "Stationeers is still running -- close it first, the plugin DLL is locked." >&2
    exit 1
fi

"$MSBUILD" "$PROJECT_DIR/AirlockCardMod.csproj" //p:Configuration=Debug //nologo //v:minimal

cp "$PROJECT_DIR/bin/Debug/$DLL_NAME" "$PLUGIN_DIR/$DLL_NAME"

SRC=$(certutil -hashfile "$PROJECT_DIR/bin/Debug/$DLL_NAME" MD5 | sed -n '2p')
DST=$(certutil -hashfile "$PLUGIN_DIR/$DLL_NAME" MD5 | sed -n '2p')
if [ "$SRC" != "$DST" ]; then
    echo "Copy verification FAILED: hashes don't match." >&2
    exit 1
fi

echo "Installed OK ($DST). Relaunch Stationeers to test."
