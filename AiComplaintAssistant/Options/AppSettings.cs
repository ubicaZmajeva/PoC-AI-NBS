namespace AiComplaintAssistant.Options;

public class AppSettings
{
    [ConfigurationKeyName("BACKEND_URI")]
    public string BackendUri { get; set; } = "http://localhost:5067"!;
}
