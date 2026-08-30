# Progress: 8 Ball Pool

## Current State
- Unity Project folder structure has been set up.
- UI Toolkit assets (UXML/USS) for Main Menu, Gameplay, and Credits created.
- Basic UI Controllers implemented.
- Main Menu and Gameplay scenes instantiated and added to Build Settings.
- Phase 2 Core Gameplay Mechanics implemented: InputManager captures drag input, GameplayUIController exposes lock toggles, shoot button, and spin control.
- **Phase 3 (Partial): Visual & Physics Setup** - Procedural generation of 2D pool sprites (felt, cushions, pockets, 16 balls, cue stick) added via Editor script. `TableSetup` dynamically builds the physics and visual layout at runtime using authoritative constants from `TableLayout`.

## Next Task
- Complete Phase 3: Trajectory Prediction (Implement prediction line rendering and physics simulation ahead of the shot).

## Blockers / Notes
- None at the moment.
