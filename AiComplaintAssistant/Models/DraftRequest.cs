namespace AiComplaintAssistant.Models;

public record DraftRequest(EmailRequest EmailRequest, List<Classification> Classifications);
