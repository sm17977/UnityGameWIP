using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RangeIndicator_Controller : MonoBehaviour
{
    public Material AARangeIndicatorMaterial;
    private float radius = 0.4f;
    public float size;
    public float borderWidth = 0.01f; 
    private float initialRadius;
    public GameObject player;
    private Lux_Player_Controller playerScript;

    void Start(){
        //playerScript = player.GetComponent<Lux_Player_Controller>();
        //radius = playerScript.GetAttackRange();
        initialRadius = radius;
    }


    // Update is called once per frame
    void Update(){
        if(AARangeIndicatorMaterial != null){
            AARangeIndicatorMaterial.SetFloat("_radius", radius);
            AARangeIndicatorMaterial.SetFloat("_borderWidth", borderWidth);
        }
        transform.localScale = new Vector3(size, 1f, size);
    }
}
