# 8 Ball Pool Technical Design

## Architecture
Monolithic Unity client architecture. The game uses a scene-based flow consisting of a Main Menu scene and a Gameplay scene. The core loop will rely on a Game State Machine to manage the progression of turns, physics simulation, and player input. 

## Components
- **Input Manager**: Captures raw touch/drag input and translates it to aim angle, power, and spin. Controls the lock states for aim and power.
- **Trajectory Predictor**: Simulates physics steps ahead of time to render the predicted path of the cue ball and object balls, incorporating spin and collisions.
- **Physics Engine (Unity)**: Handles runtime collision detection and rigidbody physics for the balls.
- **Game State Manager**: Enforces standard 8-ball rules (legal breaks, valid hits, pocketing logic, win/loss conditions) and switches turns between the two local players.
- **UI Controller**: Manages minimal screens. Gameplay UI includes lock toggles, spin control UI, and the Shoot confirmation button.
- **Table Setup & Layout**: A procedural approach (`TableSetup.cs`) builds the physics and visual elements of the table at runtime (balls, cushions, pockets) using strict mathematical constants (`TableLayout.cs`) for precise physical placement.

## Communication
- User input triggers UI state changes (e.g., revealing the Shoot button).
- Input Manager sends aim/power/spin parameters to the Trajectory Predictor every frame during the aim phase.
- Pressing "Shoot" passes final parameters to the Cue controller, applying physical forces to the Cue Ball.
- The Game State Manager listens to collision events and pocket triggers to evaluate rules after all balls stop moving.

## Key Reliability Patterns
- **Simulation/Prediction Parity**: The trajectory prediction should ideally use the same physics settings and logic as the actual shot execution.

## Important Tradeoffs
- Simple pass-and-play architecture ignores network synchronization, keeping logic localized and state management straightforward.
