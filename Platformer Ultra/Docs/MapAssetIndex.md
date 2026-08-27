# Factory Platformer Map Asset Index

Indexed on 2026-08-26 for Unity 6000.3.8f1 / URP 17.3.0.

## Project baseline

- The project is a fresh Unity 6.3 URP project with one empty gameplay scene at `Assets/Scenes/SampleScene.unity`.
- New Input System 1.18.0 and AI Navigation 2.0.10 are already installed.
- There is no project gameplay code yet. The only scripts under `Assets` are vendor/tutorial helpers.
- All usable art currently comes from three Synty packs under `Assets/Synty`.

## Inventory summary

| Pack | Files | Prefabs | FBX/model files | Materials | Textures | Scenes | Best use |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | --- |
| Polygon Sci-Fi Space | 2,078 | 663 | 686 | 113 | 90 | 4 | Primary factory shell, modular floors/walls, catwalks, machinery dressing, control panels, security props, robots, drones, portal dressing |
| Polygon Construction | 1,688 | 584 | 576 | 17 | 15 | 3 | Generators, elevators, crane, scaffold, beams, tanks, cables, roller tracks, compactors, cargo and industrial clutter |
| Polygon Generic | 1,268 | 495 | 467 | 109 | 118 | 1 | Neutral structural blockout, stairs, pipes, beams, props, generic robot and FX |
| Total under `Assets` | 5,051 | 1,742 | 1,729 | 239 | 224 | 9 | Includes Unity template assets and vendor helpers |

There are no audio files and no standalone `.anim` or Animator Controller assets in the project. Character FBX files may still contain rigs, but locomotion/combat animations are not supplied as a usable animation set.

## Primary modular factory kit

Use `Assets/Synty/PolygonSciFiSpace/Prefabs/Buildings` as the visual backbone so the room reads as one coherent industrial facility.

### Factory shell and vertical layers

- `SM_Bld_Hangar_01`, `SM_Bld_HangerFloor_01`: large-room shell and ground slab candidates.
- `SM_Bld_Floor_01` through `SM_Bld_Floor_011`: modular ground and machine-deck plates.
- `SM_Bld_Floor_Small_01` through `SM_Bld_Floor_Small_08`: compact platforming modules.
- `SM_Bld_Floor_Small_Walkway_02`, `SM_Bld_Floor_Small_Walkway_Endcap_01`: narrow catwalk route.
- `SM_Bld_Floor_Large_Walkway_02`, `SM_Bld_Floor_Large_Walkway_Endcap_01`: main circulation route.
- `SM_Bld_Floor_Small_Hand_Rail_01`, `SM_Bld_Floor_Small_Hand_Rail_End_01`: catwalk safety/readability.
- `SM_Bld_Floor_Small_Hatch_01`: service hatch and enemy-entry dressing.
- `SM_Bld_HangarPlatform_01`, `SM_Bld_Landing_Platform_01`, `SM_Bld_Bridge_Platform_01`: middle/upper landmarks.
- `SM_Bld_Lift_01`, `SM_Bld_Lift_Wall_01`: powered elevator route.
- `SM_Bld_Corridor_Single_Arch_01` through `_03` and `SM_Bld_Corridor_Double_Arch_01` through `_02`: believable structural spans and route frames.
- `SM_Bld_Wall_01` through `_06`, exterior wall variants, pillars, corner pillars and roofs: enclosing the 30–50 m factory hall.
- `SM_Bld_Wall_Mechanical_01`, `SM_Bld_Wall_Mechanical_02`, `SM_Bld_Wall_Tube_01`: machine/service zones.
- `SM_Bld_Ceiling_Pipe_Junction_01`, straight and turn variants: overhead pipe network and traversal silhouettes.

### Factory interaction dressing

Use `Assets/Synty/PolygonSciFiSpace/Prefabs/Props`.

