using System.Collections.Generic;
using ProtoBuf;

namespace flow.Backup
{
    [ProtoContract]
    public class StartBackupRequest : FlowMessage
    {
        [ProtoMember(1)]
        public string workingDir {get; set;}
        [ProtoMember(2)]
        public List<string> files {get; set;}
        [ProtoMember(3)]
        public List<BackupStorage> storages {get; set;}
    }

    [ProtoContract]
    public class StartBackupResponse : FlowMessage
    {
        [ProtoMember(1)]
        public bool started {get; set;}
    }
}
