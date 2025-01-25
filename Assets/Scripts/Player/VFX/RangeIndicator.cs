using UnityEngine;

public class RangeIndicator : MonoBehaviour {
    
    private float _size;
    private LuxPlayerController _playerController;

    void Start() {
        var parent = transform.parent.gameObject;
        _playerController = parent.GetComponent<LuxPlayerController>();
        _size = _playerController.champion.AA_range;
    }
    
    // Update is called once per frame
    void Update(){
        transform.localScale = new Vector3(_size, _size, 1);
    }
}
