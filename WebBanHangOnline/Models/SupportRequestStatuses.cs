namespace WebBanHangOnline.Models;

public static class SupportRequestStatuses
{
    public const string New = "New";
    public const string InProgress = "InProgress";
    public const string Done = "Done";

    public static readonly string[] All =
    {
        New,
        InProgress,
        Done
    };
}
