# OpenCampfire

**Mod for Cozy Campfire Club.**

OpenCampfire is an open-source project that explored the modding possibilities of Cozy Campfire Club (The Demo for now).

## Current project: All the cats!!!

The initial implementation enables selection of the four demo-locked
cat species through the game's existing cosmetic equipment pipeline, yay!

### Current available characters.

- Snowball
- Domino
- Toffee
- Patches
- Multiplayer appearance synchronization on by default, provided by the game duh!

Marmalade (the orange cat) remains selectable through the game's normal interface

### Known limitations

- Selected locked cats do not persist after restarting the game, so you'll have to pick them each time.
- Other cosmetics, like raccoons are not yet supported!

## Requirements

- Cozy Campfire Club Demo
- BepInEx 5
- .NET SDK 8 or compatible SDK capable of targeting netstandard2.1
- Git

## Building

Clone the repository:

```bash
git clone https://github.com/CarlQuack/OpenCampfire.git
cd OpenCampfire
