﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Giselle.Imaging.IO
{
    public abstract class WrappedByteStream : WrappedStream
    {
        public WrappedByteStream(Stream baseStream) : base(baseStream, false)
        {

        }

        public WrappedByteStream(Stream baseStream, bool leaveOpen) : base(baseStream, leaveOpen)
        {

        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            for (var i = 0; i < count; i++)
            {
                var b = this.ReadByte();

                if (b == -1)
                {
                    return i;
                }

                buffer[offset + i] = (byte)b;
            }

            return count;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            for (var i = 0; i < count; i++)
            {
                this.WriteByte(buffer[offset + i]);
            }

        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (this.LeaveOpen == false)
            {
                this.BaseStream.Dispose();
            }

        }

    }

}
