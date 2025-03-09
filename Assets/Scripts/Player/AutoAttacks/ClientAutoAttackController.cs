using UnityEngine;
using UnityEngine.VFX;

public class ClientAutoAttackController : MonoBehaviour {
    
    public VisualEffect vfx;
    public bool isFinished = false;
    
    private LuxPlayerController _playerController;
    private GameObject _player;
    private float _timer;
    private float _distance;
    private float _speed;
    private float _time;


    private void Start(){
        vfx = GetComponent<VisualEffect>();
    }

    public void Initialise(GameObject playerObj, Vector3 startPos, Quaternion startRot, Vector3 direction) {

        _player = playerObj;
        _playerController = playerObj.GetComponent<LuxPlayerController>();
        
        if(vfx == null) vfx = GetComponent<VisualEffect>();
        
        isFinished = false;

        transform.position = startPos;
        transform.rotation = startRot;
        
        // Distance between player and enemy
        _distance = Vector3.Distance( new Vector3(_player.transform.position.x, 1f, _player.transform.position.z), _playerController.currentAATarget.transform.position);
        
        // Particle speed
        _speed = _playerController.champion.AA_missileSpeed;
        
        // Particle lifetime
        _time = _distance/_speed;

        // Set VFX Graph exposed properties
        vfx.SetVector3("targetDirection", direction);
        vfx.SetFloat("lifetime", _time);
        vfx.SetFloat("speed", _speed);

        vfx.enabled = true;

        // Initiate timer
        _timer = _time;
    }

    private void Update(){

        if (vfx != null && vfx.enabled) {
            // Set die to true when the particle lifetime ends
            if (_timer > 0) {
                _timer -= Time.deltaTime;
            }
            else {
                isFinished = true;
                DeactivateAutoAttack();
            }
        }
    }
    
    private void DeactivateAutoAttack() {
        gameObject.SetActive(false);
        ClientObjectPool.Instance.ReturnPooledAutoAttack(gameObject);
    }
}
