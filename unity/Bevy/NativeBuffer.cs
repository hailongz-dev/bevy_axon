using System;

namespace Bevy
{
    class NativeBuffer
    {
        private byte[] _buffer;

        public NativeBuffer(int initialCapacity)
        {
            _buffer = new byte[initialCapacity];
        }

        public int Length { get; private set; } = 0;

        // 写入数据
        public void Write(byte[] data, int offset = 0, int count = -1)
        {
            if (count < 0) count = data.Length - offset;
            EnsureCapacity(Length + count);
            Array.Copy(data, offset, _buffer, Length, count);
            Length += count;
        }

        public void EnsureCapacity(int capacity)
        {
            if (capacity > _buffer.Length)
            {
                Array.Resize(ref _buffer, Math.Max(_buffer.Length * 2, capacity));
            }
        }


        // 访问内部数组（安全）
        public byte[] Buffer => _buffer;

        // 重置缓冲区
        public void Clear()
        {
            Length = 0;
        }
    }
}