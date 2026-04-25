# ASCEND — Project Context

## Meta
- Date started: 2026-04-23
- Engine: Unity
- Branch: main
- Dev: level1pr0grammer

## Project State
- PlayerTest.cs — local test movement script (non-networked)
- CharactorMovement.cs — networked movement (PurrNet)
- Assets: SimpleNaturePack, Free Low Poly Modular Character Pack
- Scene: Assets/Scenes/SampleScene.unity

## Tools Active
- RTK — token-optimized CLI (CLAUDE.md injected), always prefix `rtk`
- Caveman full mode — compressed AI comms every session
- PlanMode — required before any build/major implementation

## Progress Log

### 2026-04-23
- Init session, RTK + caveman ultra set up
- PurrNet installed (Unity 2022.3 LTS compatible)
- `CharactorMovement.cs` — WASD move (cam-relative), left click attack, PurrNet NetworkBehaviour
  - Anim params: `IsMove` (bool), `Attack` (bool)
  - Attack: ServerRpc → ObserversRpc → coroutine auto-resets bool after clip length
- `NetworkLobby.cs` — host/join UI, UDP transport IP config, disconnect

### 2026-04-25
- `PlayerTest.cs` updated for blend tree lower layer
  - Replaced `IsMove` bool → `MoveX` (float) + `MoveY` (float) local-space direction
  - Added `IsRun` bool — toggled by Shift (left or right), true only when moving
  - Added `runSpeed = 8f` field; speed switches walk/run on toggle
  - Blend tree: set 2D params to MoveX/MoveY floats in Animator Controller
- `context.md` created, gitignored, workflow rules added to CLAUDE.md
- `WeaponSO.cs` — ScriptableObject: weaponName, weaponType (enum), damage, prefab; AttackType property maps type→1H/2H/Magic
- `HotbarController.cs` — now holds `List<WeaponSO>` (was `List<GameObject>`)
- `PlayerTest.cs` — `EquipWeapon(WeaponSO)`, `GetAttackParam()` picks trigger by AttackType; animator params: Attack1H/Attack2H/AttackMagic

### 2026-04-25 (continued)
- `HotbarController.cs` — weapon prefab list, scroll wheel + keys 1-9 selection, `OnSlotChanged` event
- `PlayerTest.cs` — removed E-key equip; subscribes to `OnSlotChanged`, equips/unequips via `EquipWeapon(prefab)`

## TODOs / Next Steps
- [x] WASD movement + left click attack (networked)
- [x] Animator: Base Layer blend tree (MoveX/MoveY) + Upper layer (Attack, weight=1, upper body mask)
- [x] Walk/Run toggle (Shift) via IsRun param
- [ ] Build NetworkLobby UI (Canvas + TMP)
- [ ] Test multiplayer (host + join)
- [ ] Define game concept / mechanics
- [ ] Design level layout (SimpleNaturePack assets)
- [ ] Combat system (hitbox, damage)
- [ ] Steam transport integration
