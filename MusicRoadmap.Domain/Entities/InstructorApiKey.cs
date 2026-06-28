public class InstructorApiKey
{
    public Guid Id {get; set;} = Guid.NewGuid();
    public string InstructorId {get; set;} = string.Empty;
    public string HashedKey {get; set;} = string.Empty;
    public string WebsiteURL {get; set;} = string.Empty;
    public bool IsActive {get; set;} = true;
    public DateTime CreatedAt {get; set;} = DateTime.UtcNow;
}