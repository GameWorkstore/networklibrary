using GameWorkstore.Patterns;
using System;
using System.Text;
using UnityEngine;

namespace GameWorkstore.NetworkLibrary
{
    /// <summary>
    /// Binary stream Writer. Supports simple types, buffers, arrays, structs, and nested types
    /// </summary>
    public class NetWriter
    {
        private const int k_MaxStringLength = 1024 * 32;
        private readonly NetBuffer _buffer;
        private static Encoding _encoding;
        private static byte[] _stringWriteBuffer;

        public NetWriter()
        {
            _buffer = new NetBuffer();
            if (_encoding == null)
            {
                _encoding = new UTF8Encoding();
                _stringWriteBuffer = new byte[k_MaxStringLength];
            }
        }

        public NetWriter(byte[] buffer)
        {
            _buffer = new NetBuffer(buffer);
            if (_encoding == null)
            {
                _encoding = new UTF8Encoding();
                _stringWriteBuffer = new byte[k_MaxStringLength];
            }
        }

        public short Position { get { return (short)_buffer.Position; } }

        public byte[] ToArray()
        {
            var newArray = new byte[_buffer.AsArraySegment().Count];
            Array.Copy(_buffer.AsArraySegment().Array, newArray, _buffer.AsArraySegment().Count);
            return newArray;
        }

        public byte[] AsArray()
        {
            return AsArraySegment().Array;
        }

        internal ArraySegment<byte> AsArraySegment()
        {
            return _buffer.AsArraySegment();
        }

        // http://sqlite.org/src4/doc/trunk/www/varint.wiki

        public void WritePackedUInt32(uint value)
        {
            if (value <= 240)
            {
                Write((byte)value);
                return;
            }
            if (value <= 2287)
            {
                Write((byte)((value - 240) / 256 + 241));
                Write((byte)((value - 240) % 256));
                return;
            }
            if (value <= 67823)
            {
                Write((byte)249);
                Write((byte)((value - 2288) / 256));
                Write((byte)((value - 2288) % 256));
                return;
            }
            if (value <= 16777215)
            {
                Write((byte)250);
                Write((byte)(value & 0xFF));
                Write((byte)((value >> 8) & 0xFF));
                Write((byte)((value >> 16) & 0xFF));
                return;
            }

            // all other values of uint
            Write((byte)251);
            Write((byte)(value & 0xFF));
            Write((byte)((value >> 8) & 0xFF));
            Write((byte)((value >> 16) & 0xFF));
            Write((byte)((value >> 24) & 0xFF));
        }

        public void WritePackedUInt64(ulong value)
        {
            if (value <= 240)
            {
                Write((byte)value);
                return;
            }
            if (value <= 2287)
            {
                Write((byte)((value - 240) / 256 + 241));
                Write((byte)((value - 240) % 256));
                return;
            }
            if (value <= 67823)
            {
                Write((byte)249);
                Write((byte)((value - 2288) / 256));
                Write((byte)((value - 2288) % 256));
                return;
            }
            if (value <= 16777215)
            {
                Write((byte)250);
                Write((byte)(value & 0xFF));
                Write((byte)((value >> 8) & 0xFF));
                Write((byte)((value >> 16) & 0xFF));
                return;
            }
            if (value <= 4294967295)
            {
                Write((byte)251);
                Write((byte)(value & 0xFF));
                Write((byte)((value >> 8) & 0xFF));
                Write((byte)((value >> 16) & 0xFF));
                Write((byte)((value >> 24) & 0xFF));
                return;
            }
            if (value <= 1099511627775)
            {
                Write((byte)252);
                Write((byte)(value & 0xFF));
                Write((byte)((value >> 8) & 0xFF));
                Write((byte)((value >> 16) & 0xFF));
                Write((byte)((value >> 24) & 0xFF));
                Write((byte)((value >> 32) & 0xFF));
                return;
            }
            if (value <= 281474976710655)
            {
                Write((byte)253);
                Write((byte)(value & 0xFF));
                Write((byte)((value >> 8) & 0xFF));
                Write((byte)((value >> 16) & 0xFF));
                Write((byte)((value >> 24) & 0xFF));
                Write((byte)((value >> 32) & 0xFF));
                Write((byte)((value >> 40) & 0xFF));
                return;
            }
            if (value <= 72057594037927935)
            {
                Write((byte)254);
                Write((byte)(value & 0xFF));
                Write((byte)((value >> 8) & 0xFF));
                Write((byte)((value >> 16) & 0xFF));
                Write((byte)((value >> 24) & 0xFF));
                Write((byte)((value >> 32) & 0xFF));
                Write((byte)((value >> 40) & 0xFF));
                Write((byte)((value >> 48) & 0xFF));
                return;
            }
            // all others
            {
                Write((byte)255);
                Write((byte)(value & 0xFF));
                Write((byte)((value >> 8) & 0xFF));
                Write((byte)((value >> 16) & 0xFF));
                Write((byte)((value >> 24) & 0xFF));
                Write((byte)((value >> 32) & 0xFF));
                Write((byte)((value >> 40) & 0xFF));
                Write((byte)((value >> 48) & 0xFF));
                Write((byte)((value >> 56) & 0xFF));
            }
        }

