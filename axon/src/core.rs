use bevy::prelude::*;
use serde::Serialize;
use std::collections::HashMap;

pub trait AxonObject {
    fn axon_object_type() -> u32;
}

pub trait AxonVariant {
    fn axon_variant_type() -> u32;
}

pub trait AxonEvent {
    fn axon_event_type() -> u32;
    fn axon_event_invoke(bytes: &[u8], commands: &mut Commands<'_, '_>);
}

pub const ACTION_TYPE_SPAWN: u8 = 1;
pub const ACTION_TYPE_DESPAWN: u8 = 2;
pub const ACTION_TYPE_CHANGE: u8 = 3;
pub const ACTION_TYPE_INVOKE: u8 = 4;

#[derive(Event)]
pub struct AxonActionEvent {
    pub act: u8,
    pub id: u64,
    pub t: u32,
    pub v: Vec<u8>,
    pub client_id: u64,
}

#[derive(Event)]
pub struct AxonExitEvent;

#[derive(Component)]
pub struct AxonClient {
    pub id: u64,
}

pub type AxonEventInvoke = fn(&[u8], &mut Commands<'_, '_>);

#[derive(Resource, Default)]
pub struct AxonEventInvokeSet {
    map: HashMap<u32, AxonEventInvoke>,
}

impl AxonEventInvokeSet {
    pub fn invoke(&self, raw: &[u8], commands: &mut Commands<'_, '_>) {
        let mut dec = crate::sbin::SbinDeserializer::from_bytes(raw);
        loop {
            let act: Result<u8, _> = serde::Deserialize::deserialize(&mut dec);
            if let Ok(act) = act {
                let id: Result<u64, _> = serde::Deserialize::deserialize(&mut dec);
                if let Ok(_) = id {
                    let t: Result<u32, _> = serde::Deserialize::deserialize(&mut dec);
                    if let Ok(t) = t {
                        let v: Result<Vec<u8>, _> = serde::Deserialize::deserialize(&mut dec);
                        if let Ok(v) = v {
                            if act == ACTION_TYPE_INVOKE {
                                if let Some(invoke) = self.map.get(&t) {
                                    invoke(&v, commands);
                                }
                            }
                        } else {
                            break;
                        }
                    } else {
                        break;
                    }
                } else {
                    break;
                }
            } else {
                break;
            }
        }
    }
}

#[derive(Default)]
pub struct AxonPlugin;

impl Plugin for AxonPlugin {
    fn build(&self, app: &mut App) {
        app.init_resource::<AxonEventInvokeSet>();
        app.configure_sets(
            PostUpdate,
            (
                AxonSystemSet::Spawn,
                AxonSystemSet::Change,
                AxonSystemSet::Despawn,
            )
                .chain(), // 关键
        );
    }
}

pub trait AppAxon {
    fn add_axon_event<T: AxonEvent + Event>(&mut self);
    fn add_axon_object<T: AxonObject + Component>(&mut self);
    fn add_axon_variant<T: AxonVariant + Component + Serialize>(&mut self);
    fn send_axon_client_event<T: AxonEvent + Serialize>(
        &mut self,
        id: u64,
        event: &T,
        client_id: u64,
    );
    fn broadcast_axon_client_event<T: AxonEvent + Serialize>(&mut self, id: u64, event: &T);
}

impl AppAxon for App {
    fn add_axon_event<T: AxonEvent + Event>(&mut self) {
        let type_id = T::axon_event_type();
        let invoke = T::axon_event_invoke as AxonEventInvoke;
        self.world_mut()
            .resource_mut::<AxonEventInvokeSet>()
            .map
            .insert(type_id, invoke);
    }
    fn add_axon_object<T: AxonObject + Component>(&mut self) {
        self.add_systems(
            PostUpdate,
            (
                reg_object_add::<T>.in_set(AxonSystemSet::Spawn),
                reg_object_removed::<T>.in_set(AxonSystemSet::Despawn),
            ),
        );
    }
    fn add_axon_variant<T: AxonVariant + Component + Serialize>(&mut self) {
        self.add_systems(
            PostUpdate,
            reg_variant_change::<T>.in_set(AxonSystemSet::Change),
        );
    }
    fn send_axon_client_event<T: AxonEvent + Serialize>(
        &mut self,
        id: u64,
        event: &T,
        client_id: u64,
    ) {
        let type_id = T::axon_event_type();
        let j = crate::sbin::to_bytes(event).unwrap();
        self.world_mut().commands().trigger(AxonActionEvent {
            act: ACTION_TYPE_INVOKE,
            id: id,
            t: type_id,
            v: j,
            client_id: client_id,
        });
    }
    fn broadcast_axon_client_event<T: AxonEvent + Serialize>(&mut self, id: u64, event: &T) {
        let type_id = T::axon_event_type();
        let j = crate::sbin::to_bytes(event).unwrap();
        self.world_mut().commands().trigger(AxonActionEvent {
            act: ACTION_TYPE_INVOKE,
            id: id,
            t: type_id,
            v: j,
            client_id: 0,
        });
    }
}

#[inline]
pub fn parse_u64(bytes: &[u8]) -> u64 {
    let mut n = 0u64;
    for &b in bytes {
        if b.is_ascii_digit() {
            n = n * 10 + (b - b'0') as u64;
        }
    }
    n
}

#[inline]
pub fn parse_u32(bytes: &[u8]) -> u32 {
    parse_u64(bytes) as u32
}

#[derive(Debug, Hash, PartialEq, Eq, Clone, SystemSet)]
pub enum AxonSystemSet {
    Spawn,
    Change,
    Despawn,
}

fn reg_object_add<E: AxonObject + Component>(
    query: Query<Entity, Added<E>>,
    mut commands: Commands<'_, '_>,
) {
    for entity in query.iter() {
        let id: u64 = entity.to_bits();
        let t = E::axon_object_type();
        commands.trigger(AxonActionEvent {
            act: ACTION_TYPE_SPAWN,
            id,
            t,
            v: Vec::new(),
            client_id: 0,
        });
        println!("spawn: {}, {}", id, t);
    }
}

fn reg_object_removed<E: AxonObject + Component>(
    mut removed: RemovedComponents<E>,
    mut commands: Commands<'_, '_>,
) {
    for entity in removed.read() {
        let id: u64 = entity.to_bits();
        commands.trigger(AxonActionEvent {
            act: ACTION_TYPE_DESPAWN,
            id,
            t: E::axon_object_type(),
            v: Vec::new(),
            client_id: 0,
        });
        println!("despawn: {}, {}", id, E::axon_object_type());
    }
}

fn reg_variant_change<V: AxonVariant + Component + Serialize>(
    changed: Query<(Entity, &V), Changed<V>>,
    mut commands: Commands<'_, '_>,
) {
    for (entity, variant) in changed.iter() {
        let id: u64 = entity.to_bits();
        let t = V::axon_variant_type();
        let j = crate::sbin::to_bytes(variant).unwrap();
        commands.trigger(AxonActionEvent {
            act: ACTION_TYPE_CHANGE,
            id,
            t,
            v: j,
            client_id: 0,
        });
        println!("change: {}, {}", id, t);
    }
}
