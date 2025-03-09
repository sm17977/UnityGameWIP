using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class NetworkAutoAttackController : NetworkBehaviour {
    
    private LuxPlayerController _playerController;
    private GameObject _player;
    
    private GameObject _target;
    private LuxPlayerController _targetController;
    
    private float _timer;
    private float _distance;
    private float _speed;
    private float _time;

    
    public void Initialise(GameObject playerObj, GameObject target, Vector3 startPos) {
        Debug.Log("Init server auto attack");
        Debug.Log($"Time.deltaTime: {Time.deltaTime}, Time.timeScale: {Time.timeScale}");

        _player = playerObj;
        _playerController = _player.GetComponent<LuxPlayerController>();
        _target = target;
        _targetController = _target.GetComponent<LuxPlayerController>();

        transform.position = startPos;

        // Calculate time until damage should be applied
        _distance = Vector3.Distance(
            new Vector3(_player.transform.position.x, 1f, _player.transform.position.z), 
            _target.transform.position
        );
        _speed = _playerController.champion.AA_missileSpeed;
        _time = _distance / _speed;
        _timer = _time;
        
        Debug.Log($"Timer set to: {_timer}");

        if (IsServer && !IsValidAutoAttack()) {
            Debug.Log("Invalid AA");
            DeactivateAutoAttack();
        }
        else if(IsServer && IsValidAutoAttack()) {
            Debug.Log("Valid AA");
        }
    }

    private void Update() {
        Debug.Log("Update, time: " + _timer);
        if (!IsServer) return;

        if (_timer > 0) {
            _timer -= Time.deltaTime;
            Debug.Log($"Timer: {_timer}");
        }
        else {
            Debug.Log("Timer finished, applying damage");
            ApplyDamage();
            DeactivateAutoAttack();
        }
    }
    
    private void ApplyDamage() {
        Debug.Log("Applying damage server side");
        var damage = _playerController.champion.AA_damage;
        _targetController.health.TakeDamage(damage);
    }
    
    private void DeactivateAutoAttack() 
    {  Debug.Log("Deactviating auto attack");
        gameObject.SetActive(false);
        ServerObjectPool.Instance.ReturnPooledAutoAttack(gameObject);
    }
    
    private bool IsValidAutoAttack() {
        if (_playerController == null || _target == null) return false;

        Debug.Log("After null check");

        var distance = Vector3.Distance(
            new Vector3(_player.transform.position.x, 1f, _player.transform.position.z),
            _target.transform.position
        );

        Debug.Log("IsValid distance: " + distance);
        Debug.Log("IsValid timeSinceLastAttack: " + _playerController.timeSinceLastAttack);
        
        return distance <= _playerController.champion.AA_range &&
               _playerController.timeSinceLastAttack <= 0;
    }
    
    private void OnEnable() {
        Debug.Log($"[{gameObject.name}] OnEnable");
    }

    private void OnDisable() {
        Debug.Log($"[{gameObject.name}] OnDisable");
    }
    
    private void OnDestroy() {
        Debug.Log($"[{gameObject.name}] OnDestroy");
    }
}