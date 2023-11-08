using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Server.Packets;

namespace Server
{
    internal class Server
    {
        public TcpListener? TcpListener { get; set; }
        public ConcurrentDictionary<Guid, Client> Clients { get; set; } = new ConcurrentDictionary<Guid, Client>();
        public const int Port = 15432;
        public static readonly IPAddress ConnectionAddress = IPAddress.Any;
        public ModHandler ModHandler { get; set; }
        public CancellationTokenSource ClientCancellationSource, ListenerCancellationSource;
        public TaskFactory ClientFactory { get; set; }
        public Dictionary<Guid, Task> Tasks { get; set; } = new Dictionary<Guid, Task>();
        public event EventHandler<ClientEventArgs>? ClientClosed;
        private Task? ListenerTask;

        private bool acceptConnections = false;

        public Server()
        {
            ClientClosed += DisposeClient;
            ModHandler = new ModHandler();
            ClientCancellationSource = new CancellationTokenSource();
            ListenerCancellationSource = new CancellationTokenSource();
            ClientFactory = new TaskFactory(ClientCancellationSource.Token);
        }

        public void Start()
        {
            CancellationToken listenerCancellationToken = ListenerCancellationSource.Token;
            ListenerTask = Task.Run(Accept, listenerCancellationToken);

            while (true)
            {
                string? command = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(command))
                    continue;
                switch (command)
                {
                    case "c":
                        CloseServer();
                        return;
                    case "r":
                        ModHandler.UpdateCatalogue();
                        Console.WriteLine("Rescanned mods");
                        break;
                    case "h":
                        Console.WriteLine("Help:");
                        Console.WriteLine(" c - Closes the program.");
                        Console.WriteLine(" r - updates the mod catalogue.");
                        break;
                }
            }

        }

        public void CloseServer()
        {
            ListenerCancellationSource.Cancel();
            ClientCancellationSource.Cancel();
            Task.WaitAll(Tasks.Values.ToArray());
            ListenerTask?.Wait();
        }

        public async Task Accept()
        {
            if (TcpListener == null)
            {
                TcpListener = new TcpListener(ConnectionAddress, Port);
            }

            TcpListener.Start();
            Console.WriteLine($"Listener Opened on Port {Port}");
            try
            {
                while (true)
                {
                    TcpClient client = await TcpListener.AcceptTcpClientAsync(ListenerCancellationSource.Token);
                    ListenerCancellationSource.Token.ThrowIfCancellationRequested();
                    if (client != null)
                    {
                        Guid id;
                        do
                        {
                            id = Guid.NewGuid();
                        } while (Clients.ContainsKey(id));
                        Client cli = new Client(client, id, this);
                        Clients.TryAdd(id, cli);
                        Tasks.Add(id, ClientFactory.StartNew(async () =>
                        {
                            await HandleClient(id);
                        }, ClientCancellationSource.Token));
                    }
                }
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("Listener Closed (Operation Cancelled)");
                TcpListener.Stop();
            }
        }

        public async Task HandleClient(Guid uniqueId)
        {
            if (!Clients.ContainsKey(uniqueId))
            {
                throw new KeyNotFoundException("Cannot use client as key is invalid.");
            }

            Client cli = Clients[uniqueId];
            if (cli != null)
            {
                Console.WriteLine($"Client Connected: {cli.client.Client.RemoteEndPoint}");
                await cli.HandleConnection(ClientCancellationSource.Token);
                ClientClosed?.Invoke(this, new ClientEventArgs(uniqueId));
            }
        }

        public void DisposeClient(object? sender, ClientEventArgs args)
        {
            if (args == null)
                return;
            if (!Clients.ContainsKey(args.uniqueId))
                //Either error has occurred or the client has been disposed of already.
                return;

            Client? client;
            Clients.TryGetValue(args.uniqueId, out client);
            if (client != null)
            {
                client.client.Dispose();
            }
            Console.WriteLine(args.uniqueId + " Disconnected.");
            Clients.TryRemove(args.uniqueId, out client);
        }

        public async Task SendModList(Guid guid, CancellationToken canToken)
        {
            if (!Clients.ContainsKey(guid))
            {
                throw new KeyNotFoundException($"{Guid.NewGuid()} does not exist.");
            }

            Client client = Clients[guid];

            if (client == null || client?.stream == null || client?.stream?.CanRead == false)
            {
                ClientClosed?.Invoke(this, new ClientEventArgs(guid));
                return;
            }

            byte[] data = ModHandler.GetBytes(ModHandler.Mods);
            data[15] = 1;
            data[439242] = 1;
            Packet packet = new Packet(TransferTypes.SendModList, data);
            await client.SendPacketAsync(packet);
            Console.WriteLine("Mod List sent to " + client.client.Client.RemoteEndPoint);
        }

    }

    public class ClientEventArgs : EventArgs
    {
        public Guid uniqueId;
        public ClientEventArgs(Guid guid)
        {
            uniqueId = guid;
        }
    }
}
