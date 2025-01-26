using UnityEngine;
using UnityEngine.UIElements;

namespace CustomElements {
    [UxmlElement] // Automatically integrates with UI Builder and UXML
    public partial class RadialCooldownElement : VisualElement {
        public static readonly string ussClassName = "radial-cooldown";
        public static readonly string ussTimerClassName = "radial-cooldown__timer";

        private static readonly CustomStyleProperty<Color> s_ReadyColor = new("--ready-color");
        private static readonly CustomStyleProperty<Color> s_CooldownColor = new("--cooldown-color");

        private Color m_ReadyColor = Color.red;
        private Color m_CooldownColor = Color.blue;

        private float m_Progress;
        private Label m_TimerLabel;

        private float m_Duration;
        private float m_TimeLeft;

        private string m_Key;

        private IVisualElementScheduledItem _cooldownSchedule;


        [UxmlAttribute("key")] // Automatically binds the key attribute in UXML
        public string Key {
            get => m_Key; // Return the stored key
            set {
                m_Key = value; // Store the key
                if (m_TimeLeft <= 0 && m_TimerLabel != null)
                    m_TimerLabel.text = m_Key; // Set the default text only when not on cooldown
            }
        }

        [UxmlAttribute]
        public float Progress {
            get => m_Progress;
            set {
                m_Progress = Mathf.Clamp(value, 0f, 100f);
                UpdateTimerLabel();
                MarkDirtyRepaint();
            }
        }

        public RadialCooldownElement() {
            AddToClassList(ussClassName);

            // Add the timer label
            m_TimerLabel = new Label();
            m_TimerLabel.AddToClassList(ussTimerClassName);
            Add(m_TimerLabel);

            RegisterCallback<CustomStyleResolvedEvent>(OnCustomStyleResolved);
            generateVisualContent += GenerateVisualContent;

            Progress = 0f;
        }

        private void OnCustomStyleResolved(CustomStyleResolvedEvent evt) {
            if (customStyle.TryGetValue(s_ReadyColor, out var readyColor)) m_ReadyColor = readyColor;
            if (customStyle.TryGetValue(s_CooldownColor, out var cooldownColor)) m_CooldownColor = cooldownColor;

            MarkDirtyRepaint();
        }

        private void GenerateVisualContent(MeshGenerationContext mgc) {
            var painter2D = mgc.painter2D;
            var rect = contentRect;
            var center = rect.center;

            // Increase the radius to ensure it covers the square
            var radius = Mathf.Sqrt(Mathf.Pow(rect.width, 2) + Mathf.Pow(rect.height, 2)) * 0.5f;

            // Draw the base (full blue when on cooldown or red when ready)
            painter2D.fillColor = m_TimeLeft > 0 ? m_CooldownColor : m_ReadyColor;
            painter2D.BeginPath();
            painter2D.MoveTo(new Vector2(rect.xMin, rect.yMin));
            painter2D.LineTo(new Vector2(rect.xMax, rect.yMin));
            painter2D.LineTo(new Vector2(rect.xMax, rect.yMax));
            painter2D.LineTo(new Vector2(rect.xMin, rect.yMax));
            painter2D.ClosePath();
            painter2D.Fill();

            // If on cooldown, draw the radial progress to uncover the base color
            if (m_TimeLeft > 0) {
                painter2D.fillColor = m_ReadyColor;
                painter2D.BeginPath();
                painter2D.MoveTo(center);

                var steps = 100; // Number of steps for smooth radial segments
                var angleStep = 360f / steps;
                var progressAngle = m_Progress / 100f * 360f;

                var startAngle = -90f; // Start at -90 degrees (top middle)
                for (var i = 0; i <= steps; i++) {
                    var angle = Mathf.Min(i * angleStep, progressAngle) + startAngle;
                    var radians = angle * Mathf.Deg2Rad;

                    var point = center + new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)) * radius;
                    painter2D.LineTo(point);

                    if (angle - startAngle >= progressAngle)
                        break;
                }

                painter2D.ClosePath();
                painter2D.Fill();
            }
        }

        public void StartCooldown(float duration) {
            _cooldownSchedule?.Pause();

            m_Duration = duration;
            m_TimeLeft = duration;

            Progress = 0f;
            UpdateTimerLabel();

            _cooldownSchedule = schedule.Execute(UpdateCooldown).Every(1000 / 60).StartingIn(0);
        }

        private void UpdateCooldown() {
            if (m_TimeLeft > 0) {
                m_TimeLeft -= Time.deltaTime;
                Progress = (m_Duration - m_TimeLeft) / m_Duration * 100f;

                if (m_TimeLeft <= 0) {
                    m_TimeLeft = 0;
                    Progress = 100f;
                    m_TimerLabel.text = m_Key;
                }

                UpdateTimerLabel();
            }
        }

        private void UpdateTimerLabel() {
            if (m_TimeLeft <= 0)
                m_TimerLabel.text = m_Key;
            else
                m_TimerLabel.text = Mathf.CeilToInt(m_TimeLeft).ToString();
        }
    }
}