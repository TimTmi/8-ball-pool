# Roadmap: 8 Ball Pool

## Phase 1: Foundation
- [x] Setup Unity Project & basic folder structure
- [x] Create simple Main Menu and Gameplay scenes
- [x] Configure basic Physics 2D settings for a pool table and balls

## Phase 2: Core Gameplay Mechanics
- [x] Implement Input Manager for drag-to-aim and drag-for-power
- [x] Add lock toggles for aim and power
- [x] Implement explicit "Shoot" button logic
- [x] Implement top-right spin control

## Phase 3: Trajectory & Physics Tuning
- [x] Apply shot physics to the cue ball and damped cushion rebound on the rails
- [x] Implement basic trajectory prediction line
- [x] Add spin effect to physics simulation and trajectory
- [ ] Tune physics materials (bounciness, friction) for realistic feel

## Phase 4: Game Loop & Rules
- [x] Pocket detection: balls drop out of play, cue ball returns to the head spot on a scratch
- [ ] Implement 8-ball rules logic (turns, faults, win/loss)
- [ ] Add simple UI feedback for player turns and game over
- [ ] Polish game flow (resetting table, transitioning to Main Menu)
