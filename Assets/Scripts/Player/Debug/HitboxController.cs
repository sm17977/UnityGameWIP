using UnityEngine;

public class Hitbox : MonoBehaviour {

    private Renderer _hitboxRend;
    public SphereCollider _sphereCollider;
    public Vector3 centerPos;

    // Start is called before the first frame update
    void Start(){
        _hitboxRend = gameObject.GetComponent<Renderer>();
        _sphereCollider = gameObject.GetComponent<SphereCollider>();
    }

    // Update is called once per frame
    void Update(){
        //centerPos = _hitboxRend.bounds.center;
        centerPos = _sphereCollider.center;
    }

    void OnDrawGizmos() {
        // Gizmos.color = new Color(1, 1, 1, 800);
        // Gizmos.DrawSphere(_sphereCollider.transform.position, 0.4f);
    }
}
