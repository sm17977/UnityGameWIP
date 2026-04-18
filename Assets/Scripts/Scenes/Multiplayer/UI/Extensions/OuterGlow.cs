// Created 2024 StagPoint. Released to the public domain.

using System;
using System.Runtime.CompilerServices;
using Unity.Collections;
using UnityEngine;
using UnityEngine.UIElements;

[UxmlElement]
public partial class OuterGlow : VisualElement
{
    #region UXML attributes

    [UxmlAttribute("glow-color")]
    public Color uxmlGlowColor
    {
        get => _glowColorInline ?? Color.white;
        set => _glowColorInline = value;
    }

    [UxmlAttribute("glow-size")]
    public float uxmlGlowSize
    {
        get => _glowSizeInline ?? 10f;
        set => _glowSizeInline = value;
    }

    [UxmlAttribute("corner-segments")]
    public int uxmlCornerSegments
    {
        get => NumberOfSegments;
        set => NumberOfSegments = Mathf.Clamp(value, 4, 24);
    }

    #endregion

    #region Public properties

    public float OffsetX
    {
        get => _glowOffsetX;
        set
        {
            _glowOffsetX = value;
            MarkDirtyRepaint();
        }
    }

    public float OffsetY
    {
        get => _glowOffsetY;
        set
        {
            _glowOffsetY = value;
            MarkDirtyRepaint();
        }
    }

    public Color BackgroundColor
    {
        get => _backgroundColor;
        set
        {
            _backgroundColor = value;
            MarkDirtyRepaint();
        }
    }

    public float BorderRadius
    {
        get => _glowBorderRadius;
        set
        {
            _glowBorderRadius = value;
            MarkDirtyRepaint();
        }
    }

    public float GlowSize
    {
        get => _glowSizeInline ?? _glowSizeStyled ?? 10f;
        set
        {
            _glowSizeInline = value;
            MarkDirtyRepaint();
        }
    }

    public Color GlowColor
    {
        get => _glowColorInline ?? _glowColorStyled ?? Color.clear;
        set
        {
            _glowColorInline = value;
            MarkDirtyRepaint();
        }
    }

    public int NumberOfSegments { get; set; } = 128;

    #endregion

    #region Private fields

    private readonly CustomStyleProperty<Color> _glowColorStyleProperty =
        new("--glow-color");
    private readonly CustomStyleProperty<float> _glowSizeStyleProperty =
        new("--glow-size");
    private readonly CustomStyleProperty<float> _glowOffsetXStyleProperty =
        new("--glow-offset-x");
    private readonly CustomStyleProperty<float> _glowOffsetYStyleProperty =
        new("--glow-offset-y");
    private readonly CustomStyleProperty<Color> _glowBackgroundColorStyleProperty =
        new("--glow-background-color");
    private readonly CustomStyleProperty<float> _glowBorderRadiusStyleProperty =
        new("--glow-border-radius");

    private Color? _glowColorInline;
    private float? _glowSizeInline;
    private Color? _glowColorStyled;
    private float? _glowSizeStyled;
    private float _glowBorderRadius;
    private float _glowOffsetX;
    private float _glowOffsetY;
    private Color _backgroundColor = Color.clear;

    #endregion

    #region Constructor

    public OuterGlow()
    {
        generateVisualContent += OnGenerateVisualContent;
        RegisterCallback<CustomStyleResolvedEvent>(OnCustomStyleResolved);
    }

    #endregion

    #region Mesh generation

