using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.SceneManagement;

internal class TagRemover : IProcessSceneWithReport {

    public int callbackOrder => 0;

    public void OnProcessScene(Scene scene, BuildReport report) {

#if UNITY_STANDALONE_WIN
        foreach (GameObject obj in scene.GetRootGameObjects()) {
            if(obj.tag == "ServerOnly")
               Object.DestroyImmediate(obj);

        }
#endif
    }
}