namespace backend.Models
{
    public class Problem
    {
        public int Id { get; set; }
        public string SourceLanguage { get; set; }
        public string TargetLanguage { get; set; }
        public string SourceCode { get; set; }
        public string ExpectedTargetCode { get; set; }
        public string Explanation { get; set; }
        public string Hint { get; set; }
    }
    public class Submission
    {
        public string UserCode { get; set; }
        public string SourceLanguage { get; set; }
        public string TargetLanguage { get; set; }
        public string OriginalSourceCode { get; set; }
    }
}
