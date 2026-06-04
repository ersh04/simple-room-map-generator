public class Converter3D
{
    #region STL
    public static bool ConvertToStl(int[,] map, string outputPath)
    {
        try
        {
            using (var w = new StreamWriter(outputPath))
            {
                w.WriteLine("solid maze");

                int width = map.GetLength(0);
                int height = map.GetLength(1);
                float wallHeight = 1.0f;

                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        if (map[x, y] == 1)
                        {
                            // Front face
                            WriteTriangle(w, 0, 0, -1, x, y, 0, x + 1, y, 0, x + 1, y + 1, 0);
                            WriteTriangle(w, 0, 0, -1, x, y, 0, x + 1, y + 1, 0, x, y + 1, 0);

                            // Back face
                            WriteTriangle(w, 0, 0, 1, x + 1, y + 1, wallHeight, x + 1, y, wallHeight, x, y, wallHeight);
                            WriteTriangle(w, 0, 0, 1, x + 1, y + 1, wallHeight, x, y, wallHeight, x, y + 1, wallHeight);

                            // Left face
                            WriteTriangle(w, -1, 0, 0, x, y + 1, wallHeight, x, y + 1, 0, x, y, 0);
                            WriteTriangle(w, -1, 0, 0, x, y + 1, wallHeight, x, y, 0, x, y, wallHeight);

                            // Right face
                            WriteTriangle(w, 1, 0, 0, x + 1, y, 0, x + 1, y + 1, 0, x + 1, y + 1, wallHeight);
                            WriteTriangle(w, 1, 0, 0, x + 1, y, 0, x + 1, y + 1, wallHeight, x + 1, y, wallHeight);
                        }
                    }
                }
                w.WriteLine("endsolid maze");
                return true;
            }
        }
        catch
        {
            return false;
        }
    }
    
    /// <summary>
    /// Writes a single triangle to the STL file with the given normal and vertex coordinates.
    /// </summary>
    /// <param name="w">The StreamWriter to write to.</param>
    /// <param name="nx">The x component of the normal vector.</param>
    /// <param name="ny">The y component of the normal vector.</param>
    /// <param name="nz">The z component of the normal vector.</param>
    /// <param name="v1x">The x coordinate of the first vertex.</param>
    /// <param name="v1y">The y coordinate of the first vertex.</param>
    /// <param name="v1z">The z coordinate of the first vertex.</param>
    /// <param name="v2x">The x coordinate of the second vertex.</param>
    /// <param name="v2y">The y coordinate of the second vertex.</param>
    /// <param name="v2z">The z coordinate of the second vertex.</param>
    /// <param name="v3x">The x coordinate of the third vertex.</param>
    /// <param name="v3y">The y coordinate of the third vertex.</param>
    /// <param name="v3z">The z coordinate of the third vertex.</param>
    static void WriteTriangle(StreamWriter w, float nx, float ny, float nz, 
                              float v1x, float v1y, float v1z, 
                              float v2x, float v2y, float v2z, 
                              float v3x, float v3y, float v3z)
    {
        w.WriteLine($"  facet normal {nx} {ny} {nz}");
        w.WriteLine("    outer loop");
        w.WriteLine($"      vertex {v1x} {v1y} {v1z}");
        w.WriteLine($"      vertex {v2x} {v2y} {v2z}");
        w.WriteLine($"      vertex {v3x} {v3y} {v3z}");
        w.WriteLine("    endloop");
        w.WriteLine("  endfacet");
    }

    #endregion

    #region OBJ

    public static bool ConvertToObj(int[,] map, string outputPath)
    {
        try
        {
            using (var w = new StreamWriter(outputPath))
            {
                int width = map.GetLength(0);
                int height = map.GetLength(1);
                float wallHeight = 1.0f;
                var vertexIndices = new Dictionary<(float x, float y, float z), int>();
                var faces = new List<(int a, int b, int c, int d)>();

                w.WriteLine("# maze");
                w.WriteLine("o maze");

                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        if (map[x, y] != 1)
                        {
                            continue;
                        }

                        int v000 = AddObjVertex(w, vertexIndices, x, y, 0);
                        int v100 = AddObjVertex(w, vertexIndices, x + 1, y, 0);
                        int v110 = AddObjVertex(w, vertexIndices, x + 1, y + 1, 0);
                        int v010 = AddObjVertex(w, vertexIndices, x, y + 1, 0);
                        int v001 = AddObjVertex(w, vertexIndices, x, y, wallHeight);
                        int v101 = AddObjVertex(w, vertexIndices, x + 1, y, wallHeight);
                        int v111 = AddObjVertex(w, vertexIndices, x + 1, y + 1, wallHeight);
                        int v011 = AddObjVertex(w, vertexIndices, x, y + 1, wallHeight);

                        // Top and bottom caps.
                        faces.Add((v001, v101, v111, v011));
                        faces.Add((v010, v110, v100, v000));

                        // Emit side faces only when there is no adjacent wall.
                        if (x == 0 || map[x - 1, y] == 0)
                        {
                            faces.Add((v000, v001, v011, v010));
                        }

                        if (x == width - 1 || map[x + 1, y] == 0)
                        {
                            faces.Add((v100, v110, v111, v101));
                        }

                        if (y == 0 || map[x, y - 1] == 0)
                        {
                            faces.Add((v000, v100, v101, v001));
                        }

                        if (y == height - 1 || map[x, y + 1] == 0)
                        {
                            faces.Add((v010, v011, v111, v110));
                        }
                    }
                }

                foreach (var face in faces)
                {
                    w.WriteLine($"f {face.a} {face.b} {face.c} {face.d}");
                }

                return true;
            }
        }
        catch
        {
            return false;
        }
    }

    private static int AddObjVertex(StreamWriter w, Dictionary<(float x, float y, float z), int> vertexIndices, float x, float y, float z)
    {
        var key = (x, y, z);
        if (vertexIndices.TryGetValue(key, out int existingIndex))
        {
            return existingIndex;
        }

        int index = vertexIndices.Count + 1;
        vertexIndices[key] = index;
        w.WriteLine($"v {x} {y} {z}");
        return index;
    }
    
    #endregion
}