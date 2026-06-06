public class Converter3D
{
    private const float WallHeight = 3f;
    
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
                            WriteTriangle(w, 0, 0, 1, x + 1, y + 1, WallHeight, x + 1, y, WallHeight, x, y, WallHeight);
                            WriteTriangle(w, 0, 0, 1, x + 1, y + 1, WallHeight, x, y, WallHeight, x, y + 1, WallHeight);

                            // Left face
                            WriteTriangle(w, -1, 0, 0, x, y + 1, WallHeight, x, y + 1, 0, x, y, 0);
                            WriteTriangle(w, -1, 0, 0, x, y + 1, WallHeight, x, y, 0, x, y, WallHeight);

                            // Right face
                            WriteTriangle(w, 1, 0, 0, x + 1, y, 0, x + 1, y + 1, 0, x + 1, y + 1, WallHeight);
                            WriteTriangle(w, 1, 0, 0, x + 1, y, 0, x + 1, y + 1, WallHeight, x + 1, y, WallHeight);
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
    
    private readonly record struct WallBlock(int X, int Y, int Width, int Height);

    private static List<WallBlock> MergeWalls(int[,] map)
    {
        int width = map.GetLength(0);
        int height = map.GetLength(1);

        bool[,] visited = new bool[width, height];
        var mergedWalls = new List<WallBlock>();

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (map[x, y] != 1 || visited[x, y])
                {
                    continue;
                }

                int blockHeight = 1;
                while (y + blockHeight < height && map[x, y + blockHeight] == 1 && !visited[x, y + blockHeight])
                {
                    blockHeight++;
                }

                int blockWidth = 1;
                while (x + blockWidth < width)
                {
                    bool canExtend = true;
                    for (int dy = 0; dy < blockHeight; dy++)
                    {
                        if (map[x + blockWidth, y + dy] != 1 || visited[x + blockWidth, y + dy])
                        {
                            canExtend = false;
                            break;
                        }
                    }

                    if (!canExtend)
                    {
                        break;
                    }

                    blockWidth++;
                }

                for (int dx = 0; dx < blockWidth; dx++)
                {
                    for (int dy = 0; dy < blockHeight; dy++)
                    {
                        visited[x + dx, y + dy] = true;
                    }
                }

                mergedWalls.Add(new WallBlock(x, y, blockWidth, blockHeight));
            }
        }

        return mergedWalls;
    }

    public static bool ConvertToObj(int[,] map, string outputPath)
    {
        try
        {
            using (var w = new StreamWriter(outputPath))
            {
                var mergedWalls = MergeWalls(map);
                int width = map.GetLength(0);
                int height = map.GetLength(1);
                var vertexIndices = new Dictionary<(float x, float y, float z), int>();
                var faces = new List<(int a, int b, int c, int d)>();

                w.WriteLine("# maze");
                w.WriteLine("o maze");

                foreach (var block in mergedWalls)
                {
                    int x0 = block.X;
                    int y0 = block.Y;
                    int x1 = block.X + block.Width;
                    int y1 = block.Y + block.Height;

                    int v000 = AddObjVertex(w, vertexIndices, x0, 0, y0);
                    int v100 = AddObjVertex(w, vertexIndices, x1, 0, y0);
                    int v110 = AddObjVertex(w, vertexIndices, x1, 0, y1);
                    int v010 = AddObjVertex(w, vertexIndices, x0, 0, y1);
                    int v001 = AddObjVertex(w, vertexIndices, x0, WallHeight, y0);
                    int v101 = AddObjVertex(w, vertexIndices, x1, WallHeight, y0);
                    int v111 = AddObjVertex(w, vertexIndices, x1, WallHeight, y1);
                    int v011 = AddObjVertex(w, vertexIndices, x0, WallHeight, y1);

                    // Bottom and top caps.
                    faces.Add((v010, v110, v100, v000));
                    faces.Add((v001, v101, v111, v011));

                    // Four outer side faces of the merged block.
                    faces.Add((v000, v001, v011, v010));
                    faces.Add((v100, v110, v111, v101));
                    faces.Add((v000, v100, v101, v001));
                    faces.Add((v010, v011, v111, v110));
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