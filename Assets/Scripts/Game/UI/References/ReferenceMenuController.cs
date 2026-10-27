using UnityEngine;
using UnityEngine.UI;

public class ReferenceMenuController : MonoBehaviour
{
    [Header("Sub-Systems")]
    [SerializeField] private TabController tabController;
    [SerializeField] private DialogueReferenceTable dialogueTable;
    [SerializeField] private POIReferenceTable poiTable;
    [SerializeField] private CreditsView creditsView;

    [Header("Navigation / Controls")]
    [SerializeField] private Button closeButton;

    private void Awake()
    {
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(ClosePanel);
        }
    }

    public void OpenPanel()
    {
        gameObject.SetActive(true);
        if (tabController != null)
        {
            tabController.SelectTab(0);
        }
    }

    public void ClosePanel()
    {
        gameObject.SetActive(false);
    }
}
