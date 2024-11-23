public class BuffRecord {
    public Buff Buff { get; set; }
    public ulong SourceClientId { get; set; }
    
    public BuffRecord(Buff buff, ulong sourceClientId, ulong targetClientId) {
        Buff = buff;
        SourceClientId = sourceClientId;
    }
}