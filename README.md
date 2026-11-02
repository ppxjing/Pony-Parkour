# Toy Dash

**Toy Dash** is a single-player 3D obstacle racing adventure made with Unity 6.3.x LTS. After the hotel closes for the night, the toys secretly come to life. The player chooses a toy horse, competes against three AI racers, and follows three magical star fragments through the Beach, Kitchen, and Bedroom before morning arrives.

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
- Illustrated click-to-continue story sequences between every level
- Character selection with Flower Horse, Flower Horse B, and Horse
- A shared pause/settings menu across all three gameplay scenes
- Toy-style countdown, level selection, ranking, and results screens

## Story

As sunset fades over the seaside hotel, the guests go inside and the toy room becomes quiet. Moonlight reaches Woody and the other toys, causing Woody's golden star to glow and awaken the four friends. The star suddenly breaks into three magical fragments and sends sparkling trails through the hotel. The toys must recover every fragment and return to their places before morning.

The first trail leads to the moonlit Beach. After the first race, the toys find a glowing fragment among the rocks. Its light returns through the hotel doors and points towards the Kitchen.

The second race takes place between tables, chairs, food props, and cooking toys. When the Kitchen race ends, the second fragment appears and joins its light with the first. Together they reveal a path through the quiet corridor to the Bedroom.

After the final Bedroom race, the third fragment rises and the three pieces form a complete magical star. Its golden light awakens the surrounding toys and restores the magic of after hours. As the night begins to fade, the star guides the friends back to their places. Morning enters the room and every toy becomes still again, as though nothing happened. Woody's star gives one final glow, promising that the adventure will begin again when the lights go out.

The complete story is presented with full-screen illustrations, a parchment dialogue panel, character dialogue, narration, and click-anywhere progression.

## Gameplay

Each race contains one player and three AI opponents. All racers start after a 3, 2, 1, GO countdown and complete two laps. Ordered route points record lap progress and determine the final ranking.

The player runs, steers, jumps, and avoids hazards. Contact with a lethal obstacle or a fall zone triggers a respawn at the latest checkpoint. AI racers follow level-specific navigation routes and recover when blocked or separated from the course.

### Controls

| Input | Action |
| --- | --- |
| W / A / S / D | Move |
| Space | Jump |
| Mouse | Rotate camera |
| Escape | Open or close the pause/settings menu |
| Left Mouse Button | Capture cursor |
| Settings button | Open settings and pause the race |
| Back button | Close settings and resume the race |
| Main Menu button | Leave the race and return to the main menu |
| Mouse click on a story page | Continue to the next story page |

## Levels

### Beach

An open moonlit course built from oversized seaside objects, boards, platforms, gaps, and moving obstacles. It introduces movement, camera control, checkpoints, and the search for the first star fragment.

### Kitchen

An elevated course across counters, food props, utensils, bridges, and narrow paths. Tighter turns and rotating obstacles require more precise movement. The second fragment creates a glowing trail towards the Bedroom.

### Bedroom

The final course runs around furniture, blocks, books, and toys. Fall zones and checkpoints support the longer route. Completing the race restores the golden star and opens the full six-shot ending sequence.

## Game Systems

- Camera-relative player movement and jumping
- Third-person orbit camera
- NavMesh-based AI navigation
- Ordered route points and two-lap tracking
- AI recovery and checkpoint respawning
- Shared obstacle collision and ragdoll response
- Four-player ranking calculation
- Connected story and scene transitions
- Full-screen illustrated story system with reusable dialogue layout
- Image-based main menu, character selection, level selection, countdown, ranking, and settings UI
- Shared pause menu with cursor release, time-scale control, resume, and main-menu actions
- WebGL-compatible menu, input, story, and settings UI

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
