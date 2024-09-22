using UnityEngine;
using UnityEngine.UIElements;

[UxmlElement]
public partial class LabelAutoFit : Label
{
    public float MinFontSizeInPx { get; set; } = 10f;
    public float MaxFontSizeInPx { get; set; } = 50f;
    public int MaxFontSizeIterations { get; set; } = 20;

    // Parameterless constructor needed for UXML
    public LabelAutoFit() : this(10f, 50f)
    {
    }

    public LabelAutoFit(float minFontSizeInPx, float maxFontSizeInPx)
    {
        this.MinFontSizeInPx = minFontSizeInPx;
        this.MaxFontSizeInPx = maxFontSizeInPx;
        RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
        this.RegisterValueChangedCallback(evt => UpdateFontSize());
    }

    private void OnGeometryChanged(GeometryChangedEvent evt)
    {
        // Unregister callback to prevent recursive layout updates
        UnregisterCallback<GeometryChangedEvent>(OnGeometryChanged);
        UpdateFontSize();
        // Re-register the callback after updating the font size
        RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
    }

    private void UpdateFontSize()
    {
        if (float.IsNaN(contentRect.width) || float.IsNaN(contentRect.height))
        {
            // Cannot calculate font size yet.
            return;
        }

        float nextFontSizeInPx;
        int direction;
        int lastDirection = 0;
        float step = 1;
        int loop = 0;

        while (loop < MaxFontSizeIterations)
        {
            Vector2 preferredSize = MeasureTextSize(text, 0, VisualElement.MeasureMode.Undefined, 0, VisualElement.MeasureMode.Undefined);

            if (preferredSize.x > contentRect.width || preferredSize.y > contentRect.height)
            {
                // Text is too big, reduce font size
                direction = -1;
            }
            else
            {
                // Text is too small, increase font size
                direction = 1;
            }

            if (lastDirection != 0 && direction != lastDirection)
            {
                // Found best match.
                return;
            }
            lastDirection = direction;

            nextFontSizeInPx = resolvedStyle.fontSize + (step * direction);
            nextFontSizeInPx = Mathf.Clamp(nextFontSizeInPx, MinFontSizeInPx, MaxFontSizeInPx);
            style.fontSize = nextFontSizeInPx;
            loop++;
        }
    }
}
