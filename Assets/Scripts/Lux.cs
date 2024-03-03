using UnityEngine;

public class Lux {

    // Base
    public float movementSpeed =  2.5f;
    public float turnSpeed = 15f;
    public float windupTime = 15.625f;
    public float stoppingDistance =  0.1f;

    // Auto Attacks
    public float AA_range = 0.6f;
    public float AA_missileSpeed = 8f;
    public double AA_attackSpeed = 0.625; // Attack Speed Ratio
    public Vector3 AA_direction;

    // Q
    public float Q_range = 8f;
    public Vector3 Q_direction;
    public float Q_speed = 6f;

    // W
    public float W_range;
    public Vector3 W_direction;
    public float W_speed;

    // E
    public float E_range;
    public Vector3 E_direction;
    public float E_speed;


    // R
    public float R_range;
    public Vector3 R_direction;
    public float R_speed;



    public Lux(){

    }

}
