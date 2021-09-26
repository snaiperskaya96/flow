using System.Collections.Generic;
using ProtoBuf;

namespace flow.Crypto
{
    [ProtoContract]
    public class NewEncryptionKey : FlowMessage
    {
        [ProtoMember(1)]
        public string key64 {get; set;}
    }
}
