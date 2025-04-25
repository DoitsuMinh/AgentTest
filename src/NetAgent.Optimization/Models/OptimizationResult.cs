namespace NetAgent.Optimization.Models
{
    public class OptimizationResult
    {
        public bool IsError { get; set; } = false;
        public string OptimizedPrompt { get; set; } = string.Empty;
        public string CleanedStringPrompt { get; set; } = string.Empty;
        public string[] Suggestions { get; set; } = Array.Empty<string>();
    }


}
