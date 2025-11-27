using System.Collections.Generic;
using Mirror;

public struct HarvestSubscribeMessage : NetworkMessage
{
    public List<int> cells; // packed as [cx,cy,cx,cy,...]
}

public struct HarvestSnapshotMessage : NetworkMessage
{
    public int cellX, cellY, prototypeId;
    public List<int> harvestedLocalIndices; // sparse list
    public List<ushort> secsRemaining;      // same length or empty
}

public struct HarvestDeltaMessage : NetworkMessage
{
    public int cellX, cellY, prototypeId;
    public List<int> flippedLocalIndices; // indices that toggled (harvested <-> unharvested)
    public List<byte> newState;           // 0=unharvested,1=harvested (same length)
    public List<ushort> secsRemaining;    // for harvested entries
}

public struct HarvestInteractRequest : NetworkMessage
{
    public int cellX, cellY, prototypeId, localIndex;
}
