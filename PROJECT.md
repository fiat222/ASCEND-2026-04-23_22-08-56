# Project Name

**ASCEND** - Survival Permadeath RPG

---

## Overview

ASCEND is a 3D action-adventure survival RPG with permadeath mechanics. Players develop characters through six core stats, specialize in weapon types, and progress through biome-scaled enemy difficulty. Combat is commitment-based with stamina management and strategic weapon selection.

---

## Genre & Style

| Aspect | Detail |
|--------|--------|
| **Game Type** | Survival Permadeath RPG |
| **Rendering** | Universal Render Pipeline (URP) |
| **Platforms** | Standalone (PC), Android, WebGL |
| **View** | Third-person 3D action |
| **Session** | Multiplayer via PurrNet |

---

## Core Mechanics

### Stat System
Six core stats (1-99) using exponential diminishing returns curve `StatCurve = 1 - e^(-0.05 × stat)`:

| Stat | Primary Effect | Derived Stats |
|------|---------------|---------------|
| VIT | Max HP | Status Resistance |
| END | Max Stamina, Equip Load | - |
| AGI | Stamina Recovery | Movement Speed, Attack Speed |
| STR | Crit Damage | Guard Stability |
| DEX | Crit Rate | Perfect Guard |
| ARC | Magic Damage | Max Mana, Status Resistance |

#### Per-Stat Progress System
Each stat has its own progress bar. Fill the bar → stat levels up (1-99). Progress threshold formula:
```
threshold = progressBase + StatCurve(stat) × progressBonus
```
With configurable k, base, bonus per stat via `StatConfigSO`.

#### Min/Max Clamping
Each stat has configurable min/max range (default 1-99). Values clamped via `CentralizedStatSystem.ClampStat()`.

### Combat System
- **Weapon Types**: OneHand, TwoHand, Staff, Shield, Spear, Bow, Wand, Torch
- **Attack Types**: Slash (0.5x stagger), Blunt (1.0x), Pierce (0.7x), Magic (0.3x), Fire/Ice (0.4x)
- **Blocking/Parry**: RMB block, timing-based parry
- **Guard Break**: Stamina depletion while blocking stuns player
- **Enemy Stagger**: Two-bar system (Interrupt Bar + Stun Bar) with separate stagger damage

### Stagger System
Separate from damage. Formula:
```
rawStagger = baseStagger × (1 + Curve(stat) × staggerScaleValue)
finalStagger = rawStagger × AttackTypeMultiplier
```

---

## Project Structure

```
Assets/
├── Editor/                    # Custom Unity editors (WeaponSOEditor)
├── Import Asset/              # Third-party assets
│   ├── Drakkar/               # GameUtils library (Trails, Extensions)
│   ├── Eric VFX Studio/        # Free Game VFX
│   ├── Kevin Iglesias/         # Human animation packs
│   ├── FullOpaqueSpell/        # Magic spell VFX
│   └── [Other asset packs]
├── Material/
├── Plugins/
│   └── ParrelSync/            # Unity clone manager for multiplayer iteration
├── Prefabs/
├── Purrnet/                   # Network configuration
├── Resources/
├── Scenes/
│   ├── MainMenu.unity          # Main menu / lobby
│   ├── GameScene.unity         # Primary gameplay
│   └── Charactor develop.unity # Developer testing
├── Scripts/
│   ├── Core/
│   │   ├── Enums/
│   │   │   ├── CoreStatType.cs       # VIT, END, AGI, STR, DEX, ARC
│   │   │   └── DerivedStatType.cs    # MaxHP, CritRate, etc.
│   │   ├── Interfaces/
│   │   │   ├── IDamageable.cs        # TakeDamage, TakeStagger
│   │   │   └── IStatable.cs          # GetStat, GainProgress, etc.
│   │   └── Systems/
│   │       └── CentralizedStatSystem.cs  # Formula calculator + events
│   ├── Data/
│   │   ├── StatConfig.cs             # StatCurveCalculator
│   │   └── StatConfigSO.cs          # ScriptableObject with base stats + progress config
│   │   └── WeaponSO.cs             # Weapon data with stagger
│   ├── Dev/
│   │   ├── Player/
│   │   │   ├── PlayerStats.cs       # Stats, progress, damage, events
│   │   │   ├── PlayerCombat.cs      # Weapon handling
│   │   │   ├── PlayerMovement.cs    # Movement + camera
│   │   │   └── GhostAfterimage.cs
│   │   ├── Enemy/
│   │   │   ├── EnemyStats.cs        # HP, DEF, stagger bars, TakeStagger
│   │   │   └── EnemyAI.cs           # Behavior state machine
│   │   └── Weapons/
│   │       ├── WeaponHitbox.cs     # Damage + stagger on hit
│   │       └── [Magic weapons]
│   ├── Network/
│   │   ├── Lobby/
│   │   ├── Player/
│   │   │   └── NetworkPlayerStats.cs
│   │   └── Enemy/
│   │       └── NetworkEnemyStats.cs
│   └── UI/
│       ├── HUD/
│       └── Hotbar/
├── SO/
│   ├── Weapon/                 # Weapon ScriptableObjects
│   └── Stats/                 # StatConfigSO assets (create per character class)
└── STAT_SPEC.md               # Design document
```

