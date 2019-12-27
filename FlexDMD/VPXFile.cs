﻿/* Copyright 2019 Vincent Bousquet

   Licensed under the Apache License, Version 2.0 (the "License");
   you may not use this file except in compliance with the License.
   You may obtain a copy of the License at

       http://www.apache.org/licenses/LICENSE-2.0

   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.
   */
using FlexDMD.Actors;
using NLog;
using OpenMcdf;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text;

namespace FlexDMD
{
    public class VPXFile : IDisposable
    {
        private readonly Dictionary<string, CFStream> _images = new Dictionary<string, CFStream>();
        private CompoundFile _cf;

        public VPXFile(string filename)
        {
            _cf = new CompoundFile(filename);
            CFStorage gameStg = _cf.RootStorage.GetStorage("GameStg");
            UInt32 version = new VPXReader(gameStg.GetStream("Version")).ReadUInt32();
            void visitor(CFItem item)
            {
                if (item.Name.StartsWith("Image") && item is CFStream stream)
                    ReadImage(stream, true);
            }
            gameStg.VisitEntries(visitor, true);
        }

        public void Dispose()
        {
            _cf.Close();
        }

        public bool Contains(string name)
        {
            return _images.ContainsKey(name);
        }

        public Stream OpenStream(string name)
        {
            CFStream stream;
            if (_images.TryGetValue(name, out stream))
            {
                var data = ReadImage(stream, false);
                return new MemoryStream(data);
            }
            return null;
        }

        private byte[] ReadImage(CFStream stream, bool nameOnly)
        {
            var reader = new VPXReader(stream);
            var inJpg = false;
            int size = 0;
            var done = false;
            byte[] data = null;
            while (!done)
            {
                var bytesInRecordRemaining = reader.ReadUInt32();
                var tag = reader.ReadString(Encoding.ASCII, 4);
                switch (tag)
                {
                    case "ENDB":
                        if (inJpg)
                            inJpg = false;
                        else
                            done = true;
                        break;
                    case "NAME":
                        var name = reader.ReadLenPrefixedString(Encoding.ASCII);
                        _images[name] = stream;
                        if (nameOnly) done = true;
                        break;
                    case "PATH":
                        reader.ReadLenPrefixedString(Encoding.ASCII);
                        break;
                    case "INME":
                        reader.ReadLenPrefixedString(Encoding.ASCII);
                        break;
                    case "WDTH":
                        reader.ReadUInt32();
                        break;
                    case "HGHT":
                        reader.ReadUInt32();
                        break;
                    case "SIZE":
                        size = reader.ReadInt32();
                        break;
                    case "ALTV":
                        reader.Skip(4); // skip the float
                        break;
                    case "JPEG":
                        inJpg = true;
                        break;
                    case "DATA":
                        data = reader.Read(size);
                        if (data[0] == 0x89 && data[1] == 0x50 && data[2] == 0x4E && data[3] == 0x47 && data[4] == 0x0D && data[5] == 0x0A && data[6] == 0x1A && data[7] == 0x0A)
                        {
                            // PNG
                        }
                        else if (data[0] == 0xFF && data[1] == 0xD8)
                        {
                            // JPG
                        }
                        else if (data[0] == 0x23 && data[1] == 0x3F && data[2] == 0x52 && data[3] == 0x41 && data[4] == 0x44 && data[5] == 0x49 && data[6] == 0x41 && data[7] == 0x4E && data[8] == 0x43 && data[9] == 0x45 && data[10] == 0x0A)
                        {
                            // HDR (unsupported)
                            data = null;
                        }
                        else
                        {
                            data = null;
                        }
                        break;
                }
            }
            return data;
        }

        private class VPXReader
        {
            private CFStream _stream;
            private bool _reverse;
            private long _pos;
            private byte[] _buffer = new byte[1024];

            public VPXReader(CFStream stream)
            {
                _pos = 0;
                _stream = stream;
                _reverse = BitConverter.IsLittleEndian;
            }

            public void Skip(int count)
            {
                _pos += count;
            }

            public Int32 ReadInt32()
            {
                _stream.Read(_buffer, _pos, 4);
                _pos += 4;
                Int32 v = BitConverter.ToInt32(_buffer, 0);
                if (_reverse) v = BinaryPrimitives.ReverseEndianness(v);
                return v;
            }

            public UInt32 ReadUInt32()
            {
                _stream.Read(_buffer, _pos, 4);
                _pos += 4;
                UInt32 v = BitConverter.ToUInt32(_buffer, 0);
                if (_reverse) v = BinaryPrimitives.ReverseEndianness(v);
                return v;
            }

            public string ReadString(Encoding encoding, int length)
            {
                _stream.Read(_buffer, _pos, length);
                _pos += length;
                return encoding.GetString(_buffer, 0, length);
            }

            public string ReadLenPrefixedString(Encoding encoding)
            {
                var length = (int)ReadUInt32();
                return ReadString(encoding, length);
            }

            public byte[] Read(int length)
            {
                var buffer = new byte[length];
                _stream.Read(buffer, _pos, length);
                return buffer;
            }
        }
    }
}
