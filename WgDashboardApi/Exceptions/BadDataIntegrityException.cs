namespace WgDashboardApi.Exceptions
{
    /// <summary>
    /// Represents an error such that the database is in a logically impossible state (assuming there are no bugs) and therefore the data cannot be trusted
    /// </summary>
    public class BadDataIntegrityException : InternalServerErrorException
    {
        public BadDataIntegrityException() : base() { }
        public BadDataIntegrityException(string message) : base(message) { }
    }
}