    private void OnGenerateVisualContent(MeshGenerationContext ctx)
    {
        Rect r = contentRect;
        var painter = ctx.painter2D;

        painter.fillColor = BackgroundColor;
        painter.BeginPath();

        painter.MoveTo(new Vector2(r.x + BorderRadius + OffsetX, r.y + OffsetY));

        painter.LineTo(new Vector2(r.x + r.width - BorderRadius + OffsetX, r.y + OffsetY));
        painter.ArcTo(
            new Vector2(r.x + r.width + OffsetX, r.y + OffsetY),
            new Vector2(r.x + r.width + OffsetX, r.y + BorderRadius + OffsetY),
            BorderRadius
        );

        painter.LineTo(new Vector2(r.x + r.width + OffsetX, r.y + r.height - BorderRadius + OffsetY));
        painter.ArcTo(
            new Vector2(r.x + r.width + OffsetX, r.y + r.height + OffsetY),
            new Vector2(r.x + r.width - BorderRadius + OffsetX, r.y + r.height + OffsetY),
            BorderRadius
        );

        painter.LineTo(new Vector2(r.x + BorderRadius + OffsetX, r.y + r.height + OffsetY));
        painter.ArcTo(
            new Vector2(r.x + OffsetX, r.y + r.height + OffsetY),
            new Vector2(r.x + OffsetX, r.y + r.height - BorderRadius + OffsetY),
            BorderRadius
        );

        painter.LineTo(new Vector2(r.x + OffsetX, r.y + BorderRadius + OffsetY));
        painter.ArcTo(
            new Vector2(r.x + OffsetX, r.y + OffsetY),
            new Vector2(r.x + BorderRadius + OffsetX, r.y + OffsetY),
            BorderRadius
        );

        painter.ClosePath();
        painter.Fill();

        CalculateGlowMesh(out TempList<Vertex> verts, out TempList<ushort> indices);

        MeshWriteData mwd = ctx.Allocate(verts.Count, indices.Count);
        mwd.SetAllVertices(verts.ToSlice());
        mwd.SetAllIndices(indices.ToSlice());

        verts.Dispose();
        indices.Dispose();
    }

