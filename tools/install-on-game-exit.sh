#!/usr/bin/env bash
# Wait for a Stationeers test session to end, then install the next build and summarise the logs.
#
# Meant to be started in the background (e.g. by Claude Code with run_in_background) while the
# player is testing: when the game exits, it installs the given mod with that mod's own
# build-and-install.sh (which re-checks that the game is closed and verifies the hash) and prints
# the log lines worth reading. When this script exits, the agent that started it is notified.
#
# Usage:
#   tools/install-on-game-exit.sh <mod-dir> [--no-install] [--start-wait-minutes N]
#     <mod-dir>              folder containing build-and-install.sh, e.g. droid-standby-mod
#     --no-install           only wait and summarise the logs
#     --start-wait-minutes N if the game isn't running yet, wait up to N minutes for it to start
#                            (default 120); if it never starts, exit without installing
set -u

MOD_DIR=""
INSTALL=1
START_WAIT_MIN=120
while [ $# -gt 0 ]; do
  case "$1" in
    --no-install) INSTALL=0 ;;
    --start-wait-minutes) shift; START_WAIT_MIN="$1" ;;
    *) MOD_DIR="$1" ;;
  esac
  shift
done

GAME_EXE="rocketstation.exe"
BEPINEX_LOG="/c/Program Files (x86)/Steam/steamapps/common/Stationeers/BepInEx/LogOutput.log"
PLAYER_LOG="$USERPROFILE/AppData/LocalLow/Rocketwerkz/rocketstation/Player.log"

running() { tasklist //FI "IMAGENAME eq $GAME_EXE" 2>/dev/null | grep -qi "$GAME_EXE"; }
now() { date '+%H:%M:%S'; }

if [ "$INSTALL" = 1 ] && [ ! -f "$MOD_DIR/build-and-install.sh" ]; then
  echo "ERROR: no build-and-install.sh in '$MOD_DIR'" >&2
  exit 2
fi

# 1. If the game isn't up yet, wait for the test session to start.
if ! running; then
  echo "[$(now)] Stationeers is not running; waiting up to ${START_WAIT_MIN} min for it to start..."
  deadline=$(( $(date +%s) + START_WAIT_MIN * 60 ))
  until running; do
    if [ "$(date +%s)" -ge "$deadline" ]; then
      echo "[$(now)] The game never started; nothing installed."
      exit 3
    fi
    sleep 10
  done
  echo "[$(now)] Stationeers started."
fi

# 2. Wait for the session to end.
echo "[$(now)] Waiting for Stationeers to exit..."
while running; do sleep 5; done
echo "[$(now)] Stationeers exited."
sleep 5   # let the game release file handles and finish writing its logs

# 3. Install the next build.
if [ "$INSTALL" = 1 ]; then
  echo "=== build-and-install ($MOD_DIR) ==="
  (cd "$MOD_DIR" && bash build-and-install.sh 2>&1 | tail -4)
fi

# 4. Log summary from the session that just ended.
echo "=== BepInEx: Salty mod warnings, errors and diagnostics ==="
grep -E "Salty" "$BEPINEX_LOG" 2>/dev/null | grep -Eiv "patch succeeded|Awake\(\)|Static constructor|Mod created|Already patched" | tail -40
echo "=== Player.log: exception types (count, first line) ==="
grep -E "Exception" "$PLAYER_LOG" 2>/dev/null | sed 's/^[[:space:]]*//' | sort | uniq -c | sort -rn | head -15
echo "=== Player.log: lines mentioning Salty mods ==="
grep -E "SaltysDroid|SaltysFabricator|SaltysYield|Airlock" "$PLAYER_LOG" 2>/dev/null | sort | uniq -c | sort -rn | head -15
echo "[$(now)] Done."
