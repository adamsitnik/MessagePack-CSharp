﻿using MessagePack.Formatters;
using MessagePack.Internal;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Text;

namespace MessagePack
{
    /// <summary>
    /// A primitive types writer for the MessagePack format.
    /// </summary>
    /// <typeparam name="T">The type of buffer writer in use. Use of a concrete type avoids cost of interface dispatch.</typeparam>
    public ref struct MessagePackWriter<T> where T : IBufferWriter<byte>
    {
        /// <summary>
        /// The writer to use.
        /// </summary>
        private BufferWriter writer;

        /// <summary>
        /// Initializes a new instance of the <see cref="MessagePackWriter{T}"/> struct.
        /// </summary>
        /// <param name="writer">The writer to use.</param>
        public MessagePackWriter(IBufferWriter<byte> writer)
        {
            this.writer = new BufferWriter(writer);
        }

        /// <summary>
        /// Writes a <see cref="MessagePackCode.Nil"/> value.
        /// </summary>
        public void WriteNil()
        {
            var span = writer.GetSpan(1);
            span[0] = MessagePackCode.Nil;
            writer.Advance(1);
        }

        /// <summary>
        /// Copies bytes directly into the message pack writer.
        /// </summary>
        /// <param name="rawMessagePackBlock">The span of bytes to copy from.</param>
        public void WriteRaw(ReadOnlySpan<byte> rawMessagePackBlock) => writer.Write(rawMessagePackBlock);

        /// <summary>
        /// Copies bytes directly into the message pack writer.
        /// </summary>
        /// <param name="rawMessagePackBlock">The span of bytes to copy from.</param>
        public void WriteRaw(ReadOnlySequence<byte> rawMessagePackBlock) => rawMessagePackBlock.CopyTo(ref writer);

        /// <summary>
        /// Write the length of the next array to be written in the most compact form of
        /// <see cref="MessagePackCode.MinFixArray"/>,
        /// <see cref="MessagePackCode.Array16"/>, or
        /// <see cref="MessagePackCode.Array32"/>
        /// </summary>
        public void WriteArrayHeader(uint count)
        {
            if (count <= MessagePackRange.MaxFixArrayCount)
            {
                var span = writer.GetSpan(1);
                span[0] = (byte)(MessagePackCode.MinFixArray | count);
                writer.Advance(1);
            }
            else if (count <= ushort.MaxValue)
            {
                var span = writer.GetSpan(3);
                unchecked
                {
                    span[0] = MessagePackCode.Array16;
                    span[1] = (byte)(count >> 8);
                    span[2] = (byte)count;
                }
                writer.Advance(3);
            }
            else
            {
                var span = writer.GetSpan(5);
                unchecked
                {
                    span[0] = MessagePackCode.Array32;
                    span[1] = (byte)(count >> 24);
                    span[2] = (byte)(count >> 16);
                    span[3] = (byte)(count >> 8);
                    span[4] = (byte)count;
                }
                writer.Advance(5);
            }
        }

        /// <summary>
        /// Writes a <see cref="byte"/> value using a 1-byte code when possible, otherwise as <see cref="MessagePackCode.UInt8"/>.
        /// </summary>
        /// <param name="value">The value.</param>
        public void WriteByte(byte value)
        {
            if (value <= MessagePackCode.MaxFixInt)
            {
                var span = writer.GetSpan(1);
                span[0] = value;
                writer.Advance(1);
            }
            else
            {
                var span = writer.GetSpan(2);
                span[0] = MessagePackCode.UInt8;
                span[1] = value;
                writer.Advance(2);
            }
        }

        /// <summary>
        /// Writes an 8-bit value using a 1-byte code when possible, otherwise as <see cref="MessagePackCode.Int8"/>.
        /// </summary>
        /// <param name="value">The value.</param>
        public void WriteSByte(sbyte value)
        {
            if (value < MessagePackRange.MinFixNegativeInt)
            {
                var span = writer.GetSpan(2);
                span[0] = MessagePackCode.Int8;
                span[1] = unchecked((byte)value);
                writer.Advance(2);
            }
            else
            {
                var span = writer.GetSpan(1);
                span[0] = unchecked((byte)value);
                writer.Advance(1);
            }
        }

        /// <summary>
        /// Writes a <see cref="ushort"/> value using a 1-byte code when possible, otherwise as <see cref="MessagePackCode.UInt8"/> or <see cref="MessagePackCode.UInt16"/>.
        /// </summary>
        /// <param name="value">The value.</param>
        public void WriteUInt16(ushort value)
        {
            if (value <= MessagePackRange.MaxFixPositiveInt)
            {
                var span = writer.GetSpan(1);
                span[0] = unchecked((byte)value);
                writer.Advance(1);
            }
            else if (value <= byte.MaxValue)
            {
                var span = writer.GetSpan(2);
                span[0] = MessagePackCode.UInt8;
                span[1] = unchecked((byte)value);
                writer.Advance(2);
            }
            else
            {
                var span = writer.GetSpan(3);
                span[0] = MessagePackCode.UInt16;
                span[1] = unchecked((byte)(value >> 8));
                span[2] = unchecked((byte)value);
                writer.Advance(3);
            }
        }

        /// <summary>
        /// Writes a <see cref="short"/> using a built-in 1-byte code when within specific MessagePack-supported ranges,
        /// or the most compact of
        /// <see cref="MessagePackCode.UInt8"/>,
        /// <see cref="MessagePackCode.UInt16"/>,
        /// <see cref="MessagePackCode.Int8"/>, or
        /// <see cref="MessagePackCode.Int16"/>
        /// </summary>
        /// <param name="value">The value to write.</param>
        public void WriteInt16(short value)
        {
            if (value >= 0)
            {
                WriteUInt16((ushort)value);
            }
            else
            {
                // negative int(use int)
                if (MessagePackRange.MinFixNegativeInt <= value)
                {
                    var span = writer.GetSpan(1);
                    span[0] = unchecked((byte)value);
                    writer.Advance(1);
                }
                else if (sbyte.MinValue <= value)
                {
                    var span = writer.GetSpan(2);
                    span[0] = MessagePackCode.Int8;
                    span[1] = unchecked((byte)value);
                    writer.Advance(2);
                }
                else
                {
                    var span = writer.GetSpan(3);
                    span[0] = MessagePackCode.Int16;
                    span[1] = unchecked((byte)(value >> 8));
                    span[2] = unchecked((byte)value);
                    writer.Advance(3);
                }
            }
        }

        /// <summary>
        /// Writes an <see cref="uint"/> using a built-in 1-byte code when within specific MessagePack-supported ranges,
        /// or the most compact of
        /// <see cref="MessagePackCode.UInt8"/>,
        /// <see cref="MessagePackCode.UInt16"/>, or
        /// <see cref="MessagePackCode.UInt32"/>
        /// </summary>
        /// <param name="value">The value to write.</param>
        public void WriteUInt32(uint value)
        {
            if (value <= MessagePackRange.MaxFixPositiveInt)
            {
                var span = writer.GetSpan(1);
                span[0] = unchecked((byte)value);
                writer.Advance(1);
            }
            else if (value <= byte.MaxValue)
            {
                var span = writer.GetSpan(2);
                span[0] = MessagePackCode.UInt8;
                span[1] = unchecked((byte)value);
                writer.Advance(2);
            }
            else if (value <= ushort.MaxValue)
            {
                var span = writer.GetSpan(3);
                span[0] = MessagePackCode.UInt16;
                span[1] = unchecked((byte)(value >> 8));
                span[2] = unchecked((byte)value);
                writer.Advance(3);
            }
            else
            {
                var span = writer.GetSpan(5);
                span[0] = MessagePackCode.UInt32;
                span[1] = unchecked((byte)(value >> 24));
                span[2] = unchecked((byte)(value >> 16));
                span[3] = unchecked((byte)(value >> 8));
                span[4] = unchecked((byte)value);
                writer.Advance(5);
            }
        }

        /// <summary>
        /// Writes an <see cref="int"/> using a built-in 1-byte code when within specific MessagePack-supported ranges,
        /// or the most compact of
        /// <see cref="MessagePackCode.UInt8"/>,
        /// <see cref="MessagePackCode.UInt16"/>,
        /// <see cref="MessagePackCode.UInt32"/>,
        /// <see cref="MessagePackCode.Int8"/>,
        /// <see cref="MessagePackCode.Int16"/>,
        /// <see cref="MessagePackCode.Int32"/>
        /// </summary>
        /// <param name="value">The value to write.</param>
        public void WriteInt32(int value)
        {
            if (value >= 0)
            {
                WriteUInt32((uint)value);
            }
            else
            {
                // negative int(use int)
                if (MessagePackRange.MinFixNegativeInt <= value)
                {
                    var span = writer.GetSpan(1);
                    span[0] = unchecked((byte)value);
                    writer.Advance(1);
                }
                else if (sbyte.MinValue <= value)
                {
                    var span = writer.GetSpan(2);
                    span[0] = MessagePackCode.Int8;
                    span[1] = unchecked((byte)value);
                    writer.Advance(2);
                }
                else if (short.MinValue <= value)
                {
                    var span = writer.GetSpan(3);
                    span[0] = MessagePackCode.Int16;
                    span[1] = unchecked((byte)(value >> 8));
                    span[2] = unchecked((byte)value);
                    writer.Advance(3);
                }
                else
                {
                    var span = writer.GetSpan(5);
                    span[0] = MessagePackCode.Int32;
                    span[1] = unchecked((byte)(value >> 24));
                    span[2] = unchecked((byte)(value >> 16));
                    span[3] = unchecked((byte)(value >> 8));
                    span[4] = unchecked((byte)value);
                    writer.Advance(5);
                }
            }
        }

        /// <summary>
        /// Writes an <see cref="ulong"/> using a built-in 1-byte code when within specific MessagePack-supported ranges,
        /// or the most compact of
        /// <see cref="MessagePackCode.UInt8"/>,
        /// <see cref="MessagePackCode.UInt16"/>,
        /// <see cref="MessagePackCode.UInt32"/>,
        /// <see cref="MessagePackCode.Int8"/>,
        /// <see cref="MessagePackCode.Int16"/>,
        /// <see cref="MessagePackCode.Int32"/>
        /// </summary>
        /// <param name="value">The value to write.</param>
        public void WriteUInt64(ulong value)
        {
            if (value <= MessagePackRange.MaxFixPositiveInt)
            {
                var span = writer.GetSpan(1);
                span[0] = unchecked((byte)value);
                writer.Advance(1);
            }
            else if (value <= byte.MaxValue)
            {
                var span = writer.GetSpan(2);
                span[0] = MessagePackCode.UInt8;
                span[1] = unchecked((byte)value);
                writer.Advance(2);
            }
            else if (value <= ushort.MaxValue)
            {
                var span = writer.GetSpan(3);
                span[0] = MessagePackCode.UInt16;
                span[1] = unchecked((byte)(value >> 8));
                span[2] = unchecked((byte)value);
                writer.Advance(3);
            }
            else if (value <= uint.MaxValue)
            {
                var span = writer.GetSpan(5);
                span[0] = MessagePackCode.UInt32;
                span[1] = unchecked((byte)(value >> 24));
                span[2] = unchecked((byte)(value >> 16));
                span[3] = unchecked((byte)(value >> 8));
                span[4] = unchecked((byte)value);
                writer.Advance(5);
            }
            else
            {
                var span = writer.GetSpan(9);
                span[0] = MessagePackCode.UInt64;
                span[1] = unchecked((byte)(value >> 56));
                span[2] = unchecked((byte)(value >> 48));
                span[3] = unchecked((byte)(value >> 40));
                span[4] = unchecked((byte)(value >> 32));
                span[5] = unchecked((byte)(value >> 24));
                span[6] = unchecked((byte)(value >> 16));
                span[7] = unchecked((byte)(value >> 8));
                span[8] = unchecked((byte)value);
                writer.Advance(9);
            }
        }

        /// <summary>
        /// Writes an <see cref="long"/> using a built-in 1-byte code when within specific MessagePack-supported ranges,
        /// or the most compact of
        /// <see cref="MessagePackCode.UInt8"/>,
        /// <see cref="MessagePackCode.UInt16"/>,
        /// <see cref="MessagePackCode.UInt32"/>,
        /// <see cref="MessagePackCode.UInt64"/>,
        /// <see cref="MessagePackCode.Int8"/>,
        /// <see cref="MessagePackCode.Int16"/>,
        /// <see cref="MessagePackCode.Int32"/>,
        /// <see cref="MessagePackCode.Int64"/>
        /// </summary>
        /// <param name="value">The value to write.</param>
        public void WriteInt64(long value)
        {
            if (value >= 0)
            {
                WriteUInt64((ulong)value);
            }
            else
            {
                // negative int(use int)
                if (MessagePackRange.MinFixNegativeInt <= value)
                {
                    var span = writer.GetSpan(1);
                    span[0] = unchecked((byte)value);
                    writer.Advance(1);
                }
                else if (sbyte.MinValue <= value)
                {
                    var span = writer.GetSpan(2);
                    span[0] = MessagePackCode.Int8;
                    span[1] = unchecked((byte)value);
                    writer.Advance(2);
                }
                else if (short.MinValue <= value)
                {
                    var span = writer.GetSpan(3);
                    span[0] = MessagePackCode.Int16;
                    span[1] = unchecked((byte)(value >> 8));
                    span[2] = unchecked((byte)value);
                    writer.Advance(3);
                }
                else if (int.MinValue <= value)
                {
                    var span = writer.GetSpan(5);
                    span[0] = MessagePackCode.Int32;
                    span[1] = unchecked((byte)(value >> 24));
                    span[2] = unchecked((byte)(value >> 16));
                    span[3] = unchecked((byte)(value >> 8));
                    span[4] = unchecked((byte)value);
                    writer.Advance(5);
                }
                else
                {
                    var span = writer.GetSpan(9);
                    span[0] = MessagePackCode.Int64;
                    span[1] = unchecked((byte)(value >> 56));
                    span[2] = unchecked((byte)(value >> 48));
                    span[3] = unchecked((byte)(value >> 40));
                    span[4] = unchecked((byte)(value >> 32));
                    span[5] = unchecked((byte)(value >> 24));
                    span[6] = unchecked((byte)(value >> 16));
                    span[7] = unchecked((byte)(value >> 8));
                    span[8] = unchecked((byte)value);
                    writer.Advance(9);
                }
            }
        }

        /// <summary>
        /// Writes a <see cref="bool"/> value using either <see cref="MessagePackCode.True"/> or <see cref="MessagePackCode.False"/>.
        /// </summary>
        /// <param name="value">The value.</param>
        public void WriteBoolean(bool value)
        {
            var span = writer.GetSpan(1);
            span[0] = value ? MessagePackCode.True : MessagePackCode.False;
            writer.Advance(1);
        }

        /// <summary>
        /// Writes a <see cref="char"/> value using a 1-byte code when possible, otherwise as <see cref="MessagePackCode.UInt8"/> or <see cref="MessagePackCode.UInt16"/>.
        /// </summary>
        /// <param name="value">The value.</param>
        public void WriteChar(char value) => WriteUInt16(value);

        /// <summary>
        /// Writes a <see cref="MessagePackCode.Float32"/> value.
        /// </summary>
        /// <param name="value">The value.</param>
        public void WriteSingle(float value)
        {
            var span = writer.GetSpan(5);

            span[0] = MessagePackCode.Float32;

            var num = new Float32Bits(value);
            if (BitConverter.IsLittleEndian)
            {
                span[1] = num.Byte3;
                span[2] = num.Byte2;
                span[3] = num.Byte1;
                span[4] = num.Byte0;
            }
            else
            {
                span[1] = num.Byte0;
                span[2] = num.Byte1;
                span[3] = num.Byte2;
                span[4] = num.Byte3;
            }

            writer.Advance(5);
        }

        /// <summary>
        /// Writes a <see cref="MessagePackCode.Float64"/> value.
        /// </summary>
        /// <param name="value">The value.</param>
        public void WriteDouble(double value)
        {
            var span = writer.GetSpan(9);

            span[0] = MessagePackCode.Float64;

            var num = new Float64Bits(value);
            if (BitConverter.IsLittleEndian)
            {
                span[1] = num.Byte7;
                span[2] = num.Byte6;
                span[3] = num.Byte5;
                span[4] = num.Byte4;
                span[5] = num.Byte3;
                span[6] = num.Byte2;
                span[7] = num.Byte1;
                span[8] = num.Byte0;
            }
            else
            {
                span[1] = num.Byte0;
                span[2] = num.Byte1;
                span[3] = num.Byte2;
                span[4] = num.Byte3;
                span[5] = num.Byte4;
                span[6] = num.Byte5;
                span[7] = num.Byte6;
                span[8] = num.Byte7;
            }

            writer.Advance(9);
        }

        /// <summary>
        /// Writes a <see cref="DateTime"/> using the message code <see cref="ReservedMessagePackExtensionTypeCode.DateTime"/>.
        /// </summary>
        /// <param name="dateTime">The value to write.</param>
        public void WriteDateTime(DateTime dateTime)
        {
            // Timestamp spec
            // https://github.com/msgpack/msgpack/pull/209
            // FixExt4(-1) => seconds |  [1970-01-01 00:00:00 UTC, 2106-02-07 06:28:16 UTC) range
            // FixExt8(-1) => nanoseconds + seconds | [1970-01-01 00:00:00.000000000 UTC, 2514-05-30 01:53:04.000000000 UTC) range
            // Ext8(12,-1) => nanoseconds + seconds | [-584554047284-02-23 16:59:44 UTC, 584554051223-11-09 07:00:16.000000000 UTC) range
            dateTime = dateTime.ToUniversalTime();

            var secondsSinceBclEpoch = dateTime.Ticks / TimeSpan.TicksPerSecond;
            var seconds = secondsSinceBclEpoch - DateTimeConstants.BclSecondsAtUnixEpoch;
            var nanoseconds = (dateTime.Ticks % TimeSpan.TicksPerSecond) * DateTimeConstants.NanosecondsPerTick;

            // reference pseudo code.
            /*
            struct timespec {
                long tv_sec;  // seconds
                long tv_nsec; // nanoseconds
            } time;
            if ((time.tv_sec >> 34) == 0)
            {
                uint64_t data64 = (time.tv_nsec << 34) | time.tv_sec;
                if (data & 0xffffffff00000000L == 0)
                {
                    // timestamp 32
                    uint32_t data32 = data64;
                    serialize(0xd6, -1, data32)
                }
                else
                {
                    // timestamp 64
                    serialize(0xd7, -1, data64)
                }
            }
            else
            {
                // timestamp 96
                serialize(0xc7, 12, -1, time.tv_nsec, time.tv_sec)
            }
            */

            if ((seconds >> 34) == 0)
            {
                var data64 = unchecked((ulong)((nanoseconds << 34) | seconds));
                if ((data64 & 0xffffffff00000000L) == 0)
                {
                    // timestamp 32(seconds in 32-bit unsigned int)
                    var data32 = (UInt32)data64;
                    var span = writer.GetSpan(6);
                    span[0] = MessagePackCode.FixExt4;
                    span[1] = unchecked((byte)ReservedMessagePackExtensionTypeCode.DateTime);
                    span[2] = unchecked((byte)(data32 >> 24));
                    span[3] = unchecked((byte)(data32 >> 16));
                    span[4] = unchecked((byte)(data32 >> 8));
                    span[5] = unchecked((byte)data32);
                    writer.Advance(6);
                }
                else
                {
                    // timestamp 64(nanoseconds in 30-bit unsigned int | seconds in 34-bit unsigned int)
                    var span = writer.GetSpan(10);
                    span[0] = MessagePackCode.FixExt8;
                    span[1] = unchecked((byte)ReservedMessagePackExtensionTypeCode.DateTime);
                    span[2] = unchecked((byte)(data64 >> 56));
                    span[3] = unchecked((byte)(data64 >> 48));
                    span[4] = unchecked((byte)(data64 >> 40));
                    span[5] = unchecked((byte)(data64 >> 32));
                    span[6] = unchecked((byte)(data64 >> 24));
                    span[7] = unchecked((byte)(data64 >> 16));
                    span[8] = unchecked((byte)(data64 >> 8));
                    span[9] = unchecked((byte)data64);
                    writer.Advance(10);
                }
            }
            else
            {
                // timestamp 96( nanoseconds in 32-bit unsigned int | seconds in 64-bit signed int )
                var span = writer.GetSpan(15);
                span[0] = MessagePackCode.Ext8;
                span[1] = 12;
                span[2] = unchecked((byte)ReservedMessagePackExtensionTypeCode.DateTime);
                span[3] = unchecked((byte)(nanoseconds >> 24));
                span[4] = unchecked((byte)(nanoseconds >> 16));
                span[5] = unchecked((byte)(nanoseconds >> 8));
                span[6] = unchecked((byte)nanoseconds);
                span[7] = unchecked((byte)(seconds >> 56));
                span[8] = unchecked((byte)(seconds >> 48));
                span[9] = unchecked((byte)(seconds >> 40));
                span[10] = unchecked((byte)(seconds >> 32));
                span[11] = unchecked((byte)(seconds >> 24));
                span[12] = unchecked((byte)(seconds >> 16));
                span[13] = unchecked((byte)(seconds >> 8));
                span[14] = unchecked((byte)seconds);
                writer.Advance(15);
            }
        }

        /// <summary>
        /// Writes a span of bytes, prefixed with a length encoded as the smallest fitting from:
        /// <see cref="MessagePackCode.Bin8"/>,
        /// <see cref="MessagePackCode.Bin16"/>, or
        /// <see cref="MessagePackCode.Bin32"/>,
        /// </summary>
        /// <param name="src">The span of bytes to write.</param>
        public void WriteBytes(ReadOnlySpan<byte> src)
        {
            if (src.Length <= byte.MaxValue)
            {
                var size = src.Length + 2;
                var span = writer.GetSpan(size);

                span[0] = MessagePackCode.Bin8;
                span[1] = (byte)src.Length;

                src.CopyTo(span.Slice(2));
                writer.Advance(size);
            }
            else if (src.Length <= UInt16.MaxValue)
            {
                var size = src.Length + 3;
                var span = writer.GetSpan(size);

                unchecked
                {
                    span[0] = MessagePackCode.Bin16;
                    span[1] = (byte)(src.Length >> 8);
                    span[2] = (byte)src.Length;
                }

                src.CopyTo(span.Slice(3));
                writer.Advance(size);
            }
            else
            {
                var size = src.Length + 5;
                var span = writer.GetSpan(size);

                unchecked
                {
                    span[0] = MessagePackCode.Bin32;
                    span[1] = (byte)(src.Length >> 24);
                    span[2] = (byte)(src.Length >> 16);
                    span[3] = (byte)(src.Length >> 8);
                    span[4] = (byte)src.Length;
                }

                src.CopyTo(span.Slice(5));
                writer.Advance(size);
            }
        }

        /// <summary>
        /// Writes out an array of bytes that represent a UTF-8 encoded string, prefixed with the length using one of these message codes:
        /// <see cref="MessagePackCode.MinFixStr"/>,
        /// <see cref="MessagePackCode.Str8"/>,
        /// <see cref="MessagePackCode.Str16"/>,
        /// <see cref="MessagePackCode.Str32"/>,
        /// </summary>
        /// <param name="utf8stringBytes">The bytes to write.</param>
        public void WriteStringBytes(ReadOnlySpan<byte> utf8stringBytes)
        {
            var byteCount = utf8stringBytes.Length;
            if (byteCount <= MessagePackRange.MaxFixStringLength)
            {
                var span = writer.GetSpan(byteCount + 1);
                span[0] = (byte)(MessagePackCode.MinFixStr | byteCount);
                utf8stringBytes.CopyTo(span.Slice(1));
                writer.Advance(byteCount + 1);
            }
            else if (byteCount <= byte.MaxValue)
            {
                var span = writer.GetSpan(byteCount + 2);
                span[0] = MessagePackCode.Str8;
                span[1] = unchecked((byte)byteCount);
                utf8stringBytes.CopyTo(span.Slice(2));
                writer.Advance(byteCount + 2);
            }
            else if (byteCount <= ushort.MaxValue)
            {
                var span = writer.GetSpan(byteCount + 3);
                span[0] = MessagePackCode.Str16;
                span[1] = unchecked((byte)(byteCount >> 8));
                span[2] = unchecked((byte)byteCount);
                utf8stringBytes.CopyTo(span.Slice(3));
                writer.Advance(byteCount + 3);
            }
            else
            {
                var span = writer.GetSpan(byteCount + 5);
                span[0] = MessagePackCode.Str32;
                span[1] = unchecked((byte)(byteCount >> 24));
                span[2] = unchecked((byte)(byteCount >> 16));
                span[3] = unchecked((byte)(byteCount >> 8));
                span[4] = unchecked((byte)byteCount);
                utf8stringBytes.CopyTo(span.Slice(5));
                writer.Advance(byteCount + 5);
            }
        }

        /// <summary>
        /// Writes out a <see cref="string"/>, prefixed with the length using one of these message codes:
        /// <see cref="MessagePackCode.Nil"/>,
        /// <see cref="MessagePackCode.MinFixStr"/>,
        /// <see cref="MessagePackCode.Str8"/>,
        /// <see cref="MessagePackCode.Str16"/>,
        /// <see cref="MessagePackCode.Str32"/>,
        /// </summary>
        /// <param name="value">The value to write.</param>
        public void WriteString(string value)
        {
            if (value == null)
            {
                WriteNil();
                return;
            }

            WriteString(value.AsSpan());
        }

        /// <summary>
        /// Writes out a <see cref="string"/>, prefixed with the length using one of these message codes:
        /// <see cref="MessagePackCode.MinFixStr"/>,
        /// <see cref="MessagePackCode.Str8"/>,
        /// <see cref="MessagePackCode.Str16"/>,
        /// <see cref="MessagePackCode.Str32"/>,
        /// </summary>
        /// <param name="value">The value to write.</param>
        public void WriteString(ReadOnlySpan<char> value)
        {
            // MaxByteCount -> WritePrefix -> GetBytes has some overheads of `MaxByteCount`
            // solves heuristic length check

            // ensure buffer by MaxByteCount(faster than GetByteCount)
            var span = writer.GetSpan(StringEncoding.UTF8.GetMaxByteCount(value.Length) + 5);

            int useOffset;
            if (value.Length <= MessagePackRange.MaxFixStringLength)
            {
                useOffset = 1;
            }
            else if (value.Length <= byte.MaxValue)
            {
                useOffset = 2;
            }
            else if (value.Length <= ushort.MaxValue)
            {
                useOffset = 3;
            }
            else
            {
                useOffset = 5;
            }

            // skip length area
            var byteCount = StringEncoding.UTF8.GetBytes(value, span.Slice(useOffset));

            // move body and write prefix
            if (byteCount <= MessagePackRange.MaxFixStringLength)
            {
                if (useOffset != 1)
                {
                    span.Slice(useOffset, byteCount).CopyTo(span.Slice(1));
                }
                span[0] = (byte)(MessagePackCode.MinFixStr | byteCount);
                writer.Advance(byteCount + 1);
            }
            else if (byteCount <= byte.MaxValue)
            {
                if (useOffset != 2)
                {
                    span.Slice(useOffset, byteCount).CopyTo(span.Slice(2));
                }

                span[0] = MessagePackCode.Str8;
                span[1] = unchecked((byte)byteCount);
                writer.Advance(byteCount + 2);
            }
            else if (byteCount <= ushort.MaxValue)
            {
                if (useOffset != 3)
                {
                    span.Slice(useOffset, byteCount).CopyTo(span.Slice(3));
                }

                span[0] = MessagePackCode.Str16;
                span[1] = unchecked((byte)(byteCount >> 8));
                span[2] = unchecked((byte)byteCount);
                writer.Advance(byteCount + 3);
            }
            else
            {
                if (useOffset != 5)
                {
                    span.Slice(useOffset, byteCount).CopyTo(span.Slice(5));
                }

                span[0] = MessagePackCode.Str32;
                span[1] = unchecked((byte)(byteCount >> 24));
                span[2] = unchecked((byte)(byteCount >> 16));
                span[3] = unchecked((byte)(byteCount >> 8));
                span[4] = unchecked((byte)byteCount);
                writer.Advance(byteCount + 5);
            }
        }

        /// <summary>
        /// Writes the extension format header, using the smallest one of these codes:
        /// <see cref="MessagePackCode.FixExt1"/>,
        /// <see cref="MessagePackCode.FixExt2"/>,
        /// <see cref="MessagePackCode.FixExt4"/>,
        /// <see cref="MessagePackCode.FixExt8"/>,
        /// <see cref="MessagePackCode.FixExt16"/>,
        /// <see cref="MessagePackCode.Ext8"/>,
        /// <see cref="MessagePackCode.Ext16"/>, or
        /// <see cref="MessagePackCode.Ext32"/>.
        /// </summary>
        /// <param name="extensionHeader">The extension header.</param>
        public void WriteExtensionFormatHeader(ExtensionHeader extensionHeader)
        {
            int dataLength = (int)extensionHeader.Length;
            byte typeCode = (byte)extensionHeader.TypeCode;
            switch (dataLength)
            {
                case 1:
                    var span = writer.GetSpan(2);
                    span[0] = MessagePackCode.FixExt1;
                    span[1] = unchecked(typeCode);
                    writer.Advance(2);
                    return;
                case 2:
                    span = writer.GetSpan(2);
                    span[0] = MessagePackCode.FixExt2;
                    span[1] = unchecked(typeCode);
                    writer.Advance(2);
                    return;
                case 4:
                    span = writer.GetSpan(2);
                    span[0] = MessagePackCode.FixExt4;
                    span[1] = unchecked(typeCode);
                    writer.Advance(2);
                    return;
                case 8:
                    span = writer.GetSpan(2);
                    span[0] = MessagePackCode.FixExt8;
                    span[1] = unchecked(typeCode);
                    writer.Advance(2);
                    return;
                case 16:
                    span = writer.GetSpan(2);
                    span[0] = MessagePackCode.FixExt16;
                    span[1] = unchecked(typeCode);
                    writer.Advance(2);
                    return;
                default:
                    unchecked
                    {
                        if (dataLength <= byte.MaxValue)
                        {
                            span = writer.GetSpan(dataLength + 3);
                            span[0] = MessagePackCode.Ext8;
                            span[1] = unchecked((byte)dataLength);
                            span[2] = unchecked(typeCode);
                            writer.Advance(3);
                        }
                        else if (dataLength <= UInt16.MaxValue)
                        {
                            span = writer.GetSpan(dataLength + 4);
                            span[0] = MessagePackCode.Ext16;
                            span[1] = unchecked((byte)(dataLength >> 8));
                            span[2] = unchecked((byte)dataLength);
                            span[3] = unchecked(typeCode);
                            writer.Advance(4);
                        }
                        else
                        {
                            span = writer.GetSpan(dataLength + 6);
                            span[0] = MessagePackCode.Ext32;
                            span[1] = unchecked((byte)(dataLength >> 24));
                            span[2] = unchecked((byte)(dataLength >> 16));
                            span[3] = unchecked((byte)(dataLength >> 8));
                            span[4] = unchecked((byte)dataLength);
                            span[5] = unchecked(typeCode);
                            writer.Advance(6);
                        }
                    }

                    break;
            }
        }

        /// <summary>
        /// Writes an extension format, using the smallest one of these codes:
        /// <see cref="MessagePackCode.FixExt1"/>,
        /// <see cref="MessagePackCode.FixExt2"/>,
        /// <see cref="MessagePackCode.FixExt4"/>,
        /// <see cref="MessagePackCode.FixExt8"/>,
        /// <see cref="MessagePackCode.FixExt16"/>,
        /// <see cref="MessagePackCode.Ext8"/>,
        /// <see cref="MessagePackCode.Ext16"/>, or
        /// <see cref="MessagePackCode.Ext32"/>.
        /// </summary>
        /// <param name="extensionData">The extension data.</param>
        public void WriteExtensionFormat(ExtensionResult extensionData)
        {
            WriteExtensionFormatHeader(extensionData.Header);
            WriteRaw(extensionData.Data);
        }
    }
}