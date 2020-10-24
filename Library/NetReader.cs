using GameWorkstore.Patterns;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace GameWorkstore.NetworkLibrary
{
    public class NetReader
    {
        private readonly NetBuffer _buffer;
        private const int k_MaxStringLength = 1024 * 32;
        private const int k_InitialStringBufferSize = 1024;
        private static byte[] _stringReaderBuffer;
        private static Encoding _encoding;

        public NetReader()
        {
            _buffer = new NetBuffer();
            Initialize();
        }

        public NetReader(NetWriter writer)
        {
            _buffer = new NetBuffer(writer.AsArray());
            Initialize();
        }

        public NetReader(byte[] buffer)
        {
            _buffer = new NetBuffer(buffer);
            Initialize();
        }

        static void Initialize()
        {
            if (_encoding == null)
            {
                _stringReaderBuffer = new byte[k_InitialStringBufferSize];
                _encoding = new UTF8Encoding();
            }
        }

        public uint Position { get { return _buffer.Position; } }

        public void SeekZero()
        {
            _buffer.SeekZero();
        }

        internal void Replace(byte[] buffer)
        {
            _buffer.Replace(buffer);
        }

        // http://sqlite.org/src4/doc/trunk/www/varint.wiki
        // NOTE: big endian.

        public uint ReadPackedUInt32()
        {
            byte a0 = ReadByte();
            if (a0 < 241)
            {
                return a0;
            }
            byte a1 = ReadByte();
            if (a0 >= 241 && a0 <= 248)
            {
                return (UInt32)(240 + 256 * (a0 - 241) + a1);
            }
            byte a2 = ReadByte();
            if (a0 == 249)
            {
                return (UInt32)(2288 + 256 * a1 + a2);
            }
            byte a3 = ReadByte();
            if (a0 == 250)
            {
                return a1 + (((UInt32)a2) << 8) + (((UInt32)a3) << 16);
            }
            byte a4 = ReadByte();
            if (a0 >= 251)
            {
                return a1 + (((UInt32)a2) << 8) + (((UInt32)a3) << 16) + (((UInt32)a4) << 24);
            }
            throw new IndexOutOfRangeException("ReadPackedUInt32() failure: " + a0);
        }

        public ulong ReadPackedUInt64()
        {
            byte a0 = ReadByte();
            if (a0 < 241)
            {
                return a0;
            }
            byte a1 = ReadByte();
            if (a0 >= 241 && a0 <= 248)
            {
                return 240 + 256 * (a0 - ((UInt64)241)) + a1;
            }
            byte a2 = ReadByte();
            if (a0 == 249)
            {
                return 2288 + (((UInt64)256) * a1) + a2;
            }
            byte a3 = ReadByte();
            if (a0 == 250)
            {
                return a1 + (((UInt64)a2) << 8) + (((UInt64)a3) << 16);
            }
            byte a4 = ReadByte();
            if (a0 == 251)
            {
                return a1 + (((UInt64)a2) << 8) + (((UInt64)a3) << 16) + (((UInt64)a4) << 24);
            }


            byte a5 = ReadByte();
            if (a0 == 252)
            {
                return a1 + (((UInt64)a2) << 8) + (((UInt64)a3) << 16) + (((UInt64)a4) << 24) + (((UInt64)a5) << 32);
            }


            byte a6 = ReadByte();
            if (a0 == 253)
            {
                return a1 + (((UInt64)a2) << 8) + (((UInt64)a3) << 16) + (((UInt64)a4) << 24) + (((UInt64)a5) << 32) + (((UInt64)a6) << 40);
            }


            byte a7 = ReadByte();
            if (a0 == 254)
            {
                return a1 + (((UInt64)a2) << 8) + (((UInt64)a3) << 16) + (((UInt64)a4) << 24) + (((UInt64)a5) << 32) + (((UInt64)a6) << 40) + (((UInt64)a7) << 48);
            }


            byte a8 = ReadByte();
            if (a0 == 255)
            {
                return a1 + (((UInt64)a2) << 8) + (((UInt64)a3) << 16) + (((UInt64)a4) << 24) + (((UInt64)a5) << 32) + (((UInt64)a6) << 40) + (((UInt64)a7) << 48) + (((UInt64)a8) << 56);
            }
            throw new IndexOutOfRangeException("ReadPackedUInt64() failure: " + a0);
        }

        public NetworkInstanceId ReadNetworkId()
        {
            return new NetworkInstanceId(ReadPackedUInt32());
        }

        public NetworkSceneId ReadSceneId()
        {
            return new NetworkSceneId(ReadPackedUInt32());
        }

        public byte ReadByte()
        {
            return _buffer.ReadByte();
        }

        public sbyte ReadSByte()
        {
            return (sbyte)_buffer.ReadByte();
        }

        public short ReadShort()
        {
            ushort value = 0;
            value |= _buffer.ReadByte();
            value |= (ushort)(_buffer.ReadByte() << 8);
            return (short)value;
        }

        public ushort ReadUshort()
        {
            ushort value = 0;
            value |= _buffer.ReadByte();
            value |= (ushort)(_buffer.ReadByte() << 8);
            return value;
        }

        public int ReadInt()
        {
            uint value = 0;
            value |= _buffer.ReadByte();
            value |= (uint)(_buffer.ReadByte() << 8);
            value |= (uint)(_buffer.ReadByte() << 16);
            value |= (uint)(_buffer.ReadByte() << 24);
            return (int)value;
        }

        public uint ReadUInt()
        {
            uint value = 0;
            value |= _buffer.ReadByte();
            value |= (uint)(_buffer.ReadByte() << 8);
            value |= (uint)(_buffer.ReadByte() << 16);
            value |= (uint)(_buffer.ReadByte() << 24);
            return value;
        }

        public long ReadLong()
        {
            ulong value = 0;

            ulong other = _buffer.ReadByte();
            value |= other;

            other = ((ulong)_buffer.ReadByte()) << 8;
            value |= other;

            other = ((ulong)_buffer.ReadByte()) << 16;
            value |= other;

            other = ((ulong)_buffer.ReadByte()) << 24;
            value |= other;

            other = ((ulong)_buffer.ReadByte()) << 32;
            value |= other;

            other = ((ulong)_buffer.ReadByte()) << 40;
            value |= other;

            other = ((ulong)_buffer.ReadByte()) << 48;
            value |= other;

            other = ((ulong)_buffer.ReadByte()) << 56;
            value |= other;

            return (long)value;
        }

        public ulong ReadUlong()
        {
            ulong value = 0;
            ulong other = _buffer.ReadByte();
            value |= other;

            other = ((ulong)_buffer.ReadByte()) << 8;
            value |= other;

            other = ((ulong)_buffer.ReadByte()) << 16;
            value |= other;

            other = ((ulong)_buffer.ReadByte()) << 24;
            value |= other;

            other = ((ulong)_buffer.ReadByte()) << 32;
            value |= other;

            other = ((ulong)_buffer.ReadByte()) << 40;
            value |= other;

            other = ((ulong)_buffer.ReadByte()) << 48;
            value |= other;

            other = ((ulong)_buffer.ReadByte()) << 56;
            value |= other;
            return value;
        }

        public float ReadSingle()
        {
            return BitConverter.ToSingle(BitConverter.GetBytes(ReadUInt()), 0);
        }

        public double ReadDouble()
        {
            return BitConverter.ToDouble(BitConverter.GetBytes(ReadUlong()), 0);
        }

        public string ReadString()
        {
            ushort numBytes = ReadUshort();
            if (numBytes == 0)
                return "";

            if (numBytes >= k_MaxStringLength)
            {
                throw new IndexOutOfRangeException("ReadString() too long: " + numBytes);
            }

            while (numBytes > _stringReaderBuffer.Length)
            {
                _stringReaderBuffer = new byte[_stringReaderBuffer.Length * 2];
            }

            _buffer.ReadBytes(_stringReaderBuffer, numBytes);

            char[] chars = _encoding.GetChars(_stringReaderBuffer, 0, numBytes);
            return new string(chars);
        }

        public char ReadChar()
        {
            return (char)_buffer.ReadByte();
        }

        public bool ReadBoolean()
        {
            int value = _buffer.ReadByte();
            return value == 1;
        }

        public byte[] ReadBytes(int count)
        {
            if (count < 0)
            {
                throw new IndexOutOfRangeException("NetworkReader ReadBytes " + count);
            }
            byte[] value = new byte[count];
            _buffer.ReadBytes(value, (uint)count);
            return value;
        }

        public Vector2 ReadVector2()
        {
            return new Vector2(ReadSingle(), ReadSingle());
        }

        public Vector3 ReadVector3()
        {
            return new Vector3(ReadSingle(), ReadSingle(), ReadSingle());
        }

        public Vector4 ReadVector4()
        {
            return new Vector4(ReadSingle(), ReadSingle(), ReadSingle(), ReadSingle());
        }

        public Color ReadColor()
        {
            return new Color(ReadSingle(), ReadSingle(), ReadSingle(), ReadSingle());
        }

        public Color32 ReadColor32()
        {
            return new Color32(ReadByte(), ReadByte(), ReadByte(), ReadByte());
        }

        public Rect ReadRect()
        {
            return new Rect(ReadSingle(), ReadSingle(), ReadSingle(), ReadSingle());
        }

        public Plane ReadPlane()
        {
            return new Plane(ReadVector3(), ReadSingle());
        }

        public Ray ReadRay()
        {
            return new Ray(ReadVector3(), ReadVector3());
        }

        public Matrix4x4 ReadMatrix4x4()
        {
            Matrix4x4 m = new Matrix4x4
            {
                m00 = ReadSingle(),
                m01 = ReadSingle(),
                m02 = ReadSingle(),
                m03 = ReadSingle(),
                m10 = ReadSingle(),
                m11 = ReadSingle(),
                m12 = ReadSingle(),
                m13 = ReadSingle(),
                m20 = ReadSingle(),
                m21 = ReadSingle(),
                m22 = ReadSingle(),
                m23 = ReadSingle(),
                m30 = ReadSingle(),
                m31 = ReadSingle(),
                m32 = ReadSingle(),
                m33 = ReadSingle()
            };
            return m;
        }

        public NetworkHash128 ReadNetworkHash128()
        {
            NetworkHash128 hash;
            hash.i0 = ReadByte();
            hash.i1 = ReadByte();
            hash.i2 = ReadByte();
            hash.i3 = ReadByte();
            hash.i4 = ReadByte();
            hash.i5 = ReadByte();
            hash.i6 = ReadByte();
            hash.i7 = ReadByte();
            hash.i8 = ReadByte();
            hash.i9 = ReadByte();
            hash.i10 = ReadByte();
            hash.i11 = ReadByte();
            hash.i12 = ReadByte();
            hash.i13 = ReadByte();
            hash.i14 = ReadByte();
            hash.i15 = ReadByte();
            return hash;
        }

        public override string ToString()
        {
            return _buffer.ToString();
        }

        public TMsg ReadMessage<TMsg>() where TMsg : NetworkPacketBase, new()
        {
            var msg = new TMsg();
            msg.Deserialize(this);
            return msg;
        }

        /// <summary>
        /// READERS
        /// </summary>

        /// <summary>
        /// Float decimal values are NEVER equal to 1;
        /// In this case,we can assume that we have from 0 to 256, but 256 will never be reached;
        /// Also we dont need to cast division to float :)
        /// </summary>
        const float byteMaxValue1 = byte.MaxValue + 1;

        public Quaternion ReadQuaternion()
        {
            Vector3 base180 = ReadVector3(true);
            if (base180.x < 0) base180.x = base180.x + 360;
            if (base180.y < 0) base180.y = base180.y + 360;
            if (base180.z < 0) base180.z = base180.z + 360;
            return Quaternion.Euler(base180.x, base180.y, base180.z);
        }

        public Vector2 ReadVector2(bool signed)
        {
            Vector2 vector = Vector2.zero;
            if (signed)
            {
                ReadBools(out bool first, out bool second, out bool third, out bool fourth);

                vector.x = (first ? 1 : -1) * ReadApproxFloat();
                vector.y = (second ? 1 : -1) * ReadApproxFloat();
            }
            else
            {
                vector.x = ReadPackedUInt32() + ReadApproxFloat();
                vector.y = ReadPackedUInt32() + ReadApproxFloat();
            }
            return vector;
        }

        public Vector3 ReadVector3(bool signed)
        {
            Vector3 vector = Vector3.zero;
            if (signed)
            {
                ReadBools(out bool first, out bool second, out bool third, out bool fourth);

                vector.x = (first ? 1 : -1) * ReadApproxFloat();
                vector.y = (second ? 1 : -1) * ReadApproxFloat();
                vector.z = (third ? 1 : -1) * ReadApproxFloat();
            }
            else
            {
                vector.x = ReadPackedUInt32() + ReadApproxFloat();
                vector.y = ReadPackedUInt32() + ReadApproxFloat();
                vector.z = ReadPackedUInt32() + ReadApproxFloat();
            }
            return vector;
        }

        public Vector4 ReadVector4(bool signed)
        {
            Vector4 vector = Vector4.zero;
            if (signed)
            {
                bool first, second, third, fourth;
                ReadBools(out first, out second, out third, out fourth);

                vector.x = (first ? 1 : -1) * ReadApproxFloat();
                vector.y = (second ? 1 : -1) * ReadApproxFloat();
                vector.z = (third ? 1 : -1) * ReadApproxFloat();
                vector.w = (fourth ? 1 : -1) * ReadApproxFloat();
            }
            else
            {
                vector.x = ReadApproxFloat();
                vector.y = ReadApproxFloat();
                vector.z = ReadApproxFloat();
                vector.w = ReadApproxFloat();
            }
            return vector;
        }

        public void ReadBools(out bool first, out bool second, out bool third, out bool fourth)
        {
            byte value = ReadByte();
            first = (value & 1 << 0) != 0;
            second = (value & 1 << 1) != 0;
            third = (value & 1 << 2) != 0;
            fourth = (value & 1 << 3) != 0;
        }

        public float ReadApproxFloat()
        {
            return ReadPackedUInt32() + ReadByte() / byteMaxValue1;
        }

        /// <summary>
        /// ARRAYS
        /// </summary>

        public T[] ReadArray<T>(Func<T> readT)
        {
            var sz = ReadUshort();
            if (sz == 0) return Array.Empty<T>();
            var value = new T[sz];
            for (int i = 0; i < sz; i++) value[i] = readT();
            return value;
        }

        public T[] ReadArray<T>(bool signed, Func<bool, T> readT)
        {
            var sz = ReadUshort();
            if (sz == 0) return Array.Empty<T>();
            var value = new T[sz];
            for (int i = 0; i < sz; i++) value[i] = readT(signed);
            return value;
        }

        public bool[] ReadBooleans() { return ReadArray(ReadBoolean); }
        public byte[] ReadBytes() { return ReadArray(ReadByte); }
        public char[] ReadChars() { return ReadArray(ReadChar); }
        public ushort[] ReadUshorts() { return ReadArray(ReadUshort); }
        public uint[] ReadUInts() { return ReadArray(ReadUInt); }
        public ulong[] ReadUlongs() { return ReadArray(ReadUlong); }
        public sbyte[] ReadSBytes() { return ReadArray(ReadSByte); }
        public short[] ReadShorts() { return ReadArray(ReadShort); }
        public int[] ReadInts() { return ReadArray(ReadInt); }
        public long[] ReadLongs() { return ReadArray(ReadLong); }
        public string[] ReadStrings() { return ReadArray(ReadString); }
        public NetworkInstanceId[] ReadNetworkIds() { return ReadArray(ReadNetworkId); }
        public NetworkHash128[] ReadNetworkHash128s() { return ReadArray(ReadNetworkHash128); }
        public Vector2[] ReadVector2s() { return ReadArray(ReadVector2); }
        public Vector3[] ReadVector3s() { return ReadArray(ReadVector3); }
        public Vector4[] ReadVector4s() { return ReadArray(ReadVector4); }
        public Quaternion[] ReadQuaternions() { return ReadArray(ReadQuaternion); }
        public Vector2[] ReadVector2s(bool signed) { return ReadArray(signed, ReadVector2); }
        public Vector3[] ReadVector3s(bool signed) { return ReadArray(signed, ReadVector3); }
        public Vector4[] ReadVector4s(bool signed) { return ReadArray(signed, ReadVector4); }

        /// <summary>
        /// HIGHSPEEDARRAYS
        /// </summary>

        public void ReadArray<T>(HighSpeedArray<T> outArray, Func<T> readT)
        {
            var sz = ReadUshort();
            outArray.Clear();
            outArray.SetCapacity(sz);
            for (int i = 0; i < sz; i++) outArray.Add(readT());
        }

        public void ReadArray<T>(HighSpeedArray<T> outArray, bool signed, Func<bool, T> readT)
        {
            var sz = ReadUshort();
            outArray.Clear();
            outArray.SetCapacity(sz);
            for (int i = 0; i < sz; i++) outArray.Add(readT(signed));
        }

        public void ReadBooleans(HighSpeedArray<bool> outArray) { ReadArray(outArray, ReadBoolean); }
        public void ReadBytes(HighSpeedArray<byte> outArray) { ReadArray(outArray, ReadByte); }
        public void ReadChars(HighSpeedArray<char> outArray) { ReadArray(outArray, ReadChar); }
        public void ReadUInt16s(HighSpeedArray<ushort> outArray) { ReadArray(outArray, ReadUshort); }
        public void ReadUInts(HighSpeedArray<uint> outArray) { ReadArray(outArray, ReadUInt); }
        public void ReadUlongs(HighSpeedArray<ulong> outArray) { ReadArray(outArray, ReadUlong); }

        public void ReadSBytes(HighSpeedArray<sbyte> outArray) { ReadArray(outArray, ReadSByte); }
        public void ReadShorts(HighSpeedArray<short> outArray) { ReadArray(outArray, ReadShort); }
        public void ReadInts(HighSpeedArray<int> outArray) { ReadArray(outArray, ReadInt); }
        public void ReadLongs(HighSpeedArray<long> outArray) { ReadArray(outArray, ReadLong); }
        public void ReadStrings(HighSpeedArray<string> outArray) { ReadArray(outArray, ReadString); }
        public void ReadNetworkIds(HighSpeedArray<NetworkInstanceId> outArray) { ReadArray(outArray, ReadNetworkId); }
        public void ReadNetworkHash128s(HighSpeedArray<NetworkHash128> outArray) { ReadArray(outArray, ReadNetworkHash128); }
        public void ReadVector2s(HighSpeedArray<Vector2> outArray) { ReadArray(outArray, ReadVector2); }
        public void ReadVector3s(HighSpeedArray<Vector3> outArray) { ReadArray(outArray, ReadVector3); }
        public void ReadVector4s(HighSpeedArray<Vector4> outArray) { ReadArray(outArray, ReadVector4); }
        public void ReadQuaternions(HighSpeedArray<Quaternion> outArray) { ReadArray(outArray, ReadQuaternion); }
        public void ReadVector2s(HighSpeedArray<Vector2> outArray, bool signed) { ReadArray(outArray, signed, ReadVector2); }
        public void ReadVector3s(HighSpeedArray<Vector3> outArray, bool signed) { ReadArray(outArray, signed, ReadVector3); }
        public void ReadVector4s(HighSpeedArray<Vector4> outArray, bool signed) { ReadArray(outArray, signed, ReadVector4); }

        //dictionaries
        public void ReadDictionary<T, U>(Dictionary<T, U> outDictionary,Func<T> readT, Func<U> readU)
        {
            var sz = ReadUshort();
            outDictionary.Clear();
            for (int i = 0; i < sz; i++)
            {
                var t = readT();
                var u = readU();
                outDictionary.Add(t, u);
            }
        }


    };
}
