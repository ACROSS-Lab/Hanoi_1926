using System;

[Serializable]
public class DialogueReferenceEntry
{
    public string LocalizationKey;
    public string Reference;
}

[Serializable]
public class POIReferenceEntry
{
    public string ImageKey;
    public string LocalizationKey;
    public string Reference;
}

[Serializable]
public class CreditEntry
{
    public string Role;
    public string Name;
}
