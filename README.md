Flow
=====

Flow is comunication library based on [protobuf-net](https://github.com/protobuf-net/protobuf-net), a contract-based serializer that uses the same format as Google's [protocol-buffers](https://developers.google.com/protocol-buffers).

This project was initially part of an internal backup solution (hence some of backup-related unused messages, left for reference).

Developed using .NET core 5.0, not sure about compatibility.

## Usage

As Flow is only meant to be a channel of communication, it does not handle the connection itself, and it does require the application to provide with one or more already accepted clients.

```c#
Int32 port = 13000;
IPAddress localAddr = IPAddress.Parse("127.0.0.1");
server = new TcpListener(localAddr, port);

// Once a connection has been accepted... 

FlowConnection connection = new FlowConnection();

connection.client = client;
connection.stream = client.GetStream();

// Setup callbacks for a specific message
connection.messageDispatcher.GetMessageDelegate(typeof(flow.Connection.Handshake)).Add(OnHandshake);
connection.messageDispatcher.GetMessageDelegate(typeof(flow.Connection.Heartbeat)).Add(OnHeartbeat);

```
The application should then call FlowConnection.ProcessOutgoingMessages and FlowConnection.ProcessIncomingMessages in its socket routine to make sure flow can read and write the serialised messages as required.

```c#
 async Task Start()
{
    while (true)
    {
        foreach (FlowConnection client in connections)
        {
            // should probably check if client.client/stream is still valid 
            client.ProcessOutgoingMessages();
            client.ProcessIncomingData();
        }
        await Task.Delay(1);
    }
}

```

## Encrypting Messages
If you are feeling cryptic, you can leverage flow to protect your messages using AES enryption. The way and moment you enable this is entirely specific to your application.

For instance in our internal software we do all sort of authentication/handshake using asynchronous encryption (hence those few RSA methods in the CryptoUtils class) and once we confirmed the identity of the client we share an encryption key to be used for further communication (as AES is way more performant than using RSA when it comes to fast messaging between client and server).

In order to start using an encryption key you first have to generate one using `flow.Crypto.CryptoUtils.GenerateEncryptionKey()`, then you can simply assign it to your `FlowConnection.encryptionKey` on both ends of your connection.

You can leverage the already existing messages to do so.
```c#
byte[] encryptionKey = flow.Crypto.CryptoUtils.GenerateEncryptionKey();
flow.Crypto.NewEncryptionKey newEncryptionKey = new flow.Crypto.NewEncryptionKey
{
    key64 = System.Convert.ToBase64String(encryptionKey)
};

// Set a callback for when this message has been sent so we can
// start using the new encryption key right after and avoid decrypting
// non-encrypted messages we might receive before the client starts using it
newEncryptionKey.onSent += () =>
{
    connection.encryptionKey = encryptionKey;
    Console.WriteLine("Started using new encryption key with client {0}.", iPEndPoint.Address.ToString());
};

connection.EnqueueMessage(newEncryptionKey);
```

On the client side

```c#
async static Task OnNewEncryptionKey((FlowConnection connection, FlowMessage message) data)
{
    var newEncryptionKeyMessage = data.message as flow.Crypto.NewEncryptionKey;
    byte[] newKey = System.Convert.FromBase64String(newEncryptionKeyMessage.key64);
    data.connection.encryptionKey = newKey;
}

...
connection.messageDispatcher.GetMessageDelegate(typeof(flow.Crypto.NewEncryptionKey)).Add(OnNewEncryptionKey);
```
