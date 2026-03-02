use bevy::prelude::*;
use bevy_axon::core::*;
use bevy_axon_derive::*;
use serde::{Deserialize, Serialize};

#[derive(Component, Serialize, Deserialize, AxonVariant, Default, Debug, Clone)]
pub struct Position {
    pub x: f32,
    pub y: f32,
    pub z: f32,
}

#[derive(Component, Serialize, Deserialize, AxonVariant, Default, Debug, Clone)]
pub struct MovePosition {
    pub x: f32,
    pub y: f32,
    pub z: f32,
}

#[derive(Component, Serialize, Deserialize, AxonVariant, Default, Debug, Clone)]
pub struct Rotation {
    pub x: f32,
    pub y: f32,
    pub z: f32,
}

#[derive(Component, Serialize, Deserialize, AxonVariant, Default, Debug, Clone)]
pub struct Scale {
    pub x: f32,
    pub y: f32,
    pub z: f32,
}

#[derive(Component, Serialize, Deserialize, AxonVariant, Default, Debug, Clone)]
pub struct Size {
    pub w: f32,
    pub h: f32,
}

#[derive(Component, Serialize, Deserialize, AxonVariant, Default, Debug, Clone)]
pub struct Color {
    pub r: f32,
    pub g: f32,
    pub b: f32,
    pub a: f32,
}

pub fn run(app: &mut App) {
    app.add_axon_variant::<Position>();
    app.add_axon_variant::<MovePosition>();
    app.add_axon_variant::<Rotation>();
    app.add_axon_variant::<Scale>();
    app.add_axon_variant::<Size>();
    app.add_axon_variant::<Color>();
}
