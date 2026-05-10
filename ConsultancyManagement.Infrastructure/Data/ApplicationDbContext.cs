using ConsultancyManagement.Core.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ConsultancyManagement.Infrastructure.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser, IdentityRole, string>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<Consultant> Consultants => Set<Consultant>();
    public DbSet<SalesRecruiter> SalesRecruiters => Set<SalesRecruiter>();
    public DbSet<ManagementUser> ManagementUsers => Set<ManagementUser>();
    public DbSet<ConsultantSalesAssignment> ConsultantSalesAssignments => Set<ConsultantSalesAssignment>();
    public DbSet<SalesManagementAssignment> SalesManagementAssignments => Set<SalesManagementAssignment>();
    public DbSet<Vendor> Vendors => Set<Vendor>();
    public DbSet<DailyActivity> DailyActivities => Set<DailyActivity>();
    public DbSet<JobApplication> JobApplications => Set<JobApplication>();
    public DbSet<Submission> Submissions => Set<Submission>();
    public DbSet<Interview> Interviews => Set<Interview>();
    public DbSet<OnboardingTask> OnboardingTasks => Set<OnboardingTask>();
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<ConsultantVendorReachOut> ConsultantVendorReachOuts => Set<ConsultantVendorReachOut>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<UserNotification> UserNotifications => Set<UserNotification>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        var userEntityType = modelBuilder.Model.FindEntityType(typeof(ApplicationUser));
        if (userEntityType is not null)
        {
            foreach (var index in userEntityType.GetIndexes().ToList())
            {
                var props = index.Properties.Select(p => p.Name).ToList();
                if (index.IsUnique && props.Count == 1 &&
                    props[0] is nameof(ApplicationUser.NormalizedUserName) or nameof(ApplicationUser.NormalizedEmail))
                {
                    userEntityType.RemoveIndex(index);
                }
            }
        }

        modelBuilder.Entity<Organization>(e =>
        {
            e.HasIndex(x => x.Slug).IsUnique();
        });

        modelBuilder.Entity<ApplicationUser>(e =>
        {
            e.ToTable("Users");
            e.HasIndex(x => new { x.OrganizationId, x.EmployeeId }).IsUnique();
            e.HasIndex(x => new { x.OrganizationId, x.NormalizedUserName }).IsUnique();
            e.HasIndex(x => new { x.OrganizationId, x.NormalizedEmail }).IsUnique();
            e.HasOne(x => x.Organization)
                .WithMany(o => o.Users)
                .HasForeignKey(x => x.OrganizationId)
                .OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<IdentityRole>(e => e.ToTable("Roles"));
        modelBuilder.Entity<IdentityUserRole<string>>(e => e.ToTable("UserRoles"));
        modelBuilder.Entity<IdentityUserClaim<string>>(e => e.ToTable("UserClaims"));
        modelBuilder.Entity<IdentityUserLogin<string>>(e => e.ToTable("UserLogins"));
        modelBuilder.Entity<IdentityUserToken<string>>(e => e.ToTable("UserTokens"));
        modelBuilder.Entity<IdentityRoleClaim<string>>(e => e.ToTable("RoleClaims"));

        modelBuilder.Entity<Consultant>(e =>
        {
            e.HasIndex(x => x.UserId).IsUnique();
            e.HasOne(x => x.Organization)
                .WithMany()
                .HasForeignKey(x => x.OrganizationId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<SalesRecruiter>(e =>
        {
            e.HasIndex(x => x.UserId).IsUnique();
            e.HasOne(x => x.Organization)
                .WithMany()
                .HasForeignKey(x => x.OrganizationId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ManagementUser>(e =>
        {
            e.HasIndex(x => x.UserId).IsUnique();
            e.HasOne(x => x.Organization)
                .WithMany()
                .HasForeignKey(x => x.OrganizationId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ConsultantSalesAssignment>(e =>
        {
            e.HasOne(x => x.Consultant)
                .WithMany(c => c.SalesAssignments)
                .HasForeignKey(x => x.ConsultantId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.SalesRecruiter)
                .WithMany(s => s.ConsultantAssignments)
                .HasForeignKey(x => x.SalesRecruiterId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<SalesManagementAssignment>(e =>
        {
            e.HasOne(x => x.SalesRecruiter)
                .WithMany(s => s.ManagementAssignments)
                .HasForeignKey(x => x.SalesRecruiterId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.ManagementUser)
                .WithMany(m => m.SalesAssignments)
                .HasForeignKey(x => x.ManagementUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<DailyActivity>(e =>
        {
            e.HasIndex(x => new { x.ConsultantId, x.ActivityDate }).IsUnique();
            e.HasOne(x => x.Consultant)
                .WithMany(c => c.DailyActivities)
                .HasForeignKey(x => x.ConsultantId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<JobApplication>(e =>
        {
            e.HasOne(x => x.Consultant)
                .WithMany(c => c.JobApplications)
                .HasForeignKey(x => x.ConsultantId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Vendor>(e =>
        {
            e.HasIndex(x => new { x.OrganizationId, x.VendorCode }).IsUnique();
            e.HasOne(x => x.Organization)
                .WithMany()
                .HasForeignKey(x => x.OrganizationId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.SalesRecruiter)
                .WithMany(s => s.Vendors)
                .HasForeignKey(x => x.SalesRecruiterId)
                .OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.LinkedConsultant)
                .WithMany()
                .HasForeignKey(x => x.LinkedConsultantId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<ConsultantVendorReachOut>(e =>
        {
            e.HasOne(x => x.Consultant)
                .WithMany(c => c.VendorReachOuts)
                .HasForeignKey(x => x.ConsultantId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Submission>(e =>
        {
            e.HasOne(x => x.Consultant)
                .WithMany(c => c.Submissions)
                .HasForeignKey(x => x.ConsultantId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.SalesRecruiter)
                .WithMany(s => s.Submissions)
                .HasForeignKey(x => x.SalesRecruiterId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Vendor)
                .WithMany(v => v.Submissions)
                .HasForeignKey(x => x.VendorId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Interview>(e =>
        {
            e.HasOne(x => x.Submission)
                .WithMany(s => s.Interviews)
                .HasForeignKey(x => x.SubmissionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<OnboardingTask>(e =>
        {
            e.HasOne(x => x.Consultant)
                .WithMany(c => c.OnboardingTasks)
                .HasForeignKey(x => x.ConsultantId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Document>(e =>
        {
            e.HasOne(x => x.Consultant)
                .WithMany(c => c.Documents)
                .HasForeignKey(x => x.ConsultantId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AuditLog>(e =>
        {
            e.HasIndex(x => x.CreatedAt);
        });

        modelBuilder.Entity<UserNotification>(e =>
        {
            e.HasIndex(x => x.RecipientUserId);
            e.HasIndex(x => new { x.RecipientUserId, x.ReadAt });
        });
    }
}
