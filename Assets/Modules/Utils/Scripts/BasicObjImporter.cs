// ------------------------------------------------------------------------------------
// <copyright file="BasicObjImporter.cs" company="Technische Universität Dresden">
//      Copyright (c) Technische Universität Dresden.
//      Licensed under the MIT License.
// </copyright>
// <author>
//      Wolfgang Büschel
// </author>
// ------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;

public class BasicObjImporter
{
    /// <summary>
    /// Imports a mesh from the given obj file.
    /// </summary>
    /// <param name="filename">The path to the obj file</param>
    /// <returns>The imported mesh</returns>
    public static Mesh ImportFromFile(string filename)
    {
        try
        {
            string[] content = File.ReadAllLines(filename);

            List<Vector3> vertexList = new List<Vector3>();
            List<Vector3> normalList = new List<Vector3>();
            List<Vector2> uvList = new List<Vector2>();
            List<Tuple<int, int, int>> faceList = new List<Tuple<int, int, int>>();
            List<int> triangleList = new List<int>();

            for (int i = 0; i < content.Length; i++)
            {
                string line = content[i];
                if (line.StartsWith("vn"))
                {
                    // add new entry to list of normals
                    normalList.Add(ParseNormal(line));
                }
                else if (line.StartsWith("vt"))
                {
                    // add new entry to list of UVs
                    uvList.Add(ParseUV(line));
                }
                else if (line.StartsWith("v"))
                {
                    // add new entry to list of vertices
                    vertexList.Add(ParseVertex(line));
                }
                else if (line.StartsWith("f"))
                {
                    // reads a face, consisting of one or more triangles

                    line = line.Substring(1);
                    var tuples = line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                    int triCounter = -2; // In each face, there is one triangle for each vertex above two, e.g., four vertices -> two triangles.
                    List<int> idxList = new List<int>();

                    for (int j = 0; j < tuples.Length; j++)
                    {
                        var tuple = tuples[j].Split('/');

                        // Unfortunately, there are some alternative ways how the faces can be structured.
                        if (tuple.Length == 1)
                        {
                            int v = int.Parse(tuple[0]);
                            faceList.Add(new Tuple<int, int, int>(v, 0, 0));
                        }
                        else if (tuple.Length == 2)
                        {
                            int v = int.Parse(tuple[0]);
                            int vt = int.Parse(tuple[1]);
                            faceList.Add(new Tuple<int, int, int>(v, vt, 0));
                        }
                        else if (tuple.Length == 3)
                        {
                            int v = int.Parse(tuple[0]);
                            int vn = int.Parse(tuple[2]);
                            int vt;
                            if (tuple[1] == string.Empty)
                            {
                                vt = 0;
                            }
                            else
                            {
                                vt = int.Parse(tuple[1]);
                            }
                            faceList.Add(new Tuple<int, int, int>(v, vt, vn));
                        }
                        triCounter++;
                        idxList.Add(faceList.Count - 1);
                    }

                    // generate triangles (or rather, their indices) as a triangle fan.
                    for (int counter = 0; counter < triCounter; counter++) 
                    {
                        triangleList.Add(idxList[0]);
                        triangleList.Add(idxList[counter + 1]);
                        triangleList.Add(idxList[counter + 2]);
                    }
                }
            }

            Vector3[] vertexArray = new Vector3[faceList.Count];
            Vector2[] uvArray = new Vector2[faceList.Count];
            Vector3[] normalArray = new Vector3[faceList.Count];

            // generate the complete vertex, uv and normal arrays
            for (int i = 0; i < faceList.Count; i++)
            {
                vertexArray[i] = vertexList[faceList[i].Item1 - 1];
                if (faceList[i].Item2 >= 1)
                {
                    uvArray[i] = uvList[faceList[i].Item2 - 1];
                }

                if (faceList[i].Item3 >= 1)
                {
                    normalArray[i] = normalList[faceList[i].Item3 - 1];
                }
            }

            // create the mesh and assign all the stuff we just computed
            Mesh mesh = new Mesh();
            mesh.vertices = vertexArray;
            mesh.uv = uvArray;
            mesh.normals = normalArray;
            mesh.triangles = triangleList.ToArray();
            mesh.RecalculateBounds();

            return mesh;

        }
        catch (Exception e)
        {
            Debug.LogError("Error parsing OBJ from file: " + filename);
            return null;
        }
    }

    private static Vector3 ParseVertex(string line)
    {
        line = line.Substring(1);
        var items = line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        if (items.Length != 3) throw new Exception();
        float x = float.Parse(items[0], NumberStyles.Any, CultureInfo.InvariantCulture.NumberFormat);
        float y = float.Parse(items[1], NumberStyles.Any, CultureInfo.InvariantCulture.NumberFormat);
        float z = float.Parse(items[2], NumberStyles.Any, CultureInfo.InvariantCulture.NumberFormat);
        return new Vector3(x, y, z);
    }

    private static Vector3 ParseNormal(string line)
    {
        line = line.Substring(2);
        var items = line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        if (items.Length != 3) throw new Exception();
        float x = float.Parse(items[0], NumberStyles.Any, CultureInfo.InvariantCulture.NumberFormat);
        float y = float.Parse(items[1], NumberStyles.Any, CultureInfo.InvariantCulture.NumberFormat);
        float z = float.Parse(items[2], NumberStyles.Any, CultureInfo.InvariantCulture.NumberFormat);
        return new Vector3(x, y, z);
    }
    private static Vector2 ParseUV(string line)
    {
        line = line.Substring(2);
        var items = line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        if (items.Length != 2) throw new Exception();
        float u = float.Parse(items[0], NumberStyles.Any, CultureInfo.InvariantCulture.NumberFormat);
        float v = float.Parse(items[1], NumberStyles.Any, CultureInfo.InvariantCulture.NumberFormat);
        return new Vector2(u, v);
    }

}
