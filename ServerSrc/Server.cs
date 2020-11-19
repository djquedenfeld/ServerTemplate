using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Net;
using System.Net.Sockets;
using System.Numerics;
using System.Collections.Generic;
using System.Runtime.Serialization.Formatters.Binary;

namespace ServerTest
{
    class Server
    {
        public static int Port { get; private set; }
        public static int MaxPlayers { get; private set; }
        public delegate void PacketHandler(int fromClient, Packet packet);

        public static Dictionary<int, PacketHandler> packetHandlers;
        public static Dictionary<int, Client> clients = new Dictionary<int, Client>();
        public static Dictionary<int, Player> saveData = new Dictionary<int, Player>();
        public static Dictionary<string, int> loginInfo = new Dictionary<string, int>();

        private static UdpClient udpListener;
        private static TcpListener tcpListener;

        private static BinaryFormatter binaryFormatter;

        readonly static string SDFPath = @"C:\Users\djque\Desktop\Visual Studio Projects\C#\ServerTest\ServerTest\SDF.save";
        readonly static string LIFPath = @"C:\Users\djque\Desktop\Visual Studio Projects\C#\ServerTest\ServerTest\LIF.info";

        public static class Serialization
        {
            [Serializable]
            public struct SaveData
            {
                public int id;
                public string username;
                public float[] position;
                public float[] rotation;

                public SaveData(Player playerData)
                {
                    position = new float[3];
                    rotation = new float[4];

                    id = playerData.id;
                    username = playerData.username;
                    position[0] = playerData.position.X;
                    position[1] = playerData.position.Y;
                    position[2] = playerData.position.Z;

                    rotation[0] = playerData.rotation.X;
                    rotation[1] = playerData.rotation.Y;
                    rotation[2] = playerData.rotation.Z;
                    rotation[3] = playerData.rotation.W;
                }
            }

            public static SaveData ConvertToSaveData(Player player)
            {
                return new SaveData(player);
            }
            public static Player ConvertFromSaveData(SaveData saveData)
            {
                int id = saveData.id;
                string username = saveData.username;
                Vector3 position = new Vector3(saveData.position[0], saveData.position[1], saveData.position[2]);
                Quaternion rotation = new Quaternion(saveData.rotation[0], saveData.rotation[1], 
                    saveData.rotation[2], saveData.rotation[3]);

                Player player = new Player(id, username, position, rotation);
                return player;
            }
        }

        public static void Start(int maxPlayers, int portNum)
        {
            Port = portNum;
            MaxPlayers = maxPlayers;

            Console.Write("Starting server...\n");
            DateTime startTime = DateTime.Now;
            InitializeServerData();

            IPAddress hostIP = IPAddress.Any;

            tcpListener = new TcpListener(hostIP, Port);
            tcpListener.Start();
            tcpListener.BeginAcceptTcpClient(new AsyncCallback(TCPConnectCallback), null);

            udpListener = new UdpClient(Port);
            udpListener.BeginReceive(UDPReceiveCallback, null);

            DateTime stopTime = DateTime.Now;
            int elapsedTime = (int)stopTime.Subtract(startTime).TotalSeconds;

            if (elapsedTime <= 1)
            {
                Console.WriteLine($"Server started at IP {hostIP} on {Port} in {(float)elapsedTime / 1000} ms.\n");
            }
            else
            {
                Console.WriteLine($"Server started at IP {hostIP} on {Port} in {elapsedTime} s.\n");
            }
        }

        public static void WriteSaveData()
        {
            FileStream targetFile = File.Open(SDFPath, FileMode.Open);
            foreach (Player playerData in saveData.Values)
            {
                Serialization.SaveData dataToWrite = Serialization.ConvertToSaveData(playerData);
                binaryFormatter.Serialize(targetFile, dataToWrite);
            }
            targetFile.Close();
        }
        public static void ReadSaveData()
        {
            Serialization.SaveData readData;
            FileStream targetFile = File.Open(SDFPath, FileMode.Open);
            //TODO: Create better ID assigning method
            //Current method just increments IDs by 1
            int newID = 1;
            while(targetFile.Position!=targetFile.Length)
            {
                readData = (Serialization.SaveData)binaryFormatter.Deserialize(targetFile);
                Player playerData = Serialization.ConvertFromSaveData(readData);
                saveData.Add(newID, playerData);
                loginInfo.Add(playerData.username, newID);
                newID++;
            }
            targetFile.Close();
        }
        public void WriteLoginInfo()
        {

        }
        public void ReadLoginInfo()
        {

        }

