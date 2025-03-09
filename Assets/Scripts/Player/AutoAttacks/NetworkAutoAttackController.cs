using Unity.Netcode;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.VFX;

public class NetworkAutoAttackController : NetworkBehaviour {
    
    public VisualEffect vfx;
    public LuxPlayerController playerController;
    public GameObject player;
    public bool isFinished = false;
    
    private float _timer;
    private float _distance;
    private float _speed;
    private float _time;


    private void Start(){
        vfx = GetComponent<VisualEffect>();
    }

    public void Initialise(Vector3 startPos, Quaternion startRot) {

        isFinished = false;

        transform.position = startPos;
        transform.rotation = startRot;
        
        // Distance between player and enemy
        _distance = Vector3.Distance( new Vector3(player.transform.position.x, 1f, player.transform.position.z), playerController.currentAATarget.transform.position);
        
        // Particle speed
        _speed = playerController.champion.AA_missileSpeed;
        
        // Particle lifetime
        _time = _distance/_speed;

        // Set VFX Graph exposed properties
        vfx.SetVector3("targetDirection", playerController.direction);
        vfx.SetFloat("lifetime", _time);
        vfx.SetFloat("speed", _speed);

        vfx.enabled = true;

        // Initiate timer
        _timer = _time;
    }

    void Update(){

        if (vfx != null && vfx.enabled) {
            // Set die to true when the particle lifetime ends
            if (_timer > 0) {
                _timer -= Time.deltaTime;
            }
            else {
                isFinished = true;
            }
        }
    }
}