using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using TMPro;
using System.Linq;

public class CSVReader : MonoBehaviour
{
    private  List<HCPRecord> allRecords = new List<HCPRecord>();
    public bool isDataLoaded = false;

    // Optional: assign this in the inspector if you want a loading message
    public TextMeshProUGUI loadingStatusText;

    IEnumerator Start()
    {
        // Wait one frame before starting
        yield return null;

        if (FormHandler.instance != null && FormHandler.instance.SkipFormEntry)
        {
            isDataLoaded = true;
            if (loadingStatusText != null)
                loadingStatusText.text = "Form entry skipped.";
            yield break;
        }

        // Start loading CSV
        yield return StartCoroutine(LoadCSVAsync());
    }

    IEnumerator LoadCSVAsync()
    {
    if (FormHandler.instance != null && FormHandler.instance.SkipFormEntry)
    {
        isDataLoaded = true;
        yield break;
    }
    
    // Load CSV from Resources (without file extension)
    TextAsset csvFile = Resources.Load<TextAsset>("wbwData");

    if (csvFile == null)
    {
        Debug.LogError("CSV file not found in Resources!");
        yield break;
    }

    // Split lines (use \n and handle \r for safety)
    string[] lines = csvFile.text.Split(new[] { '\n' }, System.StringSplitOptions.RemoveEmptyEntries);

    int lineCount = 0;
    float lastYieldTime = Time.realtimeSinceStartup;
    bool isFirstLine = true;
 

  foreach (string rawLine in lines)
    {
             string line = rawLine.Trim();

            if (isFirstLine)
            {
                isFirstLine = false;
                continue;
            }
            string[] values = line.Split(',');

            // Employee Code, Name, MGR LINE, H.Q., Zone.
            // MGR LINE (index 2) is intentionally not loaded.
            if (values.Length >= 5)
            {
                allRecords.Add(new HCPRecord(
                    values[0].Trim(),
                    values[1].Trim(),
                    values[3].Trim(),
                    values[4].Trim()
                ));
            }


            lineCount++;

            // Yield only if 0.1 seconds have passed since last yield
            if (Time.realtimeSinceStartup - lastYieldTime > 0.1f)
            {
            lastYieldTime = Time.realtimeSinceStartup;
            Debug.Log($"Loading CSV... {lineCount} records");
            yield return null;
            }
        }

    // Local test entries. These do not need to exist in wbwData.csv.
    allRecords.RemoveAll(record =>
        record.employeeCode == "00000000" ||
        record.employeeCode == "11111111");
    allRecords.Add(new HCPRecord(
        "00000000",
        "TEST USER - NO UPLOAD",
        "TEST HQ",
        "TEST ZONE"
    ));
    allRecords.Add(new HCPRecord(
        "11111111",
        "DEBUG TEST USER",
        "DEBUG HQ",
        "DEBUG ZONE"
    ));

    isDataLoaded = true;
    Debug.Log("✅ CSV Loaded. Total Records: " + allRecords.Count);
// TEMP: Clear all records to test if freeze stops
    if (loadingStatusText != null)
        loadingStatusText.text = $"✅ CSV Loaded: {allRecords.Count} records.";
}


   public List<HCPRecord> GetRecordsByEmployeeCode(string code)
{
    string cleanedCode = code.Trim().ToUpperInvariant();

    return allRecords
        .Where(r => r.employeeCode != null &&
                    r.employeeCode.Trim().ToUpperInvariant() == cleanedCode)
        .ToList();
}

}
