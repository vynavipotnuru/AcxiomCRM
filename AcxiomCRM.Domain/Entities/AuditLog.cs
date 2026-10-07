namespace AcxiomCRM.Domain.Entities;

public class AuditLog
{
    public long AuditLogId { get; set; }
    public string? UserId { get; set; }
    public string? UserName { get; set; }
    public string Action { get; set; } = string.Empty; // Login, FailedLogin, Logout, Create, Update, Delete, Convert, RoleChange, Security
    public string EntityName { get; set; } = string.Empty; // Customer, Lead, Opportunity, FollowUp, User, Auth
    public string? RecordId { get; set; }
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public string? IpAddress { get; set; }
    public string Result { get; set; } = "Success"; // Success, Failure
    public string? Details { get; set; }
}
