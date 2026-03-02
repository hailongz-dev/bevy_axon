use bevy::prelude::*;
use bevy_axon::core::*;
use bevy_axon_derive::*;
use serde::{Deserialize, Serialize};
use serde_bytes::ByteBuf;

#[derive(Serialize, Deserialize, Default, Clone, Debug)]
pub struct Layer {
    pub index: i32,
    pub tiles: ByteBuf,
}

#[derive(Component, Serialize, Deserialize, AxonVariant, Default, Debug, Clone)]
pub struct Tilemap {
    pub width: i32,
    pub height: i32,
    pub size: f32,
    pub skin: Vec<u32>,
    pub layers: Vec<Layer>,
}

#[derive(Serialize, Deserialize, Default, Debug, Clone)]
pub struct TilePos {
    pub x: i32,
    pub y: i32,
}

/// 找出所有连续 tile 矩形，tile 值等于 `tile`
/// 返回 (左上, 右下) 坐标列表
pub fn find_solid_rectangles(
    tiles: &[u8],
    width: i32,
    height: i32,
    tile: u8,
    rects: &mut Vec<(TilePos, TilePos)>,
) {
    if tiles.is_empty() || width <= 0 || height <= 0 {
        return;
    }

    // 转成二维矩阵，0 = 非 tile，1 = tile
    let mut mat = vec![vec![0i32; width as usize]; height as usize];
    for y in 0..height as usize {
        for x in 0..width as usize {
            let idx = y * width as usize + x;
            mat[y][x] = if tiles[idx] == tile { 1 } else { 0 };
        }
    }

    let mut visited = vec![vec![false; width as usize]; height as usize];

    for y in 0..height as usize {
        let mut x = 0;
        while x < width as usize {
            if mat[y][x] == 1 && !visited[y][x] {
                // 找到新矩形的左上角
                let mut rect_w = 1;
                let mut rect_h = 1;

                // 向右扩展宽度
                while x + rect_w < width as usize
                    && mat[y][x + rect_w] == 1
                    && !visited[y][x + rect_w]
                {
                    rect_w += 1;
                }

                // 向下扩展高度
                'outer: while y + rect_h < height as usize {
                    for i in 0..rect_w {
                        if mat[y + rect_h][x + i] != 1 || visited[y + rect_h][x + i] {
                            break 'outer;
                        }
                    }
                    rect_h += 1;
                }

                // 标记矩形覆盖区域
                for dy in 0..rect_h {
                    for dx in 0..rect_w {
                        visited[y + dy][x + dx] = true;
                    }
                }

                // 保存矩形
                rects.push((
                    TilePos {
                        x: x as i32,
                        y: y as i32,
                    },
                    TilePos {
                        x: (x + rect_w - 1) as i32,
                        y: (y + rect_h - 1) as i32,
                    },
                ));

                x += rect_w; // 跳过已处理的宽度
            } else {
                x += 1;
            }
        }
    }
}

pub fn run(app: &mut App) {
    app.add_axon_variant::<Tilemap>();
}
