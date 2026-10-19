# Game audio sources

All selected recordings/music are CC0, free for commercial use. No attribution required; credits preserved here and in-game.

- Kenney RPG Audio: https://kenney.nl/assets/rpg-audio — cloth, book, blade, door foley. Original License.txt in RPG/.
- Kenney Impact Sounds: https://kenney.nl/assets/impact-sounds — soft impacts, concrete/carpet/wood footsteps. Original License.txt in Impact/.
- Deep Humidity — TinyWorlds: https://opengameart.org/content/deep-humidity — CC0, exploration/cave ambience. Region 1.
- Searching — yd: https://opengameart.org/content/searching — CC0, warm/dark ambient loop. Region 2.
- License: https://creativecommons.org/publicdomain/zero/1.0/

Original OGG files retained, selected files copied unchanged into Book/Assets/Unfin/Resources/Audio. sources.json maps every cue to its source. Unity applies conservative gain, slight SFX pitch variation, distance attenuation and voice/cooldown limits. Music crossfades over 3 seconds at region changes and loop boundaries. No voices, gunshots or exaggerated cartoon jumps.

Attack revision: Swing uses cloth3/cloth4, Thrust uses cloth2, and the former enemy knife variant uses cloth4. Blade slicing/drawing recordings are no longer assigned to attacks; these CC0 cloth recordings provide a softer, non-metallic swish.

Connected actions: material footsteps, jump, landing, dash, wall movement, all three melee strikes and air attacks, enemy hits/death/movement/attack, hazard reset, gate opening, collapsing/reforming terrain, map open/close and respawn. Pickups and saves remain placement markers without invented collection triggers.
