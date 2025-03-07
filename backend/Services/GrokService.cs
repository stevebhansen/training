using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using backend.Models;
using System.Collections.Generic;

namespace backend.Services
{
    public interface IGrokService
    {
        Task<bool> IsCodeTranslationCorrect(string sourceCode, string userCode, string sourceLanguage, string targetLanguage);
        string GetApiKeyInfo();
        Task<Problem> GenerateRandomQuestion(string sourceLanguage, string targetLanguage);
        Task<string> GetTranslationHint(string sourceCode, string sourceLanguage, string targetLanguage);
        Task<string> GetHint(string sourceCode, string targetLanguage);
        Task<string> GetSolutionExplanation(string sourceCode, string targetCode, string sourceLanguage, string targetLanguage);
    }

    public class GrokService : IGrokService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        
        public GrokService(string apiKey)
        {
            // Create a handler that bypasses SSL validation for development
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
            };
            
            // Create a client with our custom handler
            _httpClient = new HttpClient(handler);
            _apiKey = apiKey;
            
            // Set base address and default headers
            _httpClient.BaseAddress = new Uri("https://api.x.ai/");
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
        }

        public async Task<bool> IsCodeTranslationCorrect(string sourceCode, string userCode, string sourceLanguage, string targetLanguage)
        {
            // Option to bypass Grok API completely (for development)
            bool useGrokApi = true; // Set to true to use Grok API
            
            if (useGrokApi)
            {
                var requestData = new
                {
                    model = "grok-2-latest",
                    messages = new[]
                    {
                        new { role = "system", content = "You are a code translation evaluator. Verify if the translation from the source language to the target language is correct. Return a JSON object with a single property 'isCorrect' set to true or false." },
                        new { role = "user", content = $"Source Code ({sourceLanguage}):\n```\n{sourceCode}\n```\n\nUser's Translation ({targetLanguage}):\n```\n{userCode}\n```\n\nIs this translation correct and equivalent?" }
                    },
                    stream = false,
                    temperature = 0
                };

                var content = new StringContent(JsonSerializer.Serialize(requestData), Encoding.UTF8, "application/json");
                
                try
                {
                    Console.WriteLine("Attempting to call Grok API...");
                    var response = await _httpClient.PostAsync("v1/chat/completions", content);
                    
                    if (response.IsSuccessStatusCode)
                    {
                        var responseContent = await response.Content.ReadAsStringAsync();
                        Console.WriteLine($"Grok API Response: {responseContent}");
                        using var doc = JsonDocument.Parse(responseContent);
                        
                        // Extract the evaluation result from the Grok response
                        // This assumes Grok returns a valid JSON with the expected structure
                        var messageContent = doc.RootElement
                            .GetProperty("choices")[0]
                            .GetProperty("message")
                            .GetProperty("content")
                            .GetString();

                        // Parse the result to extract the boolean flag
                        using var resultDoc = JsonDocument.Parse(messageContent);
                        return resultDoc.RootElement.GetProperty("isCorrect").GetBoolean();
                    }
                    
                    // If API call fails, log the error and fall back to the local comparison
                    Console.WriteLine($"Grok API call failed: {response.StatusCode}");
                    if (response.Content != null)
                    {
                        var errorContent = await response.Content.ReadAsStringAsync();
                        Console.WriteLine($"Error details: {errorContent}");
                    }
                    Console.WriteLine("Falling back to local code comparison...");
                }
                catch (Exception ex)
                {
                    // Log the specific SSL error
                    var innerException = ex.InnerException;
                    while (innerException != null)
                    {
                        Console.WriteLine($"Inner Exception: {innerException.Message}");
                        innerException = innerException.InnerException;
                    }
                    
                    Console.WriteLine($"Error checking answer with Grok API: {ex.Message}");
                    Console.WriteLine("Falling back to local code comparison...");
                }
            }
            else 
            {
                Console.WriteLine("Grok API integration disabled. Using local comparison method.");
            }
            
            // Fallback: Enhanced local comparison method
            return CompareCodeTranslations(sourceCode, userCode, sourceLanguage, targetLanguage);
        }
        
        private bool CompareCodeTranslations(string sourceCode, string userCode, string sourceLanguage, string targetLanguage)
        {
            Console.WriteLine("Performing local code comparison...");
            
            // 1. Basic cleanup: Remove whitespace and normalize
            string normalizedUserCode = NormalizeCode(userCode, targetLanguage);
            string expectedCode = NormalizeCode(sourceCode, sourceLanguage);
            
            // For exact match, compare the normalized versions (strict comparison)
            bool exactMatch = string.Equals(normalizedUserCode, expectedCode, StringComparison.InvariantCultureIgnoreCase);
            
            if (exactMatch)
            {
                Console.WriteLine("Exact match found in local comparison.");
                return true;
            }
            
            // 2. Loose comparison: Check if key parts match
            // This is a simplified approach - looking for key patterns
            bool looseMatch = ContainsKeyElements(sourceCode, userCode, sourceLanguage, targetLanguage);
            
            Console.WriteLine($"Local comparison result: {looseMatch}");
            return looseMatch;
        }

        private string NormalizeCode(string code, string language)
        {
            // Remove all whitespace
            string normalized = RemoveWhitespace(code);
            
            // Remove common language-specific syntax that doesn't affect functionality
            if (language == "TypeScript")
            {
                // Remove type annotations
                normalized = System.Text.RegularExpressions.Regex.Replace(normalized, ":[a-zA-Z<>\\[\\]]+", "");
            }
            else if (language == "C#")
            {
                // Remove access modifiers
                normalized = System.Text.RegularExpressions.Regex.Replace(normalized, "public|private|protected|internal", "");
            }
            
            return normalized.ToLowerInvariant();
        }

        private bool ContainsKeyElements(string sourceCode, string userCode, string sourceLanguage, string targetLanguage)
        {
            // Extract key identifiers and patterns from both codes
            // This is a simplified approach - in a real implementation, you'd use proper parsing
            
            // For TypeScript to C# translation
            if (sourceLanguage == "TypeScript" && targetLanguage == "C#")
            {
                // Check if the C# code contains equivalent function names (camelCase to PascalCase)
                var tsFunction = System.Text.RegularExpressions.Regex.Match(sourceCode, "function\\s+([a-zA-Z0-9_]+)");
                if (tsFunction.Success)
                {
                    string funcName = tsFunction.Groups[1].Value;
                    string csharpFuncName = char.ToUpper(funcName[0]) + funcName.Substring(1);
                    
                    return userCode.Contains(csharpFuncName);
                }
            }
            // For C# to TypeScript translation
            else if (sourceLanguage == "C#" && targetLanguage == "TypeScript")
            {
                // Check if the TypeScript code contains equivalent method names (PascalCase to camelCase)
                var csharpMethod = System.Text.RegularExpressions.Regex.Match(sourceCode, "\\s+([A-Z][a-zA-Z0-9_]+)\\s*\\(");
                if (csharpMethod.Success)
                {
                    string methodName = csharpMethod.Groups[1].Value;
                    string tsMethodName = char.ToLower(methodName[0]) + methodName.Substring(1);
                    
                    return userCode.Contains(tsMethodName);
                }
            }
            
            // Default to false if we can't perform specialized comparison
            return false;
        }
        
        private string RemoveWhitespace(string code)
        {
            return new string(code.Where(c => !char.IsWhiteSpace(c)).ToArray());
        }

        public string GetApiKeyInfo()
        {
            if (string.IsNullOrEmpty(_apiKey))
            {
                return "API key is null or empty";
            }
            
            // Only show the first few and last few characters for security
            if (_apiKey.Length > 10)
            {
                return $"API key format: {_apiKey.Substring(0, 6)}...{_apiKey.Substring(_apiKey.Length - 4)} (Length: {_apiKey.Length})";
            }
            
            // If key is too short, just indicate its length
            return $"API key is too short (Length: {_apiKey.Length})";
        }

        public async Task<Problem> GenerateRandomQuestion(string sourceLanguage, string targetLanguage)
        {
            var requestData = new
            {
                model = "grok-2-latest",
                messages = new[]
                {
                    new { role = "system", content = "You are an expert programming interview question generator specializing in code translation. Generate concise interview questions that test a candidate's ability to translate between programming languages." },
                    new { role = "user", content = $"Generate a random code translation interview question from {sourceLanguage} to {targetLanguage}. The question should be realistic for an interview setting and should be moderately challenging but not too complex. The code should be under 15 lines. Return your response in JSON format with the following fields: 'sourceCode', 'expectedTargetCode', 'explanation' (explaining the key concepts and why this is a good interview question), and 'id' (a random number). Format your response as a valid JSON object with these exact field names. Do not include backticks, markdown formatting, or any other text outside of the JSON object." }
                },
                stream = false,
                temperature = 0.7 // Higher temperature for more creativity
            };

            try 
            {
                var content = new StringContent(JsonSerializer.Serialize(requestData), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync("v1/chat/completions", content);
                
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Grok Question Generation Response: {responseContent}");
                    
                    try
                    {
                        using var doc = JsonDocument.Parse(responseContent);
                        var messageContent = doc.RootElement
                            .GetProperty("choices")[0]
                            .GetProperty("message")
                            .GetProperty("content")
                            .GetString();
                            
                        Console.WriteLine($"Raw message content: {messageContent}");
                        
                        // Try to extract the JSON object from the message
                        // Look for a pattern that starts with { and ends with }
                        var jsonMatch = System.Text.RegularExpressions.Regex.Match(messageContent, @"\{.+\}", System.Text.RegularExpressions.RegexOptions.Singleline);
                        
                        if (jsonMatch.Success)
                        {
                            var jsonContent = jsonMatch.Value;
                            Console.WriteLine($"Extracted JSON: {jsonContent}");
                            
                            try 
                            {
                                // Parse the JSON response from Grok
                                using var resultDoc = JsonDocument.Parse(jsonContent);
                                
                                // Create a Problem object from the response
                                return new Problem
                                {
                                    Id = resultDoc.RootElement.TryGetProperty("id", out var id) ? 
                                        id.GetInt32() : new Random().Next(1000, 9999),
                                    SourceLanguage = sourceLanguage,
                                    TargetLanguage = targetLanguage,
                                    SourceCode = resultDoc.RootElement.GetProperty("sourceCode").GetString(),
                                    ExpectedTargetCode = resultDoc.RootElement.GetProperty("expectedTargetCode").GetString(),
                                    Explanation = resultDoc.RootElement.TryGetProperty("explanation", out var explanation) ? 
                                        explanation.GetString() : "Practice translating between languages to understand language-specific paradigms and syntax differences."
                                };
                            }
                            catch (JsonException jsonEx)
                            {
                                Console.WriteLine($"Error parsing extracted JSON: {jsonEx.Message}");
                                Console.WriteLine($"Problematic JSON: {jsonContent}");
                                return GenerateFallbackQuestion(sourceLanguage, targetLanguage);
                            }
                        }
                        else
                        {
                            Console.WriteLine("Could not extract JSON from response");
                            Console.WriteLine($"Full response content: {messageContent}");
                            return GenerateFallbackQuestion(sourceLanguage, targetLanguage);
                        }
                    }
                    catch (Exception parseEx)
                    {
                        Console.WriteLine($"Error parsing API response: {parseEx.Message}");
                        Console.WriteLine($"Raw response: {responseContent}");
                        return GenerateFallbackQuestion(sourceLanguage, targetLanguage);
                    }
                }
                else
                {
                    Console.WriteLine($"Failed to generate question: {response.StatusCode}");
                    if (response.Content != null)
                    {
                        var errorContent = await response.Content.ReadAsStringAsync();
                        Console.WriteLine($"Error details: {errorContent}");
                    }
                    
                    // Return a fallback question if API call fails
                    return GenerateFallbackQuestion(sourceLanguage, targetLanguage);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error generating question: {ex.Message}");
                return GenerateFallbackQuestion(sourceLanguage, targetLanguage);
            }
        }

        public async Task<string> GetTranslationHint(string sourceCode, string sourceLanguage, string targetLanguage)
        {
            // Default hints if Grok API is unavailable or disabled
            var defaultHints = new List<string>
            {
                "Remember that C# uses PascalCase for method names, while TypeScript typically uses camelCase.",
                "C# is statically typed, so you'll need to explicitly declare types for all variables and return values.",
                "In C#, LINQ provides methods like Where(), Select(), and ToList() which correspond to filter(), map(), and array creation in TypeScript.",
                "C# uses properties (get; set;) instead of simple fields for class members.",
                "C# uses 'foreach' instead of 'for...of' that you might use in TypeScript."
            };
            
            bool useGrokApi = false; // Set to true to use Grok API for hints
            
            if (useGrokApi && !string.IsNullOrEmpty(sourceCode))
            {
                try
                {
                    var requestData = new
                    {
                        model = "grok-2-latest",
                        messages = new[]
                        {
                            new { role = "system", content = "You are a helpful programming assistant. Provide a useful hint for translating code from one language to another without giving away the complete solution." },
                            new { role = "user", content = $"I need to translate this {sourceLanguage} code to {targetLanguage}:\n\n```\n{sourceCode}\n```\n\nPlease provide a helpful hint that guides me in the right direction without giving me the complete translation. Focus on a specific challenging aspect of this translation." }
                        },
                        stream = false,
                        temperature = 0.5
                    };

                    var content = new StringContent(JsonSerializer.Serialize(requestData), Encoding.UTF8, "application/json");
                    var response = await _httpClient.PostAsync("v1/chat/completions", content);
                    
                    if (response.IsSuccessStatusCode)
                    {
                        var responseContent = await response.Content.ReadAsStringAsync();
                        using var doc = JsonDocument.Parse(responseContent);
                        var hint = doc.RootElement
                            .GetProperty("choices")[0]
                            .GetProperty("message")
                            .GetProperty("content")
                            .GetString();
                        
                        return hint;
                    }
                    
                    Console.WriteLine($"Failed to generate hint: {response.StatusCode}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error generating hint: {ex.Message}");
                }
            }
            
            // Return a random hint from the default set if Grok API is unavailable
            var random = new Random();
            return defaultHints[random.Next(defaultHints.Count)];
        }

        public async Task<string> GetHint(string sourceCode, string targetLanguage)
        {
            var requestData = new
            {
                model = "grok-2-latest",
                messages = new[]
                {
                    new { role = "system", content = "You are a helpful programming mentor providing hints for code translation tasks without giving away the full solution." },
                    new { role = "user", content = $"I need to translate this code to {targetLanguage}:\n\n```\n{sourceCode}\n```\n\nProvide a helpful hint about how to approach the translation, focusing on a tricky part or important concept, without giving away the complete solution." }
                },
                stream = false,
                temperature = 0.3
            };

            try 
            {
                var content = new StringContent(JsonSerializer.Serialize(requestData), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync("v1/chat/completions", content);
                
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Grok Hint Response: {responseContent}");
                    
                    try
                    {
                        using var doc = JsonDocument.Parse(responseContent);
                        var hint = doc.RootElement
                            .GetProperty("choices")[0]
                            .GetProperty("message")
                            .GetProperty("content")
                            .GetString();
                            
                        return hint;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error parsing hint response: {ex.Message}");
                        Console.WriteLine($"Raw response: {responseContent}");
                        return "Hint: Consider how the language's type system and syntax differ, especially for collection operations and function declarations.";
                    }
                }
                else
                {
                    Console.WriteLine($"Failed to generate hint: {response.StatusCode}");
                    if (response.Content != null)
                    {
                        var errorContent = await response.Content.ReadAsStringAsync();
                        Console.WriteLine($"Error details: {errorContent}");
                    }
                    return "Hint: Remember to adapt the syntax to match the target language's conventions and idioms.";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error generating hint: {ex.Message}");
                return "Hint: Consider how the language's type system differs and adjust your code accordingly.";
            }
        }

        private Problem GenerateFallbackQuestion(string sourceLanguage, string targetLanguage)
        {
            // If Grok API fails, use one of our predefined questions
            if (sourceLanguage == "TypeScript" && targetLanguage == "C#") {
                return new Problem {
                    Id = new Random().Next(1000, 9999),
                    SourceLanguage = "TypeScript",
                    TargetLanguage = "C#",
                    SourceCode = @"
function processList<T>(items: T[], filterFn: (item: T) => boolean): T[] {
    return items
        .filter(filterFn)
        .map(item => item);
}",
                    ExpectedTargetCode = @"
public static List<T> ProcessList<T>(List<T> items, Func<T, bool> filterFn) {
    return items
        .Where(filterFn)
        .ToList();
}",
                    Explanation = "This question tests your understanding of generics and functional programming patterns across languages. Interviewers often ask this type of question to evaluate your ability to translate concepts like higher-order functions, type parameters, and collection operations."
                };
            } else {
                return new Problem {
                    Id = new Random().Next(1000, 9999),
                    SourceLanguage = sourceLanguage,
                    TargetLanguage = targetLanguage,
                    SourceCode = "// Fallback question when API is unavailable\nfunction add(a, b) { return a + b; }",
                    ExpectedTargetCode = "// Fallback target code\npublic int Add(int a, int b) { return a + b; }",
                    Explanation = "This is a simple translation exercise. Understanding basic syntax differences between languages is fundamental for developers who work in multi-language environments."
                };
            }
        }

        public async Task<string> GetSolutionExplanation(string sourceCode, string targetCode, string sourceLanguage, string targetLanguage)
        {
            var requestData = new
            {
                model = "grok-2-latest",
                messages = new[]
                {
                    new { role = "system", content = "You are a helpful programming teacher explaining code translations between different programming languages. Provide clear explanations about why a translated solution is correct and point out key differences between the languages." },
                    new { role = "user", content = $"Here's a {sourceLanguage} code snippet:\n\n```{sourceLanguage}\n{sourceCode}\n```\n\nAnd here's its translation to {targetLanguage}:\n\n```{targetLanguage}\n{targetCode}\n```\n\nPlease explain why this translation is correct. Focus on:\n1. Key syntax differences between the languages\n2. Important paradigm shifts or patterns\n3. Specific implementation details that had to change\n4. Why this is a good translation following best practices" }
                },
                stream = false,
                temperature = 0.3
            };

            try 
            {
                var content = new StringContent(JsonSerializer.Serialize(requestData), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync("v1/chat/completions", content);
                
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Grok Solution Explanation Response: {responseContent}");
                    
                    try
                    {
                        using var doc = JsonDocument.Parse(responseContent);
                        var explanation = doc.RootElement
                            .GetProperty("choices")[0]
                            .GetProperty("message")
                            .GetProperty("content")
                            .GetString();
                            
                        return explanation;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error parsing explanation response: {ex.Message}");
                        Console.WriteLine($"Raw response: {responseContent}");
                        return "This solution correctly translates the original code to the target language while maintaining the same functionality and following language-specific best practices.";
                    }
                }
                else
                {
                    Console.WriteLine($"Failed to generate explanation: {response.StatusCode}");
                    if (response.Content != null)
                    {
                        var errorContent = await response.Content.ReadAsStringAsync();
                        Console.WriteLine($"Error details: {errorContent}");
                    }
                    return "This translation demonstrates proper conversion of the code structure and syntax from the source language to the target language.";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error generating explanation: {ex.Message}");
                return "This is the correct translation based on the standard patterns and conventions of both languages.";
            }
        }
    }
} 