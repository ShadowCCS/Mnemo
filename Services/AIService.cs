using MnemoProject.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

public class AIService
{
    private readonly IModelProvider _aiProvider;
    private readonly AIWorker _worker;

    public AIService(IModelProvider aiProvider, AIWorker worker)
    {
        _aiProvider = aiProvider;
        _worker = worker;
    }

    public async Task GenerateLearningPathOutline(string userInput, Action<string> onComplete)
    {
        await _worker.Enqueue(async () =>
        {
            string basePrompt = $@"Create 3-7 units based on the following content. You should create this using this exact format: 
{{
  ""title"": ""Title of the learning path for example The Human Respiratory System"",
  ""units"": [
    {{
      ""unit_number"": 1,
      ""title"": ""Title of the unit, for example *Introduction*"",
      ""TheoryContent"": ""A short description of what should be covered""
    }},
  ]
}}";

            string prompt = basePrompt + "Content: " + userInput;
            string response = await _aiProvider.GenerateTextAsync(prompt);

            List<string> units = response.Split('\n', StringSplitOptions.RemoveEmptyEntries).ToList();
            onComplete(response);
        });
    }

    public async Task GenerateUnitContent(string userInput, string unitTitle, Action<string> onComplete)
    {
        await _worker.Enqueue(async () =>
        {
            string prompt = $"Generate an educational text-based study guide with clearly defined sections. The content should be structured, informative, and well-organized, use markdown. Use these markdown elements: # to ###### for headers, ** for bold, * for italic, ``` for code blocks, > for quotes, - or * for lists, [text](url) for links, ![alt](url) for images, tables with |, and ~~text~~ for strikethrough.\n\n"
                + $"Title: {unitTitle}\n\n"
                + "Introduction:\n"
                + "- Provide a brief introduction to the topic and explain why it is important.\n\n"
                + "Key Components:\n"
                + "- List and describe the major parts, concepts, or elements related to the topic.\n\n"
                + "Functions and Processes:\n"
                + "- Explain how the system or topic works, including its main functions.\n\n"
                + "Importance:\n"
                + "- Describe why this topic is essential and how it affects related areas.\n\n"
                + "Common Issues or Challenges:\n"
                + "- If applicable, mention common problems, disorders, or challenges associated with this topic.\n\n"
                + "Summary:\n"
                + "- Provide a concise conclusion that reinforces key takeaways.\n\n"
                + $"Base the knowledge around this input:\n{userInput}";

            string response = await _aiProvider.GenerateTextAsync(prompt);
            onComplete(response);
        });
    }
    
    public async Task GenerateFlashcards(string unitContent, Action<string> onComplete)
    {
        await _worker.Enqueue(async () =>
        {
            if (string.IsNullOrEmpty(unitContent))
            {
                System.Diagnostics.Debug.WriteLine("WARNING: Empty unit content passed to GenerateFlashcards");
                unitContent = "This is a sample unit for demonstration purposes.";
            }
                
            System.Diagnostics.Debug.WriteLine($"Generating flashcards for content length: {unitContent.Length}");
            
            string prompt = $@"Based on the following unit content, generate 10 specific flashcards in a question and answer format. 
The flashcards should cover the most important concepts, facts, and terminology from the unit content provided.
Each flashcard should have a clear, concise question on one side and a brief, accurate answer on the other.
Questions should focus on testing understanding of the topics covered in the unit content.

IMPORTANT: 
1. Make sure questions are directly related to the unit content provided
2. Keep answers concise and focused
3. Include a mix of definition, concept, and application questions
4. Format your response EXACTLY as a valid JSON array of flashcard objects with 'question' and 'answer' properties

Example format:
[
  {{
    ""question"": ""What is the primary function of the respiratory system?"",
    ""answer"": ""The primary function of the respiratory system is gas exchange - bringing oxygen into the body and removing carbon dioxide.""
  }},
  {{
    ""question"": ""What are alveoli?"",
    ""answer"": ""Alveoli are tiny air sacs in the lungs where gas exchange occurs between the air and bloodstream.""
  }}
]

Your response should be PURE JSON that can be parsed directly, with NO additional text before or after the JSON array.
DO NOT include any explanations, markdown formatting, or other text. ONLY return the JSON array.

Unit Content:
{unitContent}";

            try 
            {
                System.Diagnostics.Debug.WriteLine("Calling AI provider to generate flashcards...");
                string response = await _aiProvider.GenerateTextAsync(prompt);
                
                System.Diagnostics.Debug.WriteLine($"Received raw response from AI: [{response.Substring(0, Math.Min(100, response.Length))}...]");
                
                // Attempt to clean the response to ensure valid JSON
                response = CleanJsonResponse(response);
                
                System.Diagnostics.Debug.WriteLine($"Cleaned response: [{response.Substring(0, Math.Min(100, response.Length))}...]");
                
                // For debugging, provide some simple flashcards if the response is empty
                if (string.IsNullOrEmpty(response) || response.Trim() == "[]")
                {
                    System.Diagnostics.Debug.WriteLine("Empty response received, using fallback flashcards");
                    response = @"[
                        {""question"": ""What is this flashcard?"", ""answer"": ""This is a fallback flashcard because the AI didn't generate valid content.""},
                        {""question"": ""Why am I seeing this?"", ""answer"": ""The AI response was empty or could not be parsed properly.""}
                    ]";
                }
                
                System.Diagnostics.Debug.WriteLine($"Sending flashcards response to ViewModel: {response}");
                onComplete(response);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error generating flashcards: {ex.Message}");
                
                // Return fallback flashcards instead of empty array
                string fallbackJson = @"[
                    {""question"": ""What is this flashcard?"", ""answer"": ""This is a fallback flashcard because an error occurred.""},
                    {""question"": ""What error occurred?"", ""answer"": """ + ex.Message.Replace("\"", "'") + @"""}
                ]";
                
                onComplete(fallbackJson);
            }
        });
    }
    
    public async Task GenerateMultipleChoiceQuestions(string unitContent, Action<string> onComplete)
    {
        await _worker.Enqueue(async () =>
        {
            if (string.IsNullOrEmpty(unitContent))
            {
                System.Diagnostics.Debug.WriteLine("WARNING: Empty unit content passed to GenerateMultipleChoiceQuestions");
                unitContent = "This is a sample unit for demonstration purposes.";
            }
                
            System.Diagnostics.Debug.WriteLine($"Generating multiple-choice questions for content length: {unitContent.Length}");
            
            string prompt = $@"Based on the following unit content, generate 5 multiple-choice questions. 
Each question should have 4 possible answers with exactly one correct answer.
The questions should cover the most important concepts, facts, and terminology from the unit content provided.

IMPORTANT: 
1. Make sure questions are directly related to the unit content provided
2. Include a mix of definition, concept, and application questions
3. Make sure the correct answer is not obvious and that the distractors (wrong options) are plausible
4. Format your response EXACTLY as a valid JSON array of question objects with the following structure:

Example format:
[
  {{
    ""question"": ""What is the primary function of the respiratory system?"",
    ""options"": [
      ""Transport nutrients throughout the body"",
      ""Gas exchange - bringing oxygen into the body and removing carbon dioxide"", 
      ""Filter waste products from the blood"", 
      ""Generate heat for the body""
    ],
    ""correctOptionIndex"": 1
  }},
  {{
    ""question"": ""Which organ is NOT part of the respiratory system?"",
    ""options"": [
      ""Lungs"", 
      ""Trachea"", 
      ""Liver"", 
      ""Bronchi""
    ],
    ""correctOptionIndex"": 2
  }}
]

Your response should be PURE JSON that can be parsed directly, with NO additional text before or after the JSON array.
DO NOT include any explanations, markdown formatting, or other text. ONLY return the JSON array.

Unit Content:
{unitContent}";

            try
            {
                System.Diagnostics.Debug.WriteLine("Calling AI provider to generate multiple-choice questions...");
                string response = await _aiProvider.GenerateTextAsync(prompt);

                System.Diagnostics.Debug.WriteLine($"Received raw response from AI: [{response.Substring(0, Math.Min(100, response.Length))}...]");

                // Attempt to clean the response to ensure valid JSON
                response = CleanJsonResponse(response);

                System.Diagnostics.Debug.WriteLine($"Cleaned response: [{response.Substring(0, Math.Min(100, response.Length))}...]");

                // For debugging, provide some simple questions if the response is empty
                if (string.IsNullOrEmpty(response) || response.Trim() == "[]")
                {
                    System.Diagnostics.Debug.WriteLine("Empty response received, using fallback questions");
                    response = @"[
                        {
                            ""question"": ""What is this question?"", 
                            ""options"": [
                                ""A sample question"", 
                                ""A fallback question because the AI didn't generate valid content"", 
                                ""A test of multiple choice"", 
                                ""None of the above""
                            ],
                            ""correctOptionIndex"": 1
                        },
                        {
                            ""question"": ""Why am I seeing this?"", 
                            ""options"": [
                                ""The AI is working correctly"", 
                                ""You selected the wrong option"", 
                                ""The AI response was empty or could not be parsed properly"", 
                                ""The system is offline""
                            ],
                            ""correctOptionIndex"": 2
                        }
                    ]";
                }

                System.Diagnostics.Debug.WriteLine($"Sending multiple-choice questions response to ViewModel: {response}");
                onComplete(response);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error generating multiple-choice questions: {ex.Message}");

                // Return fallback questions instead of empty array
                string fallbackJson = @"[
                    {
                        ""question"": ""What is this question?"", 
                        ""options"": [
                            ""A sample question"", 
                            ""A fallback question because an error occurred"", 
                            ""A test of multiple choice"", 
                            ""None of the above""
                        ],
                        ""correctOptionIndex"": 1
                    },
                    {
                        ""question"": ""What error occurred?"", 
                        ""options"": [
                            ""The AI is working correctly"", 
                            ""You selected the wrong option"", 
                            """ + ex.Message.Replace("\"", "'") + @""", 
                            ""The system is offline""
                        ],
                        ""correctOptionIndex"": 2
                    }
                ]";
            }
        });
    }

    public async Task GenerateComplexOutput(string prompt, Action<string> onComplete)
    {
        await _worker.Enqueue(async () =>
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("Calling AI provider to generate complex output...");
                string response = await _aiProvider.GenerateTextAsync(prompt);
                
                System.Diagnostics.Debug.WriteLine($"Received raw response from AI: [{response.Substring(0, Math.Min(100, response.Length))}...]");
                
                // Attempt to clean the response to ensure valid JSON
                response = CleanJsonResponse(response);
                
                System.Diagnostics.Debug.WriteLine($"Cleaned response: [{response.Substring(0, Math.Min(100, response.Length))}...]");
                
                // For debugging, provide a fallback response if empty
                if (string.IsNullOrEmpty(response) || response.Trim() == "[]")
                {
                    System.Diagnostics.Debug.WriteLine("Empty response received, using fallback content");
                    response = @"[
                        {
                            ""theory"": ""This is a sample theory section because the AI didn't generate valid content."",
                            ""questions"": [
                                {
                                    ""question"": ""What is this content?"",
                                    ""type"": ""MultipleChoice"",
                                    ""options"": [
                                        ""Real AI-generated content"", 
                                        ""Fallback content due to an error"", 
                                        ""User-created content"", 
                                        ""None of the above""
                                    ],
                                    ""correctOptionIndex"": 1,
                                    ""explanation"": ""This is fallback content shown because the AI response was empty or invalid.""
                                }
                            ]
                        }
                    ]";
                }
                
                System.Diagnostics.Debug.WriteLine($"Sending complex output response to ViewModel: {response}");
                onComplete(response);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error generating complex output: {ex.Message}");
                
                // Return fallback response instead of empty array
                string fallbackJson = @"[
                    {
                        ""theory"": ""An error occurred while generating this content."",
                        ""questions"": [
                            {
                                ""question"": ""What error occurred?"",
                                ""type"": ""MultipleChoice"",
                                ""options"": [
                                    ""The system is working correctly"", 
                                    ""There was a network issue"", 
                                    """ + ex.Message.Replace("\"", "'") + @""", 
                                    ""The content was too complex""
                                ],
                                ""correctOptionIndex"": 2,
                                ""explanation"": ""This error prevented the AI from generating proper content.""
                            }
                        ]
                    }
                ]";
                
                onComplete(fallbackJson);
            }
        });
    }

    private string CleanJsonResponse(string response)
    {
        try
        {
            // Try to extract just the JSON array from the response
            int startBracket = response.IndexOf('[');
            int endBracket = response.LastIndexOf(']');
            
            if (startBracket >= 0 && endBracket > startBracket)
            {
                string jsonContent = response.Substring(startBracket, endBracket - startBracket + 1);
                
                // Validate that it's parseable JSON
                var options = new System.Text.Json.JsonSerializerOptions
                {
                    AllowTrailingCommas = true
                };
                
                try
                {
                    // Try to parse as JSON to verify it's valid
                    var testParse = System.Text.Json.JsonSerializer.Deserialize<object>(jsonContent, options);
                    return jsonContent;
                }
                catch (System.Text.Json.JsonException ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Cleaned JSON is still invalid: {ex.Message}");
                    // Fall through to create a valid JSON array
                }
            }
            
            // If we couldn't extract valid JSON, generate a fallback
            return @"[
                {""question"": ""What is this flashcard?"", ""answer"": ""This is a fallback flashcard because the AI response was not valid JSON.""},
                {""question"": ""Why am I seeing this?"", ""answer"": ""The AI generated a response that could not be properly formatted as JSON.""}
            ]";
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error in CleanJsonResponse: {ex.Message}");
            return "[]";
        }
    }
}
