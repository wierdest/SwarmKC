using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Swarm.Application.Extensions;
using Swarm.Infrastructure.Extensions;
using SwarmKC.Core.Session;

namespace SwarmKC;
class Program
{
    static void Main()
    {
        var services = new ServiceCollection();

        // Logging
        services.AddLogging(cfg => cfg.AddConsole());

        var configuration = new ConfigurationBuilder()
                        .AddUserSecrets<Program>() // this works because <UserSecretsId> is in Presentation.csproj
                        .Build();

        
        services.AddApplication();
        services.AddLoader();
        services.AddSingleton<GameSessionManager>();
        services.AddSingleton<SwarmKC>();

        var provider = services.BuildServiceProvider();

        using var game = provider.GetRequiredService<SwarmKC>();
        
        game.Run();
    }
}