        public void Write(NetworkInstanceId value)
        {
            WritePackedUInt32(value.Value);
        }

        public void Write(NetworkSceneId value)
        {
            WritePackedUInt32(value.Value);
        }

        public void Write(char value)
        {
            _buffer.WriteByte((byte)value);
        }

        public void Write(byte value)
        {
            _buffer.WriteByte(value);
        }

        public void Write(sbyte value)
        {
            _buffer.WriteByte((byte)value);
        }

        public void Write(short value)
        {
            _buffer.WriteByte2((byte)(value & 0xff), (byte)((value >> 8) & 0xff));
        }

        public void Write(ushort value)
        {
            _buffer.WriteByte2((byte)(value & 0xff), (byte)((value >> 8) & 0xff));
        }

        public void Write(int value)
        {
            // little endian...
            _buffer.WriteByte4(
                (byte)(value & 0xff),
                (byte)((value >> 8) & 0xff),
                (byte)((value >> 16) & 0xff),
                (byte)((value >> 24) & 0xff));
        }

        public void Write(uint value)
        {
            _buffer.WriteByte4(
                (byte)(value & 0xff),
                (byte)((value >> 8) & 0xff),
                (byte)((value >> 16) & 0xff),
                (byte)((value >> 24) & 0xff));
        }

        public void Write(long value)
        {
            _buffer.WriteByte8(
                (byte)(value & 0xff),
                (byte)((value >> 8) & 0xff),
                (byte)((value >> 16) & 0xff),
                (byte)((value >> 24) & 0xff),
                (byte)((value >> 32) & 0xff),
                (byte)((value >> 40) & 0xff),
                (byte)((value >> 48) & 0xff),
                (byte)((value >> 56) & 0xff));
        }

        public void Write(ulong value)
        {
            _buffer.WriteByte8(
                (byte)(value & 0xff),
                (byte)((value >> 8) & 0xff),
                (byte)((value >> 16) & 0xff),
                (byte)((value >> 24) & 0xff),
                (byte)((value >> 32) & 0xff),
                (byte)((value >> 40) & 0xff),
                (byte)((value >> 48) & 0xff),
                (byte)((value >> 56) & 0xff));
        }

        public void Write(float value)
        {
            Write(BitConverter.ToUInt32(BitConverter.GetBytes(value), 0));
        }

        public void Write(double value)
        {
            Write(BitConverter.ToUInt64(BitConverter.GetBytes(value), 0));
        }

        public void Write(string value)
        {
            if (value == null)
            {
                _buffer.WriteByte2(0, 0);
                return;
            }

            int len = _encoding.GetByteCount(value);

            if (len >= k_MaxStringLength)
            {
                throw new IndexOutOfRangeException("Serialize(string) too long: " + value.Length);
            }

            Write((ushort)len);
            int numBytes = _encoding.GetBytes(value, 0, value.Length, _stringWriteBuffer, 0);
            _buffer.WriteBytes(_stringWriteBuffer, (ushort)numBytes);
        }

        public void Write(bool value)
        {
            if (value)
                _buffer.WriteByte(1);
            else
                _buffer.WriteByte(0);
        }

        /*public void Write(byte[] buffer, int count)
        {
            if (count > ushort.MaxValue)
            {
                if (LogFilter.logError) { Debug.LogError("NetworkWriter Write: buffer is too large (" + count + ") bytes. The maximum buffer size is 64K bytes."); }
                return;
            }
            _buffer.WriteBytes(buffer, (ushort)count);
        }

        public void Write(byte[] buffer, int offset, int count)
        {
            if (count > ushort.MaxValue)
            {
                if (LogFilter.logError) { Debug.LogError("NetworkWriter Write: buffer is too large (" + count + ") bytes. The maximum buffer size is 64K bytes."); }
                return;
            }
            _buffer.WriteBytesAtOffset(buffer, (ushort)offset, (ushort)count);
        }

        public void WriteBytesAndSize(byte[] buffer, int count)
        {
            if (buffer == null || count == 0)
            {
                Write((ushort)0);
                return;
            }

            if (count > ushort.MaxValue)
            {
                if (LogFilter.logError) { Debug.LogError("NetworkWriter WriteBytesAndSize: buffer is too large (" + count + ") bytes. The maximum buffer size is 64K bytes."); }
                return;
            }

            Write((ushort)count);
            _buffer.WriteBytes(buffer, (ushort)count);
        }

        //NOTE: this will write the entire buffer.. including trailing empty space!
        public void WriteBytesFull(byte[] buffer)
        {
            if (buffer == null)
            {
                Write((ushort)0);
                return;
            }
            if (buffer.Length > ushort.MaxValue)
            {
                if (LogFilter.logError) { Debug.LogError("NetworkWriter WriteBytes: buffer is too large (" + buffer.Length + ") bytes. The maximum buffer size is 64K bytes."); }
                return;
            }
            Write((ushort)buffer.Length);
            _buffer.WriteBytes(buffer, (UInt16)buffer.Length);
        }*/

