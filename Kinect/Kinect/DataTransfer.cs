using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Kinect.Model;
using KinectCoordinateMapping;
using System.Security.Policy;

namespace Kinect
{
    public class DataTransferManager
    {
        public IPAddress robotIPAddress = null;
        public DataTransferManager() 
        { 
        }

        public void sendMessage_DataReady(object sender, RobotMessageEventArgs e)
        {
            if(this.robotIPAddress == null)
            {
                return;
            }    
            DataTransfer.sendMessage(e.MessagePacket, this.robotIPAddress);
        }

        public bool sendMessage(NaoMsgPacket packet)
        {
            if (this.robotIPAddress == null)
            {
                return false;
            }
            DataTransfer.sendMessage(packet, this.robotIPAddress);
            return true;
        }

        public bool logMessage(string message)
        {
            if (this.robotIPAddress == null)
            {
                return false;
            }
            NaoMsgPacket packet = new NaoMsgPacket();
            packet.Topic = "Log";
            packet.LogMessage = message;
            DataTransfer.sendMessage(packet, this.robotIPAddress);
            return true;
        }

        public async Task DiscoverMeProcessAsync()
        {
            // DiscoverMe service port number
            //const int discoverMePort = 33455;
            int discoverMePort = DataTransfer.robotPort + 1;

            // hello response datagram
            byte[] helloResponseDatagram = Encoding.UTF8.GetBytes("hello!");

            // hello request message
            const string helloRequestMessage = "hello?";

            Console.WriteLine($"{DateTimeOffset.Now:s} DiscoverMe service started.");

            // UdpClient to send/receive datagrams
            UdpClient server = new UdpClient();

            // Let me listen on the broadcast address even if someone else is already listening on it
            server.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);

            // Don't prevent anyone else from listening to the broadcast address
            server.ExclusiveAddressUse = false;

            // Only listen to broadcast messages
            IPEndPoint broadcastEndPoint = new IPEndPoint(IPAddress.Any, discoverMePort);
            server.Client.Bind(broadcastEndPoint);
            Console.WriteLine($"{DateTimeOffset.Now:s} DiscoverMe socket bound.");

            // Keep listening to see if anyone screams
            while (true)
            {
                Console.WriteLine($"{DateTimeOffset.Now:s} Listening to see if anyone screams...");
                try
                {
                    // Wait for a scream
                    var request = await server.ReceiveAsync();

                    // I heard something. Did they say 'hello?' ?
                    string message = Encoding.UTF8.GetString(request.Buffer);
                    if (message == helloRequestMessage)
                    {
                        // They said 'hello?'. Let me just holler back so they know where I am

                        Console.WriteLine(
                            $"{DateTimeOffset.Now:s} Someone at {request.RemoteEndPoint} was screaming.");

                        Console.WriteLine(
                            $"{DateTimeOffset.Now:s} Responding to {request.RemoteEndPoint} so they know where I am.");
                        this.robotIPAddress = request.RemoteEndPoint.Address;
                        // Send a response, and send it on the default listen port for the robot
                        await server.SendAsync(helloResponseDatagram, helloResponseDatagram.Length, new IPEndPoint(request.RemoteEndPoint.Address, DataTransfer.robotPort));

                        // I don't need to keep screaming now that they've found me (since I'm only looking for one thing)
                        server.Close();
                        break;
                    }
                    else
                    {
                        // Got a request but it isn't what I expected it to be so I should probably ignore it
                        Console.WriteLine(
                            $"{DateTimeOffset.Now:s} Someone at {request.RemoteEndPoint} was screaming, but I don't know what '{message}' means.");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"{DateTimeOffset.Now:s} Opps, something went wrong: {ex.Message}");

                    // Don't know what do with this error so I'll just quit
                    break;
                }
            }
            Console.WriteLine($"{DateTimeOffset.Now:s} DiscoverMe service stopped.");
        }
    }
    public static class DataTransfer
    {
        public static string robotName = "nao.local";
        public static int robotPort = 20001;

        public static void sendMessage(NaoMsgPacket packet, IPAddress remoteAddress)
        {
            // This constructor arbitrarily assigns the local port number.
            //UdpClient udpClient = new UdpClient(robotPort);
            //try
            //{
            //    udpClient.Connect(robotName, robotPort);

            //    // Convert packet data to a string
            //    string message = JsonConvert.SerializeObject(packet);

            //    // Sends a message to the host to which you have connected.
            //    Byte[] sendBytes = Encoding.UTF8.GetBytes(message);

            //    udpClient.Send(sendBytes, sendBytes.Length);

            //    udpClient.Close();
            //}
            //catch (Exception e)
            //{
            //    Console.WriteLine(e.ToString());
            //}
            UdpClient client = new UdpClient();
            //var addresses = Dns.GetHostAddresses(DataTransfer.robotName);
            //IPEndPoint ip = new IPEndPoint(IPAddress.Parse("255.255.255.255"), robotPort);
            IPEndPoint ip = new IPEndPoint(remoteAddress, robotPort);
            //if (addresses.Length > 0) { ip = new IPEndPoint(addresses[0], robotPort); }
            string message = JsonConvert.SerializeObject(packet);
            byte[] bytes = Encoding.UTF8.GetBytes(message);
            client.Send(bytes, bytes.Length, ip);
            client.Close();
            Console.WriteLine("Sent: {0} bytes to {1}", bytes.Length, ip);
        }
    }
}

