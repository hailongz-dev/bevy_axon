use renet::{ConnectionConfig, DefaultChannel, RenetClient};
use renet_netcode::{ClientAuthentication, NetcodeClientTransport};
use std::ffi::{c_char, c_float, c_int, c_uchar, CStr};
use std::net::UdpSocket;
use std::ptr::addr_of_mut;
use std::time::SystemTime;

/// 错误消息缓冲区大小
const ERROR_BUF_SIZE: usize = 512;

/// 全局固定错误消息缓冲区
static mut ERROR_BUF: [c_char; ERROR_BUF_SIZE] = [0; ERROR_BUF_SIZE];

/// 设置错误消息到固定缓冲区
fn set_error(msg: &str) {
    unsafe {
        // 清空缓冲区
        ERROR_BUF = [0; ERROR_BUF_SIZE];
        // 复制消息到缓冲区（确保不越界）
        let bytes = msg.as_bytes();
        let len = bytes.len().min(ERROR_BUF_SIZE - 1);
        for i in 0..len {
            ERROR_BUF[i] = bytes[i] as c_char;
        }
        // 确保以 null 结尾
        ERROR_BUF[len] = 0;
    }
}

/// 清除错误消息
fn clear_error() {
    unsafe {
        ERROR_BUF = [0; ERROR_BUF_SIZE];
    }
}

pub struct Game {
    buf: Vec<u8>,
    client: RenetClient,
    transport: NetcodeClientTransport,
}

/// 获取最后一次错误消息
/// 返回: 指向错误消息字符串的指针（以 null 结尾的 C 字符串）
/// 如果错误消息为空，返回 null
/// 注意: 返回的指针指向全局固定缓冲区，调用者不应释放
#[no_mangle]
pub extern "C" fn bevy_axon_ffi_errmsg() -> *const c_char {
    unsafe {
        let ptr = addr_of_mut!(ERROR_BUF);
        if (*ptr)[0] == 0 {
            std::ptr::null()
        } else {
            (*ptr).as_mut_ptr()
        }
    }
}

/// 创建游戏实例，返回 Game 指针（C# 用 IntPtr 接收）
/// addr: 服务器地址字符串（以 null 结尾的 C 字符串）
/// 失败返回 null (0)
#[no_mangle]
pub extern "C" fn bevy_axon_ffi_create(addr: *const c_char) -> *mut Game {
    if addr.is_null() {
        let msg = "[bevy_axon_ffi_create] error: addr is null";
        println!("{}", msg);
        set_error(msg);
        return std::ptr::null_mut();
    }

    let addr_str = unsafe {
        match CStr::from_ptr(addr).to_str() {
            Ok(s) => s,
            Err(e) => {
                let msg = format!(
                    "[bevy_axon_ffi_create] error: invalid utf8 string: {:?}",
                    e
                );
                println!("{}", msg);
                set_error(&msg);
                return std::ptr::null_mut();
            }
        }
    };

    let client = RenetClient::new(ConnectionConfig::default());

    let socket = match UdpSocket::bind("127.0.0.1:0") {
        Ok(s) => s,
        Err(e) => {
            let msg = format!(
                "[bevy_axon_ffi_create] error: failed to bind udp socket: {:?}",
                e
            );
            println!("{}", msg);
            set_error(&msg);
            return std::ptr::null_mut();
        }
    };

    if let Err(e) = socket.set_nonblocking(true) {
        let msg = format!(
            "[bevy_axon_ffi_create] error: failed to set nonblocking: {:?}",
            e
        );
        println!("{}", msg);
        set_error(&msg);
        return std::ptr::null_mut();
    }

    let current_time = match SystemTime::now().duration_since(SystemTime::UNIX_EPOCH) {
        Ok(t) => t,
        Err(e) => {
            let msg = format!("[bevy_axon_ffi_create] error: system time error: {:?}", e);
            println!("{}", msg);
            set_error(&msg);
            return std::ptr::null_mut();
        }
    };

    let server_addr = match addr_str.parse() {
        Ok(addr) => addr,
        Err(e) => {
            let msg = format!(
                "[bevy_axon_ffi_create] error: failed to parse server address '{}': {:?}",
                addr_str, e
            );
            println!("{}", msg);
            set_error(&msg);
            return std::ptr::null_mut();
        }
    };

    let authentication = ClientAuthentication::Unsecure {
        server_addr,
        client_id: current_time.as_millis() as u64,
        user_data: None,
        protocol_id: 0,
    };

    let transport = match NetcodeClientTransport::new(current_time, authentication, socket) {
        Ok(t) => t,
        Err(e) => {
            let msg = format!(
                "[bevy_axon_ffi_create] error: failed to create transport: {:?}",
                e
            );
            println!("{}", msg);
            set_error(&msg);
            return std::ptr::null_mut();
        }
    };

    // 清除之前的错误
    clear_error();

    let game = Box::new(Game {
        buf: Vec::new(),
        client,
        transport,
    });

    Box::into_raw(game)
}

