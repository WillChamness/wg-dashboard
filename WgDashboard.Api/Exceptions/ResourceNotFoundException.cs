namespace WgDashboard.Api.Exceptions
{
    /// <summary>
    /// Represents a situation where the request was processed, but the resource requested cannot be found
    /// </summary>
    public class ResourceNotFoundException : Exception
    {
        public ResourceNotFoundException() : base() { }

        public ResourceNotFoundException(string message) : base(message) { }
    }
}
