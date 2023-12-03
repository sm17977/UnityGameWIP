using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RangeIndicator_Controller : MonoBehaviour
{

    public Material AARangeIndicatorMaterial;
    public float radius = 0.5f;
    public float borderWidth = 0.01f; 
    private float initialRadius;

    void Start(){
        initialRadius = radius;
    }


    // Update is called once per frame
    void Update(){
        if(AARangeIndicatorMaterial != null){
            AARangeIndicatorMaterial.SetFloat("_radius", radius);
            AARangeIndicatorMaterial.SetFloat("_borderWidth", borderWidth);
        }

        float scaleFactor = radius/initialRadius;
        transform.localScale = new Vector3(scaleFactor, 1f, scaleFactor);
    }
}
