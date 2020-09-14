using System;
using System.Text;
using UnityEngine;

namespace GameWorkstore.NetworkLibrary
{
    /*
    // Binary stream Writer. Supports simple types, buffers, arrays, structs, and nested types
        */
    public class NetWriter
    {
        const int k_MaxStringLength = 1024 * 32;
        NetBuffer m_Buffer;
        static Encoding s_Encoding;
        static byte[] s_StringWriteBuffer;

        public NetWriter()
        {
            m_Buffer = new NetBuffer();
            if (s_Encoding == null)
            {
                s_Encoding = new UTF8Encoding();
                s_StringWriteBuffer = new byte[k_MaxStringLength];
            }
        }

        public NetWriter(byte[] buffer)
        {
            m_Buffer = new NetBuffer(buffer);
            if (s_Encoding == null)
            {
                s_Encoding = new UTF8Encoding();
                s_StringWriteBuffer = new byte[k_MaxStringLength];
            }
        }

        public short Position { get { return (short)m_Buffer.Position; } }

        public byte[] ToArray()
        {
            var newArray = new byte[m_Buffer.AsArraySegment().Count];
            Array.Copy(m_Buffer.AsArraySegment().Array, newArray, m_Buffer.AsArraySegment().Count);
            return newArray;
        }

        public byte[] AsArray()
        {
            return AsArraySegment().Array;
        }

        internal ArraySegment<byte> AsArraySegment()
        {
            return m_Buffer.AsArraySegment();
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
            m_Buffer.WriteByte((byte)value);
        }

        public void Write(byte value)
        {
            m_Buffer.WriteByte(value);
        }

        public void Write(sbyte value)
        {
            m_Buffer.WriteByte((byte)value);
        }

        public void Write(short value)
        {
            m_Buffer.WriteByte2((byte)(value & 0xff), (byte)((value >> 8) & 0xff));
        }

        public void Write(ushort value)
        {
            m_Buffer.WriteByte2((byte)(value & 0xff), (byte)((value >> 8) & 0xff));
        }

        public void Write(int value)
        {
            // little endian...
            m_Buffer.WriteByte4(
                (byte)(value & 0xff),
                (byte)((value >> 8) & 0xff),
                (byte)((value >> 16) & 0xff),
                (byte)((value >> 24) & 0xff));
        }

        public void Write(uint value)
        {
            m_Buffer.WriteByte4(
                (byte)(value & 0xff),
                (byte)((value >> 8) & 0xff),
                (byte)((value >> 16) & 0xff),
                (byte)((value >> 24) & 0xff));
        }

        public void Write(long value)
        {
            m_Buffer.WriteByte8(
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
            m_Buffer.WriteByte8(
                (byte)(value & 0xff),
                (byte)((value >> 8) & 0xff),
                (byte)((value >> 16) & 0xff),
                (byte)((value >> 24) & 0xff),
                (byte)((value >> 32) & 0xff),
                (byte)((value >> 40) & 0xff),
                (byte)((value >> 48) & 0xff),
                (byte)((value >> 56) & 0xff));
        }

#if !INCLUDE_IL2CPP
        private static UIntFloat s_FloatConverter;
#endif

        public void Write(float value)
        {
#if INCLUDE_IL2CPP
            Write(BitConverter.ToUInt32(BitConverter.GetBytes(value), 0));
#else
            s_FloatConverter.floatValue = value;
            Write(s_FloatConverter.intValue);
#endif
        }

        public void Write(double value)
        {
#if INCLUDE_IL2CPP
            Write(BitConverter.ToUInt64(BitConverter.GetBytes(value), 0));
#else
            s_FloatConverter.doubleValue = value;
            Write(s_FloatConverter.longValue);
#endif
        }

        public void Write(string value)
        {
            if (value == null)
            {
                m_Buffer.WriteByte2(0, 0);
                return;
            }

            int len = s_Encoding.GetByteCount(value);

            if (len >= k_MaxStringLength)
            {
                throw new IndexOutOfRangeException("Serialize(string) too long: " + value.Length);
            }

            Write((ushort)(len));
            int numBytes = s_Encoding.GetBytes(value, 0, value.Length, s_StringWriteBuffer, 0);
            m_Buffer.WriteBytes(s_StringWriteBuffer, (ushort)numBytes);
        }

        public void Write(bool value)
        {
            if (value)
                m_Buffer.WriteByte(1);
            else
                m_Buffer.WriteByte(0);
        }

        public void Write(byte[] buffer, int count)
        {
            if (count > ushort.MaxValue)
            {
                if (LogFilter.logError) { Debug.LogError("NetworkWriter Write: buffer is too large (" + count + ") bytes. The maximum buffer size is 64K bytes."); }
                return;
            }
            m_Buffer.WriteBytes(buffer, (ushort)count);
        }

        public void Write(byte[] buffer, int offset, int count)
        {
            if (count > ushort.MaxValue)
            {
                if (LogFilter.logError) { Debug.LogError("NetworkWriter Write: buffer is too large (" + count + ") bytes. The maximum buffer size is 64K bytes."); }
                return;
            }
            m_Buffer.WriteBytesAtOffset(buffer, (ushort)offset, (ushort)count);
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
            m_Buffer.WriteBytes(buffer, (ushort)count);
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
            m_Buffer.WriteBytes(buffer, (UInt16)buffer.Length);
        }

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

        public void Write(MsgBase msg)
        {
            msg.Serialize(this);
        }

        public void SeekZero()
        {
            m_Buffer.SeekZero();
        }

        public void StartMessage(short msgType)
        {
            SeekZero();

            // two bytes for size, will be filled out in FinishMessage
            m_Buffer.WriteByte2(0, 0);

            // two bytes for message type
            Write(msgType);
        }

        public void FinishMessage()
        {
            // writes correct size into space at start of buffer
            m_Buffer.FinishMessage();
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
    };
}
