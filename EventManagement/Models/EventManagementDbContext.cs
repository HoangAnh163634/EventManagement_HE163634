using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace EventManagement.Models;

public partial class EventManagementDbContext : DbContext
{
    private readonly IConfiguration _configuration;

    public EventManagementDbContext(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public EventManagementDbContext(DbContextOptions<EventManagementDbContext> options, IConfiguration configuration)
        : base(options)
    {
        _configuration = configuration;
    }

    public virtual DbSet<AuditLog> AuditLogs { get; set; }

    public virtual DbSet<CalendarSync> CalendarSyncs { get; set; }

    public virtual DbSet<Event> Events { get; set; }

    public virtual DbSet<EventStatusView> EventStatusViews { get; set; }

    public virtual DbSet<EventType> EventTypes { get; set; }

    public virtual DbSet<Feedback> Feedbacks { get; set; }

    public virtual DbSet<Notification> Notifications { get; set; }

    public virtual DbSet<Qrcode> Qrcodes { get; set; }

    public virtual DbSet<Registration> Registrations { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<SocialShare> SocialShares { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<UserRole> UserRoles { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseSqlServer(_configuration.GetConnectionString("DefaultConnection"));
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(e => e.AuditId).HasName("PK__AuditLog__A17F23B8B80C3986");

            entity.HasIndex(e => e.TableName, "IX_AuditLogs_TableName");

            entity.HasIndex(e => e.Timestamp, "IX_AuditLogs_Timestamp");

            entity.HasIndex(e => e.UserId, "IX_AuditLogs_UserID");

            entity.Property(e => e.AuditId).HasColumnName("AuditID");
            entity.Property(e => e.Action).HasMaxLength(50);
            entity.Property(e => e.Ipaddress)
                .HasMaxLength(45)
                .HasColumnName("IPAddress");
            entity.Property(e => e.RecordId).HasColumnName("RecordID");
            entity.Property(e => e.TableName).HasMaxLength(50);
            entity.Property(e => e.Timestamp)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.UserAgent).HasMaxLength(500);
            entity.Property(e => e.UserId).HasColumnName("UserID");

            entity.HasOne(d => d.User).WithMany(p => p.AuditLogs)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK__AuditLogs__UserI__084B3915");
        });

        modelBuilder.Entity<CalendarSync>(entity =>
        {
            entity.HasKey(e => e.SyncId).HasName("PK__Calendar__7E50DEA617097D15");

            entity.HasIndex(e => e.NextSyncAt, "IX_CalendarSyncs_NextSyncAt");

            entity.HasIndex(e => e.SyncStatus, "IX_CalendarSyncs_SyncStatus");

            entity.HasIndex(e => new { e.UserId, e.EventId, e.Provider }, "UQ__Calendar__0B85C44B2A8718A5").IsUnique();

            entity.Property(e => e.SyncId).HasColumnName("SyncID");
            entity.Property(e => e.EventId).HasColumnName("EventID");
            entity.Property(e => e.ExternalCalendarId)
                .HasMaxLength(255)
                .HasColumnName("ExternalCalendarID");
            entity.Property(e => e.ExternalEventId)
                .HasMaxLength(255)
                .HasColumnName("ExternalEventID");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.LastSyncedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.NextSyncAt).HasColumnType("datetime");
            entity.Property(e => e.Provider)
                .HasMaxLength(20)
                .HasDefaultValue("Google");
            entity.Property(e => e.SyncStatus).HasMaxLength(20);
            entity.Property(e => e.SyncToken).HasMaxLength(255);
            entity.Property(e => e.UserId).HasColumnName("UserID");

            entity.HasOne(d => d.Event).WithMany(p => p.CalendarSyncs)
                .HasForeignKey(d => d.EventId)
                .HasConstraintName("FK__CalendarS__Event__038683F8");

            entity.HasOne(d => d.User).WithMany(p => p.CalendarSyncs)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK__CalendarS__UserI__02925FBF");
        });