/// 退出游戏实例
/// ptr: Game 指针（从 create 返回）
#[no_mangle]
pub extern "C" fn bevy_axon_ffi_exit(ptr: *mut Game) {
    if ptr.is_null() {
        return;
    }
    unsafe {
        // 释放 Game 内存
        let _ = Box::from_raw(ptr);
    }
}

/// 检查游戏实例是否已连接
/// 返回: 1 表示已连接，0 表示未连接
#[no_mangle]
pub extern "C" fn bevy_axon_ffi_is_connected(ptr: *const Game) -> c_int {
    if ptr.is_null() {
        return 0;
    }
    let game = unsafe { &*ptr };
    if game.client.is_connected() {
        1
    } else {
        0
    }
}

/// 更新游戏实例
/// dt: 时间增量（秒）
/// out_len: 输出数据长度指针
/// 返回: 指向输出数据的指针（字节数组，C# 需立即复制）
#[no_mangle]
pub extern "C" fn bevy_axon_ffi_update(
    ptr: *mut Game,
    dt: c_float,
    out_len: *mut usize,
) -> *const u8 {
    if ptr.is_null() {
        if !out_len.is_null() {
            unsafe {
                *out_len = 0;
            }
        }
        return std::ptr::null();
    }

    let game = unsafe { &mut *ptr };
    game.buf.clear();

    let duration = std::time::Duration::from_secs_f32(dt);

    game.client.update(duration);

    if let Err(e) = game.transport.update(duration, &mut game.client) {
        let msg = format!("[bevy_axon_ffi_update] transport update error: {:?}", e);
        println!("{}", msg);
        set_error(&msg);
    }

    if game.client.is_connected() {
        while let Some(message) = game.client.receive_message(DefaultChannel::ReliableOrdered) {
            game.buf.extend_from_slice(&message);
        }

        if let Err(e) = game.transport.send_packets(&mut game.client) {
            let msg = format!(
                "[bevy_axon_ffi_update] transport send_packets error: {:?}",
                e
            );
            println!("{}", msg);
            set_error(&msg);
        }
    }

    if !out_len.is_null() {
        unsafe {
            *out_len = game.buf.len();
        }
    }

    game.buf.as_ptr()
}

/// 调用游戏实例的方法
/// raw: 输入数据指针
/// raw_len: 输入数据长度
#[no_mangle]
pub extern "C" fn bevy_axon_ffi_invoke(ptr: *mut Game, raw: *const c_uchar, raw_len: usize) {
    if ptr.is_null() || raw.is_null() || raw_len <= 0 {
        return;
    }

    let game = unsafe { &mut *ptr };
    let data = unsafe { std::slice::from_raw_parts(raw, raw_len) };

    game.client
        .send_message(DefaultChannel::ReliableOrdered, data);
}
