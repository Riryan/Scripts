using UnityEngine;

// Graveyard database partial stub.
// NOTE:
// Right now, the graveyard system (Player.graveyardTombstoneId)
// is kept purely in memory and is NOT persisted to the SQLite DB.
//
// This file exists only so the Graveyard feature compiles cleanly
// without touching the existing database schema yet.
//
// When you're ready to persist graveyardTombstoneId across logins,
// we will add the real save/load code here and update the 'characters'
// row definition in Database.cs accordingly.

public partial class Database
{
    // Intentionally left empty for now.
}
