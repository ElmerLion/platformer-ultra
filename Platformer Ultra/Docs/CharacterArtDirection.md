# Character Art Direction — Factory-Built Articulated Sculptures

## Visual promise

The player, Saboteur, and Foundry Brute are manufactured from the same hard-surface language as the vertical factory: chamfered housings, tapered armor, exposed bearings, pistons, safety rails, and restrained emissive systems. Their quality comes from silhouette, layered construction, material separation, and motion rather than texture noise or tiny greebles.

The Drone deliberately retains its existing model and flight presentation.

## Shared language

- Major forms use custom chamfered blocks, tapered prisms, and industrial blade meshes rather than raw primitive silhouettes.
- Every limb combines a graphite understructure, a distinct shell, and a steel bearing or collar.
- Detail hierarchy is approximately 70% primary mass, 20% armor and mechanisms, and 10% vents, seams, lights, and blade edges.
- Emissive surfaces remain accents. Cyan communicates player energy; hot orange communicates enemy threat and foundry pressure.
- Models are authored at their gameplay size with feet at local Y = 0 and forward along +Z.
- Animation only moves visual descendants. Gameplay roots, colliders, navigation, targeting points, and leap trajectories remain authoritative.

## Palette

| Role | Color direction |
|---|---|
| Graphite underframe | Near-black blue-gray |
| Safety shell | Factory orange |
| Friendly armor | Mid-value ceramic steel |
| Friendly energy | Cyan |
| Enemy shell | Deep machine purple |
| Enemy heat/threat | Red-orange |
| Bearings and blades | Brushed silver steel |
| Player status | Small operational green accent |

## Player — Maintenance Unit

A compact, non-combat service robot with a protected cyan visor, orange split chest shell, ceramic limbs, magnetic boots, service hands, an asymmetrical antenna, and a visible dash power pack. Two articulated backpack stabilizers open during a dash. The silhouette reads as capable and agile without suggesting the player can fight.

Motion is generated procedurally from actual controller state: distance-matched gait, torso counter-rotation, stabilized head, jump and double-jump impulse, airborne tuck, dash compression and fin deployment, and landing recoil.

## Saboteur — Cutter

A narrow, forward-weighted infiltrator with a wedge head, single orange eye slit, tall shoulder points, segmented spine, thin exposed struts, and long asymmetric knife hands. The right hand carries a forked secondary blade. Threat intent is readable before impact through a crossed-blade anticipation and fast scissor slash.

Motion is generated from motor velocity and combat events: predatory idle ticks, stalking gait, stronger chase lean, knife trails, timed attack anticipation/strike/recovery, damage recoil, and a collapsing death pose.

## Armored enemy — Foundry Brute

A four-meter pressure-driven brute with a broad shoulder arch, protected low head, deep-purple armor, furnace chest core, spinal pressure vessel, twin exhausts, piston limbs, crusher claw, and pile-driver hammer. Its mass is deliberately top-heavy while its feet remain broad enough to visually justify navigation stability.

Motion is generated from motor velocity and combat events: heavy weight transfer, delayed arms, stomping compression, core pressure buildup, regular hammer swing, special crouch/leap/slam pose, mechanical rebound, damage recoil, and a buckling death collapse.

## Technical envelopes

| Character | Collider / controller envelope | Target point |
|---|---|---|
| Player | 1.8 m high, 0.42 m radius | Y 1.2 m |
| Saboteur | 1.8 m high, 0.42 m radius | Y 1.35 m |
| Foundry Brute | 4.2 m high, 1.05 m radius | Y 3.1 m |

The original player, Saboteur, and armored prefabs, definitions, and animation controllers are snapshotted once under `Assets/Game/CharacterArt/Old` before the authoritative prefabs are regenerated.
