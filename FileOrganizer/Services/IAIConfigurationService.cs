using System.Threading.Tasks;
using FileOrganizer.Models;

namespace FileOrganizer.Services;

public interface IAIConfigurationService
{
    Task<AIServiceConfiguration> LoadConfiguration();
    Task SaveConfiguration(AIServiceConfiguration aiServiceConfiguration);
}