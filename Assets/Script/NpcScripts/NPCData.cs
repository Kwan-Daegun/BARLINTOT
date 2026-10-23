/*using UnityEngine;

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
}*/

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

/// <summary>
/// What is written on the customer's documents.
/// LEAVE A FIELD BLANK = it matches the order (correct).
/// TYPE A DIFFERENT VALUE = that document is WRONG, so the player should reject.
/// </summary>
[System.Serializable]
public struct DocumentSet
{
    [Header("Customer ID")]
    [Tooltip("Blank = matches the customer's name. Type another name to make this ID wrong.")]
    public string customerIdName;

    [Header("Deceased ID")]
    [Tooltip("Blank = matches the deceased name in the order.")]
    public string deceasedIdName;
    [Tooltip("Blank = matches the birth date in the order.")]
    public string deceasedIdBirthDate;

    [Header("Photo (customer and deceased together)")]
    public Sprite togetherPhoto;

    [Header("Death document")]
    [Tooltip("Blank = matches the deceased name in the order.")]
    public string deathDocName;
    [Tooltip("Blank = matches the death date in the order.")]
    public string deathDocDate;
    [Tooltip("Where they died. Just shown, not checked.")]
    public string deathPlace;
}

[CreateAssetMenu(fileName = "NewNPCData", menuName = "Barlin/NPC Data")]
public class NPCData : ScriptableObject
{
    public string npcName;
    public NPCType type;
    public AnomalyType anomalyType;

    public HeadstoneOrder orderDetails;

    [Header("Documents the customer hands over")]
    public DocumentSet documents;

    public GameObject normalModelPrefab;

    // ---------------------------------------------------------------- Documents

    /// <summary>The documents with blank fields filled in from the order (so blank = correct).</summary>
    public DocumentSet GetDocuments()
    {
        DocumentSet d = documents;

        if (string.IsNullOrWhiteSpace(d.customerIdName)) d.customerIdName = npcName;
        if (string.IsNullOrWhiteSpace(d.deceasedIdName)) d.deceasedIdName = orderDetails.deceasedName;
        if (string.IsNullOrWhiteSpace(d.deceasedIdBirthDate)) d.deceasedIdBirthDate = orderDetails.birthDate;
        if (string.IsNullOrWhiteSpace(d.deathDocName)) d.deathDocName = orderDetails.deceasedName;
        if (string.IsNullOrWhiteSpace(d.deathDocDate)) d.deathDocDate = orderDetails.deathDate;

        return d;
    }

    /// <summary>True when every document agrees with the customer and the order.</summary>
    public bool DocumentsMatch()
    {
        return string.IsNullOrEmpty(DescribeMismatch());
    }

    /// <summary>Explains the first thing that doesn't match, or an empty string if everything matches.</summary>
    public string DescribeMismatch()
    {
        DocumentSet d = GetDocuments();

        if (!Same(d.customerIdName, npcName))
            return $"The customer's ID says {d.customerIdName}, but they said they're {npcName}.";

        if (!Same(d.deceasedIdName, orderDetails.deceasedName))
            return $"The deceased's ID says {d.deceasedIdName}, but the order says {orderDetails.deceasedName}.";

        if (!Same(d.deceasedIdBirthDate, orderDetails.birthDate))
            return $"The deceased's ID birth date is {d.deceasedIdBirthDate}, but the order says {orderDetails.birthDate}.";

        if (!Same(d.deathDocName, orderDetails.deceasedName))
            return $"The death document says {d.deathDocName}, but the order says {orderDetails.deceasedName}.";

        if (!Same(d.deathDocDate, orderDetails.deathDate))
            return $"The death document date is {d.deathDocDate}, but the order says {orderDetails.deathDate}.";

        return string.Empty;
    }

    private static bool Same(string a, string b)
    {
        return string.Equals((a ?? "").Trim(), (b ?? "").Trim(), System.StringComparison.OrdinalIgnoreCase);
    }
}