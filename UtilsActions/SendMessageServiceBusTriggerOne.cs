using System.Text;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Configuration;
using UtilsActions.Options;

namespace UtilsActions;

public class SendMessageServiceBusTriggerOne
{
    private readonly ServiceBusOptions _serviceBusOptions;

    private readonly ServiceBusTriggerOneOptions _serviceBusTriggerOneOptions;

    public SendMessageServiceBusTriggerOne()
    {
        IConfigurationRoot config = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json")
            .AddUserSecrets<SendMessageServiceBusTriggerOne>()
            .AddEnvironmentVariables()
            .Build();

        _serviceBusOptions = config.GetSection(ServiceBusOptions.ConfigSectionName).Get<ServiceBusOptions>()
                             ?? throw new ArgumentNullException(nameof(config));
        _serviceBusTriggerOneOptions =
            config.GetSection(ServiceBusTriggerOneOptions.ConfigSectionName).Get<ServiceBusTriggerOneOptions>()
            ?? throw new ArgumentNullException(nameof(config));
    }

    [Fact]
    public async Task SendMessage_1_Test()
    {
        for (int i = 0; i < 100; i++)
        {
            await SendMessageAsync("message_1.json");
        }
    }

    private async Task SendMessageAsync(string name)
    {
        // Adjusted path to match TestData/Messages folder structure.
        string messageFilePath = Path.Combine("TestData", "Messages", name);
        if (!File.Exists(messageFilePath))
        {
            throw new FileNotFoundException("Test message file not found", messageFilePath);
        }

        string messageBody = await File.ReadAllTextAsync(messageFilePath);

        await using var client = new ServiceBusClient(_serviceBusOptions.ConnectionString);
        ServiceBusSender sender = client.CreateSender(_serviceBusTriggerOneOptions.ServiceBusTriggerOneQueue);
        var message = new ServiceBusMessage(Encoding.UTF8.GetBytes(messageBody));
        await sender.SendMessageAsync(message);
    }
}