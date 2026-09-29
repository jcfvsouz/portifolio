using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Components.Hosting.Tests;

public class UseStartupTests
{
    private class RecordingStartup : IStartup
    {
        public static IServiceCollection? ReceivedServices { get; private set; }
        public static IConfiguration? ReceivedConfiguration { get; private set; }
        public static int CallCount { get; private set; }

        public static void Reset()
        {
            ReceivedServices = null;
            ReceivedConfiguration = null;
            CallCount = 0;
        }

        public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            CallCount++;
            ReceivedServices = services;
            ReceivedConfiguration = configuration;
            services.AddSingleton<MarkerService>();
        }
    }

    private class MarkerService;

    [Fact]
    public void UseStartup_invokes_ConfigureServices_exactly_once()
    {
        // Arrange
        RecordingStartup.Reset();
        var builder = Host.CreateApplicationBuilder();

        // Act
        builder.UseStartup<RecordingStartup>();

        // Assert
        RecordingStartup.CallCount.Should().Be(1);
    }

    [Fact]
    public void UseStartup_passes_the_builder_own_services_and_configuration()
    {
        // Arrange
        RecordingStartup.Reset();
        var builder = Host.CreateApplicationBuilder();

        // Act
        builder.UseStartup<RecordingStartup>();

        // Assert
        RecordingStartup.ReceivedServices.Should().BeSameAs(builder.Services);
        RecordingStartup.ReceivedConfiguration.Should().BeSameAs(builder.Configuration);
    }

    [Fact]
    public void UseStartup_registrations_are_resolvable_after_the_host_is_built()
    {
        // Arrange
        RecordingStartup.Reset();
        var builder = Host.CreateApplicationBuilder();
        builder.UseStartup<RecordingStartup>();

        // Act
        using var host = builder.Build();
        var marker = host.Services.GetService<MarkerService>();

        // Assert
        marker.Should().NotBeNull();
    }

    [Fact]
    public void UseStartup_returns_the_same_builder_instance_for_chaining()
    {
        // Arrange
        RecordingStartup.Reset();
        var builder = Host.CreateApplicationBuilder();

        // Act
        var returned = builder.UseStartup<RecordingStartup>();

        // Assert
        returned.Should().BeSameAs(builder);
    }
}
