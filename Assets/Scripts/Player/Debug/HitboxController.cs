using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Hitbox : MonoBehaviour
{

    private Renderer hitboxRend;
    public Vector3 centerPos;

    // Start is called before the first frame update
    void Start(){
        hitboxRend = gameObject.GetComponent<Renderer>();
    }

    // Update is called once per frame
    void Update(){
        centerPos = hitboxRend.bounds.center;
    }

    void OnDrawGizmos(){
        Gizmos.color = Color.black;
        Gizmos.DrawSphere(centerPos, 0.3f);
    }
}