        private static void TCPConnectCallback(IAsyncResult result)
        {
            TcpClient client = tcpListener.EndAcceptTcpClient(result);
            tcpListener.BeginAcceptTcpClient(new AsyncCallback(TCPConnectCallback), null);

            Console.WriteLine($"Incoming connection from {client.Client.RemoteEndPoint}...");
            for (int i = 1; i <= MaxPlayers; i++)
            {
                if (clients[i].tcp.socket == null)
                {
                    clients[i].tcp.Connect(client);
                    Console.Write($"{client.Client.RemoteEndPoint} connected to server\n");
                    return;
                }
            }
            Console.WriteLine($"{client.Client.RemoteEndPoint} failed to connect: Server full\n");
        }
        private static void UDPReceiveCallback(IAsyncResult result)
        {
            try
            {
                IPEndPoint clientEndPoint = new IPEndPoint(IPAddress.Any, 0);
                byte[] data = udpListener.EndReceive(result, ref clientEndPoint);
                udpListener.BeginReceive(UDPReceiveCallback, null);

                if( data.Length<4)
                {
                    return;
                }

                using(Packet packet = new Packet(data))
                {
                    int clientID = packet.ReadInt();
                    if(clientID == 0)
                    {
                        return;
                    }

                    if(clients[clientID].udp.endPoint == null)
                    {
                        clients[clientID].udp.Connect(clientEndPoint);
                        return;
                    }

                    if(clients[clientID].udp.endPoint.ToString() == clientEndPoint.ToString())
                    {
                        clients[clientID].udp.HandleData(packet);
                    }
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine($"Error receiving UDP data: {ex}");
            }
        }
        public static void SendUDPData(IPEndPoint clientEndPoint, Packet packet)
        {
            try
            {
                if(clientEndPoint != null)
                {
                    udpListener.BeginSend(packet.ToArray(), packet.Length(), clientEndPoint, null, null);
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine($"Error sending UDP data to client {clientEndPoint}: {ex}");
            }
        }
        private static void InitializeServerData()
        {
            Console.WriteLine("Initializing Server Data...");
            DateTime startTime = DateTime.Now;

            for(int i = 1; i <= MaxPlayers; i++)
            {
                clients.Add(i, new Client(i));
            }

            binaryFormatter = new BinaryFormatter();


            DateTime mapStartTime = DateTime.Now;

            Console.WriteLine("No existing map found, beginning new map render...");
            Console.WriteLine("//NOTE: currently map overwrites on start each time!");
            WorldGenerator.RenderBitmap(4, 10);

            DateTime mapStopTime = DateTime.Now;
            int MET = (int)mapStopTime.Subtract(mapStartTime).TotalSeconds;
            if (MET <= 1)
            {
                Console.WriteLine($"Map render completed in {(float)MET / 1000} ms.");
            }
            else
            {
                Console.WriteLine($"Map render completed in {MET} s.");
            }


            if (!File.Exists(SDFPath))
            {
                File.Create(SDFPath).Close();
            }
            if (!File.Exists(LIFPath))
            {
                File.Create(LIFPath).Close();
            }

            //TODO: Populate login info from external source

            Console.WriteLine("Login info populated.");

            //TODO: Load save data from external source

            ReadSaveData();

            Console.WriteLine("Save data loaded.");


            packetHandlers = new Dictionary<int, PacketHandler>()
            {
                {(int)ClientPackets.welcomeReceived, ServerHandle.WelcomeRecieved },
                {(int)ClientPackets.messageReceived, ServerHandle.messageReceived },
                {(int)ClientPackets.inputReceived, ServerHandle.InputsReceived }
            };
            Console.WriteLine("Packets initialized.");

            DateTime stopTime = DateTime.Now;
            int elapsedTime = (int)stopTime.Subtract(startTime).TotalSeconds;
            if(elapsedTime<=1)
            {
                Console.WriteLine($"All server data initialized in {(float)elapsedTime / 1000} ms.");
            }
            else
            {
                Console.WriteLine($"All server data initialized in {elapsedTime} s.");
            } 
        }
    }
}
