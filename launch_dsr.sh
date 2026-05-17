#!/usr/bin/env bash

# --- Resolve absolute game directory ---
GAME_DIR="$(cd "$(dirname "$0")" && pwd)"

# --- Compute Steamapps directory relative to game folder ---
# Steamapps directory: two levels up
STEAMAPPS_DIR="$(realpath "$GAME_DIR/../..")"

# --- Steam AppID for DSR ---
APPID=570940

# --- Write steam_appid.txt next to this script ---
echo "$APPID" > "$GAME_DIR/steam_appid.txt"

# --- Proton prefix automatically based on compatdata ---
PREFIX="$STEAMAPPS_DIR/compatdata/$APPID/pfx"
COMPATDATA_DIR="$(dirname "$PREFIX")"

if [ ! -d "$PREFIX" ]; then
    echo "[ERROR] Proton prefix not found at: $PREFIX"
    exit 1
fi

echo "[INFO] Using Proton prefix: $PREFIX"

# --- Locate Proton executable using config_info ---
CONFIG_INFO="$PREFIX/../config_info"
if [ ! -f "$CONFIG_INFO" ]; then
    echo "[ERROR] config_info not found in prefix: $CONFIG_INFO"
    exit 1
fi

# The Proton 'files' path is on the second line
PROTON_FILES_PATH=$(sed -n '2p' "$CONFIG_INFO")

# Navigate up to find the proton executable
# 'files/share/fonts/' -> up 4 levels -> proton
PROTON_BIN="$(realpath "$PROTON_FILES_PATH/../../../proton")"

if [ ! -x "$PROTON_BIN" ]; then
    echo "[ERROR] Proton binary not found at $PROTON_BIN"
    exit 1
fi

echo "[INFO] Using Proton binary: $PROTON_BIN"

# --- Executables ---
DSR_RANDO="$GAME_DIR/DSAP.Desktop.exe"
DSR_EXE="$GAME_DIR/DarkSoulsRemastered.exe"

for f in "$DSR_RANDO" "$DSR_EXE" "$PROTON_BIN"; do
    if [ ! -f "$f" ]; then
        echo "[ERROR] Required file not found: $f"
        echo "[ERROR] Place everything in the same directory as the game!"
        exit 1
    fi
done

# --- Environment ---
export WINEPREFIX="$PREFIX"
export STEAM_COMPAT_DATA_PATH="$COMPATDATA_DIR"
export STEAM_COMPAT_CLIENT_INSTALL_PATH="$HOME/.steam/root"

# --- Launch Randomizer ---
cd "$GAME_DIR" || exit 1
"$PROTON_BIN" run ./DSAP.Desktop.exe &

# --- Launch Game ---
"$PROTON_BIN" run ./DarkSoulsRemastered.exe &
