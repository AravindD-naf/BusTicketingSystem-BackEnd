using System.Collections.Generic;

namespace BusTicketingSystem.Exceptions
{

    public class ApplicationException : Exception
    {
  
        public string ErrorCode { get; set; }


        public int StatusCode { get; set; }


        public string UserMessage { get; set; }

        public string InternalMessage { get; set; }

        public Dictionary<string, string> Errors { get; set; }

        public Dictionary<string, object> ContextData { get; set; }

        public DateTime Timestamp { get; set; }

        public ApplicationException(
            string userMessage,
            string errorCode = "ERROR_001",
            int statusCode = 500,
            string internalMessage = null,
            Exception innerException = null)
            : base(userMessage, innerException)
        {
            UserMessage = userMessage;
            ErrorCode = errorCode;
            StatusCode = statusCode;
            InternalMessage = internalMessage ?? userMessage;
            Errors = new Dictionary<string, string>();
            ContextData = new Dictionary<string, object>();
            Timestamp = DateTime.UtcNow;
        }


        public ApplicationException AddError(string field, string message)
        {
            Errors[field] = message;
            return this;
        }


        public ApplicationException AddContextData(string key, object value)
        {
            ContextData[key] = value;
            return this;
        }
    }
}
