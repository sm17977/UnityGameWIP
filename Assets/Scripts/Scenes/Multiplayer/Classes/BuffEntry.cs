using System.Collections.Generic;
using Unity.Netcode;

public struct BuffEntry : INetworkSerializable {
    public string Key;
    public string Value;
    
    public BuffEntry(string key, string value) {
        Key = key;
        Value = value;
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter {
        serializer.SerializeValue(ref Key);
        serializer.SerializeValue(ref Value);
    }
}


public static class BuffEntryListSerialization {
    public static void WriteValueSafe(this FastBufferWriter writer, in List<BuffEntry> list) {
        writer.WriteValueSafe(list.Count);
        foreach (var entry in list) {
            writer.WriteValueSafe(entry.Key);
            writer.WriteValueSafe(entry.Value);
        }
    }

    public static void ReadValueSafe(this FastBufferReader reader, out List<BuffEntry> list) {
        list = new List<BuffEntry>();
        reader.ReadValueSafe(out int count);
        for (int i = 0; i < count; i++) {
            reader.ReadValueSafe(out string key);
            reader.ReadValueSafe(out string value);
            list.Add(new BuffEntry(key, value));
        }
    }
}

