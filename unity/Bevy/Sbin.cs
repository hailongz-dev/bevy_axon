using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

namespace Bevy
{
    /// <summary>
    /// Sbin 数据类型枚举
    /// </summary>
    public enum SbinType : byte
    {
        Nil = 0,
        U8 = 1,
        U16 = 2,
        U32 = 3,
        U64 = 4,
        I8 = 5,
        I16 = 6,
        I32 = 7,
        I64 = 8,
        F32 = 9,
        F64 = 10,
        Bool = 11,
        Str = 12,
        Bytes = 13,
        Array = 14,
        Object = 15,
        End = 16,
    }

    /// <summary>
    /// Sbin 编码器 - 将数据写入字节流
    /// </summary>
    public class SbinWriter
    {
        private readonly MemoryStream _stream;
        private readonly BinaryWriter _writer;

        public SbinWriter()
        {
            _stream = new MemoryStream();
            _writer = new BinaryWriter(_stream);
        }

        public SbinWriter(MemoryStream s)
        {
            _stream = s;
            _writer = new BinaryWriter(_stream);
        }

        public SbinWriter(int capacity)
        {
            _stream = new MemoryStream(capacity);
            _writer = new BinaryWriter(_stream);
        }

        /// <summary>
        /// 获取编码后的字节数组
        /// </summary>
        public byte[] ToArray()
        {
            return _stream.ToArray();
        }

        /// <summary>
        /// 获取当前写入位置
        /// </summary>
        public long Position => _stream.Position;

        /// <summary>
        /// 写入类型标记
        /// </summary>
        private void WriteType(SbinType type)
        {
            _writer.Write((byte)type);
        }

        // ==================== 基础类型写入 ====================

        public void WriteNil()
        {
            WriteType(SbinType.Nil);
        }

        public void WriteU8(byte value)
        {
            WriteType(SbinType.U8);
            _writer.Write(value);
        }

        public void WriteU16(ushort value)
        {
            WriteType(SbinType.U16);
            _writer.Write(value);
        }

        private void WriteU32Raw(uint value)
        {
            _writer.Write(value);
        }

        public void WriteU32(uint value)
        {
            WriteType(SbinType.U32);
            _writer.Write(value);
        }

        public void WriteU64(ulong value)
        {
            WriteType(SbinType.U64);
            _writer.Write(value);
        }

        public void WriteI8(sbyte value)
        {
            WriteType(SbinType.I8);
            _writer.Write(value);
        }

        public void WriteI16(short value)
        {
            WriteType(SbinType.I16);
            _writer.Write(value);
        }

        public void WriteI32(int value)
        {
            WriteType(SbinType.I32);
            _writer.Write(value);
        }

        public void WriteI64(long value)
        {
            WriteType(SbinType.I64);
            _writer.Write(value);
        }

        public void WriteF32(float value)
        {
            WriteType(SbinType.F32);
            _writer.Write(value);
        }

        public void WriteF64(double value)
        {
            WriteType(SbinType.F64);
            _writer.Write(value);
        }

        public void WriteBool(bool value)
        {
            WriteType(SbinType.Bool);
            _writer.Write(value ? (byte)1 : (byte)0);
        }

        public void WriteStr(string value)
        {
            WriteType(SbinType.Str);
            var bytes = Encoding.UTF8.GetBytes(value);
            WriteU32Raw((uint)bytes.Length);
            _writer.Write(bytes);
            _writer.Write((byte)0);
        }

        // ==================== 字节数组写入 ====================

        public void WriteBytes(byte[] value)
        {
            WriteType(SbinType.Bytes);
            WriteU32Raw((uint)value.Length);
            _writer.Write(value);
        }

        // ==================== 动态数组写入 ====================

        /// <summary>
        /// 开始写入数组 (动态类型数组)
        /// </summary>
        public void BeginArray()
        {
            WriteType(SbinType.Array);
        }

        /// <summary>
        /// 结束数组写入
        /// </summary>
        public void EndArray()
        {
            WriteType(SbinType.End);
        }

        /// <summary>
        /// 开始写入对象
        /// </summary>
        public void BeginObject()
        {
            WriteType(SbinType.Object);
        }

        /// <summary>
        /// 结束对象写入
        /// </summary>
        public void EndObject()
        {
            WriteType(SbinType.End);
        }

        /// <summary>
        /// 写入对象键 (字符串类型)
        /// </summary>
        public void WriteKey(string key)
        {
            WriteStr(key);
        }

        // ==================== 泛型写入方法 ====================

        public void WriteValue(object value)
        {
            if (value == null)
            {
                WriteNil();
                return;
            }

            switch (value)
            {
                case byte v: WriteU8(v); break;
                case ushort v: WriteU16(v); break;
                case uint v: WriteU32(v); break;
                case ulong v: WriteU64(v); break;
                case sbyte v: WriteI8(v); break;
                case short v: WriteI16(v); break;
                case int v: WriteI32(v); break;
                case long v: WriteI64(v); break;
                case float v: WriteF32(v); break;
                case double v: WriteF64(v); break;
                case bool v: WriteBool(v); break;
                case string v: WriteStr(v); break;
                case byte[] v: WriteBytes(v); break;
                default:
                    throw new ArgumentException($"Unsupported type: {value.GetType()}");
            }
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            _writer?.Dispose();
            _stream?.Dispose();
        }
    }

