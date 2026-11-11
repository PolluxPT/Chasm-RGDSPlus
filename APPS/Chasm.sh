#!/bin/sh
BASE=$(CDPATH= cd -- "$(dirname -- "$0")" && pwd) || exit 1
ROOT="$BASE/Chasm_RGDSPlus"
RT="$ROOT/runtime"
LOGS="$ROOT/logs"
mkdir -p "$LOGS" "$ROOT/savedata/data" "$ROOT/savedata/config" "$ROOT/savedata/cache" || exit 1
LOCK="$ROOT/savedata/.running"
if ! mkdir "$LOCK" 2>/dev/null; then
 old=$(cat "$LOCK/pid" 2>/dev/null)
 case "$old" in ''|*[!0-9]*) old='';; esac
 if [ -n "$old" ] && kill -0 "$old" 2>/dev/null; then exit 1; fi
 rm -f "$LOCK/pid"; rmdir "$LOCK" 2>/dev/null && mkdir "$LOCK" || exit 1
fi
echo "$$" > "$LOCK/pid"
[ ! -f "$LOGS/Launcher.txt" ] || cp "$LOGS/Launcher.txt" "$LOGS/Previous_Launcher.txt"
exec > "$LOGS/Launcher.txt" 2>&1
LOWER=/sys/class/backlight/backlight/brightness
POWER=/sys/class/backlight/backlight/bl_power
VOLUME=/sys/class/anbernic_misc/openbor_volume
lower_saved='' power_saved='' volume_saved='' game_pid='' guard_pid='' session_tmp=''
cleanup() {
 trap - EXIT INT TERM HUP
 if [ -n "$game_pid" ]; then
  kill -TERM "$game_pid" 2>/dev/null || :
  n=0; while kill -0 "$game_pid" 2>/dev/null && [ "$n" -lt 3 ]; do sleep 1; n=$((n+1)); done
  kill -KILL "$game_pid" 2>/dev/null || :; wait "$game_pid" 2>/dev/null || :
 fi
 [ -z "$guard_pid" ] || { kill "$guard_pid" 2>/dev/null || :; wait "$guard_pid" 2>/dev/null || :; }
 if [ -n "$session_tmp" ]; then python3 "$RT/performance-mode.py" restore "$session_tmp/performance.json" >> "$LOGS/Performance.txt" 2>&1 || :; fi
 [ -z "$power_saved" ] || printf '%s\n' "$power_saved" > "$POWER" 2>/dev/null || :
 [ -z "$lower_saved" ] || printf '%s\n' "$lower_saved" > "$LOWER" 2>/dev/null || :
 [ -z "$volume_saved" ] || printf '%s\n' "$volume_saved" > "$VOLUME" 2>/dev/null || :
 case "$session_tmp" in /tmp/chasm-rgdsplus.*) rm -rf -- "$session_tmp";; esac
 rm -f "$LOCK/pid";rmdir "$LOCK" 2>/dev/null || :
 cat /proc/meminfo > "$LOGS/Memory_End.txt" 2>/dev/null || :
 dmesg 2>/dev/null | tail -n 100 > "$LOGS/Kernel_End.txt" || :
 echo "Exit state: volume=$(cat "$VOLUME" 2>/dev/null), lower brightness=$(cat "$LOWER" 2>/dev/null), lower power=$(cat "$POWER" 2>/dev/null)"
 python3 "$RT/collect-logs.py" "$ROOT" || :
}
trap cleanup EXIT
trap 'exit 130' INT
trap 'exit 143' TERM HUP
printf 'Chasm RGDS+ v1.0.0\n'; date; uname -srvm
case "$(uname -m)" in aarch64|arm64) :;; *) echo 'ERROR: ARM64 Linux required.'; exit 2;; esac
command -v python3 >/dev/null 2>&1 || { echo 'ERROR: Python 3 missing from firmware.';exit 3; }
for f in runtime/patcher/Patcher.dll game/FNA.dll game/ChasmRGDS.Host.dll runtime/dotnet/dotnet runtime/native/libFNA3D.so.0 runtime/native/libFAudio.so.0; do
 [ -s "$ROOT/$f" ] || { echo "ERROR: Missing $f. Extract the entire ZIP again.";exit 3; }
done
python3 "$RT/verify-install.py" "$ROOT" || exit 3
if [ -z "${WAYLAND_DISPLAY:-}" ] || [ -z "${XDG_RUNTIME_DIR:-}" ]; then
 for d in /var/run /run "/run/user/$(id -u)"; do
  for s in "$d"/wayland-*; do
   [ -S "$s" ] || continue
   export XDG_RUNTIME_DIR="$d" WAYLAND_DISPLAY="${s##*/}";break 2
  done
 done
