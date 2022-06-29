namespace Stl.Fusion.Extensions.Commands;

[DataContract]
public record RemoveCommand(
    [property: DataMember] Symbol TenantId,
    [property: DataMember] params string[] Keys
    ) : ICommand<Unit>, IBackendCommand
{
    public RemoveCommand() : this(default, Array.Empty<string>()) { }
}
