using UnityEngine;

public class Global_State : MonoBehaviour
{

    public bool paused = false;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void Pause(){

        if(!paused){
            Time.timeScale = 0;
            paused = true;
        }
        else{
            Time.timeScale = 1;
            paused = false;
        }
    }


}
