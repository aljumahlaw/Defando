using Defando.Models;
using Microsoft.EntityFrameworkCore;

namespace Defando.Data;

/// <summary>
/// Entity Framework Core context configuring the legal document schema.
/// </summary>
public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Folder> Folders => Set<Folder>();
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<DocumentVersion> DocumentVersions => Set<DocumentVersion>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<Settings> Settings => Set<Settings>();
    public DbSet<Outgoing> OutgoingRecords => Set<Outgoing>();
    public DbSet<Incoming> IncomingRecords => Set<Incoming>();
    public DbSet<TaskItem> Tasks => Set<TaskItem>();
    public DbSet<TaskComment> TaskComments => Set<TaskComment>();
    public DbSet<OcrQueue> OcrQueueItems => Set<OcrQueue>();
    public DbSet<OcrQueue> OcrQueue { get; set; } = null!;
    public DbSet<SharedLink> SharedLinks => Set<SharedLink>();
    public DbSet<EmailLog> EmailLogs => Set<EmailLog>();
    public DbSet<LinkAccessLog> LinkAccessLogs => Set<LinkAccessLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureUsers(modelBuilder);
        ConfigureFolders(modelBuilder);
        ConfigureDocuments(modelBuilder);
        ConfigureDocumentVersions(modelBuilder);
        ConfigureAuditLog(modelBuilder);
        ConfigureSettings(modelBuilder);
        ConfigureOutgoing(modelBuilder);
        ConfigureIncoming(modelBuilder);
        ConfigureTasks(modelBuilder);
        ConfigureTaskComments(modelBuilder);
        ConfigureOcrQueue(modelBuilder);
        ConfigureSharedLinks(modelBuilder);
        ConfigureEmailLog(modelBuilder);
        ConfigureLinkAccessLog(modelBuilder);
    }

    private static void ConfigureUsers(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(e => e.Role)
                  .HasDatabaseName("idx_users_role");

            entity.HasCheckConstraint(
                "chk_users_role",
                "role IN ('admin','senior_lawyer','junior_lawyer','legal_researcher','archiving','accountant','read_only')");

            entity.HasMany(e => e.CreatedFolders)
                  .WithOne(e => e.CreatedByUser)
                  .HasForeignKey(e => e.CreatedBy)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasMany(e => e.UploadedDocuments)
                  .WithOne(e => e.UploadedByUser)
                  .HasForeignKey(e => e.UploadedBy)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasMany(e => e.LockedDocuments)
                  .WithOne(e => e.LockedByUser)
                  .HasForeignKey(e => e.LockedBy)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasMany(e => e.DocumentVersions)
                  .WithOne(e => e.CreatedByUser)
                  .HasForeignKey(e => e.CreatedBy)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasMany(e => e.AuditLogs)
                  .WithOne(e => e.User)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasMany(e => e.OutgoingRecords)
                  .WithOne(e => e.CreatedByUser)
                  .HasForeignKey(e => e.CreatedBy)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasMany(e => e.IncomingRecords)
                  .WithOne(e => e.CreatedByUser)
                  .HasForeignKey(e => e.CreatedBy)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasMany(e => e.AssignedTasks)
                  .WithOne(e => e.AssignedToUser)
                  .HasForeignKey(e => e.AssignedTo)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.DelegatedTasks)
                  .WithOne(e => e.AssignedByUser)
                  .HasForeignKey(e => e.AssignedBy)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(e => e.TaskComments)
                  .WithOne(e => e.User)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasMany(e => e.SharedLinks)
                  .WithOne(e => e.CreatedByUser)
                  .HasForeignKey(e => e.CreatedBy)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasMany(e => e.EmailLogs)
                  .WithOne(e => e.SentByUser)
                  .HasForeignKey(e => e.SentBy)
                  .OnDelete(DeleteBehavior.SetNull);
        });
    }

    private static void ConfigureFolders(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Folder>(entity =>
        {
            entity.HasIndex(e => e.ParentId)
                  .HasDatabaseName("idx_folders_parent");

            entity.HasIndex(e => e.CreatedBy)
                  .HasDatabaseName("idx_folders_created_by");

            entity.HasOne(e => e.ParentFolder)
                  .WithMany(e => e.ChildFolders)
                  .HasForeignKey(e => e.ParentId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureDocuments(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Document>(entity =>
        {
            entity.HasIndex(e => e.FolderId)
                  .HasDatabaseName("idx_documents_folder");

            entity.HasIndex(e => e.UploadedBy)
                  .HasDatabaseName("idx_documents_uploaded_by");

            entity.HasIndex(e => e.LockedBy)
                  .HasDatabaseName("idx_documents_locked_by");

            entity.HasIndex(e => e.Tags)
                  .HasDatabaseName("idx_documents_tags")
                  .HasMethod("gin");

            entity.HasIndex(e => e.SearchVector)
                  .HasDatabaseName("idx_documents_search_vector")
                  .HasMethod("gin");

            entity.Property(e => e.Tags)
                  .HasColumnType("text[]");

            entity.Property(e => e.Metadata)
                  .HasColumnType("jsonb");

            entity.HasOne(e => e.Folder)
                  .WithMany(e => e.Documents)
                  .HasForeignKey(e => e.FolderId)
                  .OnDelete(DeleteBehavior.SetNull);
        });
    }

    private static void ConfigureDocumentVersions(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DocumentVersion>(entity =>
        {
            entity.HasIndex(e => e.DocumentId)
                  .HasDatabaseName("idx_doc_versions_document");

            entity.HasIndex(e => e.CreatedBy)
                  .HasDatabaseName("idx_doc_versions_created_by");

            entity.HasOne(e => e.Document)
                  .WithMany(e => e.Versions)
                  .HasForeignKey(e => e.DocumentId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureAuditLog(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasIndex(e => e.UserId)
                  .HasDatabaseName("idx_audit_user");

            entity.HasIndex(e => e.CreatedAt)
                  .HasDatabaseName("idx_audit_created_at");

            entity.HasIndex(e => new { e.EntityType, e.EntityId })
                  .HasDatabaseName("idx_audit_entity");
        });
    }

    private static void ConfigureSettings(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Settings>();
    }

    private static void ConfigureOutgoing(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Outgoing>(entity =>
        {
            entity.HasIndex(e => e.SendDate)
                  .HasDatabaseName("idx_outgoing_send_date");

            entity.HasIndex(e => e.RecipientName)
                  .HasDatabaseName("idx_outgoing_recipient");

            entity.HasOne(e => e.Document)
                  .WithMany(e => e.OutgoingRecords)
                  .HasForeignKey(e => e.DocumentId)
                  .OnDelete(DeleteBehavior.SetNull);
        });
    }

    private static void ConfigureIncoming(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Incoming>(entity =>
        {
            entity.HasIndex(e => e.ReceivedDate)
                  .HasDatabaseName("idx_incoming_received_date");

            entity.HasIndex(e => e.SenderName)
                  .HasDatabaseName("idx_incoming_sender");

            entity.HasIndex(e => e.Priority)
                  .HasDatabaseName("idx_incoming_priority");

            entity.HasCheckConstraint(
                "chk_incoming_priority",
                "priority IN ('normal','urgent','confidential')");

            entity.HasOne(e => e.Document)
                  .WithMany(e => e.IncomingRecords)
                  .HasForeignKey(e => e.DocumentId)
                  .OnDelete(DeleteBehavior.SetNull);
        });
    }

    private static void ConfigureTasks(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TaskItem>(entity =>
        {
            entity.HasIndex(e => e.AssignedTo)
                  .HasDatabaseName("idx_tasks_assigned_to");

            entity.HasIndex(e => e.Status)
                  .HasDatabaseName("idx_tasks_status");

            entity.HasIndex(e => e.DueDate)
                  .HasDatabaseName("idx_tasks_due_date");

            entity.HasCheckConstraint(
                "chk_tasks_priority",
                "priority IN ('low','normal','high','critical')");

            entity.HasCheckConstraint(
                "chk_tasks_status",
                "status IN ('pending','in_progress','completed','cancelled')");

            entity.HasOne(e => e.Document)
                  .WithMany(e => e.Tasks)
                  .HasForeignKey(e => e.DocumentId)
                  .OnDelete(DeleteBehavior.SetNull);
        });
    }

    private static void ConfigureTaskComments(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TaskComment>(entity =>
        {
            entity.HasIndex(e => e.TaskId)
                  .HasDatabaseName("idx_task_comments_task");

            entity.HasIndex(e => e.UserId)
                  .HasDatabaseName("idx_task_comments_user");

            entity.HasOne(e => e.Task)
                  .WithMany(e => e.Comments)
                  .HasForeignKey(e => e.TaskId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureOcrQueue(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<OcrQueue>(entity =>
        {
            entity.HasIndex(e => e.Status)
                  .HasDatabaseName("idx_ocr_queue_status");

            entity.HasIndex(e => e.DocumentId)
                  .HasDatabaseName("idx_ocr_queue_document");

            entity.HasCheckConstraint(
                "chk_ocr_status",
                "status IN ('pending','processing','completed','failed')");

            entity.HasOne(e => e.Document)
                  .WithMany(e => e.OcrQueueItems)
                  .HasForeignKey(e => e.DocumentId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureSharedLinks(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SharedLink>(entity =>
        {
            entity.HasIndex(e => e.DocumentId)
                  .HasDatabaseName("idx_shared_links_document");

            entity.HasIndex(e => new { e.IsActive, e.ExpiresAt })
                  .HasDatabaseName("idx_shared_links_active");

            entity.HasOne(e => e.Document)
                  .WithMany(e => e.SharedLinks)
                  .HasForeignKey(e => e.DocumentId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureEmailLog(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<EmailLog>(entity =>
        {
            entity.HasIndex(e => e.DocumentId)
                  .HasDatabaseName("idx_email_log_document");

            entity.HasIndex(e => e.SentBy)
                  .HasDatabaseName("idx_email_log_sent_by");

            entity.HasIndex(e => e.SentAt)
                  .HasDatabaseName("idx_email_log_sent_at");

            entity.HasCheckConstraint(
                "chk_email_status",
                "status IN ('sent','failed','queued')");

            entity.HasOne(e => e.Document)
                  .WithMany(e => e.EmailLogs)
                  .HasForeignKey(e => e.DocumentId)
                  .OnDelete(DeleteBehavior.SetNull);
        });
    }

    private static void ConfigureLinkAccessLog(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<LinkAccessLog>(entity =>
        {
            entity.HasIndex(e => e.LinkId)
                  .HasDatabaseName("idx_link_access_log_link");

            entity.HasIndex(e => e.AccessedAt)
                  .HasDatabaseName("idx_link_access_log_accessed_at");

            entity.HasOne(e => e.SharedLink)
                  .WithMany(e => e.AccessLogs)
                  .HasForeignKey(e => e.LinkId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }
}