- `SM_Prop_ControlPanel_01` through `_04`: universal E-key terminals for machine activation, routing and repair.
- `SM_Prop_Detail_Machine_01`, panel, pipe, tank, vent and wire variants: kitbash surface language for mine, smelter and assembler.
- `SM_Prop_Mine_01`: visual anchor for the raw-material source.
- `SM_Prop_Stairs_01` through `_04` and `SM_Prop_StairsPlatform_01` through `_02`: readable alternate routes.
- `SM_Prop_AirVent_Large_01`, `SM_Prop_AirVent_Small_01`, escape-pod hatches: believable security spawn points.
- `SM_Prop_Turret_Small_Floor_01`, ceiling turret, large turret and base variants: automated-defense system.
- `SM_Prop_Tank_01`, oxygen tanks, barrels and crates: production/storage dressing and route blockers.

## Secondary industrial machinery kit

Use `Assets/Synty/PolygonConstruction/Prefabs` to make the machines feel functional rather than like spaceship scenery.

### Major landmarks and moving machinery

- `Props/SM_Prop_Generator_Large_01.prefab`: generator progression landmark.
- `Props/SM_Prop_Generator_01.prefab`, `Props/SM_Prop_Generator_Small_01.prefab`: secondary power units.
- `Props/SM_Prop_Elevator_01.prefab`, `_02`, and matching frame prefabs: moving platform/elevator variants.
- `Buildings/SM_Bld_Crane_01.prefab` and `Vehicles/SM_Veh_Crane_01.prefab`: overhead crane silhouette and optional moving cargo route.
- `Props/SM_Prop_Compactor_01.prefab`, `_02`: crusher/hazard visual base.
- `Props/SM_Prop_Roller_Track_01.prefab`, `SM_Prop_Roller_Stand_01.prefab`: conveyor kitbash foundation.
- `Props/SM_Prop_Compressor_01.prefab`: smelter/assembler auxiliary machinery.
- `Buildings/SM_Bld_SmokeStack_01.prefab`: furnace/smelter exhaust landmark.

### Traversal and support dressing

- Scaffold presets, stackable scaffold, scaffold ramps and scaffold ladder: temporary-looking alternate paths.
- I-beams, support-beam extenders, crane sections and concrete pillars: believable platforms and vertical supports.
- Ladders and wood walkways: recovery routes, not primary mandatory jumps.
- Large pipes, pipe corners and pipe stacks: broad traversable pipes and industrial framing.
- Fuel tanks, water tanks and water towers: large readable silhouettes.
- Power boxes, power cables, wire spools and floodlights: dormant-to-powered state communication.
- Pallets, shipping containers, storage shelves, crates, barrels and junk stacks: cover, route shaping and factory density.
- Barricades, traffic barrels, cones and barriers: hazard-language dressing.

## Neutral structural kit

Use `Assets/Synty/PolygonGeneric/Prefabs` where the primary kits lack a clean modular piece.

- `Base`: floors, floor holes, walls, ceilings, pillars, stairs and doors for greybox-compatible structural infill.
- `Building/SM_Gen_Bld_Beam_01` through `_03`: clean structural beams.
- `Building/SM_Gen_Bld_Ladder_01`: generic vertical access.
- `Building/SM_Gen_Bld_Pipe_*`: straight, corner, T, cross, cap and valve pieces for custom pipe runs.
- `Props/SM_Gen_Prop_Chain_*` and `SM_Gen_Prop_Hook_01`: crane/hoist dressing.
- `FX/LightRay_Cube_01`, `LightRay_Round_01`, `FX_SunBeam_01`: restrained atmosphere and powered-state accents.

## Character and enemy candidates