    private void CalculateGlowMesh(out TempList<Vertex> verts, out TempList<ushort> indices)
    {
        var numberOfSegments = Mathf.Clamp(NumberOfSegments, 4, 128);
        var numVerticesPerCorner = (numberOfSegments + 1) * 2;
        var numIndicesPerCorner = numberOfSegments * 6;
        var expectedTotalVertices = numVerticesPerCorner * 4;
        var expectedTotalIndices = numIndicesPerCorner * 4 + 24;

        verts = new TempList<Vertex>(expectedTotalVertices);
        indices = new TempList<ushort>(expectedTotalIndices);

        var thickness = Mathf.Max(GlowSize, 0);

        Rect r = contentRect;
        var halfSize = new Vector3(r.width * 0.5f, r.height * 0.5f, Vertex.nearZ);
        var minDimension = Mathf.Min(r.width, r.height);
        var minHalfDimension = Mathf.Min(halfSize.x, halfSize.y);

        var radiusTopLeft = Mathf.Max(resolvedStyle.borderTopLeftRadius, 0);
        var radiusTopRight = Mathf.Max(resolvedStyle.borderTopRightRadius, 0);
        var radiusBottomLeft = Mathf.Max(resolvedStyle.borderBottomLeftRadius, 0);
        var radiusBottomRight = Mathf.Max(resolvedStyle.borderBottomRightRadius, 0);

        var innerRadiusTopLeft = minHalfDimension * Mathf.Min(minDimension * 0.5f, radiusTopLeft) / minDimension * 2f;
        var topLeft = new Vector3(innerRadiusTopLeft, innerRadiusTopLeft, Vertex.nearZ);

        var innerRadiusTopRight = minHalfDimension * Mathf.Min(minDimension * 0.5f, radiusTopRight) / minDimension * 2f;
        var topRight = new Vector3(r.width, 0, Vertex.nearZ) - new Vector3(innerRadiusTopRight, -innerRadiusTopRight, Vertex.nearZ);

        var innerRadiusBottomRight = minHalfDimension * Mathf.Min(minDimension * 0.5f, radiusBottomRight) / minDimension * 2f;
        var bottomRight = new Vector3(r.width, r.height, Vertex.nearZ) - new Vector3(innerRadiusBottomRight, innerRadiusBottomRight, Vertex.nearZ);

        var innerRadiusBottomLeft = minHalfDimension * Mathf.Min(minDimension * 0.5f, radiusBottomLeft) / minDimension * 2f;
        var bottomLeft = new Vector3(0, r.height, Vertex.nearZ) + new Vector3(innerRadiusBottomLeft, -innerRadiusBottomLeft, Vertex.nearZ);

        topLeft += new Vector3(OffsetX, OffsetY, 0);
        topRight += new Vector3(OffsetX, OffsetY, 0);
        bottomRight += new Vector3(OffsetX, OffsetY, 0);
        bottomLeft += new Vector3(OffsetX, OffsetY, 0);

        var outerRadiusTopLeft = innerRadiusTopLeft + thickness;
        var outerRadiusTopRight = innerRadiusTopRight + thickness;
        var outerRadiusBottomRight = innerRadiusBottomRight + thickness;
        var outerRadiusBottomLeft = innerRadiusBottomLeft + thickness;

        var innerColor = GlowColor;
        var outerColor = new Color(innerColor.r, innerColor.g, innerColor.b, 0.01f);

        CalculateCornerMesh(topLeft, innerColor, outerColor, verts, indices, innerRadiusTopLeft, outerRadiusTopLeft, numberOfSegments, 90 * Mathf.Deg2Rad, 90 * Mathf.Deg2Rad);
        CalculateCornerMesh(topRight, innerColor, outerColor, verts, indices, innerRadiusTopRight, outerRadiusTopRight, numberOfSegments, 0, 90 * Mathf.Deg2Rad);
        CalculateCornerMesh(bottomRight, innerColor, outerColor, verts, indices, innerRadiusBottomRight, outerRadiusBottomRight, numberOfSegments, 270 * Mathf.Deg2Rad, 90 * Mathf.Deg2Rad);
        CalculateCornerMesh(bottomLeft, innerColor, outerColor, verts, indices, innerRadiusBottomLeft, outerRadiusBottomLeft, numberOfSegments, 180 * Mathf.Deg2Rad, 90 * Mathf.Deg2Rad);

        indices.Add(0);
        indices.Add(1);
        indices.Add((ushort)(numVerticesPerCorner * 2 - 1));
        indices.Add(0);
        indices.Add((ushort)(numVerticesPerCorner * 2 - 1));
        indices.Add((ushort)(numVerticesPerCorner * 2 - 2));

        indices.Add((ushort)(numVerticesPerCorner));
        indices.Add((ushort)(numVerticesPerCorner + 1));
        indices.Add((ushort)(numVerticesPerCorner * 3 - 1));
        indices.Add((ushort)(numVerticesPerCorner));
        indices.Add((ushort)(numVerticesPerCorner * 3 - 1));
        indices.Add((ushort)(numVerticesPerCorner * 3 - 2));

        indices.Add((ushort)(numVerticesPerCorner * 2));
        indices.Add((ushort)(numVerticesPerCorner * 2 + 1));
        indices.Add((ushort)(numVerticesPerCorner * 4 - 1));
        indices.Add((ushort)(numVerticesPerCorner * 2));
        indices.Add((ushort)(numVerticesPerCorner * 4 - 1));
        indices.Add((ushort)(numVerticesPerCorner * 4 - 2));

        indices.Add((ushort)(numVerticesPerCorner - 1));
        indices.Add((ushort)(numVerticesPerCorner * 3));
        indices.Add((ushort)(numVerticesPerCorner * 3 + 1));
        indices.Add((ushort)(numVerticesPerCorner - 1));
        indices.Add((ushort)(numVerticesPerCorner - 2));
        indices.Add((ushort)(numVerticesPerCorner * 3));
    }