        public void Write(Vector2 value)
        {
            Write(value.x);
            Write(value.y);
        }

        public void Write(Vector3 value)
        {
            Write(value.x);
            Write(value.y);
            Write(value.z);
        }

        public void Write(Vector4 value)
        {
            Write(value.x);
            Write(value.y);
            Write(value.z);
            Write(value.w);
        }

        public void Write(Color value)
        {
            Write(value.r);
            Write(value.g);
            Write(value.b);
            Write(value.a);
        }

        public void Write(Color32 value)
        {
            Write(value.r);
            Write(value.g);
            Write(value.b);
            Write(value.a);
        }

        /*public void Write(Quaternion value)
        {
            Write(value.x);
            Write(value.y);
            Write(value.z);
            Write(value.w);
        }*/

        public void Write(Rect value)
        {
            Write(value.xMin);
            Write(value.yMin);
            Write(value.width);
            Write(value.height);
        }

        public void Write(Plane value)
        {
            Write(value.normal);
            Write(value.distance);
        }

        public void Write(Ray value)
        {
            Write(value.direction);
            Write(value.origin);
        }

        public void Write(Matrix4x4 value)
        {
            Write(value.m00);
            Write(value.m01);
            Write(value.m02);
            Write(value.m03);
            Write(value.m10);
            Write(value.m11);
            Write(value.m12);
            Write(value.m13);
            Write(value.m20);
            Write(value.m21);
            Write(value.m22);
            Write(value.m23);
            Write(value.m30);
            Write(value.m31);
            Write(value.m32);
            Write(value.m33);
        }

        public void Write(NetworkHash128 value)
        {
            Write(value.i0);
            Write(value.i1);
            Write(value.i2);
            Write(value.i3);
            Write(value.i4);
            Write(value.i5);
            Write(value.i6);
            Write(value.i7);
            Write(value.i8);
            Write(value.i9);
            Write(value.i10);
            Write(value.i11);
            Write(value.i12);
            Write(value.i13);
            Write(value.i14);
            Write(value.i15);
        }

        public void Write(NetworkPacketBase msg)
        {
            msg.Serialize(this);
        }

        public void SeekZero()
        {
            _buffer.SeekZero();
        }

        public void StartMessage(ushort msgType)
        {
            SeekZero();

            // two bytes for size, will be filled out in FinishMessage
            _buffer.WriteByte2(0, 0);

            // two bytes for message type
            Write(msgType);
        }

        public void FinishMessage()
        {
            // writes correct size into space at start of buffer
            _buffer.FinishMessage();
        }

        /// ----------------------
        ///         EXTENDED
        /// ----------------------

        /// <summary>
        /// WRITERS
        /// </summary>
        public void Write(Quaternion quaternion)
        {
            Vector3 base180 = quaternion.eulerAngles;
            if (base180.x > 180) base180.x = base180.x - 360;
            if (base180.y > 180) base180.y = base180.y - 360;
            if (base180.z > 180) base180.z = base180.z - 360;
            Write(base180, true);
        }
        /// <summary>
        /// Float decimal values are NEVER equal to 1;
        /// In this case,we can assume that we have from 0 to 256, but 256 will never be reached;
        /// Also we dont need to cast division to float :)
        /// </summary>
        const float byteMaxValue1 = byte.MaxValue + 1;

        public void Write(Vector2 vector, bool signed)
        {
            if (signed)
            {
                Write(vector.x >= 0, vector.y >= 0, false, false);
            }

            WriteApproxFloat(vector.x);
            WriteApproxFloat(vector.y);
        }

        public void Write(Vector3 vector, bool signed)
        {
            if (signed)
            {
                Write(vector.x >= 0, vector.y >= 0, vector.z >= 0, false);
            }

            WriteApproxFloat(vector.x);
            WriteApproxFloat(vector.y);
            WriteApproxFloat(vector.z);
        }

