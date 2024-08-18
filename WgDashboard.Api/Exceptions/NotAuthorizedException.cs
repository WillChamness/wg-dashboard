namespace WgDashboard.Api.Exceptions
{
    public class NotAuthorizedException : Exception
    {
        public NotAuthorizedException() : base() { }
        public NotAuthorizedException(string message) : base(message) { }
    }
}