    /// <summary>
    /// Sbin 解码器 - 从字节流读取数据
    /// </summary>
    public class SbinReader
    {
        private readonly byte[] _data;
        private readonly int _offset;
        private readonly int _count;
        private int _position;

        /// <summary>
        /// 从字节数组创建读取器
        /// </summary>
        public SbinReader(byte[] data)
        {
            _data = data ?? throw new ArgumentNullException(nameof(data));
            _offset = 0;
            _count = data.Length;
            _position = 0;
        }

        /// <summary>
        /// 从 ArraySegment 创建读取器
        /// </summary>
        public SbinReader(ArraySegment<byte> segment)
        {
            _data = segment.Array ?? throw new ArgumentNullException(nameof(segment));
            _offset = segment.Offset;
            _count = segment.Count;
            _position = 0;
        }

        /// <summary>
        /// 当前读取位置（相对于 segment 起始位置）
        /// </summary>
        public int Position => _position;

        /// <summary>
        /// 剩余字节数
        /// </summary>
        public int Remaining => _count - _position;

        /// <summary>
        /// 数据总长度
        /// </summary>
        public int Length => _count;

        /// <summary>
        /// 获取实际的数据索引
        /// </summary>
        private int DataIndex => _offset + _position;

        /// <summary>
        /// 读取下一个字节
        /// </summary>
        private byte ReadByte()
        {
            if (_position >= _count)
                throw new EndOfStreamException("Unexpected end of stream");
            return _data[_offset + _position++];
        }

        /// <summary>
        /// 读取类型标记
        /// </summary>
        public SbinType ReadType()
        {
            var type = (SbinType)ReadByte();
            if ((byte)type > 27)
                throw new InvalidDataException($"Invalid type: {(byte)type}");
            return type;
        }

        /// <summary>
        /// 查看下一个字节但不移动位置
        /// </summary>
        public byte PeekByte()
        {
            if (_position >= _count)
                throw new EndOfStreamException("Unexpected end of stream");
            return _data[_offset + _position];
        }


        /// <summary>
        /// 读取指定长度的字节数组
        /// </summary>
        private byte[] ReadBytes(int length)
        {
            if (_position + length > _count)
                throw new EndOfStreamException("Unexpected end of stream");
            var result = new byte[length];
            Array.Copy(_data, _offset + _position, result, 0, length);
            _position += length;
            return result;
        }

        // ==================== 基础类型读取 ====================

        public void ReadNil()
        {
            var type = ReadType();
            if (type != SbinType.Nil)
                throw new InvalidDataException($"Expected Nil, got {type}");
        }

        public byte ReadU8()
        {
            var type = ReadType();
            if (type != SbinType.U8)
                throw new InvalidDataException($"Expected U8, got {type}");
            return ReadByte();
        }

        public ushort ReadU16()
        {
            var type = ReadType();
            if (type != SbinType.U16)
                throw new InvalidDataException($"Expected U16, got {type}");
            if (_position + 2 > _count)
                throw new EndOfStreamException("Unexpected end of stream");
            var value = BitConverter.ToUInt16(_data, _offset + _position);
            _position += 2;
            return value;
        }

        private uint ReadU32Raw()
        {
            if (_position + 4 > _count)
                throw new EndOfStreamException("Unexpected end of stream");
            var value = BitConverter.ToUInt32(_data, _offset + _position);
            _position += 4;
            return value;
        }

        public uint ReadU32()
        {
            var type = ReadType();
            if (type != SbinType.U32)
                throw new InvalidDataException($"Expected U32, got {type}");
            return ReadU32Raw();
        }

        public ulong ReadU64()
        {
            var type = ReadType();
            if (type != SbinType.U64)
                throw new InvalidDataException($"Expected U64, got {type}");
            if (_position + 8 > _count)
                throw new EndOfStreamException("Unexpected end of stream");
            var value = BitConverter.ToUInt64(_data, _offset + _position);
            _position += 8;
            return value;
        }

        public sbyte ReadI8()
        {
            var type = ReadType();
            if (type != SbinType.I8)
                throw new InvalidDataException($"Expected I8, got {type}");
            return (sbyte)ReadByte();
        }

        public short ReadI16()
        {
            var type = ReadType();
            if (type != SbinType.I16)
                throw new InvalidDataException($"Expected I16, got {type}");
            if (_position + 2 > _count)
                throw new EndOfStreamException("Unexpected end of stream");
            var value = BitConverter.ToInt16(_data, _offset + _position);
            _position += 2;
            return value;
        }

        public int ReadI32()
        {
            var type = ReadType();
            if (type != SbinType.I32)
                throw new InvalidDataException($"Expected I32, got {type}");
            if (_position + 4 > _count)
                throw new EndOfStreamException("Unexpected end of stream");
            var value = BitConverter.ToInt32(_data, _offset + _position);
            _position += 4;
            return value;
        }

        public long ReadI64()
        {
            var type = ReadType();
            if (type != SbinType.I64)
                throw new InvalidDataException($"Expected I64, got {type}");
            if (_position + 8 > _count)
                throw new EndOfStreamException("Unexpected end of stream");
            var value = BitConverter.ToInt64(_data, _offset + _position);
            _position += 8;
            return value;
        }

