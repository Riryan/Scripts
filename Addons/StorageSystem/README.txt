NEW STORAGE SYSTEM (PHASE 1 - GOLD ONLY)

Files included:
- StorageContainer.cs
- IStorageOwner.cs
- IStorageAccessRule.cs
- NpcStorageAccessRule.cs
- Player_Storage.cs

FEATURES:
- Server authoritative
- Headless safe
- No UI dependency
- No NPC hard dependency
- Fixed capacity (no upgrades)
- Backward compatible

HOW TO USE:
1. Add Player.InitializeStorage(maxSlots, maxGold) during player spawn.
2. Call Cmd_DepositGold / Cmd_WithdrawGold from UI or NPC interaction.
3. Optional: enforce access rules before calling commands.

This replaces the old Warehouse system cleanly.