        public void Write(Vector4 vector, bool signed)
        {
            if (signed)
            {
                Write(vector.x >= 0, vector.y >= 0, vector.z >= 0, vector.w >= 0);
            }

            WriteApproxFloat(vector.x);
            WriteApproxFloat(vector.y);
            WriteApproxFloat(vector.z);
            WriteApproxFloat(vector.w);
        }

        public void Write(bool first, bool second, bool third, bool fourth)
        {
            Write((byte)((first ? (1 << 0) : 0) + (second ? (1 << 1) : 0) + (third ? (1 << 2) : 0) + (fourth ? (1 << 3) : 0)));
        }

        public void WriteApproxFloat(float value)
        {
            WritePackedUInt32((uint)Mathf.Abs(value));
            Write((byte)(Mathf.Repeat(Mathf.Abs(value), 1) * byteMaxValue1));
        }

        /// <summary>
        /// ARRAYS
        /// </summary>

        private void Write<T>(T[] array, Action<T> write)
        {
            Write((ushort)array.Length);
            for (int i = 0; i < array.Length; i++)
            {
                write(array[i]);
            }
        }

        private void Write<T>(T[] array, bool signed, Action<T, bool> write)
        {
            Write((ushort)array.Length);
            for (int i = 0; i < array.Length; i++)
            {
                write(array[i], signed);
            }
        }

        public void Write(bool[] array) { Write(array, Write); }
        public void Write(byte[] array) { Write(array, Write); }
        public void Write(char[] array) { Write(array, Write); }
        public void Write(ushort[] array) { Write(array, Write); }
        public void Write(uint[] array) { Write(array, Write); }
        public void Write(ulong[] array) { Write(array, Write); }
        public void Write(sbyte[] array) { Write(array, Write); }
        public void Write(short[] array) { Write(array, Write); }
        public void Write(int[] array) { Write(array, Write); }
        public void Write(long[] array) { Write(array, Write); }
        public void Write(string[] array) { Write(array, Write); }
        public void Write(NetworkInstanceId[] array) { Write(array, Write); }
        public void Write(NetworkHash128[] array) { Write(array, Write); }
        public void Write(Vector2[] array) { Write(array, Write); }
        public void Write(Vector3[] array) { Write(array, Write); }
        public void Write(Vector4[] array) { Write(array, Write); }
        public void Write(Quaternion[] array) { Write(array, Write); }
        public void Write(Vector2[] array, bool signed) { Write(array, signed, Write); }
        public void Write(Vector3[] array, bool signed) { Write(array, signed, Write); }
        public void Write(Vector4[] array, bool signed) { Write(array, signed, Write); }

        /// <summary>
        /// HIGHSPEEDARRAYS
        /// </summary>

        private void Write<T>(HighSpeedArray<T> array, Action<T> write)
        {
            Write((ushort)array.Count);
            for (int i = 0; i < array.Count; i++)
            {
                write(array[i]);
            }
        }

        private void Write<T>(HighSpeedArray<T> array, bool signed, Action<T,bool> write)
        {
            Write((ushort)array.Count);
            for (int i = 0; i < array.Count; i++)
            {
                write(array[i], signed);
            }
        }

        public void Write(HighSpeedArray<bool> array) { Write(array, Write); }
        public void Write(HighSpeedArray<byte> array) { Write(array, Write); }
        public void Write(HighSpeedArray<char> array) { Write(array, Write); }
        public void Write(HighSpeedArray<ushort> array) { Write(array, Write); }
        public void Write(HighSpeedArray<uint> array) { Write(array, Write); }
        public void Write(HighSpeedArray<ulong> array) { Write(array, Write); }
        public void Write(HighSpeedArray<sbyte> array) { Write(array, Write); }
        public void Write(HighSpeedArray<short> array) { Write(array, Write); }
        public void Write(HighSpeedArray<int> array) { Write(array, Write); }
        public void Write(HighSpeedArray<long> array) { Write(array, Write); }
        public void Write(HighSpeedArray<string> array) { Write(array, Write); }
        public void Write(HighSpeedArray<NetworkInstanceId> array) { Write(array, Write); }
        public void Write(HighSpeedArray<NetworkHash128> array) { Write(array, Write); }
        public void Write(HighSpeedArray<Vector2> array) { Write(array, Write); }
        public void Write(HighSpeedArray<Vector3> array) { Write(array, Write); }
        public void Write(HighSpeedArray<Vector4> array) { Write(array, Write); }
        public void Write(HighSpeedArray<Quaternion> array) { Write(array, Write); }
        public void Write(HighSpeedArray<Vector2> array, bool signed) { Write(array, signed, Write); }
        public void Write(HighSpeedArray<Vector3> array, bool signed) { Write(array, signed, Write); }
        public void Write(HighSpeedArray<Vector4> array, bool signed) { Write(array, signed, Write); }
    };
}
