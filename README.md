# Toy Dash

**Toy Dash** is a single-player 3D obstacle racing game made with Unity 6.3.x LTS. The player controls a toy horse, competes against three AI racers, collects three star fragments, and completes a route through the Beach, Kitchen, and Bedroom.

## Play

[Play the WebGL build](https://gdat-ap6412.github.io/2026s1-project-2-team-02/)

Platform: Desktop web browser  
Build target: WebGL  
Target playtime: Under 10 minutes

## Features

- Three connected race levels
- Three selectable toy horses
- Three AI opponents: Elephant, Giraffe, and Rabbit
- Two-lap races with live progress and final ranking
- Moving and rotating obstacles
- Jumping, fall zones, checkpoints, and respawning
- AI route navigation and anti-stuck recovery
- Character selection, settings, pause, story, and results screens

## Story

Woody and the other toys discover that their golden star has broken into three fragments. They follow the fragments through three locations and turn the search into a race.

The first fragment is found at the Beach. Its light points toward the Kitchen, where the second fragment reveals a trail to the Bedroom. At the end of the final race, the three fragments reunite and restore the golden star.

## Gameplay

Each race contains one player and three AI opponents. All racers start after a 3, 2, 1, GO countdown and complete two laps. Ordered route points record lap progress and determine the final ranking.

The player runs, steers, jumps, and avoids hazards. Contact with a lethal obstacle or a fall zone triggers a respawn at the latest checkpoint. AI racers follow level-specific navigation routes and recover when blocked or separated from the course.

### Controls

| Input | Action |
| --- | --- |
| W / A / S / D | Move |
| Space | Jump |
| Mouse | Rotate camera |
| Escape | Release cursor |
| Left Mouse Button | Capture cursor |
| Settings button | Pause the race |

## Levels

### Beach

An open course built from oversized seaside objects, boards, platforms, gaps, and moving obstacles. It introduces movement, camera control, checkpoints, and the first star fragment.

### Kitchen

An elevated course across counters, food props, utensils, bridges, and narrow paths. Tighter turns and rotating obstacles require more precise movement. The second fragment leads to the Bedroom.

### Bedroom

The final course runs around furniture, blocks, books, and toys. Fall zones and checkpoints support the longer route. Completing the race restores the golden star and opens the ending.

## Game Systems

- Camera-relative player movement and jumping
- Third-person orbit camera
- NavMesh-based AI navigation
- Ordered route points and two-lap tracking
- AI recovery and checkpoint respawning
- Shared obstacle collision and ragdoll response
- Four-player ranking calculation
- Connected story and scene transitions
- WebGL-compatible menu and settings UI

## Technology

- Unity 6.3.23f1
- C#
- Unity Input System
- Unity AI Navigation
- TextMeshPro
- Git and GitHub
- WebGL

## Project Files

- `Assets`: Game scenes, scripts, prefabs, UI, models, and imported assets
- `Packages`: Unity package configuration
- `ProjectSettings`: Unity project settings
- `WebGLBuild`: Browser build
- `REPORT.md`: Testing, fixes, team contributions, and references
- `metadata.json`: Team and repository information

## External Resources

The project uses imported character, environment, and obstacle assets, including `Fun Obstacle Course Vol. 1` and `Minicar Environment Pack`. Unity packages used by the project are listed in `Packages/manifest.json`.

## Team

- Peng Xuanjing, `ppxjing`
- Zhilan Liu, `zhilanliu`
- Yang Cheng, `YCYCYC111`

See [REPORT.md](REPORT.md) for testing results and contribution details.
