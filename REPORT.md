# Project 2 Report

## Table of Contents

- [Evaluation Plan](#evaluation-plan)
- [Evaluation Report](#evaluation-report)
- [Summary of Contributions](#summary-of-contributions)
- [References and External Resources](#references-and-external-resources)

## Evaluation Plan

The team evaluated the game through repeated internal playtests in the Unity Editor and the final WebGL build. Testing focused on whether a complete player journey could be finished in a desktop browser within ten minutes.

The main evaluation tasks were:

1. Open the game from the main menu and select a playable horse.
2. Complete the Beach, Kitchen, and Bedroom races in sequence.
3. Confirm that each race begins with the countdown, records two laps, and displays the final ranking.
4. Check that story pages lead to the correct next level.
5. Collide with moving obstacles and verify the death or recovery response.
6. Fall from the course and verify that the player returns to the most recent checkpoint.
7. Observe AI racers for navigation failures, permanent obstruction, and incorrect lap progress.
8. Test Replay, Continue, Main Menu, Settings, pause, mouse capture, and fullscreen controls.
9. Run the WebGL build in a desktop browser and check loading, input, scene changes, and completion.

The acceptance criteria were a complete three-level playthrough, readable navigation and UI, stable AI competition, functioning checkpoint recovery, working story transitions, and a total playtime below ten minutes.

## Evaluation Report

Internal evaluation identified several problems during production. These were addressed before the final build:

| Issue found | Change made | Result |
| --- | --- | --- |
| Lap progress could remain on lap one | Ordered route checks and lap completion logic were corrected | Beach and Kitchen can complete two laps and reach the ranking screen |
| Checkpoint stars were oversized or difficult to trigger | Their scale, height, collider placement, and disappearance behaviour were adjusted | Stars are readable and are collected consistently |
| Player and AI responded differently to obstacles | Shared collision recovery and ragdoll behaviour were introduced | Both player and opponents recover and continue racing |
| AI racers could remain stuck behind course objects | Anti-stuck detection and checkpoint recovery were added | AI racers return to the route instead of permanently leaving the race |
| Some large obstacles lacked reliable collision | Collider coverage was corrected, including the rotating hammer | Visible obstacle contact produces the expected response |
| Bedroom falls did not trigger recovery | A room-wide fall death zone and five registered respawn checkpoints were added | Falling causes death and returns the player to the latest checkpoint |
| UI panels and result buttons were misaligned | Anchors, spacing, scale, and visual styles were standardised | Menu, settings, story, and ranking screens remain readable at the target layout |
| Rapid Play Mode entry could leave respawn references uninitialised | The respawn system now validates its references at runtime | Checkpoint recovery remains stable across repeated tests |

The final Unity smoke test produced no console errors. The Bedroom recovery test activated a checkpoint, entered the fall death zone, and successfully returned the player to that checkpoint. The final deliverable is a Unity WebGL build containing the connected Beach, Kitchen, and Bedroom experience.

## Summary of Contributions

The summary below is based on the repository history. Integration and playtesting were shared across the team.

| Team member | Main contributions |
| --- | --- |
| Peng Xuanjing (`ppxjing`) | Obstacle placement and tuning, checkpoint stars, UI adjustments, scene integration, rotating-hammer fixes, player and race-manager integration, and gameplay testing |
| Zhilan Liu (`zhilanliu`) | Character models and materials, environment presentation and lighting, menu and countdown UI, Beach and Bedroom course construction, AI routes for those scenes, story connections, death regions, documentation, and final scene polish |
| Yang Cheng (`YCYCYC111`) | Core player and AI systems, NavMesh routes and links, obstacle behaviours, ordered AI waypoint patrol, collision physics, camera behaviour, death and respawn systems, AI recovery, and navigation data |

## References and External Resources

- [Unity 6 documentation](https://docs.unity3d.com/6000.3/Documentation/Manual/index.html)
- [Unity WebGL build documentation](https://docs.unity3d.com/6000.3/Documentation/Manual/webgl-building.html)
- [Unity AI Navigation package](https://docs.unity3d.com/Packages/com.unity.ai.navigation@latest)
- Unity Input System and TextMeshPro packages used by the project
- Third-party environment, character, and obstacle assets stored in the project under their corresponding asset folders
- Licences and attribution files distributed with imported packages are retained in the repository where supplied

The game design document is maintained in [README.md](README.md). Team membership and repository information are recorded in [metadata.json](metadata.json).
