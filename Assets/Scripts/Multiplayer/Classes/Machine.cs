namespace Multiplayer {
    
    public class Machine {
        public bool deleted;
        public int desiredServerCount;
        public bool disabled;
        public string fleetId;
        public string fleetName;
        public string hardwareType;
        public int id;
        public string ip;
        public int locationId;
        public string locationName;
        public string name;
        public string osFamily;
        public string osName;
        public string status;
        public ServerStates ServersStates { get; set; }
        public Spec Spec { get; set; }

    }
    public class ServerStates {
        public int Allocated { get; set; }
        public int Available { get; set; }
        public int Held { get; set; }
        public int Online { get; set; }
        public int Reserved { get; set; }
    }

    public class Spec {
        public int CpuCores { get; set; }
        public string CpuShortname { get; set; }
        public int CpuSpeed { get; set; }
        public string CpuType { get; set; }
        public long Memory { get; set; }
    }
    
    public class MachineStatus {
        public const string Online = "ONLINE";
        public const string Shutdown = "SHUTDOWN";
        public const string AwaitingSetup = "AWAITING_SETUP";
        public const string Booting = "BOOTING";
    }
}


