use bevy::app::ScheduleRunnerPlugin;
use bevy::prelude::*;
use bevy_axon::core::*;
use bevy_axon::server::*;
use bevy_axon_derive::*;
use bevy_renet::RenetServerPlugin;
use bevy_renet::netcode::NetcodeServerPlugin;
use serde::Deserialize;
use serde::Serialize;
use std::collections::HashMap;
use std::time::Duration;

#[derive(Component, AxonObject)]
#[type_id = 1]
#[allow(dead_code)]
struct Player {
    id: u64,
}

#[derive(Component, AxonVariant, Serialize)]
#[type_id = 2]
struct Position {
    x: f32,
    y: f32,
    r: f32,
}

#[derive(Event, AxonEvent, Deserialize)]
#[type_id = 3]
#[allow(dead_code)]
struct MoveEvent {
    x: f32,
    y: f32,
    r: f32,
}

#[derive(Resource, Default)]
pub struct ClientSet {
    pub map: HashMap<Entity, Entity>,
}

fn client_join(
    query: Query<(Entity, &AxonClient), Added<AxonClient>>,
    mut commands: Commands,
    mut client_set: ResMut<ClientSet>,
) {
    for (entity, client) in query.iter() {
        let v = commands
            .spawn(Player { id: client.id })
            .insert(Position {
                x: 5.0,
                y: 0.0,
                r: 0.0,
            })
            .id();
        client_set.map.insert(entity, v);
    }
}

fn client_leave(
    mut removed: RemovedComponents<AxonClient>,
    mut client_set: ResMut<ClientSet>,
    mut commands: Commands,
) {
    for entity in removed.read() {
        if let Some(v) = client_set.map.remove(&entity) {
            commands.entity(v).despawn();
        }
    }
}

fn test(mut query: Query<&mut Position>) {
    let mut rng = rand::thread_rng();
    for mut pos in query.iter_mut() {
        pos.x = rand::Rng::gen_range(&mut rng, 0.0..10.0);
    }
}

fn main() {
    let addr = "127.0.0.1:7777";
    println!("addr: {}", addr);
    let mut app = App::new();
    app.add_plugins(
        MinimalPlugins.set(ScheduleRunnerPlugin::run_loop(Duration::from_secs_f64(
            1.0 / 30.0,
        ))),
    );

    app.add_plugins(AxonPlugin::default());
    app.add_plugins(AxonServerPlugin::default());
    app.add_plugins((RenetServerPlugin, NetcodeServerPlugin));

    app.add_axon_object::<Player>();
    app.add_axon_variant::<Position>();
    app.add_axon_event::<MoveEvent>();

    app.init_resource::<ClientSet>();
    app.add_systems(Update, (client_join, client_leave));
    app.add_systems(Update, test);  

    app.start_server(addr);

    app.run();
}
