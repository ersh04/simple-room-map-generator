public static class Visualisation
{
    private const int MapWidth = 128;
    private const int MapHeight = 128;

    public static void Main()
    {
        Visualise(MapGenerator.GetMap(out int worldSeed), worldSeed);

        while (true)
        {
            var key = Console.ReadKey(intercept: true).Key;
            if (key == ConsoleKey.Spacebar)
            {
                Visualise(MapGenerator.GetMap(out worldSeed), worldSeed);
            }

            if (key == ConsoleKey.Escape)
            {
                break;
            }
        }
    }

    private static void Visualise(int[,] gameMap, int worldSeed)
    {
        Console.Clear();
        Console.WriteLine($"World seed: {worldSeed}");
        PrintMap(gameMap);
        Console.WriteLine();
    }

    /// <summary>
    /// Prints the generated map to the console, using '#' for walls and ' ' for empty spaces.
    /// </summary>
    /// <param name="gameMap">The map to print.</param>
    private static void PrintMap(int[,] gameMap)
    {
        for (int y = 0; y < MapHeight; y++)
        {
            for (int x = 0; x < MapWidth; x++)
            {
                Console.Write(gameMap[x, y] == 1 ? '#' : ' ');
            }

            Console.WriteLine();
        }
    }
}