#!/bin/bash

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_DIR="$SCRIPT_DIR/axon"
UNITY_PLUGINS_DIR="$SCRIPT_DIR/unity/Plugins"

echo "=== Renet Interop Build Script ==="
echo "Project: $PROJECT_DIR"
echo "Output: $UNITY_PLUGINS_DIR"
echo ""

build_macos() {
    echo ">>> Building for macOS..."
    cd "$PROJECT_DIR"
    
    cargo build --features ffi --release --target x86_64-apple-darwin
    cargo build --features ffi --release --target aarch64-apple-darwin
    
    mkdir -p "$UNITY_PLUGINS_DIR/MacOS/x86_64"
    mkdir -p "$UNITY_PLUGINS_DIR/MacOS/arm64"
    
    cp "target/x86_64-apple-darwin/release/libbevy_axon.dylib" "$UNITY_PLUGINS_DIR/MacOS/x86_64/libbevy_axon.dylib"
    cp "target/aarch64-apple-darwin/release/libbevy_axon.dylib" "$UNITY_PLUGINS_DIR/MacOS/arm64/libbevy_axon.dylib"
    
    echo "    Built: MacOS/x86_64/libbevy_axon.dylib"
    echo "    Built: MacOS/arm64/libbevy_axon.dylib"
}

build_windows() {
    echo ">>> Building for Windows..."
    cd "$PROJECT_DIR"
    
    cargo build --features ffi --release --target x86_64-pc-windows-gnu
    
    mkdir -p "$UNITY_PLUGINS_DIR/Windows/x86_64"
    
    cp "target/x86_64-pc-windows-gnu/release/bevy_axon.dll" "$UNITY_PLUGINS_DIR/Windows/x86_64/bevy_axon.dll"
    
    echo "    Built: Windows/x86_64/bevy_axon.dll"
}

build_linux() {
    echo ">>> Building for Linux..."
    cd "$PROJECT_DIR"
    
    cargo build --features ffi --release --target x86_64-unknown-linux-gnu
    
    mkdir -p "$UNITY_PLUGINS_DIR/Linux/x86_64"
    
    cp "target/x86_64-unknown-linux-gnu/release/libbevy_axon.so" "$UNITY_PLUGINS_DIR/Linux/x86_64/libbevy_axon.so"
    
    echo "    Built: Linux/x86_64/libbevy_axon.so"
}

build_ios() {
    echo ">>> Building for iOS..."
    cd "$PROJECT_DIR"
    
    cargo build --features ffi --release --target aarch64-apple-ios
    cargo build --features ffi --release --target aarch64-apple-ios-sim
    cargo build --features ffi --release --target x86_64-apple-ios
    
    mkdir -p "$UNITY_PLUGINS_DIR/iOS/arm64"
    mkdir -p "$UNITY_PLUGINS_DIR/iOS/x86_64"
    
    cp "target/aarch64-apple-ios/release/libbevy_axon.a" "$UNITY_PLUGINS_DIR/iOS/arm64/libbevy_axon.a"
    cp "target/aarch64-apple-ios-sim/release/libbevy_axon.a" "$UNITY_PLUGINS_DIR/iOS/arm64/libbevy_axon_sim.a"
    cp "target/x86_64-apple-ios/release/libbevy_axon.a" "$UNITY_PLUGINS_DIR/iOS/x86_64/libbevy_axon.a"
    
    echo "    Built: iOS/arm64/libbevy_axon.a"
    echo "    Built: iOS/Simulator/libbevy_axon.a"
}

build_android() {
    echo ">>> Building for Android..."
    cd "$PROJECT_DIR"
    
    if [ -z "$ANDROID_NDK_ROOT" ] && [ -z "$ANDROID_NDK_HOME" ]; then
        echo "    ERROR: ANDROID_NDK_ROOT or ANDROID_NDK_HOME not set"
        echo "    Skipping Android build..."
        return
    fi
    
    ANDROID_NDK="${ANDROID_NDK_ROOT:-$ANDROID_NDK_HOME}"
    
    local targets=(
        "armv7-linux-androideabi"
        "aarch64-linux-android"
        "x86_64-linux-android"
    )
    
    for target in "${targets[@]}"; do
        echo "    Building $target..."
        cargo build --features ffi --release --target "$target"
        
        case $target in
            armv7-linux-androideabi)
                mkdir -p "$UNITY_PLUGINS_DIR/Android/armeabi-v7a"
                cp "target/$target/release/libbevy_axon.so" "$UNITY_PLUGINS_DIR/Android/armeabi-v7a/libbevy_axon.so"
                ;;
            aarch64-linux-android)
                mkdir -p "$UNITY_PLUGINS_DIR/Android/arm64-v8a"
                cp "target/$target/release/libbevy_axon.so" "$UNITY_PLUGINS_DIR/Android/arm64-v8a/libbevy_axon.so"
                ;;
            x86_64-linux-android)
                mkdir -p "$UNITY_PLUGINS_DIR/Android/x86_64"
                cp "target/$target/release/libbevy_axon.so" "$UNITY_PLUGINS_DIR/Android/x86_64/libbevy_axon.so"
                ;;
        esac
    done
    
    echo "    Built: Android/armeabi-v7a/libbevy_axon.so"
    echo "    Built: Android/arm64-v8a/libbevy_axon.so"
    echo "    Built: Android/x86_64/libbevy_axon.so"
}

show_usage() {
    echo "Usage: $0 [platform]"
    echo ""
    echo "Platforms:"
    echo "  all       - Build for all platforms"
    echo "  macos     - Build for macOS (x64 + arm64)"
    echo "  windows   - Build for Windows (x64)"
    echo "  linux     - Build for Linux (x64 + arm64)"
    echo "  ios       - Build for iOS (arm64 + armv7 + simulator)"
    echo "  android   - Build for Android (armeabi-v7a + arm64-v8a + x86_64)"
    echo ""
    echo "Examples:"
    echo "  $0 all"
    echo "  $0 macos"
    echo "  $0 android"
}

main() {
    local platform="${1:-all}"
    
    case $platform in
        all)
            build_macos
            build_windows
            build_linux
            build_ios
            build_android
            ;;
        macos)
            build_macos
            ;;
        windows)
            build_windows
            ;;
        linux)
            build_linux
            ;;
        ios)
            build_ios
            ;;
        android)
            build_android
            ;;
        help|--help|-h)
            show_usage
            exit 0
            ;;
        *)
            echo "Error: Unknown platform '$platform'"
            show_usage
            exit 1
            ;;
    esac
    
    echo ""
    echo "=== Build Complete ==="
    echo ""
    echo "Unity Plugins directory: $UNITY_PLUGINS_DIR"
    echo ""
    ls -la "$UNITY_PLUGINS_DIR"
}

main "$@"
