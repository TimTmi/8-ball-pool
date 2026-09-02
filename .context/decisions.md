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

## [2026-09-01] Aim Line Is One Shape Sweep, Not a Stepped Simulation
**Context**: The trajectory line needs the cue ball's path and where it first makes contact.
**Decision**: `ShotPrediction.Predict` runs a single `Physics2D.CircleCast` of the cue ball's own shape along the aim direction and reads `RaycastHit2D.centroid` as the contact position. Pockets are excluded via `useTriggers = false`, and the cue ball's own collider is skipped or it would hit itself immediately.
**Rationale**: With no spin in the shot yet the path is a straight line, so a sweep is exact where a stepped integration would only approximate it — and it is one physics query per frame instead of dozens. When spin lands (Phase 3), this becomes the first segment of a stepped walk rather than being thrown away.

## [2026-09-01] The Sweep Radius Shortfall Is Kept Tiny
**Context**: The sweep uses slightly less than ball radius so a ball already resting against the cue ball does not register as an instant hit. The size of that shortfall is not cosmetic: it decides where contact is reported, which sets the drawn cut angle.
**Decision**: `CastRadiusScale = 0.999f`, not the 0.98 first written.
**Rationale**: Measured against exact geometry, 0.98 drew the cut up to 5.2° off on thin cuts and missed the thinnest legal cuts entirely (a 0.005u sliver of contact offsets). 0.999 holds the error under 0.2° across the whole range at negligible cost. Thin cuts are where an aim line most needs to be trusted.

## [2026-09-01] Guide Visuals Are Sprite Dots Sized From the Ball
**Context**: The line needs to render with no art in the repository, and the table has just been rescaled once already (ball 0.5u -> 0.2u).
**Decision**: `AimLine` pools small `SpriteRenderer` dots sharing one runtime-generated soft-edged disc, following `PowerBar`'s approach, and its spacing/size defaults derive from `TableLayout.BallDiameter` rather than being absolute.
**Rationale**: Sidesteps `LineRenderer` material and sorting questions under URP 2D, and matches the convention pocket sizes already follow, so the guide rescales with the table instead of silently becoming wrong the next time ball size moves.

## [2026-09-01] The Guide Outline Is Baked Into the Sprite, Not a Second Renderer
**Context**: The aim dots needed a thin outline to stay readable against the felt, and the same discs are drawn in three different colours.
**Decision**: `AimLine.CreateDiscSprite` bakes the rim as black pixels and leaves the middle white. `SpriteRenderer.color` multiplies, so the tint reaches only the fill — black stays black at any colour — and one sprite serves every dot. Rim thickness is passed per disc (`RimFraction`) so the dot and the much larger ghost ball come out the same thickness in world units, not the same fraction.
**Rationale**: The alternative, a second darker renderer behind each dot, would double an already pooled renderer count for a sub-pixel effect. Matching world thickness rather than relative thickness is what makes an outline read as a consistent stroke weight instead of growing with whatever it wraps.

## [2026-09-01] Spin Follows the 8 Ball Pool Guide, Not Real-Table Squirt
**Context**: Side spin can send the cue ball either way. Real level-cue play produces squirt — strike left, the ball leaves to the right. The 8 Ball Pool spin guide states the opposite: "If you add LEFT SPIN to the Cue Ball, it moves towards the Left Hand side from the Cue Ball's point of view."
**Decision**: Follow the guide. Left spin curves the ball left, right spin right, and side spin's headline use is swinging the rebound angle off a cushion.
**Rationale**: This project is modelled on that game, and it was given as the reference for this work. Squirt also makes side spin a pure penalty until throw and cushion effects exist to pay for it. Reversing it is one sign flip in `SpinModel.Curve` and `CushionRebound` if the real-table behaviour is ever wanted instead.

## [2026-09-01] One Spin Model, Shared by the Shot and the Preview
**Context**: An aim line that curves is only worth drawing if it curves the way the ball will. Two implementations would drift apart the first time either was tuned.
**Decision**: `SpinModel` holds every formula — contact velocity, curve, cushion rebound, decay. `CueBallSpin` and `Cushion` apply them to the live shot; `ShotPrediction` steps the same functions for the preview, reading the cue ball's real `linearDamping` and `CueController.SpeedForPower`.
**Rationale**: The prediction/simulation parity design.md asks for, made structural instead of aspirational. Tuning `ContactStrength` moves the drawn curve and the real shot together.

