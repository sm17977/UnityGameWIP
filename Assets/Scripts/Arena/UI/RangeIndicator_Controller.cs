using UnityEngine;

public class RangeIndicator_Controller : MonoBehaviour
{
    public Material AARangeIndicatorMaterial;
    public GameObject player;
    public float size;
    private float borderWidth = 0.01f;

    // Keep radius 0.5 so the range indicator circle fills the quad
    private float radius = 0.5f;
    private Lux_Player_Controller playerController;

    void Start(){
        playerController = player.GetComponent<Lux_Player_Controller>();
        size = playerController.lux.AA_range;
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
