namespace CyberForgeStudio.Models;

public sealed class ConnectedDevice
{
	public string Serial { get; init; } = string.Empty;
	public string Mode { get; init; } = string.Empty;
	public string State { get; init; } = string.Empty;
	public string? Model { get; init; }

	public string DisplayName => string.IsNullOrWhiteSpace(Model)
		? $"{Serial} | {Mode} | {State}"
		: $"{Model} | {Serial} | {Mode} | {State}";
}
