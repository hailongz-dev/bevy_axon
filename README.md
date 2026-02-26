# bevy_axon

[![Crates.io](https://img.shields.io/crates/v/bevy_axon)](https://crates.io/crates/bevy_axon)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![Rust](https://img.shields.io/badge/rust-2021-orange.svg)](https://www.rust-lang.org/)

bevy_axon enables interoperability between Rust [Bevy ECS](https://bevyengine.org/) and major game engines like Unity, Godot, and Unreal, allowing developers to share ECS logic and components across platforms.

## Overview

bevy_axon provides a bridge between Bevy's powerful ECS system and other game engines through:

- **Network Synchronization**: Real-time state replication between Bevy server and game engine clients
- **FFI Interface**: C-compatible API for seamless integration with Unity (C#), Godot (GDScript/C#), and Unreal (C++)
- **Derive Macros**: Convenient macros for marking components, variants, and events
- **Type-Safe Communication**: JSON-based serialization with type IDs for safe cross-platform data exchange

## Architecture

```
┌─────────────────┐     Network      ┌─────────────────┐
│   Unity/Godot   │ ◄──────────────► │   Bevy Axon     │
│   /Unreal       │   (renet)        │   Server        │
│   (FFI Client)  │                  │   (ECS Logic)   │
└─────────────────┘                  └─────────────────┘
```

## Unity Integration (Netcode-like API)

bevy_axon provides a Unity API similar to Unity Netcode for GameObjects, making it easy for Unity developers to get started:

| Unity Netcode | bevy_axon Unity | Description |
|--------------|-----------------|-------------|
| `NetworkManager` | `BevyClient` | Manages connection to Bevy server |
| `NetworkObject` | `BevyObject` | Represents a synchronized entity |
| `NetworkBehaviour` | `BevyBehaviour` | Base class for synchronized components |

### Quick Start for Unity

#### 1. Create a BevyClient (like NetworkManager)

```csharp
using Bevy;
using UnityEngine;

public class GameClient : MonoBehaviour
{
    public BevyClient bevyClient;
    
    void Start()
    {
        // Connect to Bevy server
        bevyClient.Connect("127.0.0.1:7777");
    }
    
    void OnDestroy()
    {
        bevyClient.Disconnect();
    }
}
```

#### 2. Create a BevyObject Prefab (like NetworkObject)

```csharp
using Bevy;
using UnityEngine;

public class PlayerObject : BevyBehaviour
{
    [BevyVariant(1)]  // type_id must match Bevy server
    public Vector3 position;
    
    [BevyVariant(2)]
    public float rotation;
    
    void Update()
    {
        transform.position = position;
        transform.rotation = Quaternion.Euler(0, rotation, 0);
    }
}
```

#### 3. Send Events to Server

```csharp
using Bevy;
using UnityEngine;

[BevyEvent(3)]  // type_id must match Bevy server
public class MoveInput
{
    public float x;
    public float y;
}

public class PlayerController : BevyBehaviour
{
    void Update()
    {
        // Send event to Bevy server
        Invoke(new MoveInput 
        { 
            x = Input.GetAxis("Horizontal"), 
            y = Input.GetAxis("Vertical") 
        });
    }
}
```

## Features

- **Component Synchronization**: Automatically sync component spawns, despawns, and changes
- **Event System**: Bidirectional event communication between client and server
- **Type Safety**: Compile-time type IDs ensure safe cross-platform type matching
- **High Performance**: Zero-copy where possible, efficient binary protocol
- **Multi-Engine Support**: FFI bindings for Unity, Godot, and Unreal Engine

## Installation

### Rust (Server)

Add to your `Cargo.toml`:

```toml
[dependencies]
bevy_axon = "0.1.0"
bevy_axon_derive = "0.1.0"
```

Or use the CLI tool:

```bash
cargo install bevy_axon_cli
```

### Unity (Client)

1. Copy the `unity/Bevy` folder to your Unity project's `Assets` directory
2. Build the `bevy_axon` native library and place it in `Assets/Plugins`

## Server Setup (Rust/Bevy)

### 1. Define Your Components

```rust
use bevy::prelude::*;
use bevy_axon::core::*;
use bevy_axon_derive::*;
use serde::{Serialize, Deserialize};

// Object component (syncs spawn/despawn) - corresponds to BevyObject
#[derive(Component, AxonObject)]
#[type_id = 1]
struct Player {
    id: u64,
}

// Variant component (syncs value changes) - corresponds to BevyBehaviour fields
#[derive(Component, AxonVariant, Serialize)]
#[type_id = 2]
struct Position {
    x: f32,
    y: f32,
    r: f32,
}

// Event (bidirectional communication) - corresponds to BevyEvent
#[derive(Event, AxonEvent, Deserialize)]
#[type_id = 3]
struct MoveEvent {
    x: f32,
    y: f32,
    r: f32,
}
```

### 2. Setup Server

```rust
use bevy::prelude::*;
use bevy_axon::core::*;
use bevy_axon::server::*;

fn main() {
    let mut app = App::new();
    
    // Add plugins
    app.add_plugins(MinimalPlugins);
    app.add_plugins(AxonPlugin::default());
    app.add_plugins(AxonServerPlugin::default());
    app.add_plugins((RenetServerPlugin, NetcodeServerPlugin));
    
    // Register types
    app.add_axon_object::<Player>();
    app.add_axon_variant::<Position>();
    app.add_axon_event::<MoveEvent>();
    
    // Start server
    app.start_server("127.0.0.1:7777");
    app.run();
}
```

## Project Structure

```
bevy_axon/
├── axon/           # Core library (Rust)
│   ├── src/
│   │   ├── lib.rs      # Library entry
│   │   ├── core.rs     # Core ECS traits and systems
│   │   ├── server.rs   # Network server implementation
│   │   └── ffi.rs      # FFI bindings for external engines
│   └── Cargo.toml
├── derive/         # Procedural macros
│   └── src/lib.rs      # AxonObject, AxonVariant, AxonEvent derives
├── cli/            # CLI tool
│   └── src/main.rs
├── demo/           # Example server implementation
│   └── src/main.rs
└── unity/          # Unity Client SDK
    └── Bevy/
        ├── BevyClient.cs       # NetworkManager equivalent
        ├── BevyObject.cs       # NetworkObject equivalent
        ├── BevyBehaviour.cs    # NetworkBehaviour equivalent
        ├── BevyVariantAttribute.cs
        └── BevyEventAttribute.cs
```

## Protocol

bevy_axon uses a simple text-based protocol over reliable ordered channels:

```
action,type_id
json_data

Actions:
- 1: Spawn
- 2: Despawn
- 3: Change
- 4: Invoke (event)
```

## Feature Flags

- `server` - Enable server-side Bevy integration (requires bevy, bevy_renet)
- `ffi` - Enable FFI bindings for external clients (requires renet, renet_netcode)

## Supported Engines

| Engine | Status | Language | Notes |
|--------|--------|----------|-------|
| Unity | ✅ Supported | C# | Netcode-like API |
| Godot | ✅ Supported | C#/GDScript | Via FFI or GDExtension |
| Unreal | 🚧 Planned | C++ | FFI bindings |
| Bevy | ✅ Native | Rust | Direct integration |

## License

MIT License - see [LICENSE](LICENSE) for details.

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## Acknowledgments

- Built with [Bevy](https://bevyengine.org/) - A refreshingly simple data-driven game engine
- Networking powered by [renet](https://github.com/lucaspoffo/renet) - Reliable UDP networking library
