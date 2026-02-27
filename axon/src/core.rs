use bevy::prelude::*;
use memchr::memchr;
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
}

#[derive(Event)]
pub struct AxonExitEvent;

#[derive(Component)]
pub struct AxonClient {
    pub id: u64,
    pub connected: bool,
}

pub type AxonEventInvoke = fn(&[u8], &mut Commands<'_, '_>);

#[derive(Resource, Default)]
pub struct AxonEventInvokeSet {
    map: HashMap<u32, AxonEventInvoke>,
}

impl AxonEventInvokeSet {
    pub fn invoke(&self, raw: &[u8], commands: &mut Commands<'_, '_>) {
        let mut i = 0;

        while i < raw.len() {
            // 找第一行
            let Some(header_end_rel) = memchr(b'\n', &raw[i..]) else {
                break;
            };
            let header_end = i + header_end_rel;

            let header = &raw[i..header_end];
            i = header_end + 1;

            // 解析 header: "type,t"
            let mut split = header.split(|&b| b == b',');

            let Some(action_bytes) = split.next() else {
                continue;
            };
            let Some(t_bytes) = split.next() else {
                continue;
            };

            // 直接从 bytes 解析整数（避免 UTF8 + split）
            let action = parse_u64(action_bytes) as u8;
            let t = parse_u32(t_bytes);

            if action != ACTION_TYPE_INVOKE {
                // 跳过下一行
                if let Some(data_end_rel) = memchr(b'\n', &raw[i..]) {
                    i += data_end_rel + 1;
                }
                continue;
            }

            // 找数据行
            let Some(data_end_rel) = memchr(b'\n', &raw[i..]) else {
                break;
            };
            let data_end = i + data_end_rel;

            if let Some(invoke) = self.map.get(&t) {
                invoke(&raw[i..data_end], commands);
            }
            i = data_end + 1;
        }
    }
}

#[derive(Default)]
pub struct AxonPlugin;

impl Plugin for AxonPlugin {
    fn build(&self, app: &mut App) {
        app.init_resource::<AxonEventInvokeSet>();
    }
}

pub trait AppAxon {
    fn add_axon_event<T: AxonEvent + Event>(&mut self);
    fn add_axon_object<T: AxonObject + Component>(&mut self);
    fn add_axon_variant<T: AxonVariant + Component + Serialize>(&mut self);
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
            Update,
            (
                reg_object_add::<T>.in_set(AxonSystemSet::Spawn),
                reg_object_removed::<T>
                    .in_set(AxonSystemSet::Despawn)
                    .after(AxonSystemSet::Change),
            ),
        );
    }
    fn add_axon_variant<T: AxonVariant + Component + Serialize>(&mut self) {
        self.add_systems(
            Update,
            reg_variant_change::<T>
                .in_set(AxonSystemSet::Change)
                .after(AxonSystemSet::Spawn),
        );
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
        let j = serde_json::to_vec(variant).unwrap();
        commands.trigger(AxonActionEvent {
            act: ACTION_TYPE_CHANGE,
            id,
            t,
            v: j,
        });
        println!("change: {}, {}", id, t);
    }
}
