using GameWorkstore.Patterns;
using System;
using System.Runtime.InteropServices;

namespace GameWorkstore.NetworkLibrary
{
    // A growable buffer class used by NetworkReader and NetworkWriter.
    // this is used instead of MemoryStream and BinaryReader/BinaryWriter to avoid allocations.
    class NetBuffer
    {
        byte[] _buffer;
        const int k_InitialSize = 64;
        const float k_GrowthFactor = 1.5f;
        const int k_BufferSizeWarning = 1024 * 1024 * 128;

        public uint Position { get; private set; }

        public NetBuffer()
        {
            _buffer = new byte[k_InitialSize];
        }

        // this does NOT copy the buffer
        public NetBuffer(byte[] buffer)
        {
            _buffer = buffer;
        }

        public byte ReadByte()
        {
            if (Position >= _buffer.Length)
            {
                throw new IndexOutOfRangeException("NetworkReader:ReadByte out of range:" + ToString());
            }

            return _buffer[Position++];
        }

        public void ReadBytes(byte[] buffer, uint count)
        {
            if (Position + count > _buffer.Length)
            {
                throw new IndexOutOfRangeException("NetworkReader:ReadBytes out of range: (" + count + ") " + ToString());
            }

            for (ushort i = 0; i < count; i++)
            {
                buffer[i] = _buffer[Position + i];
            }
            Position += count;
        }

        public void ReadChars(char[] buffer, uint count)
        {
            if (Position + count > _buffer.Length)
            {
                throw new IndexOutOfRangeException("NetworkReader:ReadChars out of range: (" + count + ") " + ToString());
            }
            for (ushort i = 0; i < count; i++)
            {
                buffer[i] = (char)_buffer[Position + i];
            }
            Position += count;
        }

        internal ArraySegment<byte> AsArraySegment()
        {
            return new ArraySegment<byte>(_buffer, 0, (int)Position);
        }

        public void WriteByte(byte value)
        {
            WriteCheckForSpace(1);
            _buffer[Position] = value;
            Position += 1;
        }

        public void WriteByte2(byte value0, byte value1)
        {
            WriteCheckForSpace(2);
            _buffer[Position] = value0;
            _buffer[Position + 1] = value1;
            Position += 2;
        }

        public void WriteByte4(byte value0, byte value1, byte value2, byte value3)
        {
            WriteCheckForSpace(4);
            _buffer[Position] = value0;
            _buffer[Position + 1] = value1;
            _buffer[Position + 2] = value2;
            _buffer[Position + 3] = value3;
            Position += 4;
        }

        public void WriteByte8(byte value0, byte value1, byte value2, byte value3, byte value4, byte value5, byte value6, byte value7)
        {
            WriteCheckForSpace(8);
            _buffer[Position] = value0;
            _buffer[Position + 1] = value1;
            _buffer[Position + 2] = value2;
            _buffer[Position + 3] = value3;
            _buffer[Position + 4] = value4;
            _buffer[Position + 5] = value5;
            _buffer[Position + 6] = value6;
            _buffer[Position + 7] = value7;
            Position += 8;
        }

        // every other Write() function in this class writes implicitly at the end-marker m_Pos.
        // this is the only Write() function that writes to a specific location within the buffer
        public void WriteBytesAtOffset(byte[] buffer, ushort targetOffset, ushort count)
        {
            uint newEnd = (uint)(count + targetOffset);

            WriteCheckForSpace((ushort)newEnd);

            if (targetOffset == 0 && count == buffer.Length)
            {
                buffer.CopyTo(_buffer, Position);
            }
            else
            {
                //CopyTo doesnt take a count :(
                for (int i = 0; i < count; i++)
                {
                    _buffer[targetOffset + i] = buffer[i];
                }
            }

            // although this writes within the buffer, it could move the end-marker
            if (newEnd > Position)
            {
                Position = newEnd;
            }
        }

        public void WriteBytes(byte[] buffer, ushort count)
        {
            WriteCheckForSpace(count);

            if (count == buffer.Length)
            {
                buffer.CopyTo(_buffer, Position);
            }
            else
            {
                //CopyTo doesnt take a count :(
                for (int i = 0; i < count; i++)
                {
                    _buffer[Position + i] = buffer[i];
                }
            }
            Position += count;
        }

        void WriteCheckForSpace(ushort count)
        {
            if (Position + count < _buffer.Length)
                return;

            int newLen = (int)(_buffer.Length * k_GrowthFactor);
            while (Position + count >= newLen)
            {
                newLen = (int)(newLen * k_GrowthFactor);
                if (newLen > k_BufferSizeWarning)
                {
                    DebugMessege.Log("NetworkBuffer size is " + newLen + " bytes!", DebugLevel.WARNING);
                }
            }

            // only do the copy once, even if newLen is increased multiple times
            byte[] tmp = new byte[newLen];
            _buffer.CopyTo(tmp, 0);
            _buffer = tmp;
        }

        public void FinishMessage()
        {
            // two shorts (size and msgType) are in header.
            ushort sz = (ushort)Position;
            sz -= 2; //size
            sz -= 4; //code;
            //write size
            _buffer[0] = (byte)(sz & 0xff);
            _buffer[1] = (byte)((sz >> 8) & 0xff);
        }

        public void SeekZero()
        {
            Position = 0;
        }

        public void Replace(byte[] buffer)
        {
            _buffer = buffer;
            Position = 0;
        }

        public override string ToString()
        {
            return string.Format("NetBuf sz:{0} pos:{1}", _buffer.Length, Position);
        }
    } // end NetBuffer

    // -- helpers for float conversion --
    // This cannot be used with IL2CPP because it cannot convert FieldOffset at the moment
    // Until that is supported the IL2CPP codepath will use BitConverter instead of this. Use
    // of BitConverter is otherwise not optimal as it allocates a byte array for each conversion.
#if !INCLUDE_IL2CPP
    [StructLayout(LayoutKind.Explicit)]
    internal struct UIntFloat
    {
        [FieldOffset(0)]
        public float floatValue;

        [FieldOffset(0)]
        public uint intValue;

        [FieldOffset(0)]
        public double doubleValue;

        [FieldOffset(0)]
        public ulong longValue;
    }

    internal class FloatConversion
    {
        public static float ToSingle(uint value)
        {
            UIntFloat uf = new UIntFloat();
            uf.intValue = value;
            return uf.floatValue;
        }

        public static double ToDouble(ulong value)
        {
            UIntFloat uf = new UIntFloat();
            uf.longValue = value;
            return uf.doubleValue;
        }
    }
#endif // !INCLUDE_IL2CPP
}
