using NetAgent.Abstractions.LLM;
using NetAgent.Abstractions.Models;
using NetAgent.Optimization.Interfaces;
using NetAgent.Optimization.Models;
using NetAgent.LLM.Monitoring;
using System.Text.Json;
using System.Text.Json.Serialization;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace NetAgent.Optimization.Optimizers
{
    public class DefaultOptimizer : IOptimizer
    {
        private readonly ILLMProvider _llmProvider;
        private readonly ILLMHealthCheck _healthCheck;

        public DefaultOptimizer(ILLMProvider llmProvider, ILLMHealthCheck healthCheck)
        {
            _llmProvider = llmProvider;
            _healthCheck = healthCheck;
        }

        public async Task<bool> IsHealthyAsync()
        {
            var healthResult = await _healthCheck.CheckHealthAsync(_llmProvider.Name);
            return healthResult.Status == HealthStatus.Healthy;
        }

        public async Task<OptimizationResult> OptimizeAsync(string prompt, string goal, string context)
        {
            if (!await IsHealthyAsync())
            {
                return new OptimizationResult()
                {
                    IsError = true,
                    OptimizedPrompt = "Provider is unhealthy, unable to generate response."
                };
            }

            var optimizationPrompt = @$"As an AI optimizer, analyze and enhance the following prompt while considering its goal and context. Apply self-improving techniques to generate an optimal version.

                                      Original Prompt: {prompt}
                                      Goal: {goal}
                                      Context: {context}

                                      Provide:
                                      1. A significantly improved version of the prompt
                                      2. Detailed reasoning for improvements
                                      3. Learning-based suggestions for future optimizations

                                      Format response:                                            
                                          ""optimizedPrompt"": ""enhanced prompt here"",
                                          ""suggestions"": [
                                              ""improvement reasoning 1"",
                                              ""learning suggestion 1"",
                                              ""future optimization tip 1""
                                          ]
                                      ";

            try
            {
                var response = await _llmProvider.GenerateAsync(new Prompt 
                { 
                    Content = optimizationPrompt
                });

                var jsonResult = System.Text.Json.JsonSerializer.Deserialize<Root>(response.Content);
                // Remove the ```json and ``` markers
                string optimizedPrompt = jsonResult.Candidates[0].Content.Parts[0].Text;
                string cleanedOptimizePrompt = optimizedPrompt.Replace("```json", "").Replace("```", "").Trim();

                var cleanedPrompt = JsonConvert.DeserializeObject<SuggestionResponse>(cleanedOptimizePrompt);


                var result = new OptimizationResult
                {
                    OptimizedPrompt = optimizedPrompt,
                    CleanedStringPrompt = cleanedPrompt.OptimizedPrompt,
                    Suggestions = cleanedPrompt.Suggestions.ToArray()
                };
                if (result != null)
                {
                    return result;
                }
            }
            catch
            {
                // Silently handle exceptions and use fallback
            }

            // Fallback if optimization fails
            return new OptimizationResult
            {
                OptimizedPrompt = prompt,
                Suggestions = new[] { 
                    "Self-improving optimization failed",
                    "Using original prompt as fallback",
                    "Consider reviewing LLM provider settings"
                }
            };
        }
    }

    public class SuggestionResponse
    {
        [JsonPropertyName("optimizedPrompt")]
        public string OptimizedPrompt { get; set; }

        [JsonPropertyName("suggestions")]
        public List<string> Suggestions { get; set; }
    }

    public class Part
    {
        [JsonPropertyName("text")]
        public string Text { get; set; }
    }

    public class Content
    {
        [JsonPropertyName("parts")]
        public List<Part> Parts { get; set; }

        [JsonPropertyName("role")]
        public string Role { get; set; }
    }

    public class Candidate
    {
        [JsonPropertyName("content")]
        public Content Content { get; set; }

        [JsonPropertyName("finishReason")]
        public string FinishReason { get; set; }

        [JsonPropertyName("avgLogprobs")]
        public double AvgLogprobs { get; set; }
    }

    public class PromptTokensDetail
    {
        [JsonPropertyName("modality")]
        public string Modality { get; set; }

        [JsonPropertyName("tokenCount")]
        public int TokenCount { get; set; }
    }

    public class CandidatesTokensDetail
    {
        [JsonPropertyName("modality")]
        public string Modality { get; set; }

        [JsonPropertyName("tokenCount")]
        public int TokenCount { get; set; }
    }

    public class UsageMetadata
    {
        [JsonPropertyName("promptTokenCount")]
        public int PromptTokenCount { get; set; }

        [JsonPropertyName("candidatesTokenCount")]
        public int CandidatesTokenCount { get; set; }

        [JsonPropertyName("totalTokenCount")]
        public int TotalTokenCount { get; set; }

        [JsonPropertyName("promptTokensDetails")]
        public List<PromptTokensDetail> PromptTokensDetails { get; set; }

        [JsonPropertyName("candidatesTokensDetails")]
        public List<CandidatesTokensDetail> CandidatesTokensDetails { get; set; }
    }

    public class Root
    {
        [JsonPropertyName("candidates")]
        public List<Candidate> Candidates { get; set; }

        [JsonPropertyName("usageMetadata")]
        public UsageMetadata UsageMetadata { get; set; }

        [JsonPropertyName("modelVersion")]
        public string ModelVersion { get; set; }
    }

}