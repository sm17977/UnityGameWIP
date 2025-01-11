using Global.Game_Modes;
using UnityEngine;

public class ProjectileSpawner : MonoBehaviour
{

    // Refs
    public Camera mainCamera;
    public GameObject player;
    private LuxPlayerController playerController;
    private GlobalState globalState;
    private Round currentRound;
    
    // Properties
    private Vector3 direction;
    public float timer;

    // Flags
    public bool canSpawn = true;

    // Viewport edges
    private Vector3 bottomLeft = Vector3.zero;
    private Vector3 topLeft = Vector3.zero;
    private Vector3 topRight = Vector3.zero;
    private Vector3 bottomRight = Vector3.zero;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start(){
        globalState = GameObject.Find("Global State").GetComponent<GlobalState>();
        playerController = player.GetComponent<LuxPlayerController>();
        if (GlobalState.GameModeManager.CurrentGameMode is Arena arena) {
            currentRound = arena.RoundManager.GetCurrentRound();
        }

        timer = currentRound.projectileFrequency;
    }

    void Update() {

        if (GlobalState.GameModeManager.CurrentGameMode is Arena arena) {
            currentRound = arena.RoundManager.GetCurrentRound();
        }

        if (!canSpawn) {
            timer -= Time.deltaTime;
            if (timer <= 0) {
                canSpawn = true;
                timer = currentRound.projectileFrequency; 
            }
        }

        if (canSpawn) {
            for(int i = 0; i < currentRound.maxProjectileCount; i++){
                SpawnProjectile(currentRound.ability);
            }
            canSpawn = false;
        }
    }
     public Vector3 GetRandomPoint() {
        // The bottom-left of the camera is (0,0); the top-right is (1,1).
        Ray bottomLeftRay = mainCamera.ViewportPointToRay(new Vector3(0f, 0f, 0));
        Ray topLeftRay = mainCamera.ViewportPointToRay(new Vector3(0f, 1f, 0));
        Ray topRightRay = mainCamera.ViewportPointToRay(new Vector3(1f, 1f, 0));
        Ray bottomRightRay = mainCamera.ViewportPointToRay(new Vector3(1f, 0f, 0));

        RaycastHit bottomLeftHit;
        RaycastHit topLeftHit;
        RaycastHit topRightHit;
        RaycastHit bottomRightHit;

        if (Physics.Raycast(bottomLeftRay, out bottomLeftHit) && bottomLeftHit.transform.name == "Terrain") {
            bottomLeft = bottomLeftHit.point + new Vector3(-1, 0, -1);
        }

        if (Physics.Raycast(topLeftRay, out topLeftHit) && topLeftHit.transform.name == "Terrain") {
            topLeft = topLeftHit.point + new Vector3(-1, 0, 1);
        }

        if (Physics.Raycast(topRightRay, out topRightHit) && topRightHit.transform.name == "Terrain") {
            topRight = topRightHit.point + new Vector3(1, 0, 1);
        }

        if (Physics.Raycast(bottomRightRay, out bottomRightHit) && bottomRightHit.transform.name == "Terrain") {
            bottomRight = bottomRightHit.point + new Vector3(1, 0, -1);
        }

        // Generate a random value between 0 and 1 to choose an edge
        float edgeSelector = Random.value;
        // Generate a random value between 0 and 1 to choose a point along the edge
        float t = Random.value;

        // Select an edge based on edgeSelector and interpolate a point along it
        if (edgeSelector < 0.25f) {
            // Bottom edge between bottomLeft and bottomRight
            return Vector3.Lerp(bottomLeft, bottomRight, t);
        } else if (edgeSelector < 0.5f) {
            // Right edge between bottomRight and topRight
            return Vector3.Lerp(bottomRight, topRight, t);
        } else if (edgeSelector < 0.75f) {
            // Top edge between topRight and topLeft
            return Vector3.Lerp(topRight, topLeft, t);
        } else {
            // Left edge between topLeft and bottomLeft
            return Vector3.Lerp(topLeft, bottomLeft, t);
        }
    }

    public void SpawnProjectile(Ability ability){
        Vector3 randomPoint = GetRandomPoint();
        Vector3 spawnPos = new Vector3(randomPoint.x, ability.spawnHeight, randomPoint.z);
        SetDirectionToPlayer(spawnPos);

        GameObject projectile = Instantiate(ability.missilePrefab, spawnPos, Quaternion.LookRotation(direction, Vector3.up));

        playerController.projectiles.Add(projectile);

        // Get script on prefab to initialize propreties
        ClientAbilityBehaviour projectileScript = projectile.GetComponent<ClientAbilityBehaviour>();
        //projectileScript?.InitProjectileProperties(direction, ability, playerController.projectiles, PlayerType.Bot);
       
    }

    public void SetDirectionToPlayer(Vector3 projectileSpawnPos){
        direction = (player.transform.position - projectileSpawnPos).normalized;
        direction.y = 0;
    }

    void OnDrawGizmos(){
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(topLeft, 0.3f);
        Gizmos.DrawSphere(topRight, 0.3f);
        Gizmos.DrawSphere(bottomLeft, 0.3f);
        Gizmos.DrawSphere(bottomRight, 0.3f);
    }
}