    private void CalculateCornerMesh(
        Vector3 offset,
        Color innerColor,
        Color outerColor,
        TempList<Vertex> vertices,
        TempList<ushort> indices,
        float innerRadius,
        float outerRadius,
        int numberOfSegments,
        float startAngle,
        float arc)
    {
        var segmentStep = 1f / numberOfSegments;
        var startVertCount = (ushort)vertices.Count;

        for (int s = 0; s < numberOfSegments + 1; s++)
        {
            var x = startAngle + segmentStep * s * arc;
            var sinSegment = Mathf.Sin(x);
            var cosSegment = Mathf.Cos(x);

            vertices.Add(new Vertex
            {
                position = new Vector3(cosSegment * innerRadius, -sinSegment * innerRadius, Vertex.nearZ) + offset,
                tint = innerColor,
            });

            vertices.Add(new Vertex
            {
                position = new Vector3(cosSegment * outerRadius, -sinSegment * outerRadius, Vertex.nearZ) + offset,
                tint = outerColor,
            });
        }

        for (ushort s = 0; s < numberOfSegments; s++)
        {
            var v0 = (ushort)(2 * s + startVertCount);
            var v1 = (ushort)(v0 + 2);

            indices.Add((ushort)(v1 + 0));
            indices.Add((ushort)(v0 + 1));
            indices.Add((ushort)(v0 + 0));

            indices.Add((ushort)(v0 + 1));
            indices.Add((ushort)(v1 + 0));
            indices.Add((ushort)(v1 + 1));
        }
    }

    #endregion

    #region Style resolution

    private void OnCustomStyleResolved(CustomStyleResolvedEvent evt)
    {
        bool dirty = false;

        if (evt.customStyle.TryGetValue(_glowColorStyleProperty, out Color customGlowColor))
        {
            if (_glowColorStyled != customGlowColor)
            {
                _glowColorStyled = customGlowColor;
                dirty = true;
            }
        }
        else if (_glowColorStyled.HasValue)
        {
            _glowColorStyled = null;
            dirty = true;
        }

        if (evt.customStyle.TryGetValue(_glowSizeStyleProperty, out float customGlowSize))
        {
            if (_glowSizeStyled != customGlowSize)
            {
                _glowSizeStyled = customGlowSize;
                dirty = true;
            }
        }
        else if (_glowSizeStyled.HasValue)
        {
            _glowSizeStyled = null;
            dirty = true;
        }

        if (evt.customStyle.TryGetValue(_glowOffsetXStyleProperty, out float customGlowOffsetX))
        {
            if (!Mathf.Approximately(_glowOffsetX, customGlowOffsetX))
            {
                _glowOffsetX = customGlowOffsetX;
                dirty = true;
            }
        }
        else if (!Mathf.Approximately(_glowOffsetX, 0f))
        {
            _glowOffsetX = 0f;
            dirty = true;
        }

        if (evt.customStyle.TryGetValue(_glowOffsetYStyleProperty, out float customGlowOffsetY))
        {
            if (!Mathf.Approximately(_glowOffsetY, customGlowOffsetY))
            {
                _glowOffsetY = customGlowOffsetY;
                dirty = true;
            }
        }
        else if (!Mathf.Approximately(_glowOffsetY, 0f))
        {
            _glowOffsetY = 0f;
            dirty = true;
        }

        if (evt.customStyle.TryGetValue(_glowBackgroundColorStyleProperty, out Color customGlowBackgroundColor))
        {
            if (_backgroundColor != customGlowBackgroundColor)
            {
                _backgroundColor = customGlowBackgroundColor;
                dirty = true;
            }
        }
        else if (_backgroundColor != Color.clear)
        {
            _backgroundColor = Color.clear;
            dirty = true;
        }

        if (evt.customStyle.TryGetValue(_glowBorderRadiusStyleProperty, out float customBorderRadius))
        {
            if (!Mathf.Approximately(_glowBorderRadius, customBorderRadius))
            {
                _glowBorderRadius = customBorderRadius;
                dirty = true;
            }
        }
        else if (!Mathf.Approximately(_glowBorderRadius, 0f))
        {
            _glowBorderRadius = 0f;
            dirty = true;
        }

        if (dirty)
            MarkDirtyRepaint();
    }

    #endregion

    #region Nested types

    private class TempList<T> : IDisposable where T : struct
    {
        public int Count => _count;

        private NativeArray<T> _array;
        private int _count;

        public TempList(int capacity)
        {
            _array = new NativeArray<T>(capacity, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(T value)
        {
            _array[_count++] = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NativeSlice<T> ToSlice()
        {
            return _array.Slice(0, _count);
        }

        public void Dispose()
        {
            if (_array.IsCreated)
                _array.Dispose();
        }
    }

    #endregion
}