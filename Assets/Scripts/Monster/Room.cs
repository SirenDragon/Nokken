using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Room
{
    [Tooltip("Name of the room (for organizational purposes).")]
    public string roomName;

    [Tooltip("Manually set the index of this room.")]
    public int roomIndex;

    [Tooltip("List of stage positions in this room (in order).")]
    public List<Transform> stagePositions = new List<Transform>();
}