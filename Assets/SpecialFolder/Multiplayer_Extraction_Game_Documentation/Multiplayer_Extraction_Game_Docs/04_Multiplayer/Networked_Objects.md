# Networked Objects

| Object | Networked? | State | Authority | Prediction | Notes |
|---|---|---|---|---|---|
| Player | Yes | HP, position, rotation, stance, equipment, combat state | TBD | TBD | Horizontal yaw rotates whole body; pitch drives upper-body pose |
| Player Hands / Weapon State | Yes | equipped item, active hand, attack index, attack phase, block state | TBD | TBD | Required for deterministic/validated combat state |
| Melee Attack | Yes | attack ID, phase, start tick, active window, combo transition | TBD | TBD | Server/authority validates hit result |
| Block | Yes | block active, orientation, impact lockout, recovery | TBD | TBD | Direction/angle affect result |
| Enemy | Yes | HP, state, position | TBD | TBD | |
| Loot | Yes | item, state | TBD | No/TBD | |
| Projectile | TBD | TBD | TBD | TBD | |
| Door | TBD | open/closed | TBD | TBD | |
| Trap | TBD | state | TBD | TBD | |
| Extraction | TBD | state/timer | TBD | TBD | |

## Combat Synchronization Requirements
- Weapon configuration must be identical for all clients.
- Attack chain index must be network-consistent.
- Attack phase/timing must be reproducible enough for hit validation.
- Block orientation and active/lockout windows must be represented in network state or derivable from authoritative state.
- A local animation alone must never be the sole source of truth for damage or block outcome.
