use bevy::prelude::*;
use bevy_axon_derive::*;
use bevy_axon::core::*;
use serde::{Deserialize, Serialize};

#[derive(Component, Serialize, Deserialize, AxonVariant, Default, Debug)]
#[type_id = 1000]
pub struct Position {
    pub x: f32,
    pub y: f32,
    pub z: f32,
}

#[derive(Component, Serialize, Deserialize, AxonVariant, Default, Debug)]
#[type_id = 1001]
pub struct Rotation {
    pub x: f32,
    pub y: f32,
    pub z: f32,
}

#[derive(Component, Serialize, Deserialize, AxonVariant, Default, Debug)]
#[type_id = 1002]
pub struct Scale {
    pub x: f32,
    pub y: f32,
    pub z: f32,
}


pub fn run(app: &mut App) {
    app.add_axon_variant::<Position>();
    app.add_axon_variant::<Rotation>();
    app.add_axon_variant::<Scale>();
}

