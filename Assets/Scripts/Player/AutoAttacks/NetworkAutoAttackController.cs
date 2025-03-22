using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class NetworkAutoAttackController : NetworkBehaviour {
    
    private LuxPlayerController _playerController;
    private GameObject _player;
    private GameObject _playerHitbox;
    
    private LuxPlayerController _targetController;
    private GameObject _target;
    private GameObject _targetHitbox;
    
    private float _timer;
    private float _distance;
    private float _speed;
    private float _time;
    private bool _logOnce;
    
    public void Initialise(GameObject playerObj, GameObject target, Vector3 startPos) {
        _player = playerObj;
        _playerController = _player.GetComponent<LuxPlayerController>();
        _playerHitbox = _player.transform.Find("model/root/Hitbox").gameObject;
        
        _target = target;
        _targetController = _target.GetComponent<LuxPlayerController>();
        _targetHitbox = _target.transform.Find("model/root/Hitbox").gameObject;
        
        transform.position = startPos;

        // Calculate time until damage should be applied
        _distance = Vector3.Distance(
            new Vector3(_player.transform.position.x, 1f, _player.transform.position.z), 
            _target.transform.position
        );
        _speed = _playerController.champion.AA_missileSpeed;
        _time = _distance / _speed;
        _timer = _time;
        
        
        if (IsServer && !IsValidAutoAttack()) {
            Debug.Log("Invalid AA");
            DeactivateAutoAttack();
        }
        else if(IsServer && IsValidAutoAttack()) {
            Debug.Log("Valid AA");
        }
    }

    private void Update() {
        if (!IsServer) return;

        if (_timer > 0) {
            _timer -= Time.deltaTime;
        }
        else {
            ApplyDamage();
            DeactivateAutoAttack();
        }
    }
    
    private void ApplyDamage() {
        Debug.Log("Applying damage server side");
        var damage = _playerController.champion.AA_damage;
        _targetController.health.TakeDamage(damage);
    }
    
    private void DeactivateAutoAttack() {
        gameObject.SetActive(false);
        ServerObjectPool.Instance.ReturnPooledAutoAttack(gameObject);
    }
    
    private bool IsValidAutoAttack() {
        if (_playerController == null || _target == null) return false;
        
        var playerCenter = new Vector3(
            _playerHitbox.transform.position.x,
            InputProcessor.floorY,
            _playerHitbox.transform.position.z);
        
        var enemyCenter = new Vector3(
            _targetHitbox.transform.position.x,
            InputProcessor.floorY,
            _targetHitbox.transform.position.z);
        
        float centerDistance = Vector3.Distance(playerCenter, enemyCenter);
        float currentEdgeDistance = Mathf.Max(0,
            centerDistance - (_playerController.hitboxColliderRadius + _targetController.hitboxColliderRadius));
        
        Debug.Log("IsValid distance: " + currentEdgeDistance);
        
        return currentEdgeDistance <= _playerController.champion.AA_range;
    }
}