namespace Goa.Functions.Core.Bootstrapping;

public interface IResponseSerializer<in T>
{
    HttpContent Serialize(T response);
}