# ASCEND — Project Context

## Meta
- Date started: 2026-04-23
- Engine: Unity
- Branch: main
- Dev: level1pr0grammer

## Project State
- Fresh Unity project (no custom scripts yet)
- Assets: SimpleNaturePack (trees, rocks, bushes, flowers, grass, mushrooms, ground)
- Scene: Assets/Scenes/SampleScene.unity
- Demo scene: Assets/SimpleNaturePack/Scenes/SimpleNaturePack_Demo.unity

## Tools Active
- RTK v0.37.1 — token-optimized CLI commands (CLAUDE.md injected)
- Caveman ultra mode — max-compressed AI comms

## Progress Log

### 2026-04-23
- Init session, RTK + caveman ultra set up
- PurrNet installed (Unity 2022.3 LTS compatible)
- `CharactorMovement.cs` — WASD move (cam-relative), left click attack, PurrNet NetworkBehaviour
  - Anim params: `IsMove` (bool), `Attack` (bool)
  - Attack: ServerRpc → ObserversRpc → coroutine auto-resets bool after clip length
- `NetworkLobby.cs` — host/join UI, UDP transport IP config, disconnect

## TODOs / Next Steps
- [x] Set up scene: NetworkManager + UDPTransport
- [x] Create player prefab (CharacterController + Animator + CharactorMovement)
- [x] Register player prefab in PurrNet NetworkManager
- [x] WASD movement + left click attack working
- [x] Animation working — Avatar from Free Low Poly Modular Character FBX assigned to Animator
- [x] Animator controller: Base Layer (IsMove bool) + Upper layer (Attack bool, weight=1, upper body mask)
- [ ] Build NetworkLobby UI (Canvas with buttons + TMP)
- [ ] Test multiplayer (host + join)
- [ ] Define game concept / mechanics
- [ ] Design level layout using SimpleNaturePack assets
- [ ] Combat system (hitbox, damage)
- [ ] Steam transport integration
