using System;
using UnityEngine;

public enum PlayerType { Player, Bot};
public class Lux_Controller : MonoBehaviour
{

    public PlayerType playerType;
    public Champion lux;
    public Buff_Manager buffManager;

    public bool canMove = true;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake(){

    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
