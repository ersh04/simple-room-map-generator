# Simple Room Map Generator

A lightweight procedural room-based map generator designed for creating dungeon layouts, room networks, and grid-based worlds.

## Features

- Procedural room generation
- Automatic room connection generation
- Configurable map size
- Adjustable generation parameters
- Lightweight and easy integration
- Suitable for roguelikes, dungeon crawlers, and procedural worlds

## Preview

Add screenshots or GIFs here.

## Installation

Clone repository:

```bash
git clone https://github.com/ersh04/simple-room-map-generator.git
```

Open project and import it into your existing project.

## Usage

Basic example:

```csharp
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
```

Example output:

```

┌───┐     ┌─────┐
│   ├─────┤     │
└─┬─┘     └──┬──┘
  │          │
┌─┴────┐  ┌──┴──┐
│      │  │     │
└──────┘  └─────┘

```

## Configuration

| Parameter | Description |
|----------|----------|
| Width | Map width |
| Height | Map height |
| Room Count | Number of generated rooms |
| Seed | Random seed |
| Corridor Size | Corridor thickness |

## Customization

You can customize:

- Room sizes
- Corridor generation
- Spawn rules
- Random seed behavior
- Generation algorithms

## Use Cases

- Roguelikes
- Dungeon generators
- Procedural levels
- Exploration games
- Grid-based worlds

## Performance

Designed for fast generation with low overhead.

## Roadmap

- [ ] More generation algorithms
- [ ] Visualization tools
- [ ] Better corridor generation
