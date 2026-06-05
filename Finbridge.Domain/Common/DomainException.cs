namespace Finbridge.Domain.Common;

/// <summary>
/// Базовое исключение для всех доменных нарушений. Ловится инфраструктурным
/// слоем и транслируется в осмысленные HTTP-ответы.
/// </summary>
public abstract class DomainException : Exception
{
    protected DomainException(string message) : base(message) { }
    protected DomainException(string message, Exception inner) : base(message, inner) { }
}
