using System.Threading.Tasks;
using FileOrganizer.Models;

namespace FileOrganizer.Services;

public interface IAIConfigurationService
{
    Task<AiServiceConfiguration> LoadConfiguration();
    Task SaveConfiguration(AiServiceConfiguration aiServiceConfiguration);
}