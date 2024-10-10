using System.Threading.Tasks;

namespace FileOrganizer.Services;

public interface IAIConfigurationService
{
    Task<AiServiceConfiguration> LoadConfiguration();
    Task SaveConfiguration(AiServiceConfiguration aiServiceConfiguration);
}