using UnityEngine;


public class Lux_W_Net : NetworkAbilityBehaviour {
    
    void Start() {
        if(!IsServer) return;
        NetworkBuffManager.Instance.AddBuff(Ability.buff, spawnedByClientId, spawnedByClientId);
        Debug.Log("Applying shield buff on server");
    }

    void Update() {
        
    }
    
}
