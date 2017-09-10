namespace wamTest
{
    public class MediaEncodeProgressDto
    {
        public EncodeStatus Status { get; set; }
        public double ProgressPercentage { get; set; }
        public string Errors { get; set; }
    }
}