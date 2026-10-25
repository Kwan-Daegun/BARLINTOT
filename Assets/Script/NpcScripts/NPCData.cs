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

    public RuntimeAnimatorController animatorController;

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

/// <summary>
/// What the customer says. LEAVE A LIST EMPTY to use the default lines.
/// You can use these in any line: {name} {deceased} {birth} {death} {message} {payment}
/// </summary>
[System.Serializable]
public struct NPCDialogue
{
    [Tooltip("When you start talking to them at the counter.")]
    [TextArea(1, 3)] public string[] greeting;
    [Tooltip("After you say Yes to taking the order.")]
    [TextArea(1, 3)] public string[] orderAccepted;
    [Tooltip("After you say No to taking the order.")]
    [TextArea(1, 3)] public string[] orderDeclined;
    [Tooltip("When they pay, after you approved every document.")]
    [TextArea(1, 3)] public string[] paid;
    [Tooltip("When you marked a document wrong and they have to leave.")]
    [TextArea(1, 3)] public string[] documentsRejected;
    [Tooltip("When you hand over the finished lapida.")]
    [TextArea(1, 3)] public string[] delivered;
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

    [Header("Dialogue (leave empty to use the default lines)")]
    public NPCDialogue dialogue;

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

    // ---------------------------------------------------------------- Dialogue

    private static readonly string[] DefaultGreeting =
    {
        "Good day. I'm {name}.",
        "I'd like to order a headstone for {deceased}.",
        "{birth} to {death}.",
        "I'd like it to say: \"{message}\""
    };
    private static readonly string[] DefaultOrderAccepted = { "Thank you. Here are their papers." };
    private static readonly string[] DefaultOrderDeclined = { "I see... I'm sorry to have bothered you." };
    private static readonly string[] DefaultPaid = { "Here is {payment}.", "Please take good care of it." };
    private static readonly string[] DefaultDocumentsRejected = { "Is something wrong with the papers?", "...I'll come back another time." };
    private static readonly string[] DefaultDelivered = { "This is it... it's perfect.", "Thank you. Rest in peace, {deceased}." };

    public string[] GreetingLines() { return Lines(dialogue.greeting, DefaultGreeting); }
    public string[] OrderAcceptedLines() { return Lines(dialogue.orderAccepted, DefaultOrderAccepted); }
    public string[] OrderDeclinedLines() { return Lines(dialogue.orderDeclined, DefaultOrderDeclined); }
    public string[] PaidLines() { return Lines(dialogue.paid, DefaultPaid); }
    public string[] DocumentsRejectedLines() { return Lines(dialogue.documentsRejected, DefaultDocumentsRejected); }
    public string[] DeliveredLines() { return Lines(dialogue.delivered, DefaultDelivered); }

    private string[] Lines(string[] custom, string[] defaults)
    {
        string[] source = (custom != null && custom.Length > 0) ? custom : defaults;
        string[] result = new string[source.Length];
        for (int i = 0; i < source.Length; i++) result[i] = Fill(source[i]);
        return result;
    }

    /// <summary>Replaces {name}, {deceased}, {birth}, {death}, {message} and {payment} in a line.</summary>
    public string Fill(string line)
    {
        if (string.IsNullOrEmpty(line)) return string.Empty;

        return line.Replace("{name}", npcName)
                   .Replace("{deceased}", orderDetails.deceasedName)
                   .Replace("{birth}", orderDetails.birthDate)
                   .Replace("{death}", orderDetails.deathDate)
                   .Replace("{message}", orderDetails.message)
                   .Replace("{payment}", PlayerStats.FormatMoney(payment));
    }

    private static bool Same(string a, string b)
    {
        return string.Equals((a ?? "").Trim(), (b ?? "").Trim(), System.StringComparison.OrdinalIgnoreCase);
    }
}