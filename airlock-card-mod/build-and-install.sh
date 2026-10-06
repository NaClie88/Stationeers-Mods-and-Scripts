#!/bin/bash
# Rebuilds AirlockCardMod and deploys it into StationeersLaunchPad's
# local mods folder, the same layout as the other Salty mods (LaunchPad
# discovers mods via an About/About.xml under this folder).
# Stationeers must be closed first -- the plugin DLL is locked while the
# game process is running.
#
# The mod used to be installed as a plain BepInEx plugin
# (BepInEx/plugins/AirlockCardMod/). That copy is removed here, otherwise
# both copies would load and every patch would apply twice.
set -e

MSBUILD="/c/Program Files (x86)/Microsoft Visual Studio/2022/BuildTools/MSBuild/Current/Bin/MSBuild.exe"
REPO_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_DIR="$REPO_DIR/AirlockCardMod"
DLL_NAME="AirlockCardMod.dll"
MOD_DIR="/c/Users/Joshua/Documents/My Games/Stationeers/mods/AirlockCardMod"
OLD_PLUGIN_DIR="/c/Program Files (x86)/Steam/steamapps/common/Stationeers/BepInEx/plugins/AirlockCardMod"

if powershell -NoProfile -Command "Get-Process rocketstation -ErrorAction SilentlyContinue" | grep -q rocketstation; then
    echo "Stationeers is still running -- close it first, the plugin DLL is locked." >&2
    exit 1
fi

"$MSBUILD" "$PROJECT_DIR/AirlockCardMod.csproj" //p:Configuration=Debug //nologo //v:minimal

mkdir -p "$MOD_DIR/About"
cp "$REPO_DIR/About/About.xml" "$MOD_DIR/About/About.xml"
cp "$PROJECT_DIR/bin/Debug/$DLL_NAME" "$MOD_DIR/$DLL_NAME"

SRC=$(certutil -hashfile "$PROJECT_DIR/bin/Debug/$DLL_NAME" MD5 | sed -n '2p')
DST=$(certutil -hashfile "$MOD_DIR/$DLL_NAME" MD5 | sed -n '2p')
if [ "$SRC" != "$DST" ]; then
    echo "Copy verification FAILED: hashes don't match." >&2
    exit 1
fi

if [ -f "$OLD_PLUGIN_DIR/$DLL_NAME" ]; then
    rm "$OLD_PLUGIN_DIR/$DLL_NAME"
    rmdir "$OLD_PLUGIN_DIR" 2>/dev/null || true
    echo "Removed old BepInEx/plugins copy (it would load alongside the mods-folder one)."
fi

echo "Installed OK ($DST) to $MOD_DIR. Relaunch Stationeers to test."
