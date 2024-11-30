public class BuffRecord {
    public Buff Buff { get; set; }
    public ulong SourceClientId { get; set; }
    
    public BuffRecord(Buff buff, ulong sourceClientId) {
        Buff = buff;
        SourceClientId = sourceClientId;
    }
}