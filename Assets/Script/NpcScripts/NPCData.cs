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
/// TYPE A DIFFERENT VALUE = that document is WRONG, so the player should mark it No.
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
    [Tooltip("Tick this if the photo is the WRONG one (for example it shows someone else). The player should mark it No.")]
    public bool photoIsWrong;

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
    /// <summary>Names of the four documents, in the order the checks are reported.</summary>
    public static readonly string[] DocumentNames = { "Customer ID", "Deceased ID", "Photo", "Death document" };

    public string npcName;
    public NPCType type;
    public AnomalyType anomalyType;

    public HeadstoneOrder orderDetails;

    [Tooltip("Money the customer pays when you approve their documents.")]
    public int payment = 500;

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

    // Is each document right? (true = right, false = wrong)
    public bool CustomerIdIsCorrect()
    {
        DocumentSet d = GetDocuments();
        return Same(d.customerIdName, npcName);
    }

    public bool DeceasedIdIsCorrect()
    {
        DocumentSet d = GetDocuments();
        return Same(d.deceasedIdName, orderDetails.deceasedName)
            && Same(d.deceasedIdBirthDate, orderDetails.birthDate);
    }

    public bool PhotoIsCorrect()
    {
        return !documents.photoIsWrong;
    }

    public bool DeathDocIsCorrect()
    {
        DocumentSet d = GetDocuments();
        return Same(d.deathDocName, orderDetails.deceasedName)
            && Same(d.deathDocDate, orderDetails.deathDate);
    }

    /// <summary>The truth for all four documents, in the order of DocumentNames.</summary>
    public bool[] GetDocumentTruth()
    {
        return new[] { CustomerIdIsCorrect(), DeceasedIdIsCorrect(), PhotoIsCorrect(), DeathDocIsCorrect() };
    }

    /// <summary>True for ghost-type customers, or when the documents were wrong (so approving them has consequences).</summary>
    public bool IsHaunted => type == NPCType.Anomaly || !DocumentsMatch();

    /// <summary>True when every document is right.</summary>
    public bool DocumentsMatch()
    {
        return CustomerIdIsCorrect() && DeceasedIdIsCorrect() && PhotoIsCorrect() && DeathDocIsCorrect();
    }

    /// <summary>Explains the first thing that is wrong, or an empty string if everything is right.</summary>
    public string DescribeMismatch()
    {
        DocumentSet d = GetDocuments();

        if (!Same(d.customerIdName, npcName))
            return $"The customer's ID says {d.customerIdName}, but they said they're {npcName}.";

        if (!Same(d.deceasedIdName, orderDetails.deceasedName))
            return $"The deceased's ID says {d.deceasedIdName}, but the order says {orderDetails.deceasedName}.";

        if (!Same(d.deceasedIdBirthDate, orderDetails.birthDate))
            return $"The deceased's ID birth date is {d.deceasedIdBirthDate}, but the order says {orderDetails.birthDate}.";

        if (!PhotoIsCorrect())
            return "The photo doesn't show the right people.";

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