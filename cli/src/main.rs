mod connect;
mod metadata;

use clap::{Parser, Subcommand};

#[derive(Parser)]
#[command(name = "axon")]
#[command(about = "Bevy Axon CLI tool for remote connection and metadata extraction")]
struct Cli {
    #[command(subcommand)]
    command: Commands,
}

#[derive(Subcommand)]
enum Commands {
    /// Connect to a remote Bevy ECS server
    Connect {
        /// Server address (e.g., 127.0.0.1:7777)
        #[arg(default_value = "127.0.0.1:7777")]
        addr: String,
    },
    /// Extract metadata from source directory
    Metadata {
        /// Source directory path
        #[arg(default_value = "src")]
        src: String,
        /// Output file path
        #[arg(default_value = "metadata.json")]
        output: String,
    },
}

fn main() {
    let cli = Cli::parse();

    match cli.command {
        Commands::Connect { addr } => {
            connect::run(&addr);
        }
        Commands::Metadata { src, output } => {
            metadata::run(&src, &output);
        }
    }
}
