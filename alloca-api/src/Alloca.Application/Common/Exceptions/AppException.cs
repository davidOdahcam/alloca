namespace Alloca.Application.Common.Exceptions;

public abstract class AppException : Exception
{
    protected AppException(string message) : base(message) { }
    public abstract int StatusCode { get; }
}

public class NotFoundException : AppException
{
    public NotFoundException(string message) : base(message) { }
    public override int StatusCode => 404;
}

public class ConflictException : AppException
{
    public ConflictException(string message) : base(message) { }
    public override int StatusCode => 409;
}

public class ForbiddenException : AppException
{
    public ForbiddenException(string message) : base(message) { }
    public override int StatusCode => 403;
}

public class UnauthorizedException : AppException
{
    public UnauthorizedException(string message) : base(message) { }
    public override int StatusCode => 401;
}

public class BusinessRuleException : AppException
{
    public BusinessRuleException(string message) : base(message) { }
    public override int StatusCode => 422;
}
