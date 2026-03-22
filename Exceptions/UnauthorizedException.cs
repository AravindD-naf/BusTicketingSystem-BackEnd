namespace BusTicketingSystem.Exceptions
{
    public class UnauthorizedException : ApplicationException
    {
        public UnauthorizedException(string message = "You are not authorized to perform this action.")
            : base(
                userMessage: message,
                errorCode: "UNAUTHORIZED",
                statusCode: 401)
        {
        }
    }
}