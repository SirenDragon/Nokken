//using System.Collections;
//using System.Collections.Generic;
//using Cinemachine;
//using UnityEngine;
//using UnityEngine.Serialization;

//public class RiverSound : MonoBehaviour
//{
//    [Header("Path")]
//    public CinemachinePathBase m_Path;
//    public GameObject Player;

//    float m_Position;
//    private CinemachinePathBase.PositionUnits m_PositionUnits = CinemachinePathBase.PositionUnits.PathUnits;

//    private void Update()
//    {
//        // Find cloasest point to player along the path
//        SetCartPosition(m_Path.FindClosestPoint(Player.transform.position, 0, -1, 10));
//    }

//    // Set cart's position to closest point
//    void SetCartPosition(float distanceAlongPath)
//    {
//        m_Position = m_Path.StandardizeUnit(distanceAlongPath, m_PositionUnits);
//        transform.position = m_Path.EvaluatePositionAtUnit(m_Position, m_PositionUnits);
//        transform.rotation = m_Path.EvaluateOrientationAtUnit(m_Position, m_PositionUnits);
//    }
//}
