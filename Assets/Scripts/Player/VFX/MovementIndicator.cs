using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovementIndicator : MonoBehaviour
{

    public Animator animator;

    // Start is called before the first frame update
    void Start(){
        animator.SetBool("hasClicked", true);
    }

    // Update is called once per frame
    void Update(){
        
    }

    public void destroy(){
        Destroy(gameObject);
    }

}