- Player avatar: `Assets/Synty/PolygonConstruction/Prefabs/Characters/SM_Chr_Builder_Overalls_01.prefab` or `SM_Chr_Builder_HighVis_01.prefab` best supports the maintenance-worker fantasy.
- Saboteur: `Assets/Synty/PolygonGeneric/Prefabs/Characters/SM_Gen_Chr_Robot_01.prefab` or `Assets/Synty/PolygonSciFiSpace/Prefabs/Vehicles/SM_Veh_Sweepo_01.prefab`.
- Security drone: `Assets/Synty/PolygonSciFiSpace/Prefabs/Vehicles/SM_Veh_Drone_Repair_01.prefab` or `SM_Veh_Drone_Attach_01.prefab`.
- Heavy unit: `Assets/Synty/PolygonSciFiSpace/Prefabs/Characters/SM_Chr_BR_War_Robot_01.prefab`.
- Maintenance pulse tool visual: construction power-tool props or a compact Sci-Fi Space weapon mesh, recontextualized as an electrical service tool.

## Signage, lighting and state communication

- `Assets/Synty/PolygonSciFiSpace/Prefabs/Signs`: loading bay, engine room, exit, arrow, electric, fire, fix, cog, toxic, number and warning signs.
- Sci-Fi Space light panels/small lights plus Construction floodlights/portable lights: cool dormant lighting, white powered lighting and red/orange alarm accents.
- Sci-Fi Space exhaust, light-ray and ship-trail FX: machine exhaust, portal energy and production motion accents after material conversion.
- Construction colored barrels, cones and safety barriers: hazard markings where custom decals are unnecessary.

## Planned map-role mapping

| Game role | Recommended asset strategy |
| --- | --- |
| Ground layer / mine | Sci-Fi hangar floor and mechanical walls; `SM_Prop_Mine_01`; roller-track conveyor; crates/pallets as stepping geometry |
| Middle layer / smelter | Sci-Fi walkway deck; Construction compressor, smoke stack, tanks and pipes kitbashed into a smelter; control panel terminal |
| Middle layer / generator | `SM_Prop_Generator_Large_01`; power boxes/cables; lift and brighter light bank for the major state transition |
| Upper layer / assembler | Hangar/landing/bridge platforms; detail-machine, tank and panel props composed into a readable assembly cell |
| Portal | Escape-pod hatch/doorframe as frame, emissive light/FX as energy surface, numbered/electric signs, final short catwalk approach |
| Conveyor network | Roller tracks plus modular walkway/floor plates; resource items move by scripted paths rather than rigidbody simulation |
| Piston/crusher | Compactor plus modular beams/plates, animated as a gameplay hazard |
| Furnace | Compressor/smoke-stack/tank kitbash with custom emissive material and timed heat/flame VFX |
| Automated defense | Existing Sci-Fi floor/ceiling turrets and turret bases |
| Enemy entries | Floor hatch, air vents, wall hatches, lift/service doors and drone charging alcoves |

## Known gaps and constraints

- No dedicated conveyor, smelter, assembler, piston, furnace or portal prefab exists. These require reusable project-owned prefabs assembled from the indexed parts.
- No audio is supplied. Factory ambience, machinery, player, combat, alarms and portal audio will need another source or generated placeholders.
- No standalone animation library is supplied. Player and ground-enemy locomotion/combat animations need a separate solution; drone motion can be procedural.
- Vendor prefabs should be treated as visual sources. Gameplay colliders, pivots, LOD/static flags, interaction anchors and NavMesh behavior must be validated on each project-owned wrapper prefab.
- Keep Synty vendor content unchanged. Build project-owned prefabs and materials in a separate `Assets/Game` hierarchy so upgrades and experimentation remain safe.

## Useful overview scenes

- `Assets/Synty/PolygonSciFiSpace/Scenes/Overview.unity`
- `Assets/Synty/PolygonSciFiSpace/Scenes/Demo_Interior.unity`
- `Assets/Synty/PolygonConstruction/Scenes/Overview.unity`
- `Assets/Synty/PolygonGeneric/Scenes/Overview.unity`

These are reference/showcase scenes only and should not become the gameplay scene.
