using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Lux_AI_Controller : MonoBehaviour
{
    public Animator animator;
    private Vector3 projectileTargetPosition;
    public float stoppingDistance;
    public float moveSpeed;
    private bool isRunning;
    public float turnSpeed;
    public GameObject hitboxGameObj;
    private SphereCollider hitboxCollider;
    private Vector3 hitboxPos;
    private Vector3 projectileSpawnPos;
    private bool isCasting;
    public GameObject projectile;
    private List<GameObject> projectiles;
    public GameObject movementIndicatorPrefab;
    public Dictionary<string, object> luxAbilityData;
    public Dictionary<string, object> qData;
    private bool hasProjectile = false;


    public float debugDistance;

    private Vector3 lastClickPosition;

    // Start is called before the first frame update
    void Start()
    {
        projectiles = new List<GameObject>();

        // Define Lux Ability Data
        luxAbilityData = new Dictionary<string, object>
        {
            {
                "Q", new Dictionary<string, object>
                {
                    {"range", 8f},
                    {"direction", null},
                    {"speed", 10f}
                }
            },
            {
                "W", new Dictionary<string, object>
                {
                    {"range", 5},
                    {"direction", new Vector3(1f, 2f, 5f)}
                }
            },
            {
                "E", new Dictionary<string, object>
                {
                    {"range", 5},
                    {"direction", new Vector3(1f, 2f, 5f)}
                }
            },
            {
                "R", new Dictionary<string, object>
                {
                    {"range", 5},
                    {"direction", new Vector3(1f, 2f, 5f)}
                }
            }
        };

        hitboxCollider = hitboxGameObj.GetComponent<SphereCollider>();
        isRunning = false;
        hitboxPos = hitboxGameObj.transform.position;
    }

    // Update is called once per frame
    void Update(){

    }


  

    void OnDrawGizmos(){
        // Draw a yellow sphere at the transform's position
        Gizmos.color = Color.black;
        Gizmos.DrawSphere(projectileSpawnPos, 0.3f);
    }


}
