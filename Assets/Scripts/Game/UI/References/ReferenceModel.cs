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
    public string LocalizationKey;
    public string NameReference;
    public string ImageReference;
}

[Serializable]
public class CreditEntry
{
    public string Role;
    public string Name;
}
