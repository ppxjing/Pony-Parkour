# Toy Dash Project Report

## Evaluation Plan

The team tested the complete game flow in Unity and the WebGL build. Testing covered race completion, lap tracking, obstacle response, checkpoint recovery, AI behaviour, UI, and scene transitions.

The target was a stable three-level playthrough in a desktop browser within ten minutes.

## Evaluation Report

### Test Results

| Test | Result |
| --- | --- |
| WebGL build loads and reaches the main menu | Pass |
| Beach completes two laps and opens the ranking screen | Pass |
| Kitchen completes two laps and opens the ranking screen | Pass |
| Bedroom fall zones return the player to the latest checkpoint | Pass |
| Moving obstacles trigger collision and recovery | Pass |
| AI racers recover after being blocked or leaving the route | Pass |
| Beach, Kitchen, and Bedroom story transitions open the correct scenes | Pass |
| Settings, pause, replay, main menu, and results UI work correctly | Pass |
| Complete three-level playthrough stays within the ten-minute target | Pass |

### Issues Resolved

| Issue | Resolution |
| --- | --- |
| Lap progress remained on lap one | Corrected ordered route checks and lap completion logic |
| Checkpoint stars were oversized or difficult to trigger | Adjusted scale, height, collider placement, and collection behaviour |
| Player and AI reacted differently to obstacles | Unified collision, ragdoll, and recovery behaviour |
| AI racers could remain stuck | Added route checks, anti-stuck detection, and checkpoint recovery |
| Rotating hammer collision was unreliable | Corrected collider coverage |
| Falling from the Bedroom course did not trigger recovery | Added a fall zone and registered respawn checkpoints |
| Menu, settings, story, and ranking UI were misaligned | Standardised anchors, spacing, and scale |
| Respawn references could fail during rapid Play Mode entry | Added runtime reference validation |

## Summary of Contributions

| Team member | Contributions |
| --- | --- |
| Peng Xuanjing (`ppxjing`) | AI core systems, AI anti-stuck recovery, story and scene connections, obstacle placement and tuning, checkpoint stars, UI adjustments, scene integration, collision fixes, and gameplay testing |
| Zhilan Liu (`zhilanliu`) | Character models and materials, environment layout, lighting, menu and countdown UI, Beach and Bedroom construction, story presentation, documentation, and scene polish |
| Yang Cheng (`YCYCYC111`) | AI core system optimisation, player movement, NavMesh and route optimisation, obstacle behaviours, camera behaviour, collision physics, death, and respawn systems |

Integration and playtesting were shared across the team.

## References and External Resources

- [Unity 6 Documentation](https://docs.unity3d.com/6000.3/Documentation/Manual/index.html)
- [Unity WebGL Documentation](https://docs.unity3d.com/6000.3/Documentation/Manual/webgl-building.html)
- [Unity AI Navigation](https://docs.unity3d.com/Packages/com.unity.ai.navigation@latest)
- Unity Input System
- TextMeshPro
- Fun Obstacle Course Vol. 1
- Minicar Environment Pack
- Imported toy horse, Elephant, Giraffe, and Rabbit models
- [Beach Stage](https://opengameart.org/content/beach-stage) by MintoDog, CC0
- [Children's March Theme](https://opengameart.org/content/childrens-march-theme) by Cleyton Kauffman, CC0
- [Flowerbed Fields](https://opengameart.org/content/flowerbed-fields-loop) by Zane Little Music, CC0
- [Sleep Talking](https://opengameart.org/content/sleep-talking-loop-fantasy-rpg-sci-fi) by KiluaBoy, CC0

The game design and project overview are maintained in [README.md](README.md). Team information is recorded in [metadata.json](metadata.json).