---

## Key Systems

| System | Description |
|--------|-------------|
| **CentralizedStatSystem** | Singleton formula calculator. All derived stats (MaxHP, CritRate, etc.) calculated here. Events for stat changes. |
| **IStatable** | Interface for stat access. Implemented by PlayerStats. |
| **StatConfigSO** | ScriptableObject template per character class. Base stats, progress config, min/max ranges. |
| **Player Stats** | Owns raw stats (1-99), progress bars, runtime values (currentHp, currentStamina). Derived stats via CentralizedStatSystem. |
| **Player Combat** | Weapon equip, attack routines, block/parry, animation events |
| **Player Movement** | CharacterController, WASD, dash, jump, Cinemachine camera, spine aim |
| **Enemy AI** | State machine (Idle/Chase/Attack/Dead), detection, pathfinding, stagger reactions |
| **Enemy Stats** | HP, DEF, two stagger bars (Interrupt/Stun), TakeStagger() implementation |
| **Weapon System** | WeaponSO with damage, scaling grades, baseStagger, staggerScaling |
| **WeaponHitbox** | Applies damage + stagger separately on trigger hit |
| **HUD** | HP/Stamina/Mana sliders, tab-toggle stat panel |
| **Hotbar** | 9-slot weapon bar with scroll/number keys, L for off-hand |
| **Network** | PurrNet lobby, host/join with IP, player spawner, TakeStagger sync |

---

## Assets

| Type | Location |
|------|----------|
| Models | Import Asset/ (Feyloom, Korzanowski, etc.) |
| Animations | Import Asset/Kevin Iglesias/ |
| VFX | Import Asset/Eric VFX Studio/, FullOpaqueSpell/ |
| Nature | Import Asset/SimpleNaturePack/ |
| Weapons | SO/Weapon/ (12 defined weapons) |

---

## Scenes

| Scene | Purpose |
|-------|---------|
| **MainMenu.unity** | Entry point with network lobby |
| **GameScene.unity** | Main gameplay with terrain and entities |
| **Charactor develop.unity** | Character testing/debug scene |

---

## Scripts

### Core (`Scripts/Core/`)
- `CoreStatType.cs` - Enum: VIT, END, AGI, STR, DEX, ARC
- `DerivedStatType.cs` - Enum: MaxHP, MaxStamina, CritRate, etc.
- `IDamageable.cs` - Interface: `TakeDamage(float)`, `TakeStagger(float)`
- `IStatable.cs` - Interface: `GetStat()`, `GainProgress()`, `GetProgress()`, `GetThreshold()`
- `CentralizedStatSystem.cs` - Singleton formula calculator + events

