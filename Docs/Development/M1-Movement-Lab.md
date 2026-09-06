# M1 Movement Lab

The current implementation is revision 2 (M1.1). It proves the complete M1
scope: motor-controlled movement, jump forgiveness, a visual rolling shell,
landing feedback, overhead camera composition, diagnostics, and tunable assets.

Revision 2 corrects grounded friction and introduces the first prefab-backed
segment lifecycle. The player and camera move through stable world coordinates;
three long segment instances provide previous/current/next coverage. After the
rear instance is fully outside the camera, it is destroyed and a newly selected
definition is instantiated ahead.

The training selector currently repeats one `movement_training` definition.
This is deliberately separate from the later seeded planner, which will choose
among many segment definitions while retaining the same runtime lifecycle.

Handling values live in
`Assets/_Project/Definitions/PlayerMovementDefinition.asset`. Camera values live
in `Assets/_Project/Definitions/MovementCameraDefinition.asset`. Change one
value at a time and repeat the same barrier/gap sequence before accepting it.

## Verification

1. With no input, grounded speed approaches 7 m/s.
2. D/Right accelerates toward 11 m/s.
3. A/Left brakes, reverses slowly, and cannot cross the rear viewport boundary.
4. Tapping Space produces a shorter jump than holding it.
5. The barrier and floor gap can be cleared.
6. The camera shows both low walls and never exposes a segment end.
7. The overlay always reports three live segment instances.
8. Segment retirement and replacement appear in searchable Console logs.
9. All EditMode tests pass without new Console errors.

See `M1.1-Segment-Streaming.md` for the architectural reasons and instructions
for adding future segment types.
