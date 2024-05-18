using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuDedicatedServer : MonoBehaviour {
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Start() {
#if DEDICATED_SERVER
        Debug.Log("DEDICATED_SERVER");
        SceneManager.LoadScene("Multiplayer");
#endif
    }
}

