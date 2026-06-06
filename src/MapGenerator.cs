public static class MapGenerator
{
    #region Constants

    private const int MapWidth = 128;
    private const int MapHeight = 128;
    private const int SlicesCount = 35;
    private const int MinRoomSize = 5;
    private const int MaxRoomSize = 10;
    // The maximum allowed aspect ratio (width/height or height/width) for a room.
    private const float MaxRoomAspectRatio = 2f;
    private const double ContinueLatestBranchChance = 0.65;
    // Room merging chance. Applying after room creation.
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

        int didSlices = 0;
        int attempts = 0;
        int maxAttempts = SlicesCount * 2000;

        // Centers of generated rooms.
        List<MapSlice> roomCenters = new List<MapSlice>();
        while (didSlices < SlicesCount && attempts < maxAttempts)
        {
            var newRoom = RandomSlice(gameMap, rng, roomCenters);
            if (newRoom.XCenter != -1 && newRoom.YCenter != -1)
            {
                roomCenters.Add(newRoom);
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

    private static bool IsWall(int[,] gameMap, int x, int y)
    {
        if (x <= 0 || x >= MapWidth - 1 || y <= 0 || y >= MapHeight - 1)
        {
            return false;
        }

        return gameMap[x, y] == 1;
    }

    #endregion

    #region Slice Creation

    private static MapSlice RandomSlice(
        int[,] gameMap,
        Random rng,
        List<MapSlice> existingRooms)
    {
        int minRoomSize = MinRoomSize;
        int maxRoomSize = Math.Max(minRoomSize, MaxRoomSize);
        int placementAttempts = 48;

        for (int attempt = 0; attempt < placementAttempts; attempt++)
        {
            int width = rng.Next(minRoomSize, maxRoomSize + 1);
            int height = rng.Next(minRoomSize, maxRoomSize + 1);

            float aspectRatio = (float)Math.Max(width, height) / Math.Min(width, height);
            if (aspectRatio > MaxRoomAspectRatio)
            {
                continue;
            }

            int left;
            int top;
            int directionUsed = -1;
            MapSlice? anchorRoom = null;

            if (existingRooms.Count == 0)
            {
                left = rng.Next(1, MapWidth - width);
                top = rng.Next(1, MapHeight - height);
            }
            else
            {
                anchorRoom = ChooseAnchorRoom(existingRooms, rng);
                directionUsed = ChooseDirectionByLongSide(width, height, rng);
                switch (directionUsed)
                {
                    case 0:
                        // Right of anchor room.
                        left = anchorRoom.Right + 1;
                        top = rng.Next(anchorRoom.Top - height + 2, anchorRoom.Bottom);
                        break;
                    case 1:
                        // Left of anchor room.
                        left = anchorRoom.Left - width;
                        top = rng.Next(anchorRoom.Top - height + 2, anchorRoom.Bottom);
                        break;
                    case 2:
                        // Down from anchor room.
                        left = rng.Next(anchorRoom.Left - width + 2, anchorRoom.Right);
                        top = anchorRoom.Bottom + 1;
                        break;
                    default:
                        // Up from anchor room.
                        left = rng.Next(anchorRoom.Left - width + 2, anchorRoom.Right);
                        top = anchorRoom.Top - height;
                        break;
                }
            }

            int right = left + width - 1;
            int bottom = top + height - 1;

            int distance = existingRooms.Count == 0 ? MinRoomSize : 0;
            if (!CanPlaceRectangularRoom(existingRooms, left, top, right, bottom, distance))
            {
                continue;
            }

            var walls = new List<(int, int)>(width * 2 + height * 2);

            for (int x = left; x <= right; x++)
            {
                gameMap[x, top] = 1;
                gameMap[x, bottom] = 1;
                walls.Add((x, top));
                if (top != bottom)
                {
                    walls.Add((x, bottom));
                }
            }

            for (int y = top + 1; y < bottom; y++)
            {
                gameMap[left, y] = 1;
                gameMap[right, y] = 1;
                walls.Add((left, y));
                if (left != right)
                {
                    walls.Add((right, y));
                }
            }

            var center = ((left + right) / 2, (top + bottom) / 2);

            return new MapSlice(center, walls.ToArray(), left, top, right, bottom);
        }

        return new MapSlice(-1, -1, Array.Empty<(int, int)>());
    }

    private static MapSlice ChooseAnchorRoom(List<MapSlice> existingRooms, Random rng)
    {
        if (existingRooms.Count == 1 || rng.NextDouble() < ContinueLatestBranchChance)
        {
            return existingRooms[^1];
        }

        return existingRooms[rng.Next(existingRooms.Count - 1)];
    }

    private static int ChooseDirectionByLongSide(int width, int height, Random rng)
    {
        // 0/1 -> right/left placement, contact side length is room height.
        // 2/3 -> down/up placement, contact side length is room width.
        bool preferHorizontalContact = width >= height;

        // 75% of the time choose directions that use the long side for contact.
        if (preferHorizontalContact)
        {
            return rng.NextDouble() < 0.75 ? (rng.Next(0, 2) == 0 ? 2 : 3) : (rng.Next(0, 2) == 0 ? 0 : 1);
        }

        return rng.NextDouble() < 0.75 ? (rng.Next(0, 2) == 0 ? 0 : 1) : (rng.Next(0, 2) == 0 ? 2 : 3);
    }

    private static bool CanPlaceRectangularRoom(
        List<MapSlice> existingRooms,
        int left,
        int top,
        int right,
        int bottom,
        int minDistance)
    {
        if (left <= 0 || top <= 0 || right >= MapWidth - 1 || bottom >= MapHeight - 1)
        {
            return false;
        }

        foreach (var room in existingRooms)
        {
            if (room.Left < 0)
            {
                continue;
            }

            bool intersectsWithMargin =
                left <= room.Right + minDistance &&
                right >= room.Left - minDistance &&
                top <= room.Bottom + minDistance &&
                bottom >= room.Top - minDistance;

            if (intersectsWithMargin)
            {
                return false;
            }
        }

        return true;
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

            if (!openedDoorway)
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

                var (isBridge, _) = IsBridgeBetweenDifferentRegions(regionMap, x, y);
                if (isBridge)
                {
                    gameMap[x, y] = 0;
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
        int left = regionMap[x - 1, y];
        int right = regionMap[x + 1, y];
        if (left != 0 && right != 0 && left != right)
        {
            return (true, true);
        }

        int up = regionMap[x, y - 1];
        int down = regionMap[x, y + 1];
        if (up != 0 && down != 0 && up != down)
        {
            return (true, false);
        }

        return (false, false);
    }

    private static bool TryOpenJointDoorway(int[,] gameMap, int[,] regionMap, int x, int y, int left, int top, int right, int bottom)
    {
        if (TryOpenJointDoorwayPair(gameMap, regionMap, x, y, x - 1, y, left, top, right, bottom))
        {
            return true;
        }

        if (TryOpenJointDoorwayPair(gameMap, regionMap, x, y, x + 1, y, left, top, right, bottom))
        {
            return true;
        }

        if (TryOpenJointDoorwayPair(gameMap, regionMap, x, y, x, y - 1, left, top, right, bottom))
        {
            return true;
        }

        return TryOpenJointDoorwayPair(gameMap, regionMap, x, y, x, y + 1, left, top, right, bottom);
    }

    private static bool TryOpenJointDoorwayPair(
        int[,] gameMap,
        int[,] regionMap,
        int x,
        int y,
        int otherX,
        int otherY,
        int left,
        int top,
        int right,
        int bottom)
    {
        if (!IsExternalWall(gameMap, otherX, otherY, left, top, right, bottom))
        {
            return false;
        }

        int deltaX = otherX - x;
        int deltaY = otherY - y;

        int firstRegionX = x - deltaX;
        int firstRegionY = y - deltaY;
        int secondRegionX = otherX + deltaX;
        int secondRegionY = otherY + deltaY;

        if (firstRegionX <= 0 || firstRegionX >= MapWidth - 1 || firstRegionY <= 0 || firstRegionY >= MapHeight - 1)
        {
            return false;
        }

        if (secondRegionX <= 0 || secondRegionX >= MapWidth - 1 || secondRegionY <= 0 || secondRegionY >= MapHeight - 1)
        {
            return false;
        }

        int firstRegion = regionMap[firstRegionX, firstRegionY];
        int secondRegion = regionMap[secondRegionX, secondRegionY];
        if (firstRegion == 0 || secondRegion == 0 || firstRegion == secondRegion)
        {
            return false;
        }

        gameMap[x, y] = 0;
        gameMap[otherX, otherY] = 0;
        return true;
    }

    private static bool IsExternalWall(int[,] gameMap, int x, int y, int left, int top, int right, int bottom)
    {
        if (x <= 0 || x >= MapWidth - 1 || y <= 0 || y >= MapHeight - 1)
        {
            return false;
        }

        if (x >= left && x <= right && y >= top && y <= bottom)
        {
            return false;
        }

        return gameMap[x, y] == 1;
    }

    #endregion

    #region MapSlice class

    private class MapSlice
    {
        public int XCenter { get; }
        public int YCenter { get; }

        public int Left { get; }
        public int Top { get; }
        public int Right { get; }
        public int Bottom { get; }

        public (int, int)[] Walls { get; }

        public MapSlice(int xCenter, int yCenter, (int, int)[] walls)
        {
            XCenter = xCenter;
            YCenter = yCenter;
            Walls = walls;
            Left = -1;
            Top = -1;
            Right = -1;
            Bottom = -1;
        }

        public MapSlice((int, int) center, (int, int)[] walls, int left, int top, int right, int bottom)
        {
            XCenter = center.Item1;
            YCenter = center.Item2;
            Walls = walls;
            Left = left;
            Top = top;
            Right = right;
            Bottom = bottom;
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

                if (TryOpenJointDoorway(gameMap, regionMap, x, y, Left, Top, Right, Bottom))
                {
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
