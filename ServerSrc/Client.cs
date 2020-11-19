using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Numerics;

namespace ServerTest
{
    class Client
    {
        public static int dataBufferSize = 4096;
        public int id;
        public Player player;
        public TCP tcp;
        public UDP udp;

        #region Client Networking
        public Client(int _id)
        {
            id = _id;
            tcp = new TCP(id);
            udp = new UDP(id);
        }
        public class TCP
        {
            public TcpClient socket;

            private readonly int id;
            private NetworkStream stream;
            private Packet receivedData;
            private byte[] receiveBuffer;
            public TCP(int _id)
            {
                id = _id;
            }
            public void Connect(TcpClient _socket)
            {
                socket = _socket;
                socket.ReceiveBufferSize = dataBufferSize;
                socket.SendBufferSize = dataBufferSize;

                stream = socket.GetStream();

                receivedData = new Packet();
                receiveBuffer = new byte[dataBufferSize];

                stream.BeginRead(receiveBuffer, 0, dataBufferSize, RecieveCallback, null);
                ServerSend.Welcome(id, "Welcome to the server");
            }

            public void SendData(Packet packet)
            {
                try
                {
                    if(socket != null)
                    {
                        stream.BeginWrite(packet.ToArray(), 0, packet.Length(), null, null);
                    }
                }
                catch(Exception ex)
                {
                    Console.Write($"Error sending data to client {id} via TCP: {ex}");
                }
            }
            private void RecieveCallback(IAsyncResult result)
            {
                try
                {
                    int byteLength = stream.EndRead(result);
                    if(byteLength <=0 )
                    {
                        Server.clients[id].Disconnect();
                        return;
                    }
                    byte[] data = new byte[byteLength];
                    Array.Copy(receiveBuffer, data, byteLength);

                    receivedData.Reset(HandleData(data));
                    stream.BeginRead(receiveBuffer, 0, dataBufferSize, RecieveCallback, null);
                }
                catch(Exception ex)
                {
                    Console.WriteLine($"Error recieving TCP data: {ex}");
                    Server.clients[id].Disconnect();
                }
            }

            private bool HandleData(byte[] data)
            {
                int packetLength = 0;

                receivedData.SetBytes(data);

                if (receivedData.UnreadLength() >= 4)
                {
                    packetLength = receivedData.ReadInt();
                    if (packetLength <= 0)
                    {
                        return true;
                    }
                }

                while (packetLength > 0 && packetLength <= receivedData.UnreadLength())
                {
                    byte[] packetBytes = receivedData.ReadBytes(packetLength);
                    ThreadManager.ExecuteOnMainThread(() =>
                    {
                        using (Packet packet = new Packet(packetBytes))
                        {
                            int packetID = packet.ReadInt();
                            Server.packetHandlers[packetID](id, packet);
                        }
                    });

                    packetLength = 0;
                    if (receivedData.UnreadLength() >= 4)
                    {
                        packetLength = receivedData.ReadInt();
                        if (packetLength <= 0)
                        {
                            return true;
                        }
                    }
                }

                if (packetLength <= 1)
                {
                    return true;
                }
                return false;
            }
            public void Disconnect()
            {
                socket.Close();
                stream = null;
                receivedData = null;
                receiveBuffer = null;
                socket = null;
            }
        }
        public class UDP
        {
            public IPEndPoint endPoint;

            private int id;

            public UDP(int _id)
            {
                id = _id;

            }

            public void Connect(IPEndPoint _endPoint)
            {
                endPoint = _endPoint;
            }

            public void SendData(Packet packet)
            {
                Server.SendUDPData(endPoint, packet);
            }

            public void HandleData(Packet packetData)
            {
                int packetLength = packetData.ReadInt();
                byte[] packetBytes = packetData.ReadBytes(packetLength);

                ThreadManager.ExecuteOnMainThread(() =>
                {
                    using (Packet packet = new Packet(packetBytes))
                    {
                        int packetID = packet.ReadInt();
                        Server.packetHandlers[packetID](id, packet);
                    }
                });
            }
            public void Disconnect()
            {
                endPoint = null;
            }
        }
        #endregion

        public void SendIntoGame(string _playerName)
        {
            player = new Player(id, _playerName, Vector3.Zero);

            foreach(Client c in Server.clients.Values)
            {
                if(c.player != null)
                {
                    if(c.id != id)
                    {
                        ServerSend.SpawnPlayer(id, c.player);
                    }
                }
            }

            foreach(Client c in Server.clients.Values)
            {
                if(c.player != null)
                {
                    ServerSend.SpawnPlayer(c.id, player);
                }
            }
        }

        public void SendIntoGame(Player _player)
        {
            player = _player;

            foreach (Client c in Server.clients.Values)
            {
                if (c.player != null)
                {
                    if (c.id != id)
                    {
                        ServerSend.SpawnPlayer(id, c.player);
                    }
                }
            }

            foreach (Client c in Server.clients.Values)
            {
                if (c.player != null)
                {
                    ServerSend.SpawnPlayer(c.id, player);
                }
            }
        }

        public void Disconnect()
        {
            Save();
            Console.WriteLine($"{tcp.socket.Client.RemoteEndPoint} has disconnected");
            Server.WriteSaveData();

            ThreadManager.ExecuteOnMainThread(() =>
            {
                player = null;
            });


            tcp.Disconnect();
            udp.Disconnect();

            Console.WriteLine("Save data written to disk\n");
        }

        public void Save()
        {
            int loginID = Server.loginInfo[player.username];
            bool existingSaveData = Server.saveData.TryGetValue(loginID, out Player saveData);
            if(existingSaveData)
            {
                Server.saveData[loginID] = player;
                Console.WriteLine($"\nExisting save data found for player: {player.username}(ID:{loginID}) overwritten");
            }
            else
            {
                Server.saveData.Add(loginID, player);
                Console.WriteLine($"\nNew save data created for player: {player.username} (ID:{loginID})");
            }
        }
    }
}
