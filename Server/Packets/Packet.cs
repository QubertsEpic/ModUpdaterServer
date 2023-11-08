using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Packets
{
    internal class Packet
    {
        public TransferTypes TransferType;
        public byte[] Data;
        public int DataLength => Data.Length;
        public byte[] Serialise()
        {
            MemoryStream stream = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(stream);

            writer.Write((int)TransferType);
            writer.Write(Data.Length);
            writer.Write(Data);

            writer.Flush();
            byte[] buffer = stream.ToArray();
            stream.Close();
            return buffer;
        }

        public static Packet FromData(byte[] data)
        {
            MemoryStream dataStream = new MemoryStream();
            dataStream.Write(data);
            dataStream.Flush();
            dataStream.Position = 0;

            BinaryReader reader = new BinaryReader(dataStream);
            TransferTypes type = (TransferTypes)reader.ReadInt32();

            int dataLength = reader.ReadInt32();
            byte[] bytes = new byte[dataLength];
            dataStream.Read(bytes);
            dataStream.Close();

            return new Packet(type, bytes);
        }

        public static async Task<Packet> FromStream(Stream stream, CancellationToken token)
        {
            if (!stream.CanRead)
            {
                throw new IOException("Stream cannot read.");
            }
            token.ThrowIfCancellationRequested();
            byte[] buffer = new byte[sizeof(int)];
            await stream.ReadAsync(buffer, 0, buffer.Length, token);
            TransferTypes type = (TransferTypes)BitConverter.ToInt32(buffer);

            buffer = new byte[sizeof(int)];
            await stream.ReadAsync(buffer, 0, buffer.Length, token);
            int length = BitConverter.ToInt32(buffer);
            buffer = new byte[length];
            await stream.ReadAsync(buffer, 0, length, token);

            return new Packet(type, buffer);
        }

        public Packet(TransferTypes types, byte[] data)
        {
            TransferType = types;
            Data = data;
        }
    }
}
