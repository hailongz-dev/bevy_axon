use renet::{ConnectionConfig, DefaultChannel, RenetClient};
use renet_netcode::{ClientAuthentication, NetcodeClientTransport};
use std::net::UdpSocket;
use std::time::{Duration, Instant, SystemTime};

fn on_raw_data(bytes: &[u8]) {
    print!("{}", std::str::from_utf8(bytes).unwrap());
}

fn main() {
    let server_addr = "127.0.0.1:7777".parse().unwrap();
    let socket = UdpSocket::bind("127.0.0.1:0").unwrap();
    socket.set_nonblocking(true).unwrap();

    let mut client = RenetClient::new(ConnectionConfig::default());

    let current_time = SystemTime::now()
        .duration_since(SystemTime::UNIX_EPOCH)
        .unwrap();
    let authentication = ClientAuthentication::Unsecure {
        server_addr,
        client_id: 0,
        user_data: None,
        protocol_id: 0,
    };

    let mut transport = NetcodeClientTransport::new(current_time, authentication, socket).unwrap();

    let delta = Duration::from_secs_f32(1.0 / 30.0);

    loop {
        let frame_start = Instant::now();

        client.update(delta);
        transport.update(delta, &mut client).unwrap();

        if client.is_connected() {
            while let Some(message) = client.receive_message(DefaultChannel::ReliableOrdered) {
                on_raw_data(&message);
            }

            while let Some(message) = client.receive_message(DefaultChannel::ReliableUnordered) {
                on_raw_data(&message);
            }

            while let Some(message) = client.receive_message(DefaultChannel::Unreliable) {
                on_raw_data(&message);
            }

            transport.send_packets(&mut client).unwrap();
        }

        let elapsed = frame_start.elapsed();

        if elapsed < delta {
            std::thread::sleep(delta - elapsed);
        }
    }
}
