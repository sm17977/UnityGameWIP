namespace Multiplayer {
    public class QueueAllocationRequest {
        
        public string allocationId {get; set;}
        public int buildConfigurationId {get; set;}
        public string payload {get; set;}
        public string regionId {get; set;}
        public bool restart {get; set;}

        public QueueAllocationRequest() {
        }

    }
}
