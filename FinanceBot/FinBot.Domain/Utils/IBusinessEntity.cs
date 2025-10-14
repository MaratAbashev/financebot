namespace FinBot.Domain.Utils;

public interface IBusinessEntity<T> where T : struct
{
    T Id { get; set; }
}
