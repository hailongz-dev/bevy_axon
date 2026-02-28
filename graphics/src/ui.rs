use bevy::prelude::*;
use bevy_axon::core::*;
use bevy_axon_derive::*;
use serde::{Deserialize, Serialize};

#[derive(Component, Serialize, Deserialize, AxonVariant, Default, Debug, Clone)]
#[type_id = 1300]
pub struct Page {
    pub p: Vec<PageValue>,
}

#[derive(Serialize, Deserialize, AxonEvent, Event, Default, Debug, Clone)]
#[type_id = 1301]
pub struct PageEvent {
    pub client_id: u64,
    pub id: u64,
    pub n: String,
    pub p: Vec<PageValue>,
}

#[derive(Serialize, Deserialize, Default, Debug, Clone)]
pub struct PageValue {
    pub k: String,
    pub v: String,
}

pub fn run(app: &mut App) {
    app.add_axon_variant::<Page>();
    app.add_axon_event::<PageEvent>();
}
