using UnityEngine;
using UnityEngine.UIElements;

namespace UI_Templates.Helpers {
    public class DrawHelper {
        private static Painter2D _painter;
        
        public enum GradientDirection {
            Vertical,
            Horizontal
        }
        
        public static void init(Painter2D painter2D) {
            _painter = painter2D;
            _painter.lineWidth = 10.0f;
            _painter.fillColor = Color.blue;
            
        }

        public static void DrawRect(int width, int height) {
            _painter.BeginPath();
            _painter.MoveTo(new Vector2(0,0));
            _painter.LineTo(new Vector2(0, height));
            _painter.LineTo(new Vector2(width, height));
            _painter.LineTo(new Vector2(width, 0));
            _painter.LineTo(new Vector2(0, 0));
            _painter.ClosePath();
            _painter.Fill();
        }
        
        public static void DrawGradientRect(int width, int height, Color startColor, Color endColor, GradientDirection direction) {
            int steps = direction == GradientDirection.Vertical ? height : width;
            
            for (int i = 0; i < steps; i++) {
                float t = (float)i / (steps - 1);
                Color currentColor = Color.Lerp(startColor, endColor, t);
                _painter.fillColor = currentColor;
                _painter.BeginPath();
                
                Vector2[] points = direction switch
                {
                    GradientDirection.Vertical => new Vector2[] {
                        new Vector2(0, i),
                        new Vector2(width, i),
                        new Vector2(width, i + 1),
                        new Vector2(0, i + 1)
                    },
                    GradientDirection.Horizontal => new Vector2[] {
                        new Vector2(i, 0),
                        new Vector2(i + 1, 0),
                        new Vector2(i + 1, height),
                        new Vector2(i, height)
                    },
                    _ => throw new System.NotSupportedException("Unsupported gradient direction")
                };
                
                _painter.MoveTo(points[0]);
                for (int p = 1; p < points.Length; p++) _painter.LineTo(points[p]);
                _painter.ClosePath();
                _painter.Fill();
            }
        }
    }
}