public static class MapGenerator
{
    private const int MapWidth = 128;
    private const int MapHeight = 128;
    private const int SlicesCount = 25;
    private const int MinDistanceBetweenSlices = 5;
    private const float MergeChance = 0.10f;

    public static int[,] GetMap(out int worldSeed)
    {
        worldSeed = Random.Shared.Next(0, 1_000_000);
        var rng = new Random(worldSeed);
        int[,] gameMap = GenerateMap(rng);
        return gameMap;
    }

    private static int[,] GenerateMap(Random rng)
    {
        int[,] gameMap = new int[MapWidth, MapHeight];
        GenerateBorders(gameMap);

        int didSlices = 0;
        int attempts = 0;
        int maxAttempts = SlicesCount * 2000;

        while (didSlices < SlicesCount && attempts < maxAttempts)
        {
            if (RandomSlice(gameMap, rng, MinDistanceBetweenSlices))
            {
                didSlices++;
            }

            attempts++;
        }

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
    /// <returns>True if a slice was successfully created, false otherwise.</returns>
    private static bool RandomSlice(int[,] gameMap, Random rng, int minDistance)
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
                            return false;
                        }

                        if (!vertical &&
                            (gameMap[x - 1, y] == 0 || x == 0 || gameMap[x + 1, y] == 0 || x == MapWidth - 1))
                        {
                            return false;
                        }
                    }
                }
            }
        }

        var possible = new List<int>();

        bool shouldRemoveWall = rng.NextDouble() < MergeChance;

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
            return false;
        }

        int cursor = possible[rng.Next(possible.Count)];
        bool removedWall = false;
        while (true)
        {
            cursor++;
            if (cursor >= walkSize)
            {
                return false;
            }

            int x = vertical ? cursor : coord;
            int y = vertical ? coord : cursor;

            if (gameMap[x, y] == 1)
            {
                if (shouldRemoveWall && !removedWall && x > 0 && x < MapWidth - 1 && y > 0 && y < MapHeight - 1)
                {
                    RemoveConnectedWall(gameMap, x, y);

                    removedWall = true;
                    continue;
                }

                break;
            }

            gameMap[x, y] = 1;
        }

        return true;
    }

    private static void RemoveConnectedWall(int[,] gameMap, int startX, int startY)
    {
        var cellsToVisit = new Queue<(int x, int y)>();
        cellsToVisit.Enqueue((startX, startY));

        while (cellsToVisit.Count > 0)
        {
            var (x, y) = cellsToVisit.Dequeue();

            if (x <= 0 || x >= MapWidth - 1 || y <= 0 || y >= MapHeight - 1)
            {
                continue;
            }

            if (gameMap[x, y] != 1)
            {
                continue;
            }

            gameMap[x, y] = 0;

            cellsToVisit.Enqueue((x - 1, y));
            cellsToVisit.Enqueue((x + 1, y));
            cellsToVisit.Enqueue((x, y - 1));
            cellsToVisit.Enqueue((x, y + 1));
        }
    }
}
