using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

public class HCPUIController : MonoBehaviour
{
    [FormerlySerializedAs("mrCodeInput")]
    public TMP_InputField employeeCodeInput;

    [FormerlySerializedAs("mrName")]
    public TextMeshProUGUI employeeName;

    public TextMeshProUGUI date;
    public TextMeshProUGUI time;

    [FormerlySerializedAs("HQ")]
    public TextMeshProUGUI hqText;

    [FormerlySerializedAs("Zone")]
    public TextMeshProUGUI zoneText;

    public CSVReader csvReader;
    public bool dataFound = false;
    public HCPRecord SelectedRecord { get; private set; }

    private bool hasSearched = false;

    private void Update()
    {
        if (employeeCodeInput.text.Length >= 8 && !hasSearched)
        {
            hasSearched = true;
            OnSearchClicked();
        }
        else if (employeeCodeInput.text.Length < 8)
        {
            hasSearched = false;
            ResetForm();
        }
    }

    private void ResetForm()
    {
        dataFound = false;
        SelectedRecord = null;
        employeeName.text = "Name: ";
        hqText.text = "HQ: ";
        zoneText.text = "Zone: ";
    }

    public void OnSearchClicked()
    {
        if (!csvReader.isDataLoaded)
        {
            Debug.Log("Data is still loading, please wait...");
            return;
        }

        string enteredEmployeeCode = employeeCodeInput.text.Trim();
        if (string.IsNullOrEmpty(enteredEmployeeCode))
        {
            ResetForm();
            Debug.Log("Please enter an Employee Code.");
            return;
        }

        List<HCPRecord> filtered =
            csvReader.GetRecordsByEmployeeCode(enteredEmployeeCode);

        if (filtered.Count == 0)
        {
            ResetForm();
            Debug.Log($"No record found for Employee Code: {enteredEmployeeCode}");
            return;
        }

        SelectedRecord = filtered[0];
        employeeName.text = "Name: " + SelectedRecord.name;
        hqText.text = "HQ: " + SelectedRecord.hq;
        zoneText.text = "Zone: " + SelectedRecord.zone;
        dataFound = true;

        Debug.Log($"Employee found: {SelectedRecord.employeeCode}");
    }
}
