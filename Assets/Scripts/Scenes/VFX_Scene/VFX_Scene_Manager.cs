using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VFX_Scene_Manager : MonoBehaviour
{

    public List<GameObject> effectsList;

    // Start is called before the first frame update
    void Start(){
        effectsList = new List<GameObject>();
    }

    // Update is called once per frame
    void Update(){

        // if(effectsList.Count > 0){
        //     for(int i = 0; i < effectsList.Count; i++){
        //         GameObject effect = effectsList[i];
        //         if(effect != null && effect.activeSelf){
        //             VFX_Timer timer = effect.GetComponent<VFX_Timer>();
        //             if(timer.die){
        //                 effectsList.RemoveAt(i);
        //                 Destroy(effect);
        //             }
        //         }
        //     }
        // }

    }
}
