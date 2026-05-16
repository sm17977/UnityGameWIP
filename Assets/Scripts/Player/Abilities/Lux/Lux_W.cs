using UnityEngine;


public class Lux_W : ClientAbilityBehaviour {
    
    
    void Start() {
        
    }

    void Update() {
        transform.position = new Vector3(
            Player.transform.position.x,
            Ability.spawnHeight,
            Player.transform.position.z
        );
    }
    
}
