# Point-to-Point Conveyor

`PF_Conveyor_PointToPoint` is one complete, scalable conveyor span. Its Start Endpoint and End Endpoint are the construction inputs; moving either endpoint rebuilds the visuals, collision, trigger zone, rollers, rails, and moving slats between them.

## Level-design workflow

1. Drag `Prefabs/PF_Conveyor_PointToPoint.prefab` into a scene, or choose **GameObject > Factory > Conveyors > Point-to-Point Conveyor**.
2. Select the conveyor and move its endpoint handles in the Scene view.
3. Set speed, direction, width, and operating state on the `ConveyorBelt` component.

The span is always straight. Diagonal and steep spans are supported, and generation deliberately does not test for walls or other level geometry. A belt may pass through other objects when that is the simplest route.

For machine-authored sockets, place `PF_Conveyor_Endpoint`, select two endpoints, make the destination the active selection, then choose **GameObject > Factory > Conveyors > Connect Selected Endpoints**.

## Moving things

- Add `ConveyorPassenger` to a physics object or character that should react to the surface trigger. Rigidbody and CharacterController motors are supported.
- Add `ConveyorCargoFollower` to factory cargo that should follow the exact straight path without physics. Call `BeginFollowing` when a machine outputs the item.
- Player controllers with their own movement integration can read `ConveyorPassenger.CurrentSurfaceVelocity` and combine it with authored movement.

The generated child hierarchy is disposable. Edit the `ConveyorBelt` settings or endpoints and use **Rebuild Conveyor** instead of hand-editing generated pieces.
