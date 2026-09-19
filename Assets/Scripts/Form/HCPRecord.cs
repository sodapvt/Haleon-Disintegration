[System.Serializable]
public class HCPRecord
{
    public string action;
    public string submissionId;
    public string employeeCode;
    public string name;
    public string hq;
    public string zone;
    public string doctorName;

    public HCPRecord(
        string employeeCode,
        string name,
        string hq,
        string zone,
        string submissionId = "",
        string doctorName = "")
    {
        this.action = "create";
        this.submissionId = submissionId;
        this.employeeCode = employeeCode;
        this.name = name;
        this.hq = hq;
        this.zone = zone;
        this.doctorName = doctorName;
    }
}
