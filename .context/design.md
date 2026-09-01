# 8 Ball Pool Technical Design

## Architecture
Monolithic Unity client architecture. The game uses a scene-based flow consisting of a Main Menu scene and a Gameplay scene. The core loop will rely on a Game State Machine to manage the progression of turns, physics simulation, and player input. 

## Components
Status is marked per component; unmarked bullet details describe the target design.

- **Input Manager** *(implemented: `InputManager.cs`)*: Captures raw touch/drag input and translates it to aim angle, power, and spin. Controls the lock states for aim and power.
- **Trajectory Predictor** *(partially implemented: `ShotPrediction.cs`, `AimLine.cs`)*: `ShotPrediction` sweeps the cue ball shape along the aim direction to find the first contact; `AimLine` draws the dotted path, a ghost ball at contact, and the struck ball's departure direction. Spin is modelled: the path curves with side spin and the post-contact cue-ball run is drawn. Rail bounces are still not predicted.
- **Physics Engine (Unity)** *(implemented)*: Handles runtime collision detection and rigidbody physics for the balls. Shot execution lives in `CueController`/`Ball`; cushion rebound damping in `Cushion`.
- **Game State Manager** *(planned, Phase 4)*: Enforces standard 8-ball rules (legal breaks, valid hits, pocketing logic, win/loss conditions) and switches turns between the two local players.
- **UI Controller** *(implemented: `GameplayUIController.cs`, `MainMenuController.cs`)*: Manages minimal screens. Gameplay UI includes lock toggles, spin control UI, and the Shoot confirmation button.
- **Table Setup & Layout** *(implemented)*: A procedural approach (`TableSetup.cs`) builds the physics and visual elements of the table at runtime (balls, cushions, pockets) using strict mathematical constants (`TableLayout.cs`) for precise physical placement.

## Communication
- User input triggers UI state changes (e.g., revealing the Shoot button).
- Input Manager runs `ShotPrediction` every frame during the aim phase and feeds the result to `AimLine`.
- Pressing "Shoot" passes final parameters to the Cue controller, applying physical forces to the Cue Ball.
- The Game State Manager listens to collision events and pocket triggers to evaluate rules after all balls stop moving.

## Key Reliability Patterns
- **Simulation/Prediction Parity**: The trajectory prediction should ideally use the same physics settings and logic as the actual shot execution.

## Important Tradeoffs
- Simple pass-and-play architecture ignores network synchronization, keeping logic localized and state management straightforward.
