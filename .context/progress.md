# Progress: 8 Ball Pool

## Current State
- Unity Project folder structure has been set up.
- UI Toolkit assets (UXML/USS) for Main Menu, Gameplay, and Credits created.
- Basic UI Controllers implemented.
- Main Menu and Gameplay scenes instantiated and added to Build Settings.
- Phase 2 Core Gameplay Mechanics implemented: InputManager captures drag input, GameplayUIController exposes lock toggles, shoot button, and spin control.
- **Phase 3 (Partial): Visual & Physics Setup** - Procedural generation of 2D pool sprites (felt, cushions, pockets, 16 balls, cue stick) added via Editor script. `TableSetup` dynamically builds the physics and visual layout at runtime using authoritative constants from `TableLayout`.
- **Phase 3 (Partial): Shot Physics & Cushion Bounce** - `CueController` (on the Table object) launches the cue ball from aim angle + power, and reports when every ball has come to rest. `Ball` owns each ball's motion state and snaps near-zero velocity to a full stop. `Cushion` drains the surplus rebound energy on rail hits. Aiming and the cue stick are suppressed while the shot plays out.
- Collider sizing in `TableSetup` was corrected: ball, pocket, and rail colliders are now authored in local space, so they match their sprites instead of being scaled to the wrong size by the transform.
- **Phase 4 (Partial): Pocketing** - The rails are now six runs with a mouth at every pocket, so balls can physically enter one. `Pocket` captures a ball when its centre reaches the hole; `Ball.Drop()` takes it out of play with a short sink. A scratched cue ball is returned to the head spot once the table settles.
- Aim/power controls rewritten (2026-08-31, same-side scheme): aim angle = direction from pointer through the cue ball (finger drags on the cue's side, pull-back style), power = pointer-to-cue-ball distance (full power at `TableLayout.HalfFeltWidth`, tunable via `_maxPowerDistance`). Replaces the old drag-delta scheme.
- Cue sprite orientation fixed (2026-08-31): cue art now has the tip (chalk) on the right (+X), matching the runtime assumption that the sprite's +X axis points at the cue ball (`InputManager.UpdateCueVisuals`). Previously the butt faced the ball.

## Next Task
- Complete Phase 3: Trajectory Prediction (Implement prediction line rendering and physics simulation ahead of the shot).
- Play-test and tune the shot speed range, rolling damping, and cushion retention values — they are reasoned defaults, not yet validated in play.

## Blockers / Notes
- Spin (`GameplayUIController.CurrentSpin`) is captured by the UI but not yet consumed by the shot; a top-down 2D cue ball needs an explicit curve/english model, planned with the trajectory work.
- Pocketing is mechanical only: no foul detection, no turn switching, no win/loss, and a returned cue ball is not checked against balls already sitting on the head spot.