fi
[ -n "${WAYLAND_DISPLAY:-}" ] || { echo 'ERROR: No firmware Wayland display found.';exit 4; }
unset LD_PRELOAD SDL_DYNAMIC_API SDL_VIDEO_DRIVER SDL_AUDIO_DRIVER CHASM_TRACE
export LANG=C LC_ALL=C
export LD_LIBRARY_PATH="$RT/native:/lib:/usr/lib:/lib64:/usr/lib64"
export DOTNET_ROOT="$RT/dotnet" DOTNET_MULTILEVEL_LOOKUP=0 DOTNET_EnableDiagnostics=0 DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1 DOTNET_GCConserveMemory=5
export FNA_PLATFORM_BACKEND=SDL2 FNA3D_FORCE_DRIVER=OpenGL FNA3D_OPENGL_FORCE_ES3=1
export SDL_VIDEODRIVER=wayland SDL_AUDIODRIVER=alsa SDL_VIDEO_FULLSCREEN_DISPLAY=0 SDL_VIDEO_WINDOW_POS=0,0
export SDL_OPENGL_ES_DRIVER=1 SDL_VIDEO_GL_DRIVER=/lib/libGLESv2.so.2 SDL_VIDEO_EGL_DRIVER=/lib/libEGL.so.1
export SDL_JOYSTICK_ALLOW_BACKGROUND_EVENTS=1
export XDG_DATA_HOME="$ROOT/savedata/data" XDG_CONFIG_HOME="$ROOT/savedata/config" XDG_CACHE_HOME="$ROOT/savedata/cache"
export CHASM_VOLUME_NODE="$VOLUME" CHASM_CONTROLLER_PROFILE="$ROOT/savedata/config/chasm-controls-v1.json"
export SDL_GAMECONTROLLERCONFIG="$(cat "$RT/rgds-controller.txt")"
session_tmp=$(mktemp -d /tmp/chasm-rgdsplus.XXXXXX) || exit 5
export TMPDIR="$session_tmp" CHASM_EXIT_REQUEST="$session_tmp/exit-request"
chmod +x "$RT/dotnet/dotnet" 2>/dev/null || :
ulimit -c 0
python3 "$RT/prepare-game.py" "$ROOT" > "$LOGS/Preparation.txt" 2>&1 || { cat "$LOGS/Preparation.txt"; exit 3; }
cat /proc/meminfo > "$LOGS/Memory_Start.txt"
if [ -r "$VOLUME" ] && [ -w "$VOLUME" ]; then
 volume_saved=$(cat "$VOLUME")
 case "$volume_saved" in 0|1|2|3|4|5|6|7|8|9|10) echo "Saved volume: $volume_saved/10";; *) volume_saved='';; esac
else echo 'WARNING: firmware volume node unavailable.'; fi
if [ -r "$LOWER" ] && [ -w "$LOWER" ]; then
 lower_saved=$(cat "$LOWER")
 case "$lower_saved" in ''|*[!0-9]*) lower_saved='';; *) printf '0\n' > "$LOWER";; esac
else echo 'WARNING: lower-screen backlight node unavailable.';fi
if [ -r "$POWER" ] && [ -w "$POWER" ]; then
 power_saved=$(cat "$POWER")
 case "$power_saved" in ''|*[!0-9]*) power_saved='';; *) printf '4\n' > "$POWER";; esac
fi
python3 "$RT/performance-mode.py" apply "$session_tmp/performance.json" > "$LOGS/Performance.txt" 2>&1 || :
cd "$ROOT/game" || exit 6
python3 -u "$RT/exit-guard.py" "$$" "$CHASM_EXIT_REQUEST" > "$LOGS/Exit_Keys.txt" 2>&1 & guard_pid=$!
"$RT/dotnet/dotnet" "$ROOT/game/ChasmRGDS.Host.dll" --ensure-controls > "$LOGS/Controls.txt" 2>&1 & game_pid=$!
wait "$game_pid"; result=$?;game_pid=''
[ "$result" -eq 0 ] || { echo "Controller setup exited: $result";exit "$result"; }
python3 "$RT/controller-map.py" "$CHASM_CONTROLLER_PROFILE" > "$session_tmp/controller-map.txt" || exit 7
export SDL_GAMECONTROLLERCONFIG="$(cat "$session_tmp/controller-map.txt")"
echo "Controller: $SDL_GAMECONTROLLERCONFIG"
echo 'Starting Chasm. Hold START+SELECT for 1.5 seconds to exit.'
: > "$LOGS/Startup_Trace.txt"
for dumphelper in "$RT"/dotnet/shared/Microsoft.NETCore.App/*/createdump; do chmod +x "$dumphelper" 2>/dev/null || :; done
export DOTNET_DbgEnableMiniDump=1 DOTNET_EnableCrashReport=1 DOTNET_EnableCrashReportOnly=1
export DOTNET_DbgMiniDumpName="$LOGS/ChasmCrash.%p" DOTNET_CreateDumpLogToFile="$LOGS/CrashCapture.txt"
export DOTNET_CreateDumpDiagnostics=1
"$RT/dotnet/dotnet" "$ROOT/game/ChasmRGDS.Host.dll" > "$LOGS/Game.txt" 2>&1 & game_pid=$!
wait "$game_pid"; result=$?;game_pid=''
echo "Game exit code: $result"
exit "$result"
