using TMPro;
using UnityEngine;

public class CSVReader : MonoBehaviour
{
    // Retained only to keep the existing scene component valid. The form now collects
    // ESE details directly, so no employee CSV is loaded or queried.
    public bool isDataLoaded = true;

    public TextMeshProUGUI loadingStatusText;

    private void Start()
    {
        if (loadingStatusText != null)
            loadingStatusText.gameObject.SetActive(false);
    }
}
