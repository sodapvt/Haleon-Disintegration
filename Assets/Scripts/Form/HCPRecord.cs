[System.Serializable]
public class HCPRecord
{
    public string action;
    public string submissionId;
    public string eseName;
    public string hq;
    public string email;
    public string region;
    public string date;

    public HCPRecord(
        string eseName,
        string hq,
        string email,
        string region,
        string date,
        string submissionId = "")
    {
        this.action = "create";
        this.submissionId = submissionId;
        this.eseName = eseName;
        this.hq = hq;
        this.email = email;
        this.region = region;
        this.date = date;
    }
}
