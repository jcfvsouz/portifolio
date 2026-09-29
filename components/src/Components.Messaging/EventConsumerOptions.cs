namespace Components.Messaging;

public class EventConsumerOptions
{
    public string Destination { get; set; } = string.Empty;
    public string? MessageKey { get; set; }
    public int PrefetchCount { get; set; } = 10;
}
