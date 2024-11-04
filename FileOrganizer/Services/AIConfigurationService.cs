using FileOrganizer.Database;
using FileOrganizer.Models;
using FileOrganizer.Shared;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace FileOrganizer.Services;

public class AIConfigurationService(AppDbContext context) : IAIConfigurationService
{
    public async Task<AIServiceConfiguration> LoadConfiguration()
    {
        var conf = await context.Configurations.OrderByDescending(c => c.CreatedDate).FirstOrDefaultAsync();
        if (conf != null)
        {
            var objConf = JsonSerializer.Deserialize<AIServiceConfiguration>(conf.Value);
            return objConf;
        }
        else
            return new AIServiceConfiguration()
            {
                CompletionService = new Service()
                {
                    SelectedProvider = "Ollama",
                    OllamaFields = new OllamaFields()
                    {
                        ModelId = "llama3.2:1b",
                        OllamaUrl = new Uri("http://localhost:11434")
                    }
                },
                EmbeddingService = new Service()
                {
                    SelectedProvider = "Ollama",
                    OllamaFields = new OllamaFields()
                    {
                        ModelId = "all-minilm",
                        OllamaUrl = new Uri("http://localhost:11434")
                    }
                }
            };
    }

    public async Task SaveConfiguration(AIServiceConfiguration aiServiceConfiguration)
    {
        var configurations = context.Configurations.ToList(); // Get all entities
        context.Configurations.RemoveRange(configurations);   // Remove all entities
        context.Configurations.Add(new Configuration()
        {
            CreatedDate = DateTime.Now,
            Value = JsonSerializer.Serialize(aiServiceConfiguration)
        });
        await context.SaveChangesAsync();
    }
}