#!/bin/sh
set -eu
cd "$(dirname "$0")/.."
dotnet build Source/Host/Host.csproj -c Release
dotnet build Source/Patcher/Patcher.csproj -c Release
python3 Source/native_audio_compat.py Source/Dependencies/FAudio.Original.so Source/libFAudio.so.0
echo 'Managed tools and native compatibility patch built. See Source/BUILD.md for release layout.'
