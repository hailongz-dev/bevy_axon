fn main() {
    // 只在 debug 模式下运行，release 模式跳过
    let profile = std::env::var("PROFILE").unwrap_or_default();
    if profile != "debug" {
        return;
    }

    // publish 验证时跳过（路径在 target/package 下）
    let manifest_dir = std::env::var("CARGO_MANIFEST_DIR").unwrap();
    if manifest_dir.contains("target/package") {
        return;
    }

    bevy_axon_cli::metadata::run(
        &format!("{}/src", manifest_dir),
        &format!("{}/../unity/BevyGraphics/axon.json", manifest_dir),
    );
}
