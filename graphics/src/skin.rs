use bevy::prelude::*;
use bevy_axon_derive::*;
use bevy_axon::core::*;
use serde::{Deserialize, Serialize};

#[derive(Component, Serialize, Deserialize, AxonVariant, Default, Debug)]
pub struct Skin {
    pub id: u32,
    pub state: Vec<String>,
}

pub fn run(app: &mut App) {
    app.add_axon_variant::<Skin>();
}

