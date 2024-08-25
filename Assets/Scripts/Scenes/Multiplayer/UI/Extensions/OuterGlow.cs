// Created 2024 StagPoint. Released to the public domain.

using System;
using System.Runtime.CompilerServices;

using Unity.Collections;

using UnityEngine;
using UnityEngine.Scripting;
using UnityEngine.UIElements;


	public class OuterGlow : VisualElement
	{
		#region Public properties
		
		public float OffsetX {
			get => _glowOffsetX;
			set => _glowOffsetX = value;
		}
		
		public float OffsetY {
			get => _glowOffsetY;
			set => _glowOffsetY = value;
		}

		public Color BackgroundColor {
			get => _backgroundColor;
			set => _backgroundColor = value;
		}

		public float BorderRadius {
			get => _glowBorderRadius;
			set => _glowBorderRadius = value;
		}

		public float GlowSize
		{
			get => _glowSizeInline ?? _glowSizeStyled ?? 10;
			set => _glowSizeInline = value;
		}

		public Color GlowColor
		{
			get => _glowColorInline ?? _glowColorStyled ?? Color.clear;
			set => _glowColorInline = value;
		}

		public int NumberOfSegments { get; set; } = 128;

		#endregion

		#region Private fields

		private CustomStyleProperty<Color> _glowColorStyleProperty = new CustomStyleProperty<Color>( "--glow-color" );
		private CustomStyleProperty<float> _glowSizeStyleProperty  = new CustomStyleProperty<float>( "--glow-size" );
		private CustomStyleProperty<float> _glowOffsetXStyleProperty  = new CustomStyleProperty<float>( "--glow-offset-x" );
		private CustomStyleProperty<float> _glowOffsetYStyleProperty  = new CustomStyleProperty<float>( "--glow-offset-y" );
		private CustomStyleProperty<Color> _glowBackgroundColorStyleProperty  = new CustomStyleProperty<Color>( "--glow-background-color" );
		private CustomStyleProperty<float> _glowBorderRadiusStyleProperty  = new CustomStyleProperty<float>( "--glow-border-radius" );

		private Color? _glowColorInline = null;
		private float? _glowSizeInline  = null;
		private Color? _glowColorStyled = null;
		private float? _glowSizeStyled  = null;
		private float _glowBorderRadius = 0f;
		private float _glowOffsetX = 0f;
		private float _glowOffsetY = 0f;
		private Color _backgroundColor = Color.clear;

		#endregion

		#region UXML support

		[Preserve]
		public new class UxmlFactory : UxmlFactory<OuterGlow, UxmlTraits>
		{
		}

		[Preserve]
		public new class UxmlTraits : VisualElement.UxmlTraits
		{
			private UxmlColorAttributeDescription glowColorAttribute              = new UxmlColorAttributeDescription { name = "glow-color", defaultValue      = Color.white };
			private UxmlFloatAttributeDescription glowSizeAttribute               = new UxmlFloatAttributeDescription { name = "glow-size", defaultValue       = 10f };
			private UxmlIntAttributeDescription   numberOfCornerSegmentsAttribute = new UxmlIntAttributeDescription { name   = "corner-segments", defaultValue = 12 };

			public override void Init( VisualElement ve, IUxmlAttributes bag, CreationContext cc )
			{
				base.Init( ve, bag, cc );
				var element = (OuterGlow)ve;

				var inlineGlowColor = Color.black;
				if( glowColorAttribute.TryGetValueFromBag( bag, cc, ref inlineGlowColor ) )
				{
					element.GlowColor = inlineGlowColor;
				}

				var inlineGlowSize = 10f;
				if( glowSizeAttribute.TryGetValueFromBag( bag, cc, ref inlineGlowSize ) )
				{
					element.GlowSize = inlineGlowSize;
				}

				element.NumberOfSegments = Mathf.Clamp( numberOfCornerSegmentsAttribute.GetValueFromBag( bag, cc ), 4, 24 );
			}
		}

		#endregion

		#region Constructor

		public OuterGlow()
		{
			generateVisualContent += OnGenerateVisualContent;
			RegisterCallback<CustomStyleResolvedEvent>( OnCustomStyleResolved );
		}

		#endregion

		#region Mesh generation

		private void OnGenerateVisualContent( MeshGenerationContext ctx )
		{
			Rect r = contentRect;
			var painter = ctx.painter2D;
			
			// painter.fillColor = BackgroundColor;
			// painter.fillColor = BackgroundColor;
			// painter.BeginPath();
			// painter.MoveTo(new Vector2(r.x + OffsetX, r.y + OffsetY));
			// painter.LineTo(new Vector2(r.x + r.width + OffsetX, r.y + OffsetY));
			// painter.LineTo(new Vector2(r.x + r.width + OffsetX, r.y + r.height + OffsetY));
			// painter.LineTo(new Vector2(r.x + OffsetX, r.y + r.height + OffsetY));
			// painter.ClosePath();
			// painter.Fill();
			
			// Draw the background fill first using Painter2D
			painter.fillColor = BackgroundColor;
			painter.BeginPath();

			// Move to the top-left corner
			painter.MoveTo(new Vector2(r.x + BorderRadius + OffsetX, r.y + OffsetY));

			// Top edge and top-right corner
			painter.LineTo(new Vector2(r.x + r.width - BorderRadius + OffsetX, r.y + OffsetY));
			painter.ArcTo(
				new Vector2(r.x + r.width + OffsetX, r.y + OffsetY),
				new Vector2(r.x + r.width + OffsetX, r.y + BorderRadius + OffsetY),
				BorderRadius
			);

			// Right edge and bottom-right corner
			painter.LineTo(new Vector2(r.x + r.width + OffsetX, r.y + r.height - BorderRadius + OffsetY));
			painter.ArcTo(
				new Vector2(r.x + r.width + OffsetX, r.y + r.height + OffsetY),
				new Vector2(r.x + r.width - BorderRadius + OffsetX, r.y + r.height + OffsetY),
				BorderRadius
			);

			// Bottom edge and bottom-left corner
			painter.LineTo(new Vector2(r.x + BorderRadius + OffsetX, r.y + r.height + OffsetY));
			painter.ArcTo(
				new Vector2(r.x + OffsetX, r.y + r.height + OffsetY),
				new Vector2(r.x + OffsetX, r.y + r.height - BorderRadius + OffsetY),
				BorderRadius
			);

			// Left edge and top-left corner
			painter.LineTo(new Vector2(r.x + OffsetX, r.y + BorderRadius + OffsetY));
			painter.ArcTo(
				new Vector2(r.x + OffsetX, r.y + OffsetY),
				new Vector2(r.x + BorderRadius + OffsetX, r.y + OffsetY),
				BorderRadius
			);

			painter.ClosePath();
			painter.Fill();
			
			calculateGlowMesh( out TempList<Vertex> verts, out TempList<ushort> indices );

			MeshWriteData mwd = ctx.Allocate( verts.Count, indices.Count );
			mwd.SetAllVertices( verts.ToSlice() );
			mwd.SetAllIndices( indices.ToSlice() );
			
			verts.Dispose();
			indices.Dispose();
		}

		private void calculateGlowMesh(out TempList<Vertex> verts, out TempList<ushort> indices)
{
    var numberOfSegments = Mathf.Clamp(NumberOfSegments, 4, 128); // Adjusted the max segments
    var numVerticesPerCorner = (numberOfSegments + 1) * 2;
    var numIndicesPerCorner = numberOfSegments * 6;
    var expectedTotalVertices = numVerticesPerCorner * 4;
    var expectedTotalIndices = numIndicesPerCorner * 4 + 24; // Additional 24 for each joining segment (6 indices per four segments)

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

    // Apply the offsets to the corners
    topLeft += new Vector3(OffsetX, OffsetY, 0);
    topRight += new Vector3(OffsetX, OffsetY, 0);
    bottomRight += new Vector3(OffsetX, OffsetY, 0);
    bottomLeft += new Vector3(OffsetX, OffsetY, 0);

    var outerRadiusTopLeft = innerRadiusTopLeft + thickness;
    var outerRadiusTopRight = innerRadiusTopRight + thickness;
    var outerRadiusBottomRight = innerRadiusBottomRight + thickness;
    var outerRadiusBottomLeft = innerRadiusBottomLeft + thickness;

    var innerColor = GlowColor;
    var outerColor = new Color(innerColor.r, innerColor.g, innerColor.b, 0.01f); // Slightly increase the alpha for smoother fade

    calculateCornerMesh(topLeft, innerColor, outerColor, verts, indices, innerRadiusTopLeft, outerRadiusTopLeft, numberOfSegments, 90 * Mathf.Deg2Rad, 90 * Mathf.Deg2Rad);
    calculateCornerMesh(topRight, innerColor, outerColor, verts, indices, innerRadiusTopRight, outerRadiusTopRight, numberOfSegments, 0, 90 * Mathf.Deg2Rad);
    calculateCornerMesh(bottomRight, innerColor, outerColor, verts, indices, innerRadiusBottomRight, outerRadiusBottomRight, numberOfSegments, 270 * Mathf.Deg2Rad, 90 * Mathf.Deg2Rad);
    calculateCornerMesh(bottomLeft, innerColor, outerColor, verts, indices, innerRadiusBottomLeft, outerRadiusBottomLeft, numberOfSegments, 180 * Mathf.Deg2Rad, 90 * Mathf.Deg2Rad);

    // Connect the corners as before, no changes needed here
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


		private void calculateCornerMesh( Vector3 offset, Color innerColor, Color outerColor, TempList<Vertex> vertices, TempList<ushort> indices, float innerRadius, float outerRadius, int numberOfSegments, float startAngle, float arc )
		{
			var segmentStep    = 1f / numberOfSegments;
			var startVertCount = (ushort)vertices.Count;

			for( int s = 0; s < numberOfSegments + 1; s++ )
			{
				var x          = startAngle + segmentStep * s * arc;
				var sinSegment = Mathf.Sin( x );
				var cosSegment = Mathf.Cos( x );

				vertices.Add( new Vertex
				{
					position = new Vector3( cosSegment * innerRadius, -sinSegment * innerRadius, Vertex.nearZ ) + offset,
					tint     = innerColor,
				} );

				vertices.Add( new Vertex()
				{
					position = new Vector3( cosSegment * outerRadius, -sinSegment * outerRadius, Vertex.nearZ ) + offset,
					tint     = outerColor,
				} );
			}

			for( ushort s = 0; s < numberOfSegments; s++ )
			{
				var v0 = 2 * s + startVertCount;
				var v1 = v0 + 2;

				indices.Add( (ushort)(v1 + 0) );
				indices.Add( (ushort)(v0 + 1) );
				indices.Add( (ushort)(v0 + 0) );

				indices.Add( (ushort)(v0 + 1) );
				indices.Add( (ushort)(v1 + 0) );
				indices.Add( (ushort)(v1 + 1) );
			}
		}

		#endregion

		#region Style resolution

		private void OnCustomStyleResolved( CustomStyleResolvedEvent evt )
		{
			if( evt.customStyle.TryGetValue( _glowColorStyleProperty, out Color customGlowColor ) )
			{
				_glowColorStyled = customGlowColor;
				MarkDirtyRepaint();
			}
			else if( _glowColorStyled.HasValue )
			{
				_glowColorStyled = null;
				MarkDirtyRepaint();
			}

			if( evt.customStyle.TryGetValue( _glowSizeStyleProperty, out float customGlowSize ) )
			{
				_glowSizeStyled = customGlowSize;
				MarkDirtyRepaint();
			}
			else if( _glowSizeStyled.HasValue )
			{
				_glowSizeStyled = null;
				MarkDirtyRepaint();
			}
			
			if( evt.customStyle.TryGetValue( _glowOffsetXStyleProperty, out float customGlowOffsetX ) )
			{
				_glowOffsetX = customGlowOffsetX;
				MarkDirtyRepaint();
			}
			else
			{
				_glowOffsetX = 0f;
				MarkDirtyRepaint();
			}
			
			if( evt.customStyle.TryGetValue( _glowOffsetYStyleProperty, out float customGlowOffsetY ) )
			{
				_glowOffsetY = customGlowOffsetY;
				MarkDirtyRepaint();
			}
			else
			{
				_glowOffsetY = 0f;
				MarkDirtyRepaint();
			}
			
			if( evt.customStyle.TryGetValue( _glowBackgroundColorStyleProperty, out Color customGlowBackgroundColor ) )
			{
				_backgroundColor = customGlowBackgroundColor;
				MarkDirtyRepaint();
			}
			else {
				_backgroundColor = Color.clear;
				MarkDirtyRepaint();
			}
			
			if( evt.customStyle.TryGetValue( _glowBorderRadiusStyleProperty, out float customBorderRadius ) )
			{
				_glowBorderRadius = customBorderRadius;
				MarkDirtyRepaint();
			}
			else {
				_glowBorderRadius = 0f;
				MarkDirtyRepaint();
			}
		}

		#endregion
		
		#region Nested types

		private class TempList<T> : IDisposable where T : struct
		{
			#region Public properties
			
			public int Count { get => _count; }

			#endregion

			#region Private fields

			private NativeArray<T> _array;
			private int            _count;

			#endregion
			
			#region Constructor
			
			public TempList( int capacity )
			{
				_array = new NativeArray<T>( capacity, Allocator.Temp, NativeArrayOptions.UninitializedMemory );
			}
			
			#endregion 
			
			#region Public functions

			[MethodImpl( MethodImplOptions.AggressiveInlining )]
			public void Add( T value )
			{
				_array[ _count++ ] = value;
			}

			[MethodImpl( MethodImplOptions.AggressiveInlining )]
			public NativeSlice<T> ToSlice()
			{
				return _array.Slice();
			}

			public void Dispose()
			{
				_array.Dispose();
			}
			
			#endregion 
		}
		
		#endregion 
	}