        public float ReadF32()
        {
            var type = ReadType();
            if (type != SbinType.F32)
                throw new InvalidDataException($"Expected F32, got {type}");
            if (_position + 4 > _count)
                throw new EndOfStreamException("Unexpected end of stream");
            var value = BitConverter.ToSingle(_data, _offset + _position);
            _position += 4;
            return value;
        }

        public double ReadF64()
        {
            var type = ReadType();
            if (type != SbinType.F64)
                throw new InvalidDataException($"Expected F64, got {type}");
            if (_position + 8 > _count)
                throw new EndOfStreamException("Unexpected end of stream");
            var value = BitConverter.ToDouble(_data, _offset + _position);
            _position += 8;
            return value;
        }

        public bool ReadBool()
        {
            var type = ReadType();
            if (type != SbinType.Bool)
                throw new InvalidDataException($"Expected Bool, got {type}");
            return ReadByte() != 0;
        }

        public string ReadStr()
        {
            var type = ReadType();
            if (type != SbinType.Str)
                throw new InvalidDataException($"Expected Str, got {type}");
            var len = (int)ReadU32Raw();
            var bytes = ReadBytes(len);
            _position++;
            return Encoding.UTF8.GetString(bytes);
        }

        // ==================== 字节数组读取 ====================

        public byte[] ReadBytes()
        {
            var type = ReadType();
            if (type != SbinType.Bytes)
                throw new InvalidDataException($"Expected Bytes, got {type}");
            var len = (int)ReadU32Raw();
            return ReadBytes(len);
        }

        // ==================== 动态数组/对象读取 ====================

        /// <summary>
        /// 检查当前位置是否是数组结束标记
        /// </summary>
        public bool IsEnd()
        {
            return PeekByte() == (byte)SbinType.End;
        }

        /// <summary>
        /// 读取数组结束标记
        /// </summary>
        public void ReadEnd()
        {
            var type = ReadType();
            if (type != SbinType.End)
                throw new InvalidDataException($"Expected End, got {type}");
        }

        /// <summary>
        /// 读取数组开始标记
        /// </summary>
        public void ReadArrayBegin()
        {
            var type = ReadType();
            if (type != SbinType.Array)
                throw new InvalidDataException($"Expected Array, got {type}");
        }

        /// <summary>
        /// 读取对象开始标记
        /// </summary>
        public void ReadObjectBegin()
        {
            var type = ReadType();
            if (type != SbinType.Object)
                throw new InvalidDataException($"Expected Object, got {type}");
        }

        /// <summary>
        /// 读取对象键
        /// </summary>
        public string ReadKey()
        {
            return ReadStr();
        }

        /// <summary>
        /// 读取动态值 (根据类型标记自动判断)
        /// </summary>
        public object ReadValue()
        {
            var type = PeekByte();
            switch ((SbinType)type)
            {
                case SbinType.Nil:
                    ReadNil();
                    return null;
                case SbinType.U8: return ReadU8();
                case SbinType.U16: return ReadU16();
                case SbinType.U32: return ReadU32();
                case SbinType.U64: return ReadU64();
                case SbinType.I8: return ReadI8();
                case SbinType.I16: return ReadI16();
                case SbinType.I32: return ReadI32();
                case SbinType.I64: return ReadI64();
                case SbinType.F32: return ReadF32();
                case SbinType.F64: return ReadF64();
                case SbinType.Bool: return ReadBool();
                case SbinType.Str: return ReadStr();
                case SbinType.Bytes: return ReadBytes();
                default:
                    throw new InvalidDataException($"Cannot read dynamic value of type: {(SbinType)type}");
            }
        }
    }

    /// <summary>
    /// Sbin 扩展方法
    /// </summary>
    public static class SbinExtensions
    {
        // ==================== 写入扩展 ====================

        public static void WriteVector2(this SbinWriter writer, UnityEngine.Vector2 v)
        {
            writer.BeginArray();
            writer.WriteF32(v.x);
            writer.WriteF32(v.y);
            writer.EndArray();
        }

        public static void WriteVector3(this SbinWriter writer, UnityEngine.Vector3 v)
        {
            writer.BeginArray();
            writer.WriteF32(v.x);
            writer.WriteF32(v.y);
            writer.WriteF32(v.z);
            writer.EndArray();
        }

        public static void WriteVector4(this SbinWriter writer, UnityEngine.Vector4 v)
        {
            writer.BeginArray();
            writer.WriteF32(v.x);
            writer.WriteF32(v.y);
            writer.WriteF32(v.z);
            writer.WriteF32(v.w);
            writer.EndArray();
        }

        public static void WriteQuaternion(this SbinWriter writer, UnityEngine.Quaternion v)
        {
            writer.BeginArray();
            writer.WriteF32(v.x);
            writer.WriteF32(v.y);
            writer.WriteF32(v.z);
            writer.WriteF32(v.w);
            writer.EndArray();
        }

        public static void WriteColor(this SbinWriter writer, UnityEngine.Color v)
        {
            writer.BeginArray();
            writer.WriteF32(v.r);
            writer.WriteF32(v.g);
            writer.WriteF32(v.b);
            writer.WriteF32(v.a);
            writer.EndArray();
        }

        public static void WriteColor32(this SbinWriter writer, UnityEngine.Color32 v)
        {
            writer.BeginArray();
            writer.WriteU8(v.r);
            writer.WriteU8(v.g);
            writer.WriteU8(v.b);
            writer.WriteU8(v.a);
            writer.EndArray();
        }