### Data (`Scripts/Data/`)
- `StatConfig.cs` - `StatCurveCalculator.Curve()` static method
- `StatConfigSO.cs` - ScriptableObject with base stats, progress config, min/max ranges
- `WeaponSO.cs` - Weapon data: damage, scaling, baseStagger, staggerScaling, GetStaggerMultiplier()

### Player (`Scripts/Dev/Player/`)
- `PlayerStats.cs` - Core stats (1-99), progress bars, derived stats via CentralizedStatSystem, RawDamage(), CalculateStagger(), TakeDamage(), TakeStagger()
- `PlayerCombat.cs` - All weapon handling, blocking, parry, animations
- `PlayerMovement.cs` - Movement, dash, jump, camera
- `GhostAfterimage.cs` - Visual effect

### Enemy (`Scripts/Dev/Enemy/`)
- `EnemyAI.cs` - Behavior state machine, detection, attacks, stagger reactions
- `EnemyStats.cs` - HP, DEF, stagger bars (Interrupt/Stun), TakeDamage(), TakeStagger()

### Weapons (`Scripts/Dev/Weapons/`)
- `WeaponHitbox.cs` - Trigger-based hitbox, applies damage + stagger separately
- `BowStringController.cs` - Bow draw mechanics
- `MagicBallDamage.cs`, `StaffPillar.cs`, `WandLaser.cs` - Magic behaviors

### UI (`Scripts/UI/`)
- `PlayerHUD.cs` - HP/Stamina/Mana display
- `PlayerStatPanel.cs` - Tab-toggle stat display (TMP)
- `HotbarController.cs` - 9-slot weapon bar

### Network (`Scripts/Network/`)
- `NetworkLobby.cs` - Host/join, IP key system
- `PlayerSpawner.cs` - Server-side spawning
- `NetworkPlayerStats.cs` - Damage/stagger sync, stat level sync
- `NetworkEnemyStats.cs` - HP sync, TakeStagger server-authoritative

---

## Data

### ScriptableObjects
- **WeaponSO** (`SO/Weapon/*/`) - 12 weapons: Axe (3), Bow, Hammer (2), Magic (2), Shield, Spear, Sword (multiple)
- **StatConfigSO** (`SO/Stats/*/`) - Per character class template (base stats, progress config, min/max ranges)

### Configs
- `StatConfig.cs` - Stat curve calculations
- `PurrNetSettings.asset` - Network configuration
- `NetworkRules.asset` - Network rules
- `NetworkPrefabs.asset` - Prefab registry

---

## Team Context

| Domain | Agent |
|--------|-------|
| Gameplay Scripts | unity-coder |
| Architecture & Systems | unity-architect |
| Game Design & Balancing | game-designer |
| Level Layout | level-designer |
| Research | research-agent |

---

## Documentation

- `STAT_SPEC.md` - Design document with stat formulas, weapon scaling, enemy stagger, leveling formulas
- `PROJECT.md` - This file

---

## Implementation Status

### Done
- [x] Per-stat progress system (each stat 1-99, progress bar fills → level up)
- [x] CentralizedStatSystem with all derived stat formulas
- [x] Min/max clamping per stat via StatConfigSO
- [x] IStatable interface for unified stat access
- [x] Separate stagger system (damage ≠ stagger)
- [x] WeaponSO with baseStagger, staggerScaling, GetStaggerMultiplier()
- [x] IDamageable with TakeStagger()
- [x] WeaponHitbox applies damage + stagger separately
- [x] EnemyStats stagger bars (Interrupt/Stun)
- [x] Network sync for TakeStagger (enemy, server-auth)

### TODO
- [ ] UI for stat progress bars
- [ ] Crit roll system (random crit check on hit)
- [ ] Crit damage application
- [ ] WeaponHitbox → GainProgress() on player attacks
- [ ] Player stagger (when player can be staggered)
- [ ] StatConfigSO asset creation in Unity
- [ ] CentralizedStatSystem auto-registration scene setup