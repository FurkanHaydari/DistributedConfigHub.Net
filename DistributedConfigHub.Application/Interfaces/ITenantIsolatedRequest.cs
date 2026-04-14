public interface ITenantIsolatedRequest
{
    string ApplicationName { get; }      
    string CallerApplicationName { get; } 
}