namespace Vigie24.Tests.Unit.Services;

using Vigie24.Services;
using Vigie24.Services.Interfaces;
using Microsoft.Extensions.Logging;

/// <summary>
/// Tests unitaires pour les services IA
/// </summary>
public class AiServiceFactoryTests
{
    [Fact]
    public void CreateService_WithInfomaniakProvider_ShouldReturnService()
    {
        // Arrange
        var settings = new AiServiceSettings
        {
            Provider = "infomaniak",
            Model = "Qwen/Qwen3.5-122B-A10B-FP8",
            ApiUrl = "https://api.infomaniak.com/v1/chat/completions",
            ApiKey = "test-key"
        };
        var loggerFactory = new LoggerFactory();

        // Act
        var service = AiServiceFactory.Create(settings, loggerFactory);

        // Assert
        Assert.NotNull(service);
        Assert.IsAssignableFrom<IAiService>(service);
    }

    [Fact]
    public void CreateService_WithDeepSeekProvider_ShouldReturnService()
    {
        // Arrange
        var settings = new AiServiceSettings
        {
            Provider = "deepseek",
            Model = "deepseek-v4-flash",
            ApiUrl = "https://api.deepseek.com/v1/chat/completions",
            ApiKey = "test-key"
        };
        var loggerFactory = new LoggerFactory();

        // Act
        var service = AiServiceFactory.Create(settings, loggerFactory);

        // Assert
        Assert.NotNull(service);
        Assert.IsAssignableFrom<IAiService>(service);
    }

    [Fact]
    public void CreateService_WithUnknownProvider_ShouldThrowException()
    {
        // Arrange
        var settings = new AiServiceSettings
        {
            Provider = "unknown",
            Model = "test-model",
            ApiUrl = "https://example.com",
            ApiKey = "test-key"
        };
        var loggerFactory = new LoggerFactory();

        // Act & Assert
        Assert.ThrowsAny<InvalidOperationException>(() =>
            AiServiceFactory.Create(settings, loggerFactory));
    }

    [Fact]
    public void CreateService_WithUnsupportedProvider_ShouldThrowException()
    {
        // Arrange
        var settings = new AiServiceSettings
        {
            Provider = "unsupported",
            Model = "",
            ApiUrl = "",
            ApiKey = "fake-key-for-test"
        };
        var loggerFactory = new LoggerFactory();

        // Act & Assert
        Assert.ThrowsAny<InvalidOperationException>(() =>
            AiServiceFactory.Create(settings, loggerFactory));
    }
}

/// <summary>
/// Tests pour AiServiceSettings
/// </summary>
public class AiServiceSettingsTests
{
    [Fact]
    public void AiServiceSettings_ShouldHaveDefaults()
    {
        // Arrange & Act
        var settings = new AiServiceSettings();

        // Assert
        Assert.Equal("infomaniak", settings.Provider);
        Assert.Equal(30, settings.TimeoutSeconds);
    }

    [Fact]
    public void AiServiceSettings_ShouldAllowCustomValues()
    {
        // Arrange & Act
        var settings = new AiServiceSettings
        {
            Provider = "deepseek",
            Model = "deepseek-v4-flash",
            ApiUrl = "https://api.deepseek.com/v1/chat/completions",
            ApiKey = "test-key",
            TimeoutSeconds = 60
        };

        // Assert
        Assert.Equal("deepseek", settings.Provider);
        Assert.Equal("deepseek-v4-flash", settings.Model);
        Assert.Equal(60, settings.TimeoutSeconds);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void AiServiceSettings_WithInvalidTimeout_ShouldAcceptButMayFailLater(int timeout)
    {
        // Note: La validation peut être ajoutée si nécessaire
        var settings = new AiServiceSettings
        {
            TimeoutSeconds = timeout
        };

        Assert.Equal(timeout, settings.TimeoutSeconds);
    }
}
