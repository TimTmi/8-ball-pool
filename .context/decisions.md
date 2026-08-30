# Architectural Decisions: 8 Ball Pool

## [2026-08-30] Local Multiplayer Only
**Context**: We need to define the scope of multiplayer functionality.
**Decision**: Stick exclusively to local pass-and-play. No online multiplayer features.
**Rationale**: Simplifies architecture massively by removing the need for server authority, state synchronization, and networking APIs.

## [2026-08-30] Touch Input Scheme
**Context**: How to implement pool controls on a mobile touch screen.
**Decision**: Dragging anywhere on screen adjusts both aim angle and power concurrently. Provide lock toggles for both. Action only triggers upon pressing an explicit "Shoot" button.
**Rationale**: Avoids misfires and allows the player precise control over parameters (especially since releasing a drag can often subtly change the input value accidentally on mobile).

## [2026-08-31] Procedural Scene & Sprite Generation
**Context**: How to create and manage the visual assets and scene layout for the pool table, pockets, and balls.
**Decision**: Use an Editor script (`SpriteGenerator.cs`) to draw basic geometric sprites (PNGs) and use a runtime script (`TableSetup.cs`) to procedurally spawn the table layout based on hardcoded `TableLayout.cs` constants.
**Rationale**: Ensures pixel-perfect crispness for 2D assets without bloating the repository with image files. Runtime scene construction ensures physics colliders and visuals are perfectly aligned with mathematical constants, reducing scene-editing errors.
