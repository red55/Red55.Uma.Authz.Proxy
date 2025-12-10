
namespace Red55.Uma.Authz.Proxy.Exceptions;

public class InvalidTransformConfigException : Yunqi.Common.Exceptions.YunqiException
{
    public InvalidTransformConfigException()
    {
    }

    public InvalidTransformConfigException(string? message) : base (message)
    {
    }

    public InvalidTransformConfigException(string? message, Exception? innerException) : base (message, innerException)
    {
    }
}
