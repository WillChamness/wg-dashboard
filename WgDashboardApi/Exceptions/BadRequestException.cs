namespace WgDashboardApi.Exceptions
{
    /// <summary>
    /// Represents a request that cannot be processed due to bad input. This is the same as ArugmentException/ArgumentNullException, 
    /// but may be more readable semantically in some situations.
    /// </summary>
    public class BadRequestException : Exception
    {
        public BadRequestException() : base() { }
        public BadRequestException(string message) : base(message) { }
    }
}
