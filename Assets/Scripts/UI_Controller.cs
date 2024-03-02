using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.InputSystem;
using System.Collections;

public class UI_Controller : MonoBehaviour
{

    [SerializeField] private UIDocument uiDocument;
    private Controls controls;
    private VisualElement qBox;
   
    private float qCooldown = 1000;
 
    void OnEnable(){

        controls = new Controls();
        controls.UI.Enable();
        controls.UI.Q.performed += _ => ActivateQ();
        
    }

    void ActivateQ(){

        qBox = uiDocument.rootVisualElement.Q<VisualElement>("q-overlay");
        var wBox = uiDocument.rootVisualElement.Q<VisualElement>("w-box");
        var eBox = uiDocument.rootVisualElement.Q<VisualElement>("e-box");
        var rBox = uiDocument.rootVisualElement.Q<VisualElement>("r-box");
        
        if (qBox != null){
            qBox.style.visibility = Visibility.Visible;
            if(!qBox.ClassListContains("bar-transition")){
                qBox.AddToClassList("bar-transition");
                StartCoroutine(WaitForTransition(3f));
            }  
        }
    }

     IEnumerator WaitForTransition(float delayInSeconds){
        yield return new WaitForSeconds(delayInSeconds); 
        qBox.style.visibility = Visibility.Hidden;
        qBox.RemoveFromClassList("bar-transition");
     }
  
}

