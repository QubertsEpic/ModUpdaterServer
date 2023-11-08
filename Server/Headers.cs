using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    internal class FullHeader
    {
        public int FullSize { get; set; }
        public int ChunkSize { get; set; }
        public int Transfers { get; set; }
        public const int DataSize = sizeof(int) * 3;
        public FullHeader(int fullSize, int chunkSize, int transfers)
        {
            FullSize = fullSize;
            ChunkSize = chunkSize;
            Transfers = transfers;
        }

        public byte[] GetBytes()
        {
            MemoryStream stream = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(stream);

            writer.Write(FullSize);
            writer.Write(ChunkSize);
            writer.Write(Transfers);

            return stream.ToArray();
        }

        public static FullHeader FromBytes(byte[] data)
        {
            MemoryStream stream = new MemoryStream(data);
            stream.Position = 0;
            BinaryReader reader = new BinaryReader(stream);

            int fullsize = reader.ReadInt32();
            int chunksize = reader.ReadInt32();
            int transfers = reader.ReadInt32();

            return new FullHeader(fullsize, chunksize, transfers);
        }
    }
    internal class ChunkHeader
    {
        public int Size { get; set; }
        public int HashSize { get; set; }
        public int CurrentPosition { get; set; }
        public const int DataSize = sizeof(int) * 3;
        public ChunkHeader(int size, int hashSize, int currentPosition)
        {
            Size = size;
            HashSize = hashSize;
            CurrentPosition = currentPosition;
        }

        public byte[] GetBytes()
        {
            MemoryStream stream = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(stream);

            writer.Write(Size);
            writer.Write(HashSize);
            writer.Write(CurrentPosition);

            return stream.ToArray();
        }

        public static ChunkHeader FromBytes(byte[] data)
        {
            MemoryStream stream = new MemoryStream(data);
            stream.Position = 0;
            BinaryReader reader = new BinaryReader(stream);

            int size = reader.ReadInt32();
            int hashSize = reader.ReadInt32();
            int currentPosition = reader.ReadInt32();

            return new ChunkHeader(size, hashSize, currentPosition);
        }
    }
}
