namespace wamTest
{
    public enum EncodeStatus
    {
        NotFound = 0,
        Queued,
        Scheduled,
        Processing,
        Finished,
        Error,
        Canceled,
        Canceling,
        Copying
    }
}