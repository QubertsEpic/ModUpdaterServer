using Server.ModCatalogue;
using Server.Packets;
using System.ComponentModel;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Numerics;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Transactions;

namespace Server
{
    internal class Client
    {
        public TcpClient client;
        public EndPoint? remoteEndPoint => client.Client.RemoteEndPoint;
        public NetworkStream stream;
        public Guid UniqueId;
        private Server server;
        private CancellationToken token;
        public Client(TcpClient client, Guid id, Server server)
        {
            this.client = client;
            this.stream = client.GetStream();
            this.UniqueId = id;
            this.server = server;
        }

        public async Task HandleConnection(CancellationToken tok)
        {
            if (client == null)
                return;
            if (client.Connected == false)
                return;
            token = tok;
            try
            {
                while (stream != null)
                {
                    byte[] bytes = await ReadBytesAsync(stream, sizeof(int));
                    TransferTypes type = (TransferTypes)BitConverter.ToInt32(bytes, 0);

                    bytes = await ReadBytesAsync(stream, sizeof(int));
                    int dataLength = BitConverter.ToInt32(bytes, 0);
                    if (dataLength != 0)
                    {
                        bytes = await ReadBytesAsync(stream, dataLength);
                    }
                    token.ThrowIfCancellationRequested();

                    switch (type)
                    {
                        case TransferTypes.Ping:
                            Packet packet = new Packet(TransferTypes.Ping, new byte[0]);
                            await SendPacketAsync(packet);
                            Console.WriteLine($"Pinged {client.Client.RemoteEndPoint} {DateTime.Now}");

                            break;
                        case TransferTypes.SendModList:
                            Console.WriteLine("Sending mod list " + remoteEndPoint);
                            await server.SendModList(UniqueId, token);
                            break;
                        case TransferTypes.GetSpecificMods:
                            Console.WriteLine("Getting mod lists.");
                            Dictionary<int, Mod> modList = server.ModHandler.SelectedMods(bytes);
                            Dictionary<int, byte[]> modData = server.ModHandler.GetModData(modList);
                            await SendModData(modData);
                            break;

                    }
                }
            }
            catch (ObjectDisposedException)
            {
                //Client Disconnect
            }
            catch (IOException)
            {
                //Client Disconnect
            }
            catch (OperationCanceledException)
            {
                server.DisposeClient(this, new ClientEventArgs(UniqueId));
            }
        }
        private (MemoryStream stream, BinaryWriter writer) MemoryBinaryWriterCreator()
        {
            MemoryStream memoryStream = new MemoryStream();
            BinaryWriter memWriter = new BinaryWriter(memoryStream);
            return (memoryStream, memWriter);
        }
        //ToDo: Change this into it's own class that handles the mod list.
        private async Task SendModData(Dictionary<int, byte[]> data)
        {
            if(data == null || data.Count < 1)
            {
                Console.WriteLine("Cannot send empty data");
                return;
            }
            Console.WriteLine("Sending Mod Data.");
            MemoryStream stream = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(stream);
            foreach(var keyValue in data)
            {
                writer.Write(keyValue.Key);
                stream.Write(keyValue.Value, 0, keyValue.Value.Length);
            }
            byte[] bytes = stream.ToArray();
            stream.Close();
            Packet packet = new Packet(TransferTypes.SendSpecificMods, bytes);
            await SendPacketAsync(packet);
            
        }
        public async Task SendPacketAsync(Packet packet)
        {
            if (packet == null)
                throw new ArgumentNullException(nameof(packet));
            if (packet.TransferType == TransferTypes.Invalid)
                throw new InvalidOperationException("Packet has not been setup correctly.");

            byte[] data = packet.Serialise();
            await SendDataAsync(data);
        }
        private async Task SendDataAsync(byte[] data, int chunkSize = 524288)
        {
            if (data == null || data.Length < 1)
            {
                throw new ArgumentNullException("data is null cannot send.");
            }
            MemoryStream memstream = new MemoryStream();
            memstream.Write(data);
            memstream.Flush();
            memstream.Position = 0;

            MD5 md5 = MD5.Create();
            int transfers = (int)Math.Ceiling((double)data.Length / (double)chunkSize);
            long bytesLeft = data.Length;

            FullHeader header = new FullHeader(data.Length, chunkSize, transfers);
            byte[] head = header.GetBytes();
            await stream.WriteAsync(head, 0, head.Length);
            await stream.FlushAsync();

            for (int i = 0; i < transfers; i++)
            {
                Console.WriteLine($"Sent Packet {i}: byte {memstream.Position}");
                byte[] bytes = new byte[bytesLeft > chunkSize ? chunkSize : bytesLeft];
                memstream.Read(bytes, 0, bytes.Length);
                bytesLeft -= bytes.Length;
                byte[] hash = md5.ComputeHash(bytes,0, bytes.Length);

                TransferState state;
                do
                {
                    //Writing Order:
                    //Hash
                    await stream.WriteAsync(hash, 0, hash.Length, token);
                    //Data
                    await stream.WriteAsync(bytes, 0, bytes.Length, token);

                    //Flush the data (send it)
                    await stream.FlushAsync(token);
                    //Await for the user to send back the data.
                    byte[] sendBack = await ReadBytesAsync(stream, sizeof(int));
                    state = (TransferState)BitConverter.ToInt32(sendBack);
                } while (state == TransferState.Failed);

                if (state == TransferState.Cancel)
                {
                    Console.WriteLine("Transfer Cancelled");
                    break;
                }
            }


            token.ThrowIfCancellationRequested();
        }
        public async Task<byte[]> RecieveData()
        {
            if (!stream.CanRead)
            {
                throw new IOException("Stream was unable to read.");
            }

            MemoryStream memStream = new MemoryStream();
            MD5 md5 = MD5.Create();

            byte[] headerData = await ReadBytesAsync(stream, FullHeader.DataSize);
            FullHeader header = FullHeader.FromBytes(headerData);
            int bytesLeft = header.FullSize;
            try
            {
                for (int i = 0; i < header.Transfers; i++)
                {
                    byte[] data;
                    TransferState state = TransferState.Failed;
                    do
                    {
                        data = new byte[bytesLeft > header.ChunkSize ? header.ChunkSize : bytesLeft];
                        bytesLeft -= data.Length;

                        token.ThrowIfCancellationRequested();
                        
                        byte[] hash = await ReadBytesAsync(stream, MD5.HashSizeInBytes);
                        data = await ReadBytesAsync(stream, data.Length);

                        byte[] compHash = md5.ComputeHash(data);
                        byte[] transferState;

                        if (hash.SequenceEqual(compHash)) state = TransferState.Successful;
                        transferState = BitConverter.GetBytes((int)state);
                        await stream.WriteAsync(transferState, 0, transferState.Length, token);
                        await stream.FlushAsync(token);

                    } while (state == TransferState.Failed);

                    memStream.Write(data);
                }
            }
            catch (OperationCanceledException)
            {
                byte[] transferState = BitConverter.GetBytes((int)TransferState.Cancel);
                CancellationTokenSource source = new CancellationTokenSource();
                source.CancelAfter(1000);
                await stream.WriteAsync(transferState, 0, transferState.Length, source.Token);
                await stream.FlushAsync(source.Token);

                throw new OperationCanceledException(token);
            }

            return memStream.ToArray();
        }




        private async Task<MemoryStream> ReadToStreamAsync(NetworkStream stream, int length)
        {
            MemoryStream ms = new MemoryStream();
            byte[] data = new byte[length];
            await stream.ReadAsync(data, 0, length, token);
            token.ThrowIfCancellationRequested();
            stream.Write(data);
            return ms;
        }

        public async Task<byte[]> ReadBytesAsync(NetworkStream stream, int length)
        {
            if (stream.CanRead == false)
            {
                throw new IOException("Stream was unable to read.");
            }
            byte[] data = new byte[length];
            int bytesRead = await stream.ReadAsync(data, 0, length, token);
            token.ThrowIfCancellationRequested();

            if (bytesRead < 1)
            {
                throw new IOException("Bytes read was less than 1.");
            }
            return data;
        }
        public int KBtoB(int Kilobytes) => 1024 * Kilobytes;
    }

    public enum TransferState : int
    {
        Failed = 0,
        Successful = 1,
        Cancel = 2
    }
}
