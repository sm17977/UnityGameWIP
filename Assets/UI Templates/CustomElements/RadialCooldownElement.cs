using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace CustomElements
{
    [UxmlElement] // Automatically integrates with UI Builder and UXML
    public partial class RadialCooldownElement : VisualElement
    {
        public static readonly string ussClassName = "radial-cooldown";
        public static readonly string ussTimerClassName = "radial-cooldown__timer";

        private static readonly CustomStyleProperty<Color> s_CooldownColor = new("--cooldown-color");
        private static readonly CustomStyleProperty<StyleBackground> s_ReadyImage = new("--ready-image");

        private Color m_CooldownColor = Color.blue;
        private StyleBackground m_ReadyImage;

        private float m_Progress;
        private Label m_TimerLabel;

        private float m_Duration;
        private float m_TimeLeft;

        private string m_Key;

        private IVisualElementScheduledItem _cooldownSchedule;

        [UxmlAttribute("key")] // Automatically binds the key attribute in UXML
        public string Key
        {
            get => m_Key;
            set
            {
                m_Key = value;
                
            }
        }

        [UxmlAttribute]
        public float Progress
        {
            get => m_Progress;
            set
            {
                m_Progress = Mathf.Clamp(value, 0f, 100f);
                UpdateTimerLabel();
                MarkDirtyRepaint();
            }
        }

        public RadialCooldownElement()
        {
            AddToClassList(ussClassName);

            // Add the timer label
            m_TimerLabel = new Label();
            m_TimerLabel.AddToClassList(ussTimerClassName);
            m_TimerLabel.text = ""; 
            Add(m_TimerLabel);

            RegisterCallback<CustomStyleResolvedEvent>(OnCustomStyleResolved);
            generateVisualContent += GenerateVisualContent;

            Progress = 0f;
        }


        private void OnCustomStyleResolved(CustomStyleResolvedEvent evt)
        {
            if (customStyle.TryGetValue(s_CooldownColor, out var cooldownColor)) 
                m_CooldownColor = cooldownColor;

            // Remove any previous ability class
            RemoveFromClassList("q-skill");
            RemoveFromClassList("w-skill");
            RemoveFromClassList("e-skill");
            RemoveFromClassList("r-skill");

            // Add the appropriate ability class based on key
            switch (Key)
            {
                case "Q": AddToClassList("q-skill"); break;
                case "W": AddToClassList("w-skill"); break;
                case "E": AddToClassList("e-skill"); break;
                case "R": AddToClassList("r-skill"); break;
            }

            MarkDirtyRepaint();
        }





        private void GenerateVisualContent(MeshGenerationContext mgc)
        {
            if (m_TimeLeft <= 0) return; // Don't draw the cooldown effect if ability is ready

            var painter2D = mgc.painter2D;
            var rect = contentRect;
            var center = rect.center;

            float radius = Mathf.Sqrt(rect.width * rect.width + rect.height * rect.height) * 0.5f;

            painter2D.fillColor = m_CooldownColor;
            painter2D.BeginPath();
            painter2D.MoveTo(center);

            int steps = 100;
            float angleStep = 360f / steps;
            float progressAngle = (m_Progress / 100f) * 360f;
            float startAngle = -90f; // Start at top

            for (int i = 0; i <= steps; i++)
            {
                float angle = Mathf.Min(i * angleStep, progressAngle) + startAngle;
                float radians = angle * Mathf.Deg2Rad;

                Vector2 point = center + new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)) * radius;
                painter2D.LineTo(point);

                if (angle - startAngle >= progressAngle)
                    break;
            }

            painter2D.ClosePath();
            painter2D.Fill();
        }

        public void StartCooldown(float duration)
        {
            _cooldownSchedule?.Pause();

            m_Duration = duration;
            m_TimeLeft = duration;

            Progress = 0f;
            UpdateTimerLabel();

            _cooldownSchedule = schedule.Execute(UpdateCooldown).Every(1000 / 60).StartingIn(0);
        }

        private void UpdateCooldown()
        {
            if (m_TimeLeft > 0)
            {
                m_TimeLeft -= Time.deltaTime;
                Progress = (m_Duration - m_TimeLeft) / m_Duration * 100f;

                if (m_TimeLeft <= 0)
                {
                    m_TimeLeft = 0;
                    Progress = 100f;
                    m_TimerLabel.text = m_Key;
                }

                UpdateTimerLabel();
            }
        }

        private void UpdateTimerLabel() {
            if (m_TimeLeft <= 0)
                m_TimerLabel.text = ""; // Hide text when ability is ready
            else
                m_TimerLabel.text = Mathf.CeilToInt(m_TimeLeft).ToString(); // Show countdown when active
        }

    }
}