        modelBuilder.Entity<Event>(entity =>
        {
            entity.HasKey(e => e.EventId).HasName("PK__Events__7944C87061502565");

            entity.HasIndex(e => e.IsDeleted, "IX_Events_IsDeleted");

            entity.HasIndex(e => e.OrganizerId, "IX_Events_OrganizerID");

            entity.HasIndex(e => e.StartDate, "IX_Events_StartDate");

            entity.HasIndex(e => e.Status, "IX_Events_Status");

            entity.Property(e => e.EventId).HasColumnName("EventID");
            entity.Property(e => e.Address).HasMaxLength(500);
            entity.Property(e => e.AverageRating).HasColumnType("decimal(3, 2)");
            entity.Property(e => e.BannerImageUrl).HasMaxLength(500);
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Currency)
                .HasMaxLength(3)
                .HasDefaultValue("VND");
            entity.Property(e => e.EndDate).HasColumnType("datetime");
            entity.Property(e => e.EventName).HasMaxLength(200);
            entity.Property(e => e.EventTypeId).HasColumnName("EventTypeID");
            entity.Property(e => e.FeedbackSummary).HasMaxLength(255);
            entity.Property(e => e.IsPublic).HasDefaultValue(true);
            entity.Property(e => e.LastModified)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Location).HasMaxLength(255);
            entity.Property(e => e.OrganizerId).HasColumnName("OrganizerID");
            entity.Property(e => e.Price).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.PrivacyLevel)
                .HasMaxLength(20)
                .HasDefaultValue("Public");
            entity.Property(e => e.RegistrationDeadline).HasColumnType("datetime");
            entity.Property(e => e.StartDate).HasColumnType("datetime");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("Upcoming");
            entity.Property(e => e.Tags).HasMaxLength(500);
            entity.Property(e => e.UpdatedAt).HasColumnType("datetime");

            entity.HasOne(d => d.EventType).WithMany(p => p.Events)
                .HasForeignKey(d => d.EventTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Events__EventTyp__467D75B8");

            entity.HasOne(d => d.Organizer).WithMany(p => p.Events)
                .HasForeignKey(d => d.OrganizerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Events__Organize__4589517F");
        });

        modelBuilder.Entity<EventStatusView>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("EventStatusView");

            entity.Property(e => e.AverageRating).HasColumnType("decimal(3, 2)");
            entity.Property(e => e.ComputedStatus)
                .HasMaxLength(9)
                .IsUnicode(false);
            entity.Property(e => e.Currency).HasMaxLength(3);
            entity.Property(e => e.EndDate).HasColumnType("datetime");
            entity.Property(e => e.EventId).HasColumnName("EventID");
            entity.Property(e => e.EventName).HasMaxLength(200);
            entity.Property(e => e.EventTypeId).HasColumnName("EventTypeID");
            entity.Property(e => e.EventTypeName).HasMaxLength(100);
            entity.Property(e => e.Location).HasMaxLength(255);
            entity.Property(e => e.OrganizerId).HasColumnName("OrganizerID");
            entity.Property(e => e.OrganizerName).HasMaxLength(100);
            entity.Property(e => e.Price).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.PrivacyLevel).HasMaxLength(20);
            entity.Property(e => e.SetStatus).HasMaxLength(20);
            entity.Property(e => e.StartDate).HasColumnType("datetime");
        });

        modelBuilder.Entity<EventType>(entity =>
        {
            entity.HasKey(e => e.EventTypeId).HasName("PK__EventTyp__A9216B1F82B8958A");

            entity.HasIndex(e => e.EventTypeName, "UQ__EventTyp__29BD765FD412A6C0").IsUnique();

            entity.Property(e => e.EventTypeId).HasColumnName("EventTypeID");
            entity.Property(e => e.ColorCode).HasMaxLength(7);
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.EventTypeName).HasMaxLength(100);
            entity.Property(e => e.IconClass).HasMaxLength(50);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
        });

        modelBuilder.Entity<Feedback>(entity =>
        {
            entity.HasKey(e => e.FeedbackId).HasName("PK__Feedback__6A4BEDF631AC721C");

            entity.ToTable("Feedback");

            entity.HasIndex(e => e.EventId, "IX_Feedback_EventID");

            entity.HasIndex(e => e.Rating, "IX_Feedback_Rating");

            entity.HasIndex(e => e.RegistrationId, "UQ__Feedback__6EF5883159CFCFC5").IsUnique();

            entity.Property(e => e.FeedbackId).HasColumnName("FeedbackID");
            entity.Property(e => e.AttendeeId).HasColumnName("AttendeeID");
            entity.Property(e => e.EventId).HasColumnName("EventID");
            entity.Property(e => e.FeedbackType).HasMaxLength(20);
            entity.Property(e => e.IsApproved).HasDefaultValue(true);
            entity.Property(e => e.IsPublic).HasDefaultValue(true);
            entity.Property(e => e.RegistrationId).HasColumnName("RegistrationID");
            entity.Property(e => e.SubmittedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");

            entity.HasOne(d => d.Attendee).WithMany(p => p.Feedbacks)
                .HasForeignKey(d => d.AttendeeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Feedback__Attend__5C6CB6D7");

            entity.HasOne(d => d.Event).WithMany(p => p.Feedbacks)
                .HasForeignKey(d => d.EventId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Feedback__EventI__5B78929E");

            entity.HasOne(d => d.Registration).WithOne(p => p.Feedback)
                .HasForeignKey<Feedback>(d => d.RegistrationId)
                .HasConstraintName("FK__Feedback__Regist__5A846E65");
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(e => e.NotificationId).HasName("PK__Notifica__20CF2E32B2E6B2BA");

            entity.HasIndex(e => e.IsRead, "IX_Notifications_IsRead");

            entity.HasIndex(e => e.SentAt, "IX_Notifications_SentAt");

            entity.HasIndex(e => e.Status, "IX_Notifications_Status");

            entity.HasIndex(e => e.UserId, "IX_Notifications_UserID");

            entity.Property(e => e.NotificationId).HasColumnName("NotificationID");
            entity.Property(e => e.ErrorMessage).HasMaxLength(1000);
            entity.Property(e => e.EventId).HasColumnName("EventID");
            entity.Property(e => e.ExpiresAt).HasColumnType("datetime");
            entity.Property(e => e.Link).HasMaxLength(255);
            entity.Property(e => e.NotificationType).HasMaxLength(50);
            entity.Property(e => e.Priority)
                .HasMaxLength(10)
                .HasDefaultValue("Medium");
            entity.Property(e => e.ReadAt).HasColumnType("datetime");
            entity.Property(e => e.SentAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("Sent");
            entity.Property(e => e.Subject).HasMaxLength(255);
            entity.Property(e => e.Title).HasMaxLength(200);
            entity.Property(e => e.UserId).HasColumnName("UserID");

            entity.HasOne(d => d.Event).WithMany(p => p.Notifications)
                .HasForeignKey(d => d.EventId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK__Notificat__Event__67DE6983");

            entity.HasOne(d => d.SentByNavigation).WithMany(p => p.NotificationSentByNavigations)
                .HasForeignKey(d => d.SentBy)
                .HasConstraintName("FK__Notificat__SentB__68D28DBC");

            entity.HasOne(d => d.User).WithMany(p => p.NotificationUsers)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK__Notificat__UserI__66EA454A");
        });

        modelBuilder.Entity<Qrcode>(entity =>
        {
            entity.HasKey(e => e.QrcodeId).HasName("PK__QRCode__62FECDF236FE5E7C");

            entity.ToTable("QRCode");

            entity.HasIndex(e => e.EventId, "IX_QRCode_EventID");

            entity.HasIndex(e => e.IsActive, "IX_QRCode_IsActive");

            entity.HasIndex(e => e.RegistrationId, "UQ__QRCode__6EF58831DF10237E").IsUnique();

            entity.HasIndex(e => e.QrcodeValue, "UQ__QRCode__EA5BB921DAF607FE").IsUnique();

            entity.Property(e => e.QrcodeId).HasColumnName("QRCodeID");
            entity.Property(e => e.EventId).HasColumnName("EventID");
            entity.Property(e => e.ExpiresAt).HasColumnType("datetime");
            entity.Property(e => e.GeneratedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.QrcodeImageUrl)
                .HasMaxLength(500)
                .HasColumnName("QRCodeImageUrl");
            entity.Property(e => e.QrcodeValue)
                .HasMaxLength(255)
                .HasColumnName("QRCodeValue");
            entity.Property(e => e.RegistrationId).HasColumnName("RegistrationID");
            entity.Property(e => e.UsedAt).HasColumnType("datetime");

            entity.HasOne(d => d.Event).WithMany(p => p.Qrcodes)
                .HasForeignKey(d => d.EventId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__QRCode__EventID__7167D3BD");

            entity.HasOne(d => d.Registration).WithOne(p => p.Qrcode)
                .HasForeignKey<Qrcode>(d => d.RegistrationId)
                .HasConstraintName("FK__QRCode__Registra__7073AF84");

            entity.HasOne(d => d.UsedByNavigation).WithMany(p => p.Qrcodes)
                .HasForeignKey(d => d.UsedBy)
                .HasConstraintName("FK__QRCode__UsedBy__725BF7F6");
        });

        modelBuilder.Entity<Registration>(entity =>
        {
            entity.HasKey(e => e.RegistrationId).HasName("PK__Registra__6EF58830DAA84D36");

            entity.HasIndex(e => e.AttendeeId, "IX_Registrations_AttendeeID");

            entity.HasIndex(e => e.EventId, "IX_Registrations_EventID");

            entity.HasIndex(e => e.Status, "IX_Registrations_Status");

            entity.HasIndex(e => new { e.EventId, e.AttendeeId }, "UQ_Event_Attendee")
                .IsUnique()
                .HasFilter("([IsDeleted]=(0))");

            entity.Property(e => e.RegistrationId).HasColumnName("RegistrationID");
            entity.Property(e => e.AttendeeId).HasColumnName("AttendeeID");
            entity.Property(e => e.CancellationReason).HasMaxLength(500);
            entity.Property(e => e.CheckInLocation).HasMaxLength(255);
            entity.Property(e => e.CheckInMethod).HasMaxLength(20);
            entity.Property(e => e.CheckInTime).HasColumnType("datetime");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.EventId).HasColumnName("EventID");
            entity.Property(e => e.RegistrationDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.SpecialRequests).HasMaxLength(1000);
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("Registered");
            entity.Property(e => e.UpdatedAt).HasColumnType("datetime");

            entity.HasOne(d => d.Attendee).WithMany(p => p.RegistrationAttendees)
                .HasForeignKey(d => d.AttendeeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Registrat__Atten__50FB042B");

            entity.HasOne(d => d.CheckInByNavigation).WithMany(p => p.RegistrationCheckInByNavigations)
                .HasForeignKey(d => d.CheckInBy)
                .HasConstraintName("FK__Registrat__Check__51EF2864");

            entity.HasOne(d => d.Event).WithMany(p => p.Registrations)
                .HasForeignKey(d => d.EventId)
                .HasConstraintName("FK__Registrat__Event__5006DFF2");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.RoleId).HasName("PK__Roles__8AFACE3A1623BF62");

            entity.HasIndex(e => e.RoleName, "UQ__Roles__8A2B61606A7DE3D8").IsUnique();

            entity.Property(e => e.RoleId).HasColumnName("RoleID");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Description).HasMaxLength(255);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.RoleName).HasMaxLength(50);
        });

        modelBuilder.Entity<SocialShare>(entity =>
        {
            entity.HasKey(e => e.ShareId).HasName("PK__SocialSh__D32A3F8E3A12FCE5");

            entity.HasIndex(e => e.EventId, "IX_SocialShares_EventID");

            entity.HasIndex(e => e.Platform, "IX_SocialShares_Platform");

            entity.Property(e => e.ShareId).HasColumnName("ShareID");
            entity.Property(e => e.EventId).HasColumnName("EventID");
            entity.Property(e => e.Ipaddress)
                .HasMaxLength(45)
                .HasColumnName("IPAddress");
            entity.Property(e => e.Platform).HasMaxLength(50);
            entity.Property(e => e.ShareStatus).HasMaxLength(20);
            entity.Property(e => e.ShareText).HasMaxLength(1000);
            entity.Property(e => e.SharedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.SharedUrl).HasMaxLength(255);
            entity.Property(e => e.UserAgent).HasMaxLength(500);
            entity.Property(e => e.UserId).HasColumnName("UserID");

            entity.HasOne(d => d.Event).WithMany(p => p.SocialShares)
                .HasForeignKey(d => d.EventId)
                .HasConstraintName("FK__SocialSha__Event__7814D14C");

            entity.HasOne(d => d.User).WithMany(p => p.SocialShares)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK__SocialSha__UserI__7908F585");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PK__Users__1788CCACB8556C2F");

            entity.HasIndex(e => e.Email, "IX_Users_Email");

            entity.HasIndex(e => new { e.IsActive, e.IsDeleted }, "IX_Users_IsActive_IsDeleted");

            entity.HasIndex(e => e.Email, "UQ__Users__A9D10534F875BE69").IsUnique();

            entity.Property(e => e.UserId).HasColumnName("UserID");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.EmailVerificationToken).HasMaxLength(255);
            entity.Property(e => e.FullName).HasMaxLength(100);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.LastLoginAt).HasColumnType("datetime");
            entity.Property(e => e.PasswordHash).HasMaxLength(255);
            entity.Property(e => e.PasswordResetExpires).HasColumnType("datetime");
            entity.Property(e => e.PasswordResetToken).HasMaxLength(255);
            entity.Property(e => e.PhoneNumber).HasMaxLength(20);
            entity.Property(e => e.ProfileImageUrl).HasMaxLength(500);
            entity.Property(e => e.UpdatedAt).HasColumnType("datetime");
        });

        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.HasKey(e => new { e.UserId, e.RoleId }).HasName("PK__UserRole__AF27604FEB6DE6F6");

            entity.Property(e => e.UserId).HasColumnName("UserID");
            entity.Property(e => e.RoleId).HasColumnName("RoleID");
            entity.Property(e => e.AssignedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.IsActive).HasDefaultValue(true);

            entity.HasOne(d => d.AssignedByNavigation).WithMany(p => p.UserRoleAssignedByNavigations)
                .HasForeignKey(d => d.AssignedBy)
                .HasConstraintName("FK__UserRoles__Assig__2EA5EC27");

            entity.HasOne(d => d.Role).WithMany(p => p.UserRoles)
                .HasForeignKey(d => d.RoleId)
                .HasConstraintName("FK__UserRoles__RoleI__2DB1C7EE");

            entity.HasOne(d => d.User).WithMany(p => p.UserRoleUsers)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK__UserRoles__UserI__2CBDA3B5");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
