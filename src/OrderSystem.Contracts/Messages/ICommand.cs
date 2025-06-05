namespace Shared.Contracts.Messages;

public interface ICommand
{
    string CorrelationId { get; }
}