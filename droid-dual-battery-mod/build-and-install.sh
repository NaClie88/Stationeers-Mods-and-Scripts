#!/bin/bash
# Rebuilds SaltysDroidDualBattery and deploys it into StationeersLaunchPad's
# local mods folder (NOT BepInEx/plugins -- LaunchPad only discovers mods
# via an About/About.xml under this folder; see UpdateNotes.md for why).
# Stationeers must be closed first -- the plugin DLL is locked while the
# game process is running.
set -e

MSBUILD="/c/Program Files (x86)/Microsoft Visual Studio/2022/BuildTools/MSBuild/Current/Bin/MSBuild.exe"
REPO_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_DIR="$REPO_DIR/SaltysDroidDualBattery"
DLL_NAME="SaltysDroidDualBattery.dll"
MOD_DIR="/c/Users/Joshua/Documents/My Games/Stationeers/mods/SaltysDroidDualBattery"

if powershell -NoProfile -Command "Get-Process rocketstation -ErrorAction SilentlyContinue" | grep -q rocketstation; then
    echo "Stationeers is still running -- close it first, the plugin DLL is locked." >&2
    exit 1
fi

"$MSBUILD" "$PROJECT_DIR/SaltysDroidDualBattery.csproj" //p:Configuration=Debug //nologo //v:minimal

mkdir -p "$MOD_DIR/About"
cp "$REPO_DIR/About/About.xml" "$MOD_DIR/About/About.xml"
cp "$PROJECT_DIR/bin/Debug/$DLL_NAME" "$MOD_DIR/$DLL_NAME"

SRC=$(certutil -hashfile "$PROJECT_DIR/bin/Debug/$DLL_NAME" MD5 | sed -n '2p')
DST=$(certutil -hashfile "$MOD_DIR/$DLL_NAME" MD5 | sed -n '2p')
if [ "$SRC" != "$DST" ]; then
    echo "Copy verification FAILED: hashes don't match." >&2
    exit 1
fi

echo "Installed OK ($DST) to $MOD_DIR. Relaunch Stationeers to test."
