using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RangeIndicator_Controller : MonoBehaviour
{
    public Material AARangeIndicatorMaterial;
    public GameObject player;
    public float size;
    public float borderWidth;

    // Keep radius 0.5 so the range indicator circle fills the quad
    private float radius = 0.5f;
    private Lux_Player_Controller playerController;

    void Start(){
        Lux lux = new Lux();
        playerController = player.GetComponent<Lux_Player_Controller>();
        size = lux.AA_range;
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
