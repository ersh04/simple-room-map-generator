public static class MapGenerator
{
    #region Constants

    private const int MapWidth = 128;
    private const int MapHeight = 128;
    private const int SlicesCount = 35;
    private const int MinDistanceBetweenSlices = 5;
    private const float MergeChance = 0.10f;

    #endregion

    #region Public API

    public static int[,] GetMap(out int worldSeed)
    {
        worldSeed = Random.Shared.Next(0, 1_000_000);
        var rng = new Random(worldSeed);
        int[,] gameMap = GenerateMap(rng);
        return gameMap;
    }

    #endregion

    #region Main logic

    private static int[,] GenerateMap(Random rng)
    {
        int[,] gameMap = new int[MapWidth, MapHeight];
        GenerateBorders(gameMap);

        int didSlices = 0;
        int attempts = 0;
        int maxAttempts = SlicesCount * 2000;

        // Ceneters of the rooms (slices) that were successfully created.
        List<MapSlice> roomCenters = new List<MapSlice>();

        while (didSlices < SlicesCount && attempts < maxAttempts)
        {
            var newRoomCenter = RandomSlice(gameMap, rng, MinDistanceBetweenSlices);
            if (newRoomCenter.XCenter != -1 && newRoomCenter.YCenter != -1)
            {
                roomCenters.Add(newRoomCenter);
                didSlices++;
            }

            attempts++;
        }

        foreach (var room in roomCenters)
        {
            room.TryRemoveWall(gameMap, rng);
        }

        EnsureAllSlicesConnected(gameMap, roomCenters, rng);

        return gameMap;
    }

    private static void GenerateBorders(int[,] gameMap)
    {
        for (int i = 0; i < MapWidth; i++)
        {
            gameMap[i, 0] = 1;
            gameMap[i, MapHeight - 1] = 1;
        }

        for (int i = 0; i < MapHeight; i++)
        {
            gameMap[0, i] = 1;
            gameMap[MapWidth - 1, i] = 1;
        }
    }

    /// <summary>
    /// Attempts to create a random slice (wall) in the map.
    /// The slice is either vertical or horizontal, and it starts from a random coordinate on the chosen axis.
    /// The method checks if the slice can be placed without violating the minimum distance requirement from existing slices.
    /// If it can be placed, it extends the slice until it hits another wall or the edge of the map.
    /// Returns true if a slice was successfully created, false otherwise.
    /// </summary>
    /// <param name="gameMap">The map in which to create the slice.</param>
    /// <param name="rng">The random number generator to use for slice placement.</param>
    /// <param name="minDistance">The minimum distance required between slices.</param>
    /// <returns>A MapSlice object representing the created slice, or a MapSlice with coordinates
    /// (-1, -1) if no slice was created.</returns>
    private static MapSlice RandomSlice(int[,] gameMap, Random rng, int minDistance)
    {
        bool vertical = rng.Next(0, 2) == 0;
        int axisSize = vertical ? MapHeight : MapWidth;
        int walkSize = vertical ? MapWidth : MapHeight;
        int coord = rng.Next(minDistance, axisSize - minDistance);
        
        for (int d = -minDistance; d <= minDistance; d++)
        {
            int check = coord + d;
            if (check >= 1 && check < axisSize - 1 && check != coord)
            {
                for (int i = 1; i < walkSize - 1; i++)
                {
                    int x = vertical ? i : check;
                    int y = vertical ? check : i;

                    if (gameMap[x, y] == 1)
                    {
                        if (vertical &&
                            (gameMap[x, y - 1] == 0 || y == 0 || gameMap[x, y + 1] == 0 || y == MapHeight - 1))
                        {
                            return new MapSlice((-1, -1), Array.Empty<(int, int)>());
                        }

                        if (!vertical &&
                            (gameMap[x - 1, y] == 0 || x == 0 || gameMap[x + 1, y] == 0 || x == MapWidth - 1))
                        {
                            return new MapSlice((-1, -1), Array.Empty<(int, int)>());
                        }
                    }
                }
            }
        }

        var possible = new List<int>();

        for (int i = 0; i < walkSize - 1; i++)
        {
            int x1 = vertical ? i : coord;
            int y1 = vertical ? coord : i;
            int x2 = vertical ? i + 1 : coord;
            int y2 = vertical ? coord : i + 1;

            if (gameMap[x1, y1] == 1 && gameMap[x2, y2] == 0)
            {
                possible.Add(i);
            }
        }

        if (possible.Count == 0)
        {
            return new MapSlice((-1, -1), Array.Empty<(int, int)>());
        }

        int cursor = possible[rng.Next(possible.Count)];
        List<(int, int)> walls = new List<(int, int)>();
        int firstCarved = -1;
        int lastCarved = -1;
        while (true)
        {
            cursor++;
            if (cursor >= walkSize)
            {
                return new MapSlice((-1, -1), Array.Empty<(int, int)>());
            }

            int x = vertical ? cursor : coord;
            int y = vertical ? coord : cursor;

            if (gameMap[x, y] == 1)
            {
                break;
            }

            gameMap[x, y] = 1;
            walls.Add((x, y));

            if (firstCarved == -1)
            {
                firstCarved = cursor;
            }

            lastCarved = cursor;
        }

        if (firstCarved == -1)
        {
            return new MapSlice((-1, -1), Array.Empty<(int, int)>());
        }

        int center = (firstCarved + lastCarved) / 2;
        return new MapSlice(vertical ? (center, coord) : (coord, center), walls.ToArray());
    }

    private static bool IsWall(int[,] gameMap, int x, int y)
    {
        if (x <= 0 || x >= MapWidth - 1 || y <= 0 || y >= MapHeight - 1)
        {
            return false;
        }

        return gameMap[x, y] == 1;
    }

    #endregion
    
    #region Slices Connect

    private static void EnsureAllSlicesConnected(int[,] gameMap, List<MapSlice> slices, Random rng)
    {
        int maxAttempts = MapWidth * MapHeight;

        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            int[,] regionMap = CreateRegionMap(gameMap, out int regionCount);
            if (regionCount <= 1)
            {
                return;
            }

            bool openedDoorway = false;
            if (slices.Count > 0)
            {
                int startIndex = rng.Next(0, slices.Count);
                for (int i = 0; i < slices.Count; i++)
                {
                    var slice = slices[(startIndex + i) % slices.Count];
                    if (slice.TryCreateDoorway(gameMap, regionMap))
                    {
                        openedDoorway = true;
                        break;
                    }
                }
            }

            if (!openedDoorway && !TryOpenAnyBridgeWall(gameMap, regionMap))
            {
                return;
            }
        }
    }

    private static int[,] CreateRegionMap(int[,] gameMap, out int regionCount)
    {
        int[,] regionMap = new int[MapWidth, MapHeight];
        regionCount = 0;

        for (int y = 1; y < MapHeight - 1; y++)
        {
            for (int x = 1; x < MapWidth - 1; x++)
            {
                if (gameMap[x, y] != 0 || regionMap[x, y] != 0)
                {
                    continue;
                }

                regionCount++;
                var queue = new Queue<(int x, int y)>();
                queue.Enqueue((x, y));
                regionMap[x, y] = regionCount;

                while (queue.Count > 0)
                {
                    var current = queue.Dequeue();
                    TryMarkRegionCell(gameMap, regionMap, current.x - 1, current.y, regionCount, queue);
                    TryMarkRegionCell(gameMap, regionMap, current.x + 1, current.y, regionCount, queue);
                    TryMarkRegionCell(gameMap, regionMap, current.x, current.y - 1, regionCount, queue);
                    TryMarkRegionCell(gameMap, regionMap, current.x, current.y + 1, regionCount, queue);
                }
            }
        }

        return regionMap;
    }

    private static void TryMarkRegionCell(int[,] gameMap, int[,] regionMap, int x, int y, int regionId,
        Queue<(int x, int y)> queue)
    {
        if (x <= 0 || x >= MapWidth - 1 || y <= 0 || y >= MapHeight - 1)
        {
            return;
        }

        if (gameMap[x, y] != 0 || regionMap[x, y] != 0)
        {
            return;
        }

        regionMap[x, y] = regionId;
        queue.Enqueue((x, y));
    }

    /// <summary>
    /// Tries to open any wall that serves as a bridge between two different regions
    /// (slices) in the map. This is a fallback method that is used when all attempts to create
    /// doorways through slices have failed. It iterates through all walls in the map and checks
    /// if any of them is a bridge between different regions.
    /// </summary>
    private static bool TryOpenAnyBridgeWall(int[,] gameMap, int[,] regionMap)
    {
        for (int y = 1; y < MapHeight - 1; y++)
        {
            for (int x = 1; x < MapWidth - 1; x++)
            {
                if (!IsWall(gameMap, x, y))
                {
                    continue;
                }

                var (isBridge, isVertical) = IsBridgeBetweenDifferentRegions(regionMap, x, y);
                if (isBridge)
                {
                    if (isVertical)
                    {
                        gameMap[x, y - 1] = 0;
                    }
                    else
                    {
                        gameMap[x - 1, y] = 0;
                    }
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Checks if the wall at the given coordinates serves as a bridge between two different regions in the map.
    /// A wall is considered a bridge if it has non-zero region IDs on both sides and the region IDs are different.
    /// </summary>
    private static (bool, bool) IsBridgeBetweenDifferentRegions(int[,] regionMap, int x, int y)
    {
        // Check left and right neighbors
        int left = regionMap[x - 1, y];
        int right = regionMap[x + 1, y];
        if (left != 0 && right != 0 && left != right)
        {
            return (true, true);
        }

        // Check up and down neighbors
        int up = regionMap[x, y - 1];
        int down = regionMap[x, y + 1];
        if (up != 0 && down != 0 && up != down)
        {
            return (true, false);
        }

        return (false, false);
    }

    #endregion

    #region MapSlice class

    private class MapSlice
    {
        public int XCenter { private set; get; }
        public int YCenter { private set; get; }

        public (int, int)[] Walls { get; private set; }
        
        public MapSlice(int xCenter, int yCenter, (int, int)[] walls)
        {
            XCenter = xCenter;
            YCenter = yCenter;
            Walls = walls;
        }

        public MapSlice((int, int) center, (int, int)[] walls)
        {
            XCenter = center.Item1;
            YCenter = center.Item2;
            Walls = walls;
        }

        public bool TryCreateDoorway(int[,] gameMap, int[,] regionMap)
        {
            foreach (var wall in Walls)
            {
                int x = wall.Item1;
                int y = wall.Item2;
                if (!IsWall(gameMap, x, y))
                {
                    continue;
                }

                var (isBridge, isVertical) = IsBridgeBetweenDifferentRegions(regionMap, x, y);
                if (isBridge)
                {
                    if (isVertical)
                    {
                        gameMap[x, YCenter] = 0;
                    }
                    else
                    {
                        gameMap[XCenter, y] = 0;
                    }
                    return true;
                }
            }

            return false;
        }

        public bool TryRemoveWall(int[,] gameMap, Random rng)
        {
            bool shouldRemoveWall = rng.NextDouble() < MergeChance;

            if (!shouldRemoveWall)
            {
                return false;
            }

            foreach (var wall in Walls)
            {
                if (IsWall(gameMap, wall.Item1, wall.Item2))
                {
                    gameMap[wall.Item1, wall.Item2] = 0;
                    return true;
                }
            }

            return false;
        }
    }
    
    #endregion
}
