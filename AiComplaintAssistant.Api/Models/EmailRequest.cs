namespace AiComplaintAssistant.Api.Models;

public record EmailRequest(string EmailAddress, string Subject, string Content);