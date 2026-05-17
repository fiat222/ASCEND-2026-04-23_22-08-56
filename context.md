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

### 2026-05-05
- Explored Free Slash VFX asset — Projectile prefabs in `Prefabs/Projectiles/`, `Projectile.cs` handles forward movement + distance stop + spawn on finish. Razengan textures already in asset.
- `PlayerCombat.cs` — added `ExecuteMagicBall()` (animation event method): instantiates magic ball prefab facing crosshair, sets speed/distance, calls `Projectile.Fire()`. Fields: `magicBallPrefab`, `magicPoint`, `magicBallSpeed`, `magicBallDistance`.
- `PlayerCombat.cs` — added `IsShieldIdle` parameter (`idleShieldParam`). Shield removed from `idle1HParam` group. All idle clear/restore/equip updated.
- `PlayerCombat.cs` — Staff + Wand auto-assign `magicPoint` from child named `MagicPoint` on equip (same pattern as Bow → ShotPoint).

## Animator Transition Rules

### Pattern (ALL states follow this)
**Base Layer (idle/block states):**
- Any State → `<State>`: condition `Is<X>Idle = false` + state-specific bool/trigger
- `IsInAction` is SET in code as a flag only — NOT used as animator transition condition
- Exit: `RestoreIdle()` sets correct idle bool true → animator flows back naturally (no hardcoded "→ Idle" transition needed)
- No Exit Time on entry. Transition duration ~0.1–0.15s.

**Attack Layer (layer 1):**
- `Empty` → `<AttackState>`: trigger condition only
- `<AttackState>` → `Empty`: Exit Time = 1.0, no other condition

### Block params + their idle guard
| Param | Idle guard | WeaponType |
|---|---|---|
| `Is1HBlock` | `Is1HIdle = false` | OneHand (no off-hand) |
| `Is2HBlock` | `Is2HIdle = false` | TwoHand |
| `IsShieldBlock` | `IsShieldIdle = false` | Shield (main hand) |
| `IsShieldSwordBlock` | `Is1HIdle = false` | OneHand + Shield off-hand |

### Attack layer triggers
| Trigger | State name |
|---|---|
| `Attack1H` | `Attack_1H` |
| `Attack2H` | `Attack_2H` |
| `AttackMagic` | `Attack_Magic` |
| `AttackShield` | `Attack_Shield` |
| `AttackSpear` | `Attack_Spear` |
| `Attack_Bow` | `Attack_Bow` |
| `ShieldSwordParry` | `ShieldSword_Parry` |
| `Skill_Staff` | `Staff_Skill` |
| `Skill_Wand` | `Wand_Skill` |

## TODOs / Next Steps
- [ ] Magic projectile: destroy on hit + play Impact prefab (`SpawnWhenFinish` on `Projectile` component, or `OnCollisionEnter` script)
- [ ] Build NetworkLobby UI (Canvas + TMP)
- [ ] Test multiplayer (host + join)
- [ ] Define game concept / mechanics
- [ ] Design level layout (SimpleNaturePack assets)
- [x] Combat system Phase 1 — stat system, hitbox, damage formula (2026-05-16)
- [ ] Steam transport integration
- [ ] Create NetworkPlayer prefab — add NetworkPlayerMovement + NetworkPlayerCombat, configure fields
- [ ] Add .meta files for new folders so Unity tracks them (auto on reimport)

### 2026-05-10
- Scripts refactored to professional folder structure (no more flat Test/)
  - `Core/Interfaces/` — IDamageable.cs
  - `Data/` — WeaponSO.cs (unchanged)
  - `Dev/Player/` — PlayerMovement.cs, PlayerCombat.cs (local/no-network dev)
  - `Dev/Weapons/` — BowStringController.cs, ProjectilePrefab.cs
  - `Dev/Weapons/Magic/` — StaffSpell.cs, StaffPillar.cs, WandLaser.cs
  - `Network/Lobby/` — NetworkLobby.cs, PlayerSpawner.cs
  - `Network/Player/` — CharactorMovement.cs (old prototype), NetworkPlayerMovement.cs (NEW), NetworkPlayerCombat.cs (NEW)
  - `UI/Hotbar/` — HotbarController.cs (unchanged)
- Deleted: `Test/CameraController.cs` (empty), `Test/PlayerTest(Old).cs` (superseded)
- GUIDs preserved — .cs + .meta moved together
- `GameOverview.md` created at root, gitignored — full game concept doc
- `NetworkPlayerMovement.cs` — full port of PlayerMovement to PurrNet NetworkBehaviour
  - isOwner guard, OnSpawned, SyncMoveAnimServerRpc (blend tree x/y/run), SyncJumpServerRpc
- `NetworkPlayerCombat.cs` — full port of PlayerCombat to PurrNet NetworkBehaviour
  - isOwner guard, OnSpawned, RequestAttackServerRpc → PlayAttackObserversRpc
  - All weapon types supported; weapon equip local-only (visual) for now

## Script → Prefab Map
| Script | Prefab |
|--------|--------|
| PlayerMovement.cs | Player root |
| PlayerCombat.cs | Player root |
| HotbarController.cs | HUD Canvas |
| BowStringController.cs | Bow weapon prefab |
| ProjectilePrefab.cs | Arrow prefab; SpearThrow prefab |
| StaffSpell.cs | Staff weapon prefab |
| StaffPillar.cs | StaffImpact VFX prefab |
| WandLaser.cs | Wand weapon prefab |
| IDamageable.cs | Interface — impl on Enemy/destructible prefabs |
| WeaponSO.cs | ScriptableObject (Assets/Data/, not a prefab) |
| NetworkPlayerMovement.cs | NetworkPlayer prefab (PurrNet-spawned) |
| NetworkPlayerCombat.cs | NetworkPlayer prefab (PurrNet-spawned) |
| NetworkLobby.cs | NetworkManager scene GameObject |
| PlayerSpawner.cs | NetworkManager scene GameObject |
| StatConfig.cs | ScriptableObject-free static helper (Data/) |
| PlayerStats.cs | Player root (alongside PlayerCombat) |
| EnemyStats.cs | Skeleton prefab root |
| EnemyAI.cs | Skeleton prefab root |
| WeaponHitbox.cs | Child of weapon prefab (needs Collider) |

### 2026-05-16
- STAT_SPEC.md implemented — Phase 1 (local dev combat)
  - `Data/StatConfig.cs` — StatConfig class + StatCurveCalculator static helper
  - `Data/WeaponSO.cs` — added ScalingGrade enum + strScaling/dexScaling/arcScaling fields + StrScale/DexScale/ArcScale properties
  - `Dev/Player/PlayerStats.cs` — VIT/STR/DEX/ARC/AGI/END 1-99, MaxHP/MaxStamina/MaxMana derived stats, RawDamage(WeaponSO), TakeDamage, OnDied event
  - `Dev/Enemy/EnemyStats.cs` — HP, DEF, interrupt bar, stun bar (STAT_SPEC compliant), IDamageable impl with DEF formula, world-space HP slider support, OnDied event
  - `Dev/Enemy/EnemyAI.cs` — full rewrite: uses EnemyStats, subscribes OnDied, death state, finds nearest player from all tagged "Player"
  - `Dev/Weapons/WeaponHitbox.cs` — trigger hitbox on weapon child, EnableHitbox/DisableHitbox (animation events), one-hit-per-swing HashSet, calls IDamageable.TakeDamage with PlayerStats.RawDamage
  - `Dev/Player/PlayerCombat.cs` — added PlayerStats ref, _hitbox field, OnHitboxOpen/OnHitboxClose anim event methods, hitbox.Setup() on equip
