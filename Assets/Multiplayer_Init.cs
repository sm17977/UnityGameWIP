using UnityEngine;

public class Multiplayer_Init : MonoBehaviour
{

    public GameObject playerPrefab;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start(){

        Vector3 initPos = new Vector3(0f, 0.5f, 0f);
        Instantiate(playerPrefab, initPos, Quaternion.identity);

        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
