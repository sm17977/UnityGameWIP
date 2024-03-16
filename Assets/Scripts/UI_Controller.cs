using UnityEngine;
using UnityEngine.UIElements;
using System.Collections;

public class UI_Controller : MonoBehaviour
{

    [SerializeField] private UIDocument uiDocument;
    private Controls controls;
    private VisualElement abilityBox;
   
    private float qCooldown = 1000;
 
    void OnEnable(){

        controls = new Controls();
        controls.UI.Enable();
        controls.UI.Q.performed += _ => ActivateAbilityAnimation("Q");
        controls.UI.W.performed += _ => ActivateAbilityAnimation("W");
        controls.UI.E.performed += _ => ActivateAbilityAnimation("E");
        controls.UI.R.performed += _ => ActivateAbilityAnimation("R");
        
    }

    void ActivateAbilityAnimation(string abilityKey){

        string overlayElementName = abilityKey.ToLower() + "-overlay";
        abilityBox = uiDocument.rootVisualElement.Q<VisualElement>(overlayElementName);
        
        if (abilityBox != null){
            abilityBox.style.visibility = Visibility.Visible;
            if(!abilityBox.ClassListContains("bar-transition")){
                abilityBox.AddToClassList("bar-transition");
                StartCoroutine(WaitForTransition(3f, abilityBox));
            }  
        }
    }

     IEnumerator WaitForTransition(float delayInSeconds, VisualElement box){
        yield return new WaitForSeconds(delayInSeconds); 
        box.style.visibility = Visibility.Hidden;
        box.RemoveFromClassList("bar-transition");
     }
  
}

