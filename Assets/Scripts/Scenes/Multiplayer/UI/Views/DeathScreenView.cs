using System.Linq;
using System.Threading.Tasks;
using CustomElements;
using Multiplayer.UI;
using UnityEngine.UIElements;

public delegate void OnRematch();

public delegate Task OnLeaveLobby();
public class DeathScreenView : View {

    public event OnRematch Rematch;
    public event OnLeaveLobby LeaveLobby;
    
    private Label _resultTitle;
    private Label _resultInfo;
    
    private Label _rank;
    private Label _abilitiesLanded;
    private Label _abilitiesDodged;
    private Label _apm;
    private Label _duration;
    
    private Button _rematchBtn;
    private Button _leaveLobbyBtn;
    
    public DeathScreenView(VisualElement parentContainer, VisualTreeAsset vta) {
        Template = vta.Instantiate().Children().FirstOrDefault();
        ParentContainer = parentContainer;
        BindUIElements();
    }
    
    private void BindUIElements() {
        _rank = Template.Q<Label>("stat-rank");
        _abilitiesLanded = Template.Q<Label>("stat-landed");
        _abilitiesDodged = Template.Q<Label>("stat-dodged");
        _apm = Template.Q<Label>("stat-apm");
        _duration = Template.Q<Label>("stat-duration");
        
        _rematchBtn = Template.Q<Button>("rematch-btn");
        _rematchBtn.RegisterCallback<ClickEvent>((evt) => Rematch());
        _leaveLobbyBtn = Template.Q<Button>("back-btn");
        _leaveLobbyBtn.RegisterCallback<ClickEvent>( (evt) => { 
            var _= LeaveLobby?.Invoke();
        });
    }   
    
    public override void Update() {
        throw new System.NotImplementedException();
    }

    public override void RePaint() {
        throw new System.NotImplementedException();
    }
}
