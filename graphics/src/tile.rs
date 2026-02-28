use bevy::prelude::*;
use bevy_axon::core::*;
use bevy_axon_derive::*;
use serde::{Deserialize, Serialize};

#[derive(Serialize, Deserialize, Default, Clone, Debug)]
pub struct Tile {
    pub skin: u32,
    pub flags: u32,
}

#[derive(Serialize, Deserialize, Default, Clone, Debug)]
pub struct Layer {
    pub index: i32,
    pub tiles: Vec<Tile>,
}

#[derive(Component, Serialize, Deserialize, AxonVariant, Default, Debug, Clone)]
pub struct Tilemap {
    pub width: i32,
    pub height: i32,
    pub size: f32,
    pub layers: Vec<Layer>,
}

#[derive(Serialize, Deserialize, Default, Debug, Clone)]
pub struct TilePos {
    pub x: i32,
    pub y: i32,
}

pub fn run(app: &mut App) {
    app.add_axon_variant::<Tilemap>();
}
