using UnityEngine;

public class VATDebugger : MonoBehaviour
{
    public VATController vat;
    public int stopIndex = 0;

    void Reset()
    {
        vat = GetComponent<VATController>();
    }
}