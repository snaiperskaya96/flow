using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Threading;

namespace flow.Connection
{
    public class FlowConnection
    {
        public TcpClient client { get; set; }
        public NetworkStream stream { get; set; }
        public bool receivingPacket { get; set; }
        public int incomingDataOffset { get; set; }
        public byte[] incomingData { get; set; }
        public Mutex clientMutex { get; set; }
        Queue<flow.FlowMessage> outgoingQueue { get; set; }
        readonly static byte[] magic = new byte[] { 0xF1, 0x04 };
        protected byte[] header { get; set; }
        int packetOffset { get; set; }
        int packetSize { get; set; }
        public byte[] encryptionKey { get; set; }
        public MessageDispatcher messageDispatcher { get; protected set; }
        public DateTime lastMessageTimeStamp { get; protected set; }
        public string clientId { get; set; }

        public FlowConnection()
        {
            clientMutex = new Mutex();
            incomingData = new byte[1024];
            outgoingQueue = new Queue<flow.FlowMessage>();
            header = new byte[] { magic[0], magic[1], 0x00, 0x00, 0x00, 0x00 };
            lastMessageTimeStamp = DateTime.Now;
            ResetData();
            encryptionKey = null;
            messageDispatcher = new MessageDispatcher();
        }

        public void EnqueueMessage(FlowMessage message)
        {
            clientMutex.WaitOne();
            outgoingQueue.Enqueue(message);
            clientMutex.ReleaseMutex();
        }

        public void ProcessIncomingData()
        {
            while (stream.DataAvailable)
            {
                int readBytes = stream.Read(incomingData, incomingDataOffset, incomingData.Count() - incomingDataOffset);
                OnIncomingDataChanged(readBytes);
            }
        }

        public void Disconnect()
        {
            if (client != null)
            {
                client.Dispose();
            }
            ResetData(true);
        }

        public void ProcessOutgoingMessages()
        {
            clientMutex.WaitOne();

            while (outgoingQueue.Count > 0)
            {
                flow.FlowMessage message = outgoingQueue.Dequeue();
                byte[] buffer = flow.Serialization.Serialization.Serialize(message);

                if (encryptionKey != null)
                {
                    buffer = flow.Crypto.CryptoUtils.EncryptData(encryptionKey, buffer);
                }

                int bufferLen = buffer.Count();

                header[2] = (byte)(bufferLen >> 24);
                header[3] = (byte)(bufferLen >> 16);
                header[4] = (byte)(bufferLen >> 8);
                header[5] = (byte)bufferLen;

                try
                {
                    stream.Write(header, 0, 6);
                    stream.Write(buffer, 0, bufferLen);
                    Console.WriteLine("Sent {0} bytes ({1} + {2})", bufferLen + 6, bufferLen, 6);
                    message.onSent?.Invoke();
                }
                catch (Exception)
                {
                    Console.WriteLine("Connection closed. Resetting data.");
                    ResetData(true);
                }
            }

            clientMutex.ReleaseMutex();
        }

        public void OnIncomingDataChanged(int addedBytes)
        {
            incomingDataOffset += addedBytes;
            if (!receivingPacket)
            {
                for (int i = 0; i < incomingDataOffset; i++)
                {
                    if (i < incomingDataOffset - 1 && incomingData[i] == magic[0] && incomingData[i + 1] == magic[1])
                    {
                        packetOffset = i;
                        receivingPacket = true;
                        packetSize = -1;
                        Console.WriteLine("found packet offset at {0}", i);
                        break;
                    }
                }
            }

            if (receivingPacket && packetSize == -1)
            {
                if (incomingDataOffset - packetOffset >= 6)
                {
                    packetSize = incomingData[packetOffset + 5] | incomingData[packetOffset + 4] << 8 | incomingData[packetOffset + 3] << 16 | incomingData[packetOffset + 2] << 24;
                    Console.WriteLine("found packet size: {0}b", packetSize);
                }
            }

            if (receivingPacket && packetSize > 0)
            {
                if (incomingDataOffset - packetOffset >= 6 + packetSize)
                {
                    byte[] messageData = null;

                    if (encryptionKey != null)
                    {
                        messageData = flow.Crypto.CryptoUtils.DecryptData(encryptionKey, incomingData.AsMemory(packetOffset + 6, packetSize).ToArray());
                    }
                    else
                    {
                        messageData = incomingData.AsMemory(packetOffset + 6, packetSize).ToArray();
                    }

                    flow.FlowMessage message = flow.Serialization.Serialization.Deserialize<flow.FlowMessage>(messageData);
                    Console.WriteLine("deserialized flow message: {0}", message);
                    lastMessageTimeStamp = DateTime.Now;
                    messageDispatcher.OnMessageReceived(this, message);

                    int endOfPacketOffset = packetOffset + 6 + packetSize;
                    for (int i = endOfPacketOffset; i < incomingDataOffset; i++)
                    {
                        incomingData[i - endOfPacketOffset] = incomingData[i];
                    }

                    packetSize = -1;
                    packetOffset = -1;
                    receivingPacket = false;
                    int leftOver = incomingDataOffset - endOfPacketOffset;
                    incomingDataOffset = 0;
                    OnIncomingDataChanged(leftOver);
                    return;
                }
            }

            if (incomingDataOffset >= incomingData.Count())
            {
                ResetData();
            }
        }

        void ResetData(bool withEncrpytionKey = false)
        {
            incomingDataOffset = 0;
            packetSize = -1;
            packetOffset = -1;
            receivingPacket = false;

            if (withEncrpytionKey)
            {
                encryptionKey = null;
            }
        }
    }

}