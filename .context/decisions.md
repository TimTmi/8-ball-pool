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

## [2026-08-31] Cushion Rebound Damped in Script, Not by the Material
**Context**: Rails felt as bouncy as the balls. Physics 2D combines the bounciness of two materials by taking the *higher* of the two, so the springy `BallMaterial` (0.95, needed for realistic ball-to-ball contact) always wins over `CushionMaterial` (0.8) and the rail material has no effect.
**Decision**: Keep the materials as-is and let a `Cushion` component drain the surplus energy in `OnCollisionEnter2D`, splitting the post-bounce velocity into rebound (perpendicular) and slide (along-rail) components and scaling each.
**Rationale**: Contact callbacks run after the solver, so correcting there is exact and keeps a single tunable per effect. The rejected alternative — lowering `BallMaterial` bounciness — would have deadened ball-to-ball collisions too, which is the one contact that really is near-elastic in pool.

## [2026-08-31] Shots Assign Velocity Rather Than Accumulate Force
**Context**: How to translate aim angle + normalised power into cue ball motion.
**Decision**: `Ball.Launch` sets `linearVelocity` directly from a speed lerped between a min and max shot speed on `CueController`.
**Rationale**: A ball is always at rest when it is struck, so an impulse of mass x speed produces exactly this velocity — the result is identical but independent of frame ordering and of the ball's mass, which keeps shots reproducible for the upcoming trajectory prediction (prediction/simulation parity).

## [2026-08-31] Physics Step Raised to 100 Hz
**Context**: A full-power break moves the cue ball ~0.5 units per 50 Hz step — wider than a ball (0.5u) and than the rails are thick (0.4u).
**Decision**: Fixed Timestep 0.02 -> 0.01, velocity iterations 8 -> 12, position iterations 3 -> 6, and the restitution velocity threshold 1.0 -> 0.2.
**Rationale**: Continuous collision detection prevents outright tunnelling, but contact resolution in a dense rack still needs the smaller step to stay believable; the lower restitution threshold keeps slow balls bouncing off cushions instead of dying on contact. 16 rigidbodies at 100 Hz is negligible on a mobile target.

## [2026-08-31] Rails Are Cut Open at the Pockets, and Capture Is Centre-Based
**Context**: Pocketing a ball needs the ball to be able to enter the pocket. The rails were four unbroken boxes running the full length of the table, so a ball could only ever skim across a pocket trigger while bouncing off the cushion in front of it.
**Decision**: `TableLayout.GetRailSegments()` returns six rail runs with a `PocketMouthHalfWidth` (0.45u, ~1.8 ball widths) gap at every pocket, and `Pocket` captures a ball only when the distance between centres is within `PocketRadius` — not on trigger overlap.
**Rationale**: Trigger overlap fires at centre distance 0.65u, which would swallow any ball merely rolling along a rail past a pocket. Keeping the trigger wide but testing the centre gives an early, cheap broad phase with a capture point that matches what the player sees. `Ball` carries an out-of-bounds failsafe so a ball that squeezes past a mouth is pocketed instead of rolling away forever, which would also leave the table permanently unsettled.