- Next: Phase 2 — NetworkEnemyStats + NetworkPlayerCombat damage sync
- Pending Unity Editor work: add WeaponHitbox child + Trigger Collider to weapon prefabs; add OnHitboxOpen/OnHitboxClose anim events to attack clips; add EnemyStats + world HP slider to Skeleton prefab

### 2026-05-16 (Phase 2 — local dev)
- Phase 2 combat implemented (all code done, Unity Editor setup manual):
  - `Dev/Enemy/EnemyStats.cs` — added OnHit, OnInterrupted, OnStunned events
  - `Dev/Enemy/EnemyAI.cs` — hit reaction (GetDamaged trigger, OnDestroy unsub, ResetTrigger guard), death anim (Die trigger + OnDeathAnimEnd anim event + SinkAndDestroy coroutine, _sinking guard)
  - `Dev/Player/PlayerStats.cs` — OnHpChanged event, 5 new derived props (StaminaRecovery, EquipLoad, CritRate, CritDamage, MovementSpeed)
  - `UI/HUD/PlayerHUD.cs` — NEW: subscribes OnHpChanged, drives normalized Slider
  - `UI/HUD/PlayerStatPanel.cs` — NEW: TAB toggle, shows 6 core stats + 8 derived stats via TMP_Text
- Pending Unity Editor work (Phase 2):
  - Enemy animator: GetDamaged trigger + state + Any State transition
  - Enemy animator: Die trigger + Death state + Any State transition
  - Death clip: OnDeathAnimEnd animation event at last frame
  - Canvas: HP Slider (bottom-left) + PlayerHUD component assigned
  - Canvas: Stat Panel (14 TMP_Text fields) + PlayerStatPanel component assigned
- Additional fixes this session:
  - GhostAfterimage: URP _BaseColor fix, mesh+mat Destroy, IsDashing flag (no false trigger on skeleton collision)
  - EnemyAI: skeleton stops at attackRange, no longer walks into player
  - PlayerStatPanel: Tab key (script must be on always-active parent, not panel itself), stat labels prefixed
- [x] Phase 2 DONE (2026-05-16)
- Next: Phase 3 — 1H deflect (parry), 1H+Shield block/defense

### 2026-05-16 (Phase 3 — local dev)
- Phase 3 combat implemented (all code done, Unity Editor setup manual):
  - `Data/WeaponSO.cs` — added `baseGuardStability` field (shields set this)
  - `Dev/Player/PlayerStats.cs` — added `currentStamina` runtime, `IsBlocking`/`IsParrying` flags, `GuardStability` prop (STRCurve×50 + weapon base), `SetGuardBase()`, stamina recovery in Update, `OnParried`/`OnGuardBreak`/`OnStaminaChanged` events, guard logic in TakeDamage (parry=0 dmg, block=stamina drain per GuardCost formula, guard break=fall through full dmg)
  - `Dev/Player/PlayerCombat.cs` — 1H: Track1HBlock → Track1HParry (0.3s window coroutine), parry window sets IsParrying; Shield+Sword: sets playerStats.IsBlocking on RMB toggle; subscribes OnGuardBreak → HandleGuardBreak → GuardBreakRoutine (trigger + anim wait + unlock); EquipWeapon calls SetGuardBase; InterruptUpperOnly clears parry/block flags
  - `UI/HUD/PlayerHUD.cs` — added staminaSlider + manaSlider (mana static=full, system pending)
- Pending Unity Editor work (Phase 3):
  - Animator: add `GuardBreak` trigger param
  - Attack Layer: `Empty → Guard_Break` state (GuardBreak trigger), `Guard_Break → Empty` (Exit Time 1.0)
  - Guard_Break clip needed (stumble/stagger — can reuse GetDamaged clip)
  - Canvas: add Stamina Slider (green) + Mana Slider (blue) below HP bar; assign in PlayerHUD Inspector
  - Shield WeaponSO: set `baseGuardStability` (30–60 recommended)
- [x] Phase 3 DONE (2026-05-16)
- Next: Phase 4 — Network HP sync, client join, damage over wire (PurrNet)

### 2026-05-17 (WeaponSO projectile refactor)
- `Data/WeaponSO.cs` — removed `throwPrefab`; projectile section now unified: `projectilePrefab` (prefab to spawn), `projectileForce` (physics: Bow/Spear), `projectileSpeed`+`projectileRange` (Projectile.cs: Staff/Wand), `manaCost` (Staff/Wand)
- `Editor/WeaponSOEditor.cs` — NEW: custom Inspector hides Projectile section for non-projectile types (OneHand/TwoHand/Shield/Torch); Bow/Spear show force; Staff/Wand show speed+range+manaCost
- `Dev/Player/PlayerCombat.cs` — removed hardcoded `arrowPrefab`, `magicBallPrefab`, `arrowForce`, `spearForce`, `magicBallManaCost`; all Execute methods now read from `_equippedSO`
- `Network/Player/NetworkPlayerCombat.cs` — same refactor: removed hardcoded prefab/force/cost fields; Execute methods use `_equippedSO`
- TODO (not configured): Staff skill (`StaffSpell` component + charge release) and Wand skill (`WandLaser` component + laser hold) have no WeaponSO config yet — need dedicated skill prefab refs or component-level setup per weapon prefab

### 2026-05-17 (Phase 4 — host-authoritative damage)
- `Network/Enemy/NetworkEnemyStats.cs` — NEW: NetworkBehaviour+IDamageable; TakeDamage routes to host via [ServerRpc(requireOwnership:false)]; host calls EnemyStats.TakeDamage then [ObserversRpc] SyncHpRpc → EnemyStats.SyncHp on all clients
- `Network/Player/NetworkPlayerStats.cs` — NEW: same pattern for player HP; SyncHpRpc calls PlayerStats.SyncHp → OnHpChanged fires → PlayerHUD updates unchanged
- `Dev/Enemy/EnemyStats.cs` — added SyncHp(float hp): sets currentHp, RefreshSlider, fires OnHit + OnDied (EnemyAI death anim fires on all clients)
- `Dev/Player/PlayerStats.cs` — added SyncHp(float hp): sets currentHp, fires OnHpChanged + OnDied
- `Dev/Enemy/EnemyAI.cs` — host-only guard: `if (!isOffline && !isServer) return` skips AI on pure clients
- [x] Phase 4 code done
- Pending Unity Editor work (Phase 4):
  - Enemy prefab: add NetworkIdentity + NetworkTransform + NetworkEnemyStats; reorder NetworkEnemyStats ABOVE EnemyStats
  - Player prefab (networked): add NetworkPlayerStats; reorder ABOVE PlayerStats
  - Pre-placed scene enemies: add NetworkIdentity (PurrNet auto-registers)
  - Runtime enemies: host spawns via NetworkManager.main.Spawn()

### 2026-05-17 (Phase 1+2 — NetworkTest scene + lobby key)
- `Network/Lobby/NetworkLobby.cs` — REWRITTEN: key-based lobby system; Host clicks → gets local IP+port → encodes to base36 key (~10 chars) → displays key; Client types key → decodes IP:port → connects; Stop button closes server+client
- Key encoding: IP (4 bytes) + port (2 bytes) = 6 bytes → base36 string; encode/decode static methods in NetworkLobby; works on LAN only (no relay)
- Deleted: PasskeyAuth.cs (not needed — key IS the join credential)
- Pending Unity Editor work (Phase 1+2):
  - Create `Assets/Scenes/NetworkTest.unity` (duplicate SampleScene or build fresh)
  - Canvas: Host button, Join button, Stop button, Key InputField (client), Key display Text (host), Status text
  - NetworkLobby Inspector: assign all 6 fields
  - Phase 4 Editor setup still required first (NetworkIdentity on Enemy/Player prefabs)
