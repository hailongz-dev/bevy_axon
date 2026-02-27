use crate::core::*;
use bevy::prelude::*;
use bevy_renet::netcode::{NetcodeServerTransport, ServerAuthentication, ServerConfig};
use bevy_renet::renet::{ConnectionConfig, DefaultChannel, ServerEvent};
use bevy_renet::*;
use std::collections::HashMap;
use std::io::Write;
use std::net::{SocketAddr, UdpSocket};
use std::time::SystemTime;

#[derive(Resource, Default)]
struct AxonServerClientSet {
    map: HashMap<u64, Entity>,
}

#[derive(Default)]
struct AxonServerEntitySnapshot {
    t: u32,
    m: HashMap<u32, Vec<u8>>,
}

#[derive(Resource, Default)]
pub struct AxonServerSnapshot {
    buf: Vec<u8>,
    entities: HashMap<u64, AxonServerEntitySnapshot>,
}

impl AxonServerSnapshot {
    fn snapshot(&mut self) -> &[u8] {
        self.buf.clear();
        for (id, entity) in self.entities.iter() {
            write!(
                &mut self.buf,
                "{},{},{}\n\n",
                ACTION_TYPE_SPAWN, id, entity.t
            )
            .unwrap();
            for (t, variant) in entity.m.iter() {
                writeln!(&mut self.buf, "{},{},{}", ACTION_TYPE_CHANGE, id, t).unwrap();
                self.buf.extend_from_slice(variant);
                self.buf.push(b'\n');
            }
        }
        &self.buf
    }
}

#[derive(Default)]
pub struct AxonServerPlugin;

impl Plugin for AxonServerPlugin {
    fn build(&self, app: &mut App) {
        app.init_resource::<AxonServerClientSet>();
        app.init_resource::<AxonServerSnapshot>();
        app.add_systems(PreUpdate, server_axon_system);
        app.add_observer(server_axon_event_system);
        app.add_observer(server_axon_action_system);
    }
}

const CHANNELS: [u8; 3] = [
    DefaultChannel::ReliableOrdered as u8,
    DefaultChannel::ReliableUnordered as u8,
    DefaultChannel::Unreliable as u8,
];

fn server_axon_event_system(
    trigger: On<RenetServerEvent>,
    mut commands: Commands,
    mut client_set: ResMut<AxonServerClientSet>,
    mut snapshot: ResMut<AxonServerSnapshot>,
    mut srv: ResMut<RenetServer>,
) {
    let event = trigger.event();

    match event.0 {
        ServerEvent::ClientConnected { client_id } => {
            println!("Client {} connected", client_id);
            let entity = commands
                .spawn(AxonClient {
                    id: client_id,
                    connected: true,
                })
                .id();
            client_set.map.insert(client_id, entity);
            let data = snapshot.snapshot().to_vec();
            if !data.is_empty() {
                srv.send_message(client_id, DefaultChannel::ReliableOrdered, data.clone());
            }
        }
        ServerEvent::ClientDisconnected { client_id, reason } => {
            println!("Client {} disconnected: {:?}", client_id, reason);
            if let Some(entity) = client_set.map.remove(&client_id) {
                commands.entity(entity).despawn();
            }
        }
    }
}

fn server_axon_system(
    mut srv: ResMut<RenetServer>,
    mut commands: Commands,
    event_set: Res<AxonEventInvokeSet>,
) {
    let client_ids = srv.clients_id();

    if !client_ids.is_empty() {
        for client_id in client_ids {
            for channel in CHANNELS {
                while let Some(message) = srv.receive_message(client_id, channel) {
                    event_set.invoke(&message, &mut commands);
                }
            }
        }
    }
}

fn server_axon_action_system(
    event: On<AxonActionEvent>,
    mut srv: ResMut<RenetServer>,
    mut snapshot: ResMut<AxonServerSnapshot>,
) {
    let action = event.event();
    println!("action: {}", action.act);
    match action.act {
        ACTION_TYPE_SPAWN => {
            snapshot.entities.insert(
                action.id,
                AxonServerEntitySnapshot {
                    t: action.t,
                    m: HashMap::new(),
                },
            );
            srv.broadcast_message(
                DefaultChannel::ReliableOrdered,
                format!("{},{},{}\n\n", ACTION_TYPE_SPAWN, action.id, action.t).into_bytes(),
            );
        }
        ACTION_TYPE_DESPAWN => {
            let id = action.id;
            snapshot.entities.remove(&id);
            srv.broadcast_message(
                DefaultChannel::ReliableOrdered,
                format!("{},{}\n\n", ACTION_TYPE_DESPAWN, id).into_bytes(),
            );
        }
        ACTION_TYPE_CHANGE => {
            let id = action.id;
            let t = action.t;
            let v = &action.v;
            if let Some(m) = snapshot.entities.get_mut(&id) {
                m.m.insert(t, v.to_vec());
            }
            snapshot.buf.clear();
            write!(snapshot.buf, "{},{},{}\n", ACTION_TYPE_CHANGE, id, t).unwrap();
            snapshot.buf.extend_from_slice(&v);
            snapshot.buf.push(b'\n');
            srv.broadcast_message(DefaultChannel::ReliableOrdered, snapshot.buf.clone());
        }
        ACTION_TYPE_INVOKE => {
            let id = action.id;
            let t = action.t;
            let v = &action.v;
            snapshot.buf.clear();
            write!(snapshot.buf, "{},{},{}\n", ACTION_TYPE_INVOKE, id, t).unwrap();
            snapshot.buf.extend_from_slice(&v);
            snapshot.buf.push(b'\n');
            srv.broadcast_message(DefaultChannel::ReliableOrdered, snapshot.buf.clone());
        }
        _ => {}
    }
}

pub trait AppServerAxon {
    fn start_server(&mut self, addr: &str);
}

impl AppServerAxon for App {
    fn start_server(&mut self, addr: &str) {
        let server = RenetServer::new(ConnectionConfig::default());

        let socket = UdpSocket::bind(addr).expect("Failed to bind UDP socket");
        socket
            .set_nonblocking(true)
            .expect("Failed to set nonblocking");

        let server_addr: SocketAddr = addr.parse().expect("Failed to parse address");
        let current_time = SystemTime::now()
            .duration_since(SystemTime::UNIX_EPOCH)
            .expect("Time went backwards");
        let server_config = ServerConfig {
            current_time,
            max_clients: 64,
            protocol_id: 0,
            public_addresses: vec![server_addr],
            authentication: ServerAuthentication::Unsecure,
        };
        let transport =
            NetcodeServerTransport::new(server_config, socket).expect("Failed to create transport");

        self.insert_resource(server);
        self.insert_resource(transport);
    }
}
