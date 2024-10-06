using UnityEngine;
using UnityEngine.UIElements;

[UxmlElement]  // Necessary for UXML registration
public partial class LabelAutoFit : Label
{
    public float MinFontSizeInPx { get; set; } = 10f; // Minimum font size to ensure readability
    private float MaxFontSizeInPx; // Maximum font size from USS (initial font size)
    public int MaxFontSizeIterations { get; set; } = 20;
    public float SizeTolerance { get; set; } = 0.5f; // Tolerance to prevent jittering

    private float originalWidth; // Store original width
    private float originalHeight; // Store original height
    private bool isOriginalSizeStored = false;

    private bool isUpdatingFontSize = false;
    private bool isLayoutStable = false;

    public LabelAutoFit()
    {
        RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
    }

    private void OnGeometryChanged(GeometryChangedEvent evt)
    {
        if (!isOriginalSizeStored)
        {
            // Store the original container size and font size when the UI first loads
            originalWidth = contentRect.width;
            originalHeight = contentRect.height;
            MaxFontSizeInPx = resolvedStyle.fontSize; // Use USS-defined font size as max
            isOriginalSizeStored = true;
        }

        // Only trigger update if the layout is not currently being stabilized
        if (!isUpdatingFontSize && !isLayoutStable)
        {
            isUpdatingFontSize = true;

            // Schedule the font size update to avoid recursion
            schedule.Execute(() =>
            {
                UpdateFontSize();
                isUpdatingFontSize = false;
            });
        }
    }

    private void UpdateFontSize()
    {
        if (float.IsNaN(contentRect.width) || float.IsNaN(contentRect.height))
        {
            return;
        }

        float currentFontSize = resolvedStyle.fontSize;
        float nextFontSizeInPx;
        int direction = 0;
        int lastDirection = 0;
        float step = 1;
        int loop = 0;

        while (loop < MaxFontSizeIterations)
        {
            // Measure the current size of the text content
            Vector2 preferredSize = MeasureTextSize(text, 0, VisualElement.MeasureMode.Undefined, 0, VisualElement.MeasureMode.Undefined);

            // If text is too large for the container, shrink the font size
            if (preferredSize.x > contentRect.width || preferredSize.y > contentRect.height)
            {
                direction = -1; // Shrink font size
            }
            else if (currentFontSize < MaxFontSizeInPx && preferredSize.x < originalWidth && preferredSize.y < originalHeight)
            {
                direction = 1; // Grow font size back to the original size (but respect the USS-defined max)
            }
            else
            {
                break; // Exit if the size is optimal
            }

            if (lastDirection != 0 && direction != lastDirection)
            {
                break; // Font size balance found
            }
            lastDirection = direction;

            nextFontSizeInPx = currentFontSize + (step * direction);
            nextFontSizeInPx = Mathf.Clamp(nextFontSizeInPx, MinFontSizeInPx, MaxFontSizeInPx);

            // Avoid jittering when the size change is minimal
            if (Mathf.Abs(nextFontSizeInPx - currentFontSize) < SizeTolerance)
            {
                break; // Font size is optimized within tolerance
            }

            // Apply the new font size
            style.fontSize = nextFontSizeInPx;
            currentFontSize = nextFontSizeInPx;

            loop++;
        }

        // Allow the parent container to grow back to its original size
        if (contentRect.width >= originalWidth || contentRect.height >= originalHeight)
        {
            style.width = new StyleLength(StyleKeyword.Auto); // Allow the container to resize automatically
            style.height = new StyleLength(StyleKeyword.Auto);
        }

        // Force re-layout
        schedule.Execute(StabilizeLayout);
    }

    private void StabilizeLayout()
    {
        isLayoutStable = true;
        schedule.Execute(() => isLayoutStable = false);
    }
}
