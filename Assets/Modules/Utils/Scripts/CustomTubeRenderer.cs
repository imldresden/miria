// ------------------------------------------------------------------------------------
// <copyright file="CustomTubeRenderer.cs">
//      Copyright (c) Mathias Soeholm, Technische Universität Dresden
//		Licensed under the MIT License.
// </copyright>
// <author>
//      Mathias Soeholm, modifications by Wolfgang Büschel
// </author>
// <comment>
//		Source: https://gist.github.com/mathiassoeholm/15f3eeda606e9be543165360615c8bef
//		Original file comment:
//		Author: Mathias Soeholm
//		Date: 05/10/2016
//		No license, do whatever you want with this script
// </comment>
// ------------------------------------------------------------------------------------

using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class CustomTubeRenderer : MonoBehaviour
{
	Vector3[] positions;
	Color[] colors;
	float[] radii;

	public int Sides;
	public float startWidth;
	public float endWidth;
	public Color Color;
	
	private Vector3[] vertices;
	private Color[] vertexColors;
	private Mesh mesh;
	private MeshFilter meshFilter;
	private MeshRenderer meshRenderer;

	public Material material
	{
		get { return meshRenderer.material; }
		set { meshRenderer.material = value; }
	}

	void Awake()
	{
		meshFilter = GetComponent<MeshFilter>();
		if (meshFilter == null)
		{
			meshFilter = gameObject.AddComponent<MeshFilter>();
		}

		meshRenderer = GetComponent<MeshRenderer>();
		if (meshRenderer == null)
		{
			meshRenderer = gameObject.AddComponent<MeshRenderer>();
		}

		mesh = new Mesh();
		meshFilter.mesh = mesh;
	}

	private void OnEnable()
	{
		meshRenderer.enabled = true;
	}

	private void OnDisable()
	{
		meshRenderer.enabled = false;
	}

	private void OnValidate()
	{
		Sides = Mathf.Max(3, Sides);
	}

	public void SetPositions(List<List<Vector3>> segmentsList, List<List<Color>> colorSegmentsList = null)
	{
		bool IsUsingPerVertexColor = true;
		if (segmentsList == null || segmentsList.Count < 1)
		{
			return;
		}

		int TotalPoints = 0;		
		for (int i = 0; i < segmentsList.Count; i++)
		{
			TotalPoints += segmentsList[i].Count+2;
			if (colorSegmentsList == null || colorSegmentsList.Count != segmentsList.Count || segmentsList[i].Count != colorSegmentsList[i].Count)
			{
				IsUsingPerVertexColor = false;
			}
		}

		positions = new Vector3[TotalPoints];
		radii = new float[TotalPoints];

		int CurrentPoint = 0;
		for (int i = 0; i < segmentsList.Count; i++)
		{
			var Segment = segmentsList[i];
			if (Segment.Count < 2)
			{
				return;
			}

			Vector3 v0offset = (Segment[0] - Segment[1]) * 0.01f;
			positions[CurrentPoint] = v0offset + Segment[0];
			radii[CurrentPoint] = 0.0f;
			CurrentPoint++;

			for (int p = 0; p < Segment.Count; p++)
			{
				positions[CurrentPoint] = Segment[p];
				radii[CurrentPoint] = Mathf.Lerp(startWidth, endWidth, (float)CurrentPoint / TotalPoints);
				CurrentPoint++;
			}

			Vector3 v1offset = (Segment[Segment.Count - 1] - Segment[Segment.Count - 2]) * 0.01f;
			positions[CurrentPoint] = v1offset + Segment[Segment.Count - 1];
			radii[CurrentPoint] = 0.0f;
			CurrentPoint++;
		}

		colors = new Color[TotalPoints];
		CurrentPoint = 0;
		if (IsUsingPerVertexColor)
		{
			for (int i = 0; i < segmentsList.Count; i++)
			{
				var ColorList = colorSegmentsList[i];
				if (ColorList.Count < 2)
				{
					return;
				}

                colors[CurrentPoint] = ColorList[0];
				CurrentPoint++;

				for (int p = 0; p < ColorList.Count; p++)
				{
					colors[CurrentPoint] = ColorList[p];
					CurrentPoint++;
				}

				colors[CurrentPoint] = ColorList[ColorList.Count - 1];
				CurrentPoint++;
			}
		}
		else
		{
			for(int i = 0; i < TotalPoints; i++)
            {
				colors[i] = Color;
            }
		}
		GenerateMesh();
	}

	private void GenerateMesh()
	{
		if (mesh == null || positions == null || positions.Length <= 1)
		{
			mesh = new Mesh();
			return;
		}

		var verticesLength = Sides*positions.Length;
		if (vertices == null || vertices.Length != verticesLength)
		{
			vertices = new Vector3[verticesLength];
			vertexColors = new Color[verticesLength];

			var indices = GenerateIndices();
			var uvs = GenerateUVs();

			if (verticesLength > mesh.vertexCount)
			{
				mesh.vertices = vertices;
				mesh.triangles = indices;
				mesh.uv = uvs;
			}
			else
			{
				mesh.triangles = indices;
				mesh.vertices = vertices;
				mesh.uv = uvs;
			}
		}

		var currentVertIndex = 0;

		for (int i = 0; i < positions.Length; i++)
		{
			var circle = CalculateCircle(i);
			foreach (var vertex in circle)
			{
				vertices[currentVertIndex] = vertex;
				vertexColors[currentVertIndex] = colors[i];
				currentVertIndex++;
			}
		}

		mesh.vertices = vertices;
		mesh.colors = vertexColors;
		mesh.RecalculateNormals();
		mesh.RecalculateBounds();

		meshFilter.mesh = mesh;
	}

	private Vector2[] GenerateUVs()
	{
		var uvs = new Vector2[positions.Length*Sides];

		for (int segment = 0; segment < positions.Length; segment++)
		{
			for (int side = 0; side < Sides; side++)
			{
				var vertIndex = (segment * Sides + side);
				var u = side/(Sides-1f);
				var v = segment/(positions.Length-1f);

				uvs[vertIndex] = new Vector2(u, v);
			}
		}

		return uvs;
	}

	private int[] GenerateIndices()
	{
		// Two triangles and 3 vertices
		var indices = new int[positions.Length*Sides*2*3];

		var currentIndicesIndex = 0;
		for (int segment = 1; segment < positions.Length; segment++)
		{
			for (int side = 0; side < Sides; side++)
			{
				var vertIndex = (segment*Sides + side);
				var prevVertIndex = vertIndex - Sides;

				// Triangle one
				indices[currentIndicesIndex++] = prevVertIndex;
				indices[currentIndicesIndex++] = (side == Sides - 1) ? (vertIndex - (Sides - 1)) : (vertIndex + 1);
				indices[currentIndicesIndex++] = vertIndex;
				

				// Triangle two
				indices[currentIndicesIndex++] = (side == Sides - 1) ? (prevVertIndex - (Sides - 1)) : (prevVertIndex + 1);
				indices[currentIndicesIndex++] = (side == Sides - 1) ? (vertIndex - (Sides - 1)) : (vertIndex + 1);
				indices[currentIndicesIndex++] = prevVertIndex;
			}
		}

		return indices;
	}

	private Vector3[] CalculateCircle(int index)
	{
		var dirCount = 0;
		var forward = Vector3.zero;

		// If not first index
		if (index > 0)
		{
			forward += (positions[index] - positions[index - 1]).normalized;
			dirCount++;
		}

		// If not last index
		if (index < positions.Length-1)
		{
			forward += (positions[index + 1] - positions[index]).normalized;
			dirCount++;
		}

		// Forward is the average of the connecting edges directions
		forward = (forward/dirCount).normalized;
		var side = Vector3.Cross(forward, forward+new Vector3(.123564f, .34675f, .756892f)).normalized;
		var up = Vector3.Cross(forward, side).normalized;

		var circle = new Vector3[Sides];
		var angle = 0f;
		var angleStep = (2*Mathf.PI)/Sides;

		var t = index / (positions.Length-1f);
		var radius = radii[index];

		for (int i = 0; i < Sides; i++)
		{
			var x = Mathf.Cos(angle);
			var y = Mathf.Sin(angle);

			circle[i] = positions[index] + side*x* radius + up*y* radius;

			angle += angleStep;
		}

		return circle;
	}
}