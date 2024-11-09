public class BuffRecord {
    public Buff Buff { get; set; }
    public ulong AttackerClientId { get; set; }
    
    public BuffRecord(Buff buff, ulong attackerClientId) {
        Buff = buff;
        AttackerClientId = attackerClientId;
    }
}