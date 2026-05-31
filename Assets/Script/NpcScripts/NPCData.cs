using UnityEngine;

public enum NPCType { Normal, Anomaly }
public enum AnomalyType { None, Physical, NonPhysical }

[System.Serializable]
public struct HeadstoneOrder
{
    public string deceasedName;
    public string birthDate;
    public string deathDate;
    public string message;
}

[CreateAssetMenu(fileName = "NewNPCData", menuName = "Barlin/NPC Data")]
public class NPCData : ScriptableObject
{
    public string npcName;
    public NPCType type;
    public AnomalyType anomalyType;

    public HeadstoneOrder orderDetails;

    public GameObject normalModelPrefab;

    public GameObject physicalMorphPrefab;
    public string logicErrorDescription;
}