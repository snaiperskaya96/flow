using System.Collections.Generic;
using ProtoBuf;

namespace flow.Backup
{
    [ProtoContract]
    public class BackupStorage
    {
        [ProtoMember(1)]
        public string name {get; set;}

        [ProtoMember(2)]
        public Dictionary<string, string> meta {get; set;}
    }
}