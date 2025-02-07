using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuDedicatedServer : MonoBehaviour {
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Start() {
#if UNITY_SERVER
        Debug.Log("UNITY_SERVER");
        SceneManager.LoadScene("Multiplayer");
#endif
    }
}

