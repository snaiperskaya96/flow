using System.Collections.Generic;
using ProtoBuf;

namespace flow.Connection
{
    [ProtoContract]
    public class Handshake : FlowMessage
    {
        [ProtoMember(1)]
        public string license64 {get; set;}
    }

    [ProtoContract]
    public class Heartbeat : FlowMessage
    {

    }

    [ProtoContract]
    public class HeartbeatResponse : FlowMessage
    {

    }
}
