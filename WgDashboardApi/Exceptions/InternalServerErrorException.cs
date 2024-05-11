namespace WgDashboardApi.Exceptions
{
    /// <summary>
    /// Represnets some error on the server that forces the server to stop processing the request immediately
    /// </summary>
    public class InternalServerErrorException : Exception
    {
        public InternalServerErrorException() : base() { }
        public InternalServerErrorException(string message) : base(message) { }
    }
}
