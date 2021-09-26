using flow.Backup;
using flow.Connection;
using flow.Crypto;
using ProtoBuf;

namespace flow
{
    [ProtoContract]
    [ProtoInclude(1, typeof(NewEncryptionKey))]
    [ProtoInclude(2, typeof(Handshake))]
    [ProtoInclude(3, typeof(StartBackupRequest))]
    [ProtoInclude(4, typeof(StartBackupResponse))]
    [ProtoInclude(5, typeof(Heartbeat))]
    [ProtoInclude(6, typeof(HeartbeatResponse))]
    public class FlowMessage
    {
        public delegate void OnSent();
        public OnSent onSent {get; set;}
    }
}