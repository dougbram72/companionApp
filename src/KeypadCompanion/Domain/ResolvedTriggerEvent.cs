namespace KeypadCompanion.Domain;

public sealed record ResolvedTriggerEvent(
    InputId InputId,
    TriggerType TriggerType,
    DateTimeOffset Timestamp);