        public static void WriteRect(this SbinWriter writer, UnityEngine.Rect v)
        {
            writer.BeginArray();
            writer.WriteF32(v.x);
            writer.WriteF32(v.y);
            writer.WriteF32(v.width);
            writer.WriteF32(v.height);
            writer.EndArray();
        }

        public static void WriteBounds(this SbinWriter writer, UnityEngine.Bounds v)
        {
            writer.BeginArray();
            writer.WriteVector3(v.center);
            writer.WriteVector3(v.size);
            writer.EndArray();
        }

        // ==================== 读取扩展 ====================

        public static UnityEngine.Vector2 ReadVector2(this SbinReader reader)
        {
            reader.ReadArrayBegin();
            var x = reader.ReadF32();
            var y = reader.ReadF32();
            reader.ReadEnd();
            return new UnityEngine.Vector2(x, y);
        }

        public static UnityEngine.Vector3 ReadVector3(this SbinReader reader)
        {
            reader.ReadArrayBegin();
            var x = reader.ReadF32();
            var y = reader.ReadF32();
            var z = reader.ReadF32();
            reader.ReadEnd();
            return new UnityEngine.Vector3(x, y, z);
        }

        public static UnityEngine.Vector4 ReadVector4(this SbinReader reader)
        {
            reader.ReadArrayBegin();
            var x = reader.ReadF32();
            var y = reader.ReadF32();
            var z = reader.ReadF32();
            var w = reader.ReadF32();
            reader.ReadEnd();
            return new UnityEngine.Vector4(x, y, z, w);
        }

        public static UnityEngine.Quaternion ReadQuaternion(this SbinReader reader)
        {
            reader.ReadArrayBegin();
            var x = reader.ReadF32();
            var y = reader.ReadF32();
            var z = reader.ReadF32();
            var w = reader.ReadF32();
            reader.ReadEnd();
            return new UnityEngine.Quaternion(x, y, z, w);
        }

        public static UnityEngine.Color ReadColor(this SbinReader reader)
        {
            reader.ReadArrayBegin();
            var r = reader.ReadF32();
            var g = reader.ReadF32();
            var b = reader.ReadF32();
            var a = reader.ReadF32();
            reader.ReadEnd();
            return new UnityEngine.Color(r, g, b, a);
        }

        public static UnityEngine.Color32 ReadColor32(this SbinReader reader)
        {
            reader.ReadArrayBegin();
            var r = reader.ReadU8();
            var g = reader.ReadU8();
            var b = reader.ReadU8();
            var a = reader.ReadU8();
            reader.ReadEnd();
            return new UnityEngine.Color32(r, g, b, a);
        }

        public static UnityEngine.Rect ReadRect(this SbinReader reader)
        {
            reader.ReadArrayBegin();
            var x = reader.ReadF32();
            var y = reader.ReadF32();
            var width = reader.ReadF32();
            var height = reader.ReadF32();
            reader.ReadEnd();
            return new UnityEngine.Rect(x, y, width, height);
        }

        public static UnityEngine.Bounds ReadBounds(this SbinReader reader)
        {
            reader.ReadArrayBegin();
            var center = reader.ReadVector3();
            var size = reader.ReadVector3();
            reader.ReadEnd();
            return new UnityEngine.Bounds(center, size);
        }

        // ==================== System.Serializable 支持 ====================

        /// <summary>
        /// 序列化支持 [System.Serializable] 特性的对象
        /// </summary>
        public static void WriteSerializable(this SbinWriter writer, object obj)
        {
            if (obj == null)
            {
                writer.WriteNil();
                return;
            }

            var type = obj.GetType();

            // 处理可空类型
            var underlyingType = Nullable.GetUnderlyingType(type);
            if (underlyingType != null)
            {
                if (obj == null)
                {
                    writer.WriteNil();
                    return;
                }

                type = underlyingType;
                obj = Convert.ChangeType(obj, underlyingType);
            }

            // 基础类型直接写入
            if (TryWritePrimitive(writer, obj))
                return;

            // 处理数组
            if (type.IsArray)
            {
                WriteArray(writer, (Array)obj);
                return;
            }

            // 处理泛型列表
            if (typeof(IList).IsAssignableFrom(type) && type.IsGenericType)
            {
                WriteList(writer, (IList)obj);
                return;
            }

            // 处理泛型字典
            if (typeof(IDictionary).IsAssignableFrom(type) && type.IsGenericType)
            {
                WriteDictionary(writer, (IDictionary)obj);
                return;
            }

            // 处理标记了 [Serializable] 的类/结构体
            if (type.IsDefined(typeof(SerializableAttribute), false) || type.IsValueType)
            {
                WriteSerializableObject(writer, obj);
                return;
            }

            throw new ArgumentException($"Type {type} is not supported for serialization");
        }

