use bevy::prelude::*;

pub mod core;
pub mod skin;
pub mod tile;
pub mod ui;

pub fn run(app: &mut App) {
    core::run(app);
    skin::run(app);
    tile::run(app);
    ui::run(app);
}
