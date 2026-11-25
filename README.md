# NEON VEIL

**Chapter 1: The Dead District** is my 2D sci-fi horror platformer for CSC231 Project 2. It takes place in Veyra-9, a city quarantined after an alien outbreak in 2196. You play as Subject 09, a former security operative rebuilt with damaged armor, a cybernetic arm, and a blue optical implant. He is trying to reach the extraction tower before the city is lost.

The chapter is one connected level with five areas: Awakening, Abandoned Metro, Industrial Sector, Quarantine Zone, and Extraction Tower. The early areas introduce movement, jumping, combat, and hazards before the later sections ask you to combine them. My target for a first playthrough is 2–5 minutes; that timing still needs a fresh-player test.

## How to play

| Action | Control |
| --- | --- |
| Move | A/D or Left/Right Arrow |
| Jump | Space (hold for a higher jump) |
| Sword attack | Left mouse button |
| Phase Strike | Left Shift |
| Pause or resume | Escape |

Phase Strike is Subject 09's signature move: a forward energy-blade lunge with a cooldown. The HUD shows health, Energy Cells, enemy defeats, and when Phase Strike is ready. The music button mutes the soundtrack while leaving game effects audible. The volume control adjusts the audio level.

To finish, defeat **seven real enemies** and reach the extraction beacon. The first training bot teaches combat and does not count toward the seven. Energy Cells are optional collectibles; the HUD tracks how many you have found. Checkpoints let you continue from a later point after a Game Over.

## What's in the level

- Subject 09 has idle, sprint, jump, fall, hurt/death, attack, and Phase Strike animations.
- Sentinels and Mimics have different movement and attacks. Security lasers add a separate hazard.
- Platforms, gaps, and raised routes make jumping part of the level instead of just moving across flat ground.
- The title screen leads into a short story introduction. There is a HUD, a pause menu with resume/restart/quit, a Game Over screen, and a Level Complete screen.
- Jump, landing, pickup, hurt, combat, and extraction cues are paired with background music. Landing, pickup, hit, and Phase Strike also have visual effects.
- Each area has its own environment art, with layered backgrounds and parallax for depth.

## Opening the project

This project uses **Unity 6.6 (6000.6.1f1)** and the Unity Input System. Open the project folder in Unity Hub, then open `Assets/Scenes/NeonVeilChapter1.unity`. That is the chapter scene enabled in Build Settings.

## Tools and credits

I chose the game concept, direction, and final changes. I used OpenAI Codex to help with Unity scripting, scene setup, and revisions, and OpenAI image generation for the original character, enemy, environment, and effect art. I reviewed the result in Unity. The project does not use an existing game character.

Audio used in the project:

- **Ruskerdax**, [Bleak Terminal](https://opengameart.org/content/bleak-terminal) — gameplay music.
- **SRG774**, [Dark Sci-Fi Audio Pack](https://opengameart.org/content/dark-sci-fi-audio-pack) — extraction cue.
- **Spring Spring**, [Various Sound Effects](https://opengameart.org/content/various-sound-effects-0) — jump, pickup, and hurt source sounds.
- **Thimras**, [Metal footsteps on concrete](https://opengameart.org/content/metal-footsteps-on-concrete) — landing sound.
- **Kenney**, [Sci-Fi Sounds](https://kenney.nl/assets/sci-fi-sounds) — thruster, impact, laser, and force-field effects.
- **Joth**, [Ambience Pack 1 – Sci Fi Horror](https://opengameart.org/content/ambience-pack-1-sci-fi-horror) — an unused music draft kept in the project.

`Jump_CyberTakeoff.wav` combines parts of Spring Spring's jump sound and Kenney's thruster effect. `Death_SuitShutdown.wav` combines the suit hurt recording with a new descending power-down tone. Some earlier audio drafts remain in the project but are not used in the current scene.
