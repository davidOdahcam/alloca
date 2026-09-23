namespace Alloca.Domain.Common.Exceptions;

public abstract class AppException : Exception
{
    protected AppException(string message) : base(message)
    {
        Code = ErrorCodes.Unknown;
        Args = Array.Empty<object>();
    }

    protected AppException(string code, string fallbackMessage, params object[] args) : base(fallbackMessage)
    {
        Code = code;
        Args = args ?? Array.Empty<object>();
    }

    public abstract int StatusCode { get; }

    /// <summary>Código estável usado pelo frontend para tradução (i18n).</summary>
    public string Code { get; }

    /// <summary>Argumentos para interpolação na mensagem traduzida (ex.: {minutos}).</summary>
    public object[] Args { get; }
}

public class NotFoundException : AppException
{
    public NotFoundException(string message) : base(message) { }
    public NotFoundException(string code, string fallbackMessage, params object[] args)
        : base(code, fallbackMessage, args) { }
    public override int StatusCode => 404;
}

public class ConflictException : AppException
{
    public ConflictException(string message) : base(message) { }
    public ConflictException(string code, string fallbackMessage, params object[] args)
        : base(code, fallbackMessage, args) { }
    public override int StatusCode => 409;
}

public class ForbiddenException : AppException
{
    public ForbiddenException(string message) : base(message) { }
    public ForbiddenException(string code, string fallbackMessage, params object[] args)
        : base(code, fallbackMessage, args) { }
    public override int StatusCode => 403;
}

public class UnauthorizedException : AppException
{
    public UnauthorizedException(string message) : base(message) { }
    public UnauthorizedException(string code, string fallbackMessage, params object[] args)
        : base(code, fallbackMessage, args) { }
    public override int StatusCode => 401;
}

public class BusinessRuleException : AppException
{
    public BusinessRuleException(string message) : base(message) { }
    public BusinessRuleException(string code, string fallbackMessage, params object[] args)
        : base(code, fallbackMessage, args) { }
    public override int StatusCode => 422;
}
