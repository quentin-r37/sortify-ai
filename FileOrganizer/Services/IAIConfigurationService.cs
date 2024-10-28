using FileOrganizer.Models;
using System.Threading.Tasks;

namespace FileOrganizer.Services;

public interface IAIConfigurationService
{
    Task<AIServiceConfiguration> LoadConfiguration();
    Task SaveConfiguration(AIServiceConfiguration aiServiceConfiguration);
}