        /// <summary>
        /// 尝试写入基础类型
        /// </summary>
        private static bool TryWritePrimitive(SbinWriter writer, object obj)
        {
            switch (obj)
            {
                case byte v:
                    writer.WriteU8(v);
                    return true;
                case sbyte v:
                    writer.WriteI8(v);
                    return true;
                case short v:
                    writer.WriteI16(v);
                    return true;
                case ushort v:
                    writer.WriteU16(v);
                    return true;
                case int v:
                    writer.WriteI32(v);
                    return true;
                case uint v:
                    writer.WriteU32(v);
                    return true;
                case long v:
                    writer.WriteI64(v);
                    return true;
                case ulong v:
                    writer.WriteU64(v);
                    return true;
                case float v:
                    writer.WriteF32(v);
                    return true;
                case double v:
                    writer.WriteF64(v);
                    return true;
                case bool v:
                    writer.WriteBool(v);
                    return true;
                case string v:
                    writer.WriteStr(v);
                    return true;
                case byte[] v:
                    writer.WriteBytes(v);
                    return true;
                case UnityEngine.Vector2 v:
                    writer.WriteVector2(v);
                    return true;
                case UnityEngine.Vector3 v:
                    writer.WriteVector3(v);
                    return true;
                case UnityEngine.Vector4 v:
                    writer.WriteVector4(v);
                    return true;
                case UnityEngine.Quaternion v:
                    writer.WriteQuaternion(v);
                    return true;
                case UnityEngine.Color v:
                    writer.WriteColor(v);
                    return true;
                case UnityEngine.Color32 v:
                    writer.WriteColor32(v);
                    return true;
                case UnityEngine.Rect v:
                    writer.WriteRect(v);
                    return true;
                case UnityEngine.Bounds v:
                    writer.WriteBounds(v);
                    return true;
                default: return false;
            }
        }

        /// <summary>
        /// 写入数组
        /// </summary>
        private static void WriteArray(SbinWriter writer, Array array)
        {
            var elementType = array.GetType().GetElementType();

            // 基础类型数组使用优化格式
            if (elementType == typeof(byte))
            {
                writer.WriteBytes((byte[])array);
                return;
            }

            // 其他类型使用动态数组格式
            writer.BeginArray();
            foreach (var item in array)
            {
                writer.WriteSerializable(item);
            }

            writer.EndArray();
        }

        /// <summary>
        /// 写入列表
        /// </summary>
        private static void WriteList(SbinWriter writer, IList list)
        {
            writer.BeginArray();
            foreach (var item in list)
            {
                writer.WriteSerializable(item);
            }

            writer.EndArray();
        }

        /// <summary>
        /// 写入字典
        /// </summary>
        private static void WriteDictionary(SbinWriter writer, IDictionary dict)
        {
            writer.BeginObject();
            foreach (DictionaryEntry entry in dict)
            {
                // 键必须是字符串或可转换为字符串
                string key = entry.Key?.ToString() ?? "";
                writer.WriteKey(key);
                writer.WriteSerializable(entry.Value);
            }

            writer.EndObject();
        }

        /// <summary>
        /// 写入可序列化对象（字段和属性）
        /// </summary>
        private static void WriteSerializableObject(SbinWriter writer, object obj)
        {
            var type = obj.GetType();
            writer.BeginObject();

            // 写入字段
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);
            foreach (var field in fields)
            {
                // 跳过非序列化字段
                if (field.IsDefined(typeof(NonSerializedAttribute), false))
                    continue;

                writer.WriteKey(field.Name);
                var value = field.GetValue(obj);
                writer.WriteSerializable(value);
            }

            // 写入属性（只有公共 getter/setter 的属性）
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (var prop in properties)
            {
                if (!prop.CanRead || !prop.CanWrite)
                    continue;

                // 跳过索引器
                if (prop.GetIndexParameters().Length > 0)
                    continue;

                writer.WriteKey(prop.Name);
                var value = prop.GetValue(obj);
                writer.WriteSerializable(value);
            }

            writer.EndObject();
        }

        // ==================== 反序列化支持 ====================

        /// <summary>
        /// 反序列化到支持 [System.Serializable] 特性的对象
        /// </summary>
        public static T ReadSerializable<T>(this SbinReader reader)
        {
            return (T)ReadSerializable(reader, typeof(T));
        }

        /// <summary>
        /// 反序列化到指定类型
        /// </summary>
        public static object ReadSerializable(this SbinReader reader, Type targetType)
        {
            // 检查是否为可空类型
            var underlyingType = Nullable.GetUnderlyingType(targetType);
            var actualType = underlyingType ?? targetType;

            // 检查是否为 Nil
            if (reader.PeekByte() == (byte)SbinType.Nil)
            {
                reader.ReadNil();
                return null;
            }

            // 基础类型
            if (TryReadPrimitive(reader, actualType, out var primitiveValue))
                return primitiveValue;

            // 数组
            if (actualType.IsArray)
            {
                return ReadArray(reader, actualType);
            }

            // 泛型列表
            if (typeof(IList).IsAssignableFrom(actualType) && actualType.IsGenericType)
            {
                return ReadList(reader, actualType);
            }

            // 泛型字典
            if (typeof(IDictionary).IsAssignableFrom(actualType) && actualType.IsGenericType)
            {
                return ReadDictionary(reader, actualType);
            }

            // 可序列化对象
            if (actualType.IsDefined(typeof(SerializableAttribute), false) || actualType.IsValueType)
            {
                return ReadSerializableObject(reader, actualType);
            }

            throw new ArgumentException($"Type {actualType} is not supported for deserialization");
        }

        /// <summary>
        /// 尝试读取基础类型
        /// </summary>
        private static bool TryReadPrimitive(SbinReader reader, Type type, out object value)
        {
            value = null;

            if (type == typeof(byte))
            {
                value = reader.ReadU8();
                return true;
            }

            if (type == typeof(sbyte))
            {
                value = reader.ReadI8();
                return true;
            }

            if (type == typeof(short))
            {
                value = reader.ReadI16();
                return true;
            }

            if (type == typeof(ushort))
            {
                value = reader.ReadU16();
                return true;
            }

            if (type == typeof(int))
            {
                value = reader.ReadI32();
                return true;
            }

            if (type == typeof(uint))
            {
                value = reader.ReadU32();
                return true;
            }

            if (type == typeof(long))
            {
                value = reader.ReadI64();
                return true;
            }

            if (type == typeof(ulong))
            {
                value = reader.ReadU64();
                return true;
            }

            if (type == typeof(float))
            {
                value = reader.ReadF32();
                return true;
            }

            if (type == typeof(double))
            {
                value = reader.ReadF64();
                return true;
            }

            if (type == typeof(bool))
            {
                value = reader.ReadBool();
                return true;
            }

            if (type == typeof(string))
            {
                value = reader.ReadStr();
                return true;
            }

            if (type == typeof(UnityEngine.Vector2))
            {
                value = reader.ReadVector2();
                return true;
            }

            if (type == typeof(UnityEngine.Vector3))
            {
                value = reader.ReadVector3();
                return true;
            }

            if (type == typeof(UnityEngine.Vector4))
            {
                value = reader.ReadVector4();
                return true;
            }

            if (type == typeof(UnityEngine.Quaternion))
            {
                value = reader.ReadQuaternion();
                return true;
            }

            if (type == typeof(UnityEngine.Color))
            {
                value = reader.ReadColor();
                return true;
            }

            if (type == typeof(UnityEngine.Color32))
            {
                value = reader.ReadColor32();
                return true;
            }

            if (type == typeof(UnityEngine.Rect))
            {
                value = reader.ReadRect();
                return true;
            }

            if (type == typeof(UnityEngine.Bounds))
            {
                value = reader.ReadBounds();
                return true;
            }

            return false;
        }

        /// <summary>
        /// 读取数组
        /// </summary>
        private static object ReadArray(SbinReader reader, Type arrayType)
        {
            var elementType = arrayType.GetElementType();
            var type = reader.ReadType();

            // 优化格式数组
            if (type == SbinType.Bytes && elementType == typeof(byte))
                return reader.ReadBytes();

            // 动态数组格式
            if (type != SbinType.Array)
                throw new InvalidDataException($"Expected Array, got {type}");

            var list = new List<object>();
            while (!reader.IsEnd())
            {
                list.Add(reader.ReadSerializable(elementType));
            }

            reader.ReadEnd();

            // 转换为数组
            var array = Array.CreateInstance(elementType, list.Count);
            for (int i = 0; i < list.Count; i++)
            {
                array.SetValue(list[i], i);
            }

            return array;
        }

        /// <summary>
        /// 读取列表
        /// </summary>
        private static object ReadList(SbinReader reader, Type listType)
        {
            var elementType = listType.GetGenericArguments()[0];
            var listTypeGeneric = typeof(List<>).MakeGenericType(elementType);
            var list = (IList)Activator.CreateInstance(listTypeGeneric);

            reader.ReadArrayBegin();
            while (!reader.IsEnd())
            {
                list.Add(reader.ReadSerializable(elementType));
            }

            reader.ReadEnd();

            return list;
        }

        /// <summary>
        /// 读取字典
        /// </summary>
        private static object ReadDictionary(SbinReader reader, Type dictType)
        {
            var genericArgs = dictType.GetGenericArguments();
            var keyType = genericArgs[0];
            var valueType = genericArgs[1];

            var dictTypeGeneric = typeof(Dictionary<,>).MakeGenericType(keyType, valueType);
            var dict = (IDictionary)Activator.CreateInstance(dictTypeGeneric);

            reader.ReadObjectBegin();
            while (!reader.IsEnd())
            {
                var keyStr = reader.ReadKey();
                var value = reader.ReadSerializable(valueType);

                // 转换键类型
                object key;
                if (keyType == typeof(string))
                    key = keyStr;
                else if (keyType == typeof(int))
                    key = int.Parse(keyStr);
                else if (keyType == typeof(uint))
                    key = uint.Parse(keyStr);
                else if (keyType == typeof(long))
                    key = long.Parse(keyStr);
                else if (keyType == typeof(ulong))
                    key = ulong.Parse(keyStr);
                else
                    throw new NotSupportedException($"Dictionary key type {keyType} is not supported");

                dict.Add(key, value);
            }

            reader.ReadEnd();

            return dict;
        }

        /// <summary>
        /// 读取可序列化对象
        /// </summary>
        private static object ReadSerializableObject(SbinReader reader, Type type)
        {
            // 值类型直接创建，引用类型使用 Activator
            object instance;
            if (type.IsValueType)
                instance = Activator.CreateInstance(type);
            else
            {
                var ctor = type.GetConstructor(Type.EmptyTypes);
                if (ctor == null)
                    throw new InvalidOperationException($"Type {type} must have a parameterless constructor");
                instance = ctor.Invoke(null);
            }

            reader.ReadObjectBegin();

            // 创建字段和属性的映射
            var fields = new Dictionary<string, FieldInfo>();
            foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                if (!field.IsDefined(typeof(NonSerializedAttribute), false))
                    fields[field.Name] = field;
            }

            var properties = new Dictionary<string, PropertyInfo>();
            foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (prop.CanRead && prop.CanWrite && prop.GetIndexParameters().Length == 0)
                    properties[prop.Name] = prop;
            }

            // 读取键值对
            while (!reader.IsEnd())
            {
                var key = reader.ReadKey();

                if (fields.TryGetValue(key, out var field))
                {
                    var value = reader.ReadSerializable(field.FieldType);
                    field.SetValue(instance, value);
                }
                else if (properties.TryGetValue(key, out var prop))
                {
                    var value = reader.ReadSerializable(prop.PropertyType);
                    prop.SetValue(instance, value);
                }
                else
                {
                    // 跳过未知字段
                    reader.ReadValue();
                }
            }

            reader.ReadEnd();
            return instance;
        }

        // ==================== 测试方法 ====================

#if UNITY_EDITOR || ENABLE_TESTS
        /// <summary>
        /// 运行所有 Sbin 测试
        /// </summary>
        public static void RunTests()
        {
            UnityEngine.Debug.Log("Running Sbin Tests...");

            int passed = 0;
            int failed = 0;

            // 基础类型测试
            try
            {
                TestPrimitives();
                passed++;
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError($"TestPrimitives failed: {e.Message}");
                failed++;
            }

            // 字符串测试
            try
            {
                TestString();
                passed++;
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError($"TestString failed: {e.Message}");
                failed++;
            }

            // 动态数组测试
            try
            {
                TestDynamicArray();
                passed++;
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError($"TestDynamicArray failed: {e.Message}");
                failed++;
            }

            // 对象测试
            try
            {
                TestObject();
                passed++;
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError($"TestObject failed: {e.Message}");
                failed++;
            }

            // Unity 类型测试
            try
            {
                TestUnityTypes();
                passed++;
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError($"TestUnityTypes failed: {e.Message}");
                failed++;
            }

            // 可序列化对象测试
            try
            {
                TestSerializable();
                passed++;
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError($"TestSerializable failed: {e.Message}");
                failed++;
            }

            // ArraySegment 测试
            try
            {
                TestArraySegment();
                passed++;
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError($"TestArraySegment failed: {e.Message}");
                failed++;
            }

            UnityEngine.Debug.Log($"Sbin Tests Complete: {passed} passed, {failed} failed");
        }

        private static void AssertEqual<T>(T expected, T actual, string message)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new Exception($"{message}: expected {expected}, got {actual}");
        }

        private static void AssertTrue(bool condition, string message)
        {
            if (!condition)
                throw new Exception(message);
        }

        private static void TestPrimitives()
        {
            var writer = new SbinWriter();

            // 写入各种基础类型
            writer.WriteU8(42);
            writer.WriteU16(1000);
            writer.WriteU32(100000);
            writer.WriteU64(10000000000);
            writer.WriteI8(-42);
            writer.WriteI16(-1000);
            writer.WriteI32(-100000);
            writer.WriteI64(-10000000000);
            writer.WriteF32(3.14f);
            writer.WriteF64(3.14159265359);
            writer.WriteBool(true);
            writer.WriteBool(false);

            var data = writer.ToArray();
            var reader = new SbinReader(data);

            // 读取并验证
            AssertEqual((byte)42, reader.ReadU8(), "U8");
            AssertEqual((ushort)1000, reader.ReadU16(), "U16");
            AssertEqual((uint)100000, reader.ReadU32(), "U32");
            AssertEqual((ulong)10000000000, reader.ReadU64(), "U64");
            AssertEqual((sbyte)-42, reader.ReadI8(), "I8");
            AssertEqual((short)-1000, reader.ReadI16(), "I16");
            AssertEqual((int)-100000, reader.ReadI32(), "I32");
            AssertEqual((long)-10000000000, reader.ReadI64(), "I64");
            AssertEqual(3.14f, reader.ReadF32(), "F32");
            AssertEqual(3.14159265359, reader.ReadF64(), "F64");
            AssertEqual(true, reader.ReadBool(), "Bool true");
            AssertEqual(false, reader.ReadBool(), "Bool false");

            UnityEngine.Debug.Log("TestPrimitives passed");
        }

        private static void TestString()
        {
            var writer = new SbinWriter();
            writer.WriteStr("Hello, World!");
            writer.WriteStr("");
            writer.WriteStr("中文测试");

            var data = writer.ToArray();
            var reader = new SbinReader(data);

            AssertEqual("Hello, World!", reader.ReadStr(), "String 1");
            AssertEqual("", reader.ReadStr(), "Empty string");
            AssertEqual("中文测试", reader.ReadStr(), "Unicode string");

            UnityEngine.Debug.Log("TestString passed");
        }

        private static void TestDynamicArray()
        {
            var writer = new SbinWriter();
            writer.BeginArray();
            writer.WriteU8(1);
            writer.WriteU8(2);
            writer.WriteU8(3);
            writer.EndArray();

            var data = writer.ToArray();
            var reader = new SbinReader(data);

            reader.ReadArrayBegin();
            AssertEqual((byte)1, reader.ReadU8(), "Dynamic array[0]");
            AssertEqual((byte)2, reader.ReadU8(), "Dynamic array[1]");
            AssertEqual((byte)3, reader.ReadU8(), "Dynamic array[2]");
            reader.ReadEnd();

            UnityEngine.Debug.Log("TestDynamicArray passed");
        }

        private static void TestObject()
        {
            var writer = new SbinWriter();
            writer.BeginObject();
            writer.WriteKey("name");
            writer.WriteStr("Player");
            writer.WriteKey("level");
            writer.WriteI32(10);
            writer.WriteKey("health");
            writer.WriteF32(100.5f);
            writer.EndObject();

            var data = writer.ToArray();
            var reader = new SbinReader(data);

            reader.ReadObjectBegin();
            AssertEqual("name", reader.ReadKey(), "Key 1");
            AssertEqual("Player", reader.ReadStr(), "Value 1");
            AssertEqual("level", reader.ReadKey(), "Key 2");
            AssertEqual(10, reader.ReadI32(), "Value 2");
            AssertEqual("health", reader.ReadKey(), "Key 3");
            AssertEqual(100.5f, reader.ReadF32(), "Value 3");
            reader.ReadEnd();

            UnityEngine.Debug.Log("TestObject passed");
        }

        private static void TestUnityTypes()
        {
            var writer = new SbinWriter();

            writer.WriteVector2(new UnityEngine.Vector2(1, 2));
            writer.WriteVector3(new UnityEngine.Vector3(1, 2, 3));
            writer.WriteVector4(new UnityEngine.Vector4(1, 2, 3, 4));
            writer.WriteQuaternion(new UnityEngine.Quaternion(0, 0, 0, 1));
            writer.WriteColor(new UnityEngine.Color(1, 0, 0, 1));
            writer.WriteColor32(new UnityEngine.Color32(255, 128, 64, 255));

            var data = writer.ToArray();
            var reader = new SbinReader(data);

            var v2 = reader.ReadVector2();
            AssertEqual(1f, v2.x, "Vector2.x");
            AssertEqual(2f, v2.y, "Vector2.y");

            var v3 = reader.ReadVector3();
            AssertEqual(1f, v3.x, "Vector3.x");
            AssertEqual(2f, v3.y, "Vector3.y");
            AssertEqual(3f, v3.z, "Vector3.z");

            var v4 = reader.ReadVector4();
            AssertEqual(1f, v4.x, "Vector4.x");
            AssertEqual(4f, v4.w, "Vector4.w");

            var q = reader.ReadQuaternion();
            AssertEqual(0f, q.x, "Quaternion.x");
            AssertEqual(1f, q.w, "Quaternion.w");

            var c = reader.ReadColor();
            AssertEqual(1f, c.r, "Color.r");
            AssertEqual(0f, c.g, "Color.g");

            var c32 = reader.ReadColor32();
            AssertEqual((byte)255, c32.r, "Color32.r");
            AssertEqual((byte)128, c32.g, "Color32.g");

            UnityEngine.Debug.Log("TestUnityTypes passed");
        }

        [System.Serializable]
        private class TestPlayerData
        {
            public string name;
            public int level;
            public float health;
            public UnityEngine.Vector3 position;
            public List<string> items;
            public TestPlayerStats stats;
        }

        [System.Serializable]
        private class TestPlayerStats
        {
            public int strength;
            public int agility;
        }

        private static void TestSerializable()
        {
            var player = new TestPlayerData
            {
                name = "Hero",
                level = 10,
                health = 100.5f,
                position = new UnityEngine.Vector3(1, 2, 3),
                items = new List<string> { "Sword", "Shield", "Potion" },
                stats = new TestPlayerStats { strength = 15, agility = 12 }
            };

            var writer = new SbinWriter();
            writer.WriteSerializable(player);
            var data = writer.ToArray();

            var reader = new SbinReader(data);
            var restored = reader.ReadSerializable<TestPlayerData>();

            AssertEqual("Hero", restored.name, "Player.name");
            AssertEqual(10, restored.level, "Player.level");
            AssertEqual(100.5f, restored.health, "Player.health");
            AssertEqual(1f, restored.position.x, "Player.position.x");
            AssertEqual(3, restored.items.Count, "Player.items.Count");
            AssertEqual("Shield", restored.items[1], "Player.items[1]");
            AssertEqual(15, restored.stats.strength, "Player.stats.strength");
            AssertEqual(12, restored.stats.agility, "Player.stats.agility");

            UnityEngine.Debug.Log("TestSerializable passed");
        }

        private static void TestArraySegment()
        {
            // 创建一个大缓冲区，只使用其中一部分
            var buffer = new byte[100];
            var writer = new SbinWriter();
            writer.WriteU32(12345);
            writer.WriteStr("Test");
            var data = writer.ToArray();

            // 复制到缓冲区中间
            Array.Copy(data, 0, buffer, 10, data.Length);

            // 使用 ArraySegment 读取
            var segment = new ArraySegment<byte>(buffer, 10, data.Length);
            var reader = new SbinReader(segment);

            AssertEqual((uint)12345, reader.ReadU32(), "ArraySegment U32");
            AssertEqual("Test", reader.ReadStr(), "ArraySegment String");
            AssertTrue(reader.Position == reader.Length, "Should be at end");

            UnityEngine.Debug.Log("TestArraySegment passed");
        }
#endif
    }
}