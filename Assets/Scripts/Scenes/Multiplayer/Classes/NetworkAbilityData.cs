using Unity.Netcode;

public class NetworkAbilityData : INetworkSerializable {
    public string abilityName;
    public float currentCooldown;
    public float maxCooldown;
    public string key;
    public bool onCooldown;

    // Default constructor for deserialization
    public NetworkAbilityData() {}

    // Constructor to initialize the data from an Ability ScriptableObject
    public NetworkAbilityData(Ability ability) {
        abilityName = ability.abilityName;
        currentCooldown = ability.currentCooldown;
        maxCooldown = ability.maxCooldown;
        key = ability.key;
        onCooldown = ability.serverOnCooldown;
    }

    // Implement the Write and Read methods for custom serialization
    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter {
        // Serialize each field
        serializer.SerializeValue(ref abilityName);
        serializer.SerializeValue(ref currentCooldown);
        serializer.SerializeValue(ref maxCooldown);
        serializer.SerializeValue(ref key);
        serializer.SerializeValue(ref onCooldown);
    }
}