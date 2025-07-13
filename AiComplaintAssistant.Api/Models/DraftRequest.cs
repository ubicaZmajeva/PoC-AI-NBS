using System.Collections.Generic;

namespace AiComplaintAssistant.Api.Models;

public record DraftRequest(EmailRequest EmailRequest, List<Classification> Classifications);
