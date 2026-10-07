using AcxiomCRM.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AcxiomCRM.Infrastructure.Data.Configurations;

public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.HasKey(c => c.CustomerId);
        builder.Property(c => c.CustomerCode).HasMaxLength(30).IsRequired();
        builder.HasIndex(c => c.CustomerCode).IsUnique();

        builder.Property(c => c.CustomerName).HasMaxLength(150).IsRequired();
        builder.Property(c => c.Email).HasMaxLength(150).IsRequired();
        builder.HasIndex(c => c.Email).IsUnique();

        builder.Property(c => c.Phone).HasMaxLength(20).IsRequired();
        builder.HasIndex(c => c.Phone).IsUnique();

        builder.Property(c => c.CompanyName).HasMaxLength(150);
        builder.Property(c => c.Address).HasMaxLength(250);
        builder.Property(c => c.City).HasMaxLength(100);
        builder.Property(c => c.State).HasMaxLength(100);
        builder.Property(c => c.OwnerId).HasMaxLength(450);
        builder.Property(c => c.CreatedBy).HasMaxLength(150);
    }
}

public class LeadConfiguration : IEntityTypeConfiguration<Lead>
{
    public void Configure(EntityTypeBuilder<Lead> builder)
    {
        builder.HasKey(l => l.LeadId);
        builder.Property(l => l.LeadCode).HasMaxLength(30).IsRequired();
        builder.HasIndex(l => l.LeadCode).IsUnique();

        builder.Property(l => l.LeadName).HasMaxLength(150).IsRequired();
        builder.Property(l => l.Email).HasMaxLength(150).IsRequired();
        builder.Property(l => l.Phone).HasMaxLength(20).IsRequired();
        builder.Property(l => l.CompanyName).HasMaxLength(150);
        builder.Property(l => l.Source).HasMaxLength(100);
        builder.Property(l => l.AssignedTo).HasMaxLength(450);
        builder.Property(l => l.ExpectedValue).HasColumnType("decimal(18,2)");
    }
}

public class OpportunityConfiguration : IEntityTypeConfiguration<Opportunity>
{
    public void Configure(EntityTypeBuilder<Opportunity> builder)
    {
        builder.HasKey(o => o.OpportunityId);
        builder.Property(o => o.OpportunityName).HasMaxLength(150).IsRequired();
        builder.Property(o => o.Amount).HasColumnType("decimal(18,2)");
        builder.Property(o => o.AssignedTo).HasMaxLength(450);

        builder.HasOne(o => o.Customer)
               .WithMany(c => c.Opportunities)
               .HasForeignKey(o => o.CustomerId)
               .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(o => o.Lead)
               .WithMany()
               .HasForeignKey(o => o.LeadId)
               .OnDelete(DeleteBehavior.SetNull);
    }
}

public class FollowUpConfiguration : IEntityTypeConfiguration<FollowUp>
{
    public void Configure(EntityTypeBuilder<FollowUp> builder)
    {
        builder.HasKey(f => f.FollowUpId);
        builder.Property(f => f.Subject).HasMaxLength(150).IsRequired();
        builder.Property(f => f.AssignedTo).HasMaxLength(450);

        builder.HasOne(f => f.Customer)
               .WithMany(c => c.FollowUps)
               .HasForeignKey(f => f.CustomerId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(f => f.Lead)
               .WithMany(l => l.FollowUps)
               .HasForeignKey(f => f.LeadId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(f => f.Opportunity)
               .WithMany(o => o.FollowUps)
               .HasForeignKey(f => f.OpportunityId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}

public class ActivityConfiguration : IEntityTypeConfiguration<Activity>
{
    public void Configure(EntityTypeBuilder<Activity> builder)
    {
        builder.HasKey(a => a.ActivityId);
        builder.Property(a => a.Subject).HasMaxLength(150).IsRequired();
        builder.Property(a => a.AssignedTo).HasMaxLength(450);
        builder.Property(a => a.Status).HasMaxLength(50);

        builder.HasOne(a => a.Customer)
               .WithMany(c => c.Activities)
               .HasForeignKey(a => a.CustomerId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(a => a.Lead)
               .WithMany(l => l.Activities)
               .HasForeignKey(a => a.LeadId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.HasKey(a => a.AuditLogId);
        builder.Property(a => a.Action).HasMaxLength(100).IsRequired();
        builder.Property(a => a.EntityName).HasMaxLength(100).IsRequired();
        builder.Property(a => a.RecordId).HasMaxLength(50);
        builder.Property(a => a.UserId).HasMaxLength(450);
        builder.Property(a => a.UserName).HasMaxLength(150);
        builder.Property(a => a.IpAddress).HasMaxLength(50);
        builder.Property(a => a.Result).HasMaxLength(50);

        builder.HasIndex(a => a.CreatedDate);
        builder.HasIndex(a => a.EntityName);
        builder.HasIndex(a => a.UserId);
    }
}
