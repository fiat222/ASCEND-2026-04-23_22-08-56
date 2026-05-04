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

### 2026-04-26
- `WeaponSO.cs` — added `Sprite icon` field; added `Shield`+`Spear` to WeaponType/AttackType enums; added `gripPositionOffset` (Vector3), `gripRotationOffset` (Vector3 euler), `useOffHand` (bool) under [Header("Grip")]
- `HotbarController.cs` — 9 fixed slots; `slotIcons[9]` (Image) + `slotHighlights[9]` (GameObject); `RefreshUI()` sets sprite/enabled per slot; `SetHighlight()` activates selected slot highlight
- `PlayerTest.cs` — added `idleSpearParam`/`attackShieldParam`/`attackSpearParam`; `EquipWeapon` sets idle bools per WeaponType (Shield→1H idle, Spear→spear idle); `GetAttackParam` handles Shield/Spear triggers
- `PlayerTest.cs` — added `offHandSlot` (Transform) for left-hand weapons (e.g. Shield); `EquipWeapon` routes to `offHandSlot` if `so.useOffHand`; applies `gripPositionOffset`/`gripRotationOffset` via `localPosition`/`localEulerAngles` (not SetLocalPositionAndRotation — rotation bug workaround)

### 2026-05-03
- `PlayerCombat.cs` — fixed `HandleStaffInput()`: staff skill (right-click) was missing `SetInAction(false)` + `RestoreIdle()` after animation finished, causing player to be stuck in action state. Replaced inline trigger with `StaffSkillRoutine()` coroutine that waits for clip length then unlocks, matching the pattern used by `AttackRoutine` and other weapon handlers.
- `PlayerCombat.cs` — fixed 1s delay in `AttackRoutine` and `SpearThrowRoutine` using `WaitUntil` + `GetNextAnimatorStateInfo`.
- [x] WeaponSO.cs & PlayerCombat.cs — added `holdPositionOffset` and `holdRotationOffset`. Implemented `UpdateWeaponGrip(bool)` to switch weapon transform offsets when entering/exiting hold states.
- [x] PlayerMovement.cs — Refactored movement to follow camera direction (LookRotation); added Cursor locking; switched to `Input.GetAxis` with damp time for smoother Blend Tree transitions.
- [x] PlayerMovement.cs — Character now always faces crosshair (camera forward) every frame regardless of movement input. `rotationSpeed` bumped to 20 for snappier TPS feel. `sqrMagnitude` threshold tightened to 0.001f.
- [x] Reverted cursor lock back to `PlayerMovement.cs` because the project uses Cinemachine. User will set up a Cinemachine FreeLook or Virtual Camera to handle the mouse input and camera orbiting.

### 2026-05-04 (Late Night)
- Fixed compilation error in `PlayerMovement.cs` by replacing `CinemachineInputAxisState` with the correct `AxisState`.
- Implemented `AxisState` for both X and Y mouse axes.
- Refactored rotation logic: `xAxis` now rotates the entire Player root (Yaw), and `yAxis` rotates only the `cameraTarget` (Pitch).
- Verified Cinemachine Virtual Camera setup: `Binding Mode = Lock to Target`, `Aim = Same as Follow Target`.

### 2026-05-04 (Session)
- `PlayerMovement.cs` — full rewrite for smoother 3PS feel: accel/decel via `MoveTowards`, smooth move dir `Vector3.Lerp`, smooth body rotation while moving, smoothed animator Speed param
- `PlayerMovement.cs` — user reverted to stable version (simplified): single `spineBone`, `paramMoveX/Y/IsRun/Jump/IsGround` as serialized strings, `AxisState` xAxis/yAxis
- `PlayerMovement.cs` — jump fixed: grounded check first → `SetBool(paramIsGround)` → if grounded + Space → `SetTrigger(paramJump)` + set `_verticalVel`
- `PlayerMovement.cs` — dash added (velocity burst): `V` key triggers dash in current move dir (fallback: forward), `dashSpeed=15f`, `dashDuration=0.2f`, `dashCooldown=1f`; dash overrides movement via early `return`
- `PlayerMovement.cs` — run changed from hold-Shift to toggle-Shift (`_isRunning` bool field)

## TODOs / Next Steps
- [ ] Build NetworkLobby UI (Canvas + TMP)
- [ ] Test multiplayer (host + join)
- [ ] Define game concept / mechanics
- [ ] Design level layout (SimpleNaturePack assets)
- [ ] Combat system (hitbox, damage)
- [ ] Steam transport integration
