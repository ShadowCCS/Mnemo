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
            string prompt = $"Generate an educational text-based study guide with clearly defined sections. The content should be structured, informative, and well-organized, use markdown. Follow this format:\n\n"
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
}
