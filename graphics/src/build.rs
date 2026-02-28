#[cfg(feature = "generate-metadata")]
fn main() {
    // publish 验证时跳过（路径在 target/package 下）
    let manifest_dir = std::env::var("CARGO_MANIFEST_DIR").unwrap();

    bevy_axon_cli::metadata::run(
        &format!("{}/src", manifest_dir),
        &format!("{}/../unity/BevyGraphics/axon.json", manifest_dir),
    );

}


#[cfg(not(feature = "generate-metadata"))]
fn main() {
}
