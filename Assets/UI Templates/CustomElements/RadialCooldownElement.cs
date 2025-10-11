using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace CustomElements {
    [UxmlElement]
    public partial class RadialCooldownElement : VisualElement {
        public static readonly string ussClassName = "radial-cooldown";
        public static readonly string ussTimerClassName = "radial-cooldown__timer";

        private static readonly CustomStyleProperty<Color> s_CooldownColor = new("--cooldown-color");
        private static readonly CustomStyleProperty<StyleBackground> s_ReadyImage = new("--ready-image");
        private static readonly CustomStyleProperty<string> s_Shimmer = new("--shimmer");

        private Color _cooldownColor = Color.blue;
        private StyleBackground _readyImage;

        private float _progress;
        private Label _timerLabel;

        private float _duration;
        private float _timeLeft;

        private string _key;
        
        private bool _drawShimmer;
        private float _shimmerProgress;
        private bool _isShimmering;
        private IVisualElementScheduledItem _shimmerSchedule;

  
        private IVisualElementScheduledItem _cooldownSchedule;
        private float _cooldownFade = 1f;
        private IVisualElementScheduledItem _fadeSchedule;

        [UxmlAttribute("key")] 
        public string Key {
            get => _key;
            set => _key = value;
        }

        [UxmlAttribute]
        public float Progress {
            get => _progress;
            set {
                _progress = Mathf.Clamp(value, 0f, 100f);
                UpdateTimerLabel();
                MarkDirtyRepaint();
            }
        }

        public RadialCooldownElement() {
            AddToClassList(ussClassName);
            
            _timerLabel = new Label();
            _timerLabel.AddToClassList(ussTimerClassName);
            _timerLabel.text = "";
            Add(_timerLabel);

            RegisterCallback<CustomStyleResolvedEvent>(OnCustomStyleResolved);
            generateVisualContent += GenerateVisualContent;

            Progress = 0f;
        }

        private void OnCustomStyleResolved(CustomStyleResolvedEvent evt) {
            if (customStyle.TryGetValue(s_CooldownColor, out var cooldownColor))
                _cooldownColor = cooldownColor;
            
            RemoveFromClassList("q-skill");
            RemoveFromClassList("w-skill");
            RemoveFromClassList("e-skill");
            RemoveFromClassList("r-skill");
            
            switch (Key) {
                case "Q":
                    AddToClassList("q-skill");
                    break;
                case "W":
                    AddToClassList("w-skill");
                    break;
                case "E":
                    AddToClassList("e-skill");
                    break;
                case "R":
                    AddToClassList("r-skill");
                    break;
            }
            
            bool shouldShimmer = false;
            if (customStyle.TryGetValue(s_Shimmer, out var shimmerValue)) {
                shouldShimmer = shimmerValue.Equals("true", System.StringComparison.OrdinalIgnoreCase);
            }
            
            if (shouldShimmer && !_isShimmering) {
                _isShimmering = true;
                ShimmerEffect();
            }
            else if (!shouldShimmer) {
                _isShimmering = false;
            }
            MarkDirtyRepaint();
        }

        private void GenerateVisualContent(MeshGenerationContext mgc) {
            var painter2D = mgc.painter2D;
            var rect = contentRect;
            
            // Draw shimmer if enabled
            if (_drawShimmer) DrawShimmer(painter2D, rect);
            
            // Draw the radial cooldown effect if it’s active or if it is fading out
            if (_timeLeft > 0 || _cooldownFade > 0f)
                DrawRadialCooldownEffect(painter2D, rect);
        }

        private void DrawRadialCooldownEffect(Painter2D painter2D, Rect rect) {
            var center = rect.center;
            var radius = Mathf.Sqrt(rect.width * rect.width + rect.height * rect.height) * 0.5f;
            
            // Multiply the cooldown color’s alpha by _cooldownFade so it gradually fades
            Color fadedColor = new Color(_cooldownColor.r, _cooldownColor.g, _cooldownColor.b, _cooldownColor.a * _cooldownFade);

            painter2D.fillColor = fadedColor;
            painter2D.BeginPath();
            painter2D.MoveTo(center);

            var steps = 100;
            var angleStep = 360f / steps;
            var progressAngle = _progress / 100f * 360f;
            var startAngle = -90f;

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
        
        private void DrawShimmer(Painter2D painter2D, Rect rect) {
            float shimmerWidth = rect.width * 0.3f;
            float xOffset = Mathf.Lerp(-shimmerWidth, rect.width, _shimmerProgress);
            int segments = 10;
            
            for (int i = 0; i < segments; i++) {
                float t0 = i / (float)segments;
                float t1 = (i + 1) / (float)segments;
                float segX0 = xOffset + t0 * shimmerWidth;
                float segX1 = xOffset + t1 * shimmerWidth;
                float segMid = (t0 + t1) / 2f;
                float alphaMultiplier = 1f - Mathf.Abs(segMid - 0.5f) * 2f;
                float alpha = 0.3f * alphaMultiplier * (1 - _shimmerProgress);
                Color segColor = new Color(1f, 1f, 1f, alpha);
                
                painter2D.fillColor = segColor;
                painter2D.BeginPath();
                painter2D.MoveTo(new Vector2(segX0, 0));
                painter2D.LineTo(new Vector2(segX1, 0));
                painter2D.LineTo(new Vector2(segX1 - 10, rect.height));
                painter2D.LineTo(new Vector2(segX0 - 10, rect.height));
                painter2D.ClosePath();
                painter2D.Fill();
            }
        }

        public void StartCooldown(float duration) {
            Debug.Log("Start Cooldown");
            
            _cooldownSchedule?.Pause();
            _fadeSchedule?.Pause();
            ShimmerEffect();

            _duration = duration;
            _timeLeft = duration;
            _cooldownFade = 1f; // reset fade to fully visible

            Progress = 0f;
            UpdateTimerLabel();

            _cooldownSchedule = schedule.Execute(UpdateCooldown).Every(1000 / 60).StartingIn(0);
        }

        private void ShimmerEffect() {
            _shimmerSchedule?.Pause();
            _drawShimmer = true;
            _shimmerProgress = 0f;
            float shimmerDuration = 0.5f;
            
            _shimmerSchedule = schedule.Execute(() => {
                _shimmerProgress += Time.deltaTime / shimmerDuration;
                if (_shimmerProgress >= 1f) {
                    _drawShimmer = false;
                    _shimmerProgress = 0f;
                }
                MarkDirtyRepaint();
            }).Every(16);
        }
        
        private void UpdateCooldown() {
            if (_timeLeft > 0) {
                _timeLeft -= Time.deltaTime;
                Progress = (_duration - _timeLeft) / _duration * 100f;

                if (_timeLeft <= 0) {
                    _timeLeft = 0;
                    Progress = 100f;
                    _timerLabel.text = _key;
                    
                    if (_fadeSchedule == null) {
                        float fadeDuration = 0.3f; 
                        _fadeSchedule = schedule.Execute(() => {
                            _cooldownFade -= Time.deltaTime / fadeDuration;
                            if (_cooldownFade <= 0f) {
                                _cooldownFade = 0f;
                                _fadeSchedule.Pause();
                                _fadeSchedule = null;
                            }
                            MarkDirtyRepaint();
                        }).Every(16);
                    }
                }

                UpdateTimerLabel();
            }
        }

        private void UpdateTimerLabel() {
            if (_timeLeft <= 0)
                _timerLabel.text = ""; 
            else
                _timerLabel.text = Mathf.CeilToInt(_timeLeft).ToString(); 
        }
    }
}
