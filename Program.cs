using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text;
using Giselle.Imaging.Algorithms.LZW;
using Giselle.Imaging.Collections;
using Giselle.Imaging.IO;

namespace GIFPrototyping
{
    public class Program
    {
        public static void Main()
        {
            var directory = @"C:\Users\Seil\Desktop\Test\Gif\Input";

            foreach (var path in Directory.GetFiles(directory, "*", SearchOption.AllDirectories))
            {
                Console.WriteLine($"===== {path}");

                using (var input = new FileStream(path, FileMode.Open))
                {
                    var processor = new DataProcessor(input) { IsLittleEndian = true };
                    var header = processor.ReadBytes(6);
                    var width = processor.ReadUShort();
                    var height = processor.ReadUShort();

                    var gctInfo = new BitVector32(processor.ReadByte());
                    var gctPresent = gctInfo[0x80];
                    var gctBitDepth = gctInfo[BitVector32.CreateSection(0x07)] + 1;

                    var backgroundColorIndex = processor.ReadByte();
                    var pixelAspectRatio = processor.ReadByte();

                    var gct = gctPresent ? processor.ReadBytes((1 << gctBitDepth) * 3) : new byte[0];

                    while (true)
                    {
                        var blockCode0 = processor.ReadByte();

                        if (blockCode0 == 0x21)
                        {
                            var blockCode1 = processor.ReadByte();
                            var subBlocks = new List<MemoryStream>();

                            while (true)
                            {
                                var subBlockLength = processor.ReadByte();

                                if (subBlockLength == 0)
                                {
                                    break;
                                }

                                var subBlockBytes = processor.ReadBytes(subBlockLength);
                                subBlocks.Add(new MemoryStream(subBlockBytes));
                            }

                            if (blockCode1 == 0xF9) // Graphic Control Extension for frame
                            {
                                var subProcessor0 = new DataProcessor(subBlocks[0]) { IsLittleEndian = true };
                                var bitFields = subProcessor0.ReadByte();
                                var frameDelay = subProcessor0.ReadUShort();
                                var transparentColorIndex = subProcessor0.ReadByte();
                            }
                            else if (blockCode1 == 0xFF) // Application Extension
                            {
                                var subProcessor0 = new DataProcessor(subBlocks[0]) { IsLittleEndian = true };
                                var applicationNameBytes = subProcessor0.ReadBytes(8);
                                var verificationBytes = subProcessor0.ReadBytes(3);
                                var merge = Encoding.ASCII.GetString(applicationNameBytes.Append(verificationBytes).ToArray());

                                var subProcessor1 = new DataProcessor(subBlocks[1]) { IsLittleEndian = true };
                                var dataSubBlockIndex = subProcessor1.ReadByte(); // Alwasy 1
                                var repetitions = subProcessor1.ReadUShort();
                            }

                        }
                        else if (blockCode0 == 0x2C)
                        {
                            var frameX = processor.ReadUShort(); // Coord in image
                            var frameY = processor.ReadUShort();
                            var frameWidth = processor.ReadUShort();
                            var frameHeight = processor.ReadUShort();

                            var lctInfo = new BitVector32(processor.ReadByte());
                            var lctPresent = lctInfo[0x80];
                            var lctBitDepth = lctInfo[BitVector32.CreateSection(0x07)] + 1;
                            var lct = lctPresent ? processor.ReadBytes((1 << lctBitDepth) * 3) : new byte[0];
                            var minimumLZWCodeSize = processor.ReadByte();
                            Console.WriteLine($"localColorTable : {lctInfo}, minimumLZWCodeSize : {minimumLZWCodeSize}");
                            var p = new LZWProcessor(minimumLZWCodeSize, 2);

                            while (true)
                            {
                                var dataLength = processor.ReadByte();

                                if (dataLength == 0)
                                {
                                    break;
                                }

                                var dataBytes = processor.ReadBytes(dataLength);

                                using (var s = new BitStream(new MemoryStream(dataBytes)))
                                {
                                    while (true)
                                    {
                                        var bits = (int)Math.Log2(p.NextKey + 1);
                                        var code = 0;

                                        for (var i = 0; i < bits; i++)
                                        {
                                            var b = s.ReadBit();
                                            var shift = bits - i - 1;
                                            code |= b << shift;
                                        }

                                        var key = p.Decode(code);
                                        Console.WriteLine(bits + " " + code + " " + key);

                                        if (key == -1)
                                        {
                                            break;
                                        }

                                    }

                                }

                            }

                        }
                        else if (blockCode0 == 0x3B)
                        {
                            break;
                        }

                    }

                }

            }

        }

    }

}