## [2026-09-01] The Rail Owns the Whole Rebound, Spin Included
**Context**: Side spin has to alter the rebound off a cushion, but `Cushion` and `CueBallSpin` would both be handling the same collision, and Unity does not define which component's callback runs first.
**Decision**: `Cushion` reads `CueBallSpin.SideSpin` off the colliding body and applies `SpinModel.CushionRebound` itself, then tells the ball to spend most of that spin.
**Rationale**: One owner for the rail response means no race for the same velocity. `CushionRebound` preserves speed and only turns the direction, since a cushion never hands energy back and the rail's own damping has already been applied by that point.

## [2026-09-01] The Preview Steps at the Physics Rate
**Context**: `ShotPrediction` originally walked at a fixed 0.02s while the project runs physics at 0.01s.
**Decision**: The walk uses `Time.fixedDeltaTime`, capped by step count and by a 15u path length.
**Rationale**: Measured against the same model at the physics rate, the coarser clock integrated damping badly enough to draw the line up to 0.22u — over two ball radii — past where the ball really stops. Matching the clock makes the two identical by construction rather than by luck, and follows the setting if it ever changes again. Cost is 6-95 casts for a realistic shot; only a soft shot into an open table reaches the cap.

## [2026-09-02] Rules Are IShotRule Components Discovered on the Table Object
**Context**: The rules phase needed a home for fouls, groups, and win/loss. The ask was that rules be addable/removable like components.
**Decision**: `IShotRule { void Evaluate(ShotReport, GameState, RuleFindings) }`; `RulesController` discovers active rule components with `GetComponents<IShotRule>()` each shot. Rules are read-only evaluators that accumulate findings on a shared `RuleFindings`; the controller applies them. Match state lives in a pure `GameState` class.
**Rationale**: Adding/removing/toggling a rule becomes an Inspector operation, per the request. Pure evaluation over frozen data keeps the logic unit-testable (coding rules) and removes callback-order races by construction. Rejected: a single `EightBallRules` class (not component-removable) and per-rule verdict merging (rules never read each other's findings; the controller owns application).

## [2026-09-02] Shot Facts Are Frozen by a Recorder, Not Observed Live
**Context**: Rules need pocketings, but Unity gives no ordering guarantee between multiple `OnCollisionEnter2D`/trigger callbacks or multiple settle subscribers.
**Decision**: `ShotRecorder` subscribes to the balls' new `Ball.OnPocketed` events (pocket capture and the out-of-bounds failsafe both funnel through `Ball.Drop()`) and re-emits one `OnShotRecorded(ShotReport)` when the table settles. `CueController` gained `OnShotStarted` (fired on an accepted shot) and lost its private scratch auto-restore — the cue ball stays down until a rule brings it back.
**Rationale**: One coherent report event removes ordering hazards by construction instead of by subscription-order luck. The deleted `ReturnScratchedCueBall` was a rule living in the physics layer; a defensive fallback in `RulesController` restores the cue ball if all scratch rules are removed, so the game cannot stall.

## [2026-09-02] Ball In Hand: Free Placement Anywhere, Mobile-Game 8-Ball Conventions
**Context**: After a scratch the opponent needs the cue ball back. Full standard fouls were out of scope for this pass.
**Decision**: A scratch hands the opponent ball in hand anywhere on the table: `RulesController` restores the cue ball on the head spot, `InputManager` runs a placement mode (ball follows the finger, red tint over illegal spots, commit on legal release), and the shoot button stays locked until `CompleteBallInHand`. The 8-ball follows the 8 Ball Pool mobile game: no called shots, potting the 8 in the same stroke as the last group ball loses, 8 on the break is respotted at the foot spot.
**Rationale**: Matches the reference game's behaviour and keeps the first rules pass small. Head-string-only placement on breaks and shooter's choice on mixed pots are the known simplifications; both would only touch the rule components.

## [2026-09-02] TurnManager Demoted to Verdict Executor
**Context**: The old TurnManager subscribed to `CueController.OnTableSettled` and unconditionally flipped the player — the one rule that existed was hardcoded into the flow.
**Decision**: `TurnManager` lost its `CueController` subscription and now only exposes `BeginTurn(int)`; `RulesController` decides who plays next and calls it. Initial state stays Player 1 with no announcement (as before).
**Rationale**: One decision point (rules) and one executor. A defensive fallback restores a pocketed cue ball on the head spot if the scratch rule component is removed, so the table can never be left cue-less by an Inspector change.
