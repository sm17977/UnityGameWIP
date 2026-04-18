namespace Scenes.Multiplayer.EdgeGapAPI
{

    public class Ports {
        public Port gameport;
    }

    public class Port {
        public int external;
        public string name;
    }
    
    public class GetDeploymentResponse {
        public string request_id;
        public string fqdn;
        public string app_name;
        public string app_version;
        public string current_status;
        public bool running;
        public bool whitelisting_active;

        public string start_time;
        public string removal_time;

        public int? elapsed_time;

        public string last_status;
        public bool error;
        public string error_detail;

        public Ports ports;
        public string public_ip;
        public object[] sessions;
        public object location;
        public string[] tags;

        public string sockets;
        public string sockets_usage;

        public string command;
        public string arguments;

        public int? max_duration;
    }
}
