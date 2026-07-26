public enum WhitelistAccessOutcome
{
    Student,
    Guider,
    NotFound,
    Denied
}

public static class WhitelistAccessResultMapper
{
    public static WhitelistAccessOutcome Map(string response)
    {
        switch (response?.Trim())
        {
            case "GRANTED":
            case "GRANTED_NEW":
                return WhitelistAccessOutcome.Student;

            case "GRANTED_TEACHER":
            case "GRANTED_NEW_TEACHER":
                return WhitelistAccessOutcome.Guider;

            case "NOT_FOUND":
                return WhitelistAccessOutcome.NotFound;

            default:
                return WhitelistAccessOutcome.Denied;
        }
    }
}
