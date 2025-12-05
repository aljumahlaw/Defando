# PHASE 1 – TYPE & USAGE INVENTORY REPORT

**Date:** Generated from codebase analysis  
**Status:** READ-ONLY DIAGNOSTIC - NO MODIFICATIONS MADE

---

## 1. Inventory.SmtpSettings

### SmtpSettings Variant #1
- **Namespace:** `LegalDocSystem.Models`
- **FilePath:** `src/Models/SmtpSettings.cs`
- **Properties:**
  - `Id (int)`
  - `Enabled (bool)`
  - `Host (string)`
  - `Port (int)`
  - `Username (string)`
  - `Password (string)`
  - `UseSsl (bool)`
  - `FromName (string)`
  - `FromAddress (string)`
  - `NotifyOnSharedLinkCreated (bool)`
  - `NotifyOnSharedLinkAccessed (bool)`
  - `NotifyOnTaskReminder (bool)`

### SmtpSettings Variant #2
- **Namespace:** `LegalDocSystem.Services`
- **FilePath:** `src/Services/IEmailService.cs`
- **Properties:**
  - `Host (string)`
  - `Port (int)`
  - `Username (string)`
  - `Password (string)`
  - `UseSsl (bool)`
  - `FromName (string)`
  - `FromAddress (string)`
  - `Enabled (bool)`

**Note:** Two different `SmtpSettings` classes exist in different namespaces. The `LegalDocSystem.Models.SmtpSettings` has additional notification properties (`NotifyOnSharedLinkCreated`, `NotifyOnSharedLinkAccessed`, `NotifyOnTaskReminder`) and an `Id` property, while `LegalDocSystem.Services.SmtpSettings` does not.

---

## 2. Inventory.Models

### Incoming
- **Namespace:** `LegalDocSystem.Models`
- **FilePath:** `src/Models/Incoming.cs`
- **Properties:**
  - `IncomingId (int)`
  - `DocumentId (int?)`
  - `Document (Document?)`
  - `IncomingNumber (string)`
  - `ReceivedDate (DateTime)`
  - `SenderName (string)`
  - `SenderEmail (string)`
  - `Subject (string)`
  - `SenderAddress (string?)`
  - `OriginalNumber (string?)`
  - `Notes (string?)`
  - `Priority (string)`
  - `RequiresResponse (bool)`
  - `ResponseDeadline (DateTime?)`
  - `CreatedBy (int?)`
  - `CreatedByUser (User?)`

### Outgoing
- **Namespace:** `LegalDocSystem.Models`
- **FilePath:** `src/Models/Outgoing.cs`
- **Properties:**
  - `OutgoingId (int)`
  - `DocumentId (int?)`
  - `Document (Document?)`
  - `OutgoingNumber (string)`
  - `SendDate (DateTime)`
  - `RecipientName (string)`
  - `RecipientEmail (string)`
  - `Subject (string)`
  - `DeliveryMethod (string)`
  - `TrackingNumber (string?)`
  - `RecipientAddress (string)`
  - `Notes (string)`
  - `CreatedBy (int?)`
  - `CreatedByUser (User?)`

### User
- **Namespace:** `LegalDocSystem.Models`
- **FilePath:** `src/Models/User.cs`
- **Properties:**
  - `UserId (int)`
  - `Username (string)`
  - `PasswordHash (string)`
  - `FullName (string)`
  - `Email (string?)`
  - `Role (string)`
  - `IsActive (bool)`
  - `CreatedAt (DateTime)`
  - `LastLogin (DateTime?)`
  - `FailedLoginAttempts (int)`
  - `LockedUntil (DateTime?)`
  - `CreatedFolders (ICollection<Folder>)`
  - `LockedDocuments (ICollection<Document>)`
  - `UploadedDocuments (ICollection<Document>)`
  - `DocumentVersions (ICollection<DocumentVersion>)`
  - `AuditLogs (ICollection<AuditLog>)`
  - `OutgoingRecords (ICollection<Outgoing>)`
  - `IncomingRecords (ICollection<Incoming>)`
  - `AssignedTasks (ICollection<TaskItem>)`
  - `DelegatedTasks (ICollection<TaskItem>)`
  - `TaskComments (ICollection<TaskComment>)`
  - `SharedLinks (ICollection<SharedLink>)`
  - `EmailLogs (ICollection<EmailLog>)`

### TaskItem
- **Namespace:** `LegalDocSystem.Models`
- **FilePath:** `src/Models/TaskItem.cs`
- **Properties:**
  - `TaskId (int)`
  - `TaskTitle (string)`
  - `TaskDescription (string?)`
  - `AssignedTo (int)`
  - `AssignedToUser (User)`
  - `AssignedBy (int)`
  - `AssignedByUser (User)`
  - `DocumentId (int?)`
  - `Document (Document?)`
  - `Priority (string)`
  - `Status (string)`
  - `DueDate (DateTime?)`
  - `CompletedAt (DateTime?)`
  - `Notes (string?)`
  - `CreatedAt (DateTime)`
  - `UpdatedAt (DateTime)`
  - `Comments (ICollection<TaskComment>)`

### OcrQueue
- **Namespace:** `LegalDocSystem.Models`
- **FilePath:** `src/Models/OcrQueue.cs`
- **Properties:**
  - `QueueId (int)`
  - `DocumentId (int)`
  - `Document (Document)`
  - `Status (string)`
  - `ErrorMessage (string?)`
  - `CreatedAt (DateTime)`
  - `ProcessedAt (DateTime?)`

---

## 3. Inventory.Interfaces

### INotificationService
- **Namespace:** `LegalDocSystem.Services`
- **FilePath:** `src/Services/INotificationService.cs`
- **Methods:**
  - `ShowSuccess(string message, int duration = 3000) (void)`
  - `ShowError(string message, int duration = 5000) (void)`
  - `ShowWarning(string message, int duration = 4000) (void)`
  - `ShowInfo(string message, int duration = 3000) (void)`
- **Properties:** None (interface has no properties)

### IFileStorageService
- **Namespace:** `LegalDocSystem.Services`
- **FilePath:** `src/Services/IFileStorageService.cs`
- **Methods:**
  - `SaveFileAsync(Stream fileStream, string fileName) (Task<string>)`
  - `GetFileAsync(string filePath) (Task<Stream>)`
  - `DeleteFileAsync(string filePath) (Task)`
  - `FileExistsAsync(string filePath) (Task<bool>)`
  - `GetFileSizeAsync(string filePath) (Task<long>)`
  - `GenerateFilePath(string originalFileName) (string)`
- **Properties:** None (interface has no properties)

---

## 4. Inventory.ServicesUsingSmtpSettings

### EmailService
- **FilePath:** `src/Services/EmailService.cs`
- **How SmtpSettings is obtained:** 
  - Retrieved from database via `ApplicationDbContext.Settings` table
  - Settings are stored as key-value pairs (e.g., "smtp_host", "smtp_port", etc.)
  - Cached in private field `_cachedSettings`
  - Retrieved via `GetSmtpSettingsAsync()` method
- **Methods using SmtpSettings:**
  - `SendEmailAsync(...)` - Uses: `Enabled`, `Host`, `Port`, `UseSsl`, `FromName`, `FromAddress`, `Username`, `Password`
  - `SendTestEmailAsync(string toEmail)` - Uses: `Enabled`, `Host`, `Port`, `UseSsl`, `FromName`, `FromAddress`, `Username`, `Password`
  - `ValidateSmtpSettingsAsync()` - Uses: `Host`, `Port`, `Username`, `Password`, `FromAddress`, `UseSsl`
  - `GetSmtpSettingsAsync()` - Reads from database and constructs `SmtpSettings` object, uses: `Host`, `Port`, `Username`, `Password`, `UseSsl`, `FromName`, `FromAddress`, `Enabled`, `NotifyOnSharedLinkCreated`, `NotifyOnSharedLinkAccessed`, `NotifyOnTaskReminder`
  - `SaveSmtpSettingsAsync(SmtpSettings settings)` - Writes to database, uses: `Host`, `Port`, `Username`, `Password`, `UseSsl`, `FromName`, `FromAddress`, `Enabled`, `NotifyOnSharedLinkCreated`, `NotifyOnSharedLinkAccessed`, `NotifyOnTaskReminder`
  - `SendSharedLinkCreatedNotificationAsync(...)` - Uses: `Enabled`, `NotifyOnSharedLinkCreated`
  - `SendSharedLinkAccessedNotificationAsync(...)` - Uses: `Enabled`, `NotifyOnSharedLinkAccessed`
- **SmtpSettings properties used:**
  - `Host (string)`
  - `Port (int)`
  - `Username (string)`
  - `Password (string)`
  - `UseSsl (bool)`
  - `FromName (string)`
  - `FromAddress (string)`
  - `Enabled (bool)`
  - `NotifyOnSharedLinkCreated (bool)`
  - `NotifyOnSharedLinkAccessed (bool)`
  - `NotifyOnTaskReminder (bool)`

### SharedLinkService
- **FilePath:** `src/Services/SharedLinkService.cs`
- **How SmtpSettings is obtained:** 
  - Does not directly use `SmtpSettings`
  - Uses `IEmailService` via dependency injection (obtained from `IServiceProvider`)
- **Methods using EmailService (which uses SmtpSettings):**
  - `CreateSharedLinkAsync(...)` - Calls `emailService.SendSharedLinkCreatedNotificationAsync(...)`
- **SmtpSettings properties indirectly used:** 
  - Via `IEmailService.SendSharedLinkCreatedNotificationAsync()`, which checks: `Enabled`, `NotifyOnSharedLinkCreated`

### BackgroundJobsService
- **FilePath:** `src/Services/BackgroundJobsService.cs`
- **How SmtpSettings is obtained:** 
  - Does not directly use `SmtpSettings`
  - Uses `IEmailService` via dependency injection (obtained from `IServiceProvider`)
- **Methods using EmailService (which uses SmtpSettings):**
  - `SendEmailNotificationsAsync()` - Calls `emailService.SendEmailWithRetryAsync(...)` for task reminders
- **SmtpSettings properties indirectly used:** 
  - Via `IEmailService.SendEmailWithRetryAsync()`, which uses: `Enabled`, `Host`, `Port`, `UseSsl`, `FromName`, `FromAddress`, `Username`, `Password`
  - Note: Task reminder notifications should check `NotifyOnTaskReminder`, but this check is not currently implemented in `SendEmailNotificationsAsync()`

---

## 5. Inventory.RazorDependencies

### SmtpSettings.razor
- **FilePath:** `src/Components/Pages/SmtpSettings.razor`
- **Types used:**
  - `SmtpSettings` (from `LegalDocSystem.Services` namespace, inferred from usage)
- **Symbol names in @code:**
  - `private SmtpSettings smtpSettings = new();`
- **Property accesses:**
  - `smtpSettings.Enabled`
  - `smtpSettings.Host`
  - `smtpSettings.Port`
  - `smtpSettings.Username`
  - `smtpSettings.Password`
  - `smtpSettings.UseSsl`
  - `smtpSettings.FromName`
  - `smtpSettings.FromAddress`
  - `smtpSettings.NotifyOnSharedLinkCreated`
  - `smtpSettings.NotifyOnSharedLinkAccessed`
  - `smtpSettings.NotifyOnTaskReminder`
- **@rendermode InteractiveServer:**
  - Line 2: `@rendermode InteractiveServer`

### Incoming.razor
- **FilePath:** `src/Components/Pages/Incoming.razor`
- **Types used:**
  - `Incoming` (from `LegalDocSystem.Models`)
  - `INotificationService`
- **Symbol names in @code:**
  - `private List<LegalDocSystem.Models.Incoming>? incomingList;`
  - `private LegalDocSystem.Models.Incoming? selectedIncoming;`
  - `private IncomingModel incomingModel = new();` (local model class)
- **Property accesses:**
  - `incoming.IncomingNumber`
  - `incoming.ReceivedDate`
  - `incoming.SenderName`
  - `incoming.Subject`
  - `incoming.Priority`
  - `incoming.RequiresResponse`
  - `incoming.ResponseDeadline`
  - `incoming.Document`
  - `incoming.Document.DocumentId`
  - `incoming.Document.DocumentName`
  - `incoming.SenderAddress`
  - `incoming.OriginalNumber`
  - `incoming.Notes`
  - `incoming.CreatedByUser`
  - `incoming.CreatedByUser.FullName`
  - `selectedIncoming.IncomingId`
  - `selectedIncoming.IncomingNumber`
  - `selectedIncoming.ReceivedDate`
  - `selectedIncoming.SenderName`
  - `selectedIncoming.SenderAddress`
  - `selectedIncoming.Subject`
  - `selectedIncoming.OriginalNumber`
  - `selectedIncoming.Priority`
  - `selectedIncoming.RequiresResponse`
  - `selectedIncoming.ResponseDeadline`
  - `selectedIncoming.Document`
  - `selectedIncoming.Document.DocumentId`
  - `selectedIncoming.Document.DocumentName`
  - `selectedIncoming.CreatedByUser`
  - `selectedIncoming.CreatedByUser.FullName`
  - `selectedIncoming.Notes`
- **INotificationService usage:**
  - `NotificationService.ShowError(...)`
  - `NotificationService.ShowSuccess(...)`
- **@rendermode InteractiveServer:**
  - Line 3: `@rendermode InteractiveServer`

### Outgoing.razor
- **FilePath:** `src/Components/Pages/Outgoing.razor`
- **Types used:**
  - `Outgoing` (from `LegalDocSystem.Models`)
  - `INotificationService`
- **Symbol names in @code:**
  - `private List<LegalDocSystem.Models.Outgoing>? outgoingList;`
  - `private LegalDocSystem.Models.Outgoing? selectedOutgoing;`
  - `private OutgoingModel outgoingModel = new OutgoingModel();` (local model class)
- **Property accesses:**
  - `outgoing.OutgoingNumber`
  - `outgoing.SendDate`
  - `outgoing.RecipientName`
  - `outgoing.Subject`
  - `outgoing.DeliveryMethod`
  - `outgoing.Document`
  - `outgoing.Document.DocumentId`
  - `outgoing.Document.DocumentName`
  - `selectedOutgoing.OutgoingId`
  - `selectedOutgoing.OutgoingNumber`
  - `selectedOutgoing.SendDate`
  - `selectedOutgoing.RecipientName`
  - `selectedOutgoing.RecipientAddress`
  - `selectedOutgoing.Subject`
  - `selectedOutgoing.DeliveryMethod`
  - `selectedOutgoing.TrackingNumber`
  - `selectedOutgoing.Document`
  - `selectedOutgoing.Document.DocumentId`
  - `selectedOutgoing.Document.DocumentName`
  - `selectedOutgoing.Notes`
  - `selectedOutgoing.CreatedBy`
  - `outgoingModel.OutgoingId`
  - `outgoingModel.OutgoingNumber`
  - `outgoingModel.SendDate`
  - `outgoingModel.RecipientName`
  - `outgoingModel.RecipientAddress`
  - `outgoingModel.Subject`
  - `outgoingModel.DeliveryMethod`
  - `outgoingModel.TrackingNumber`
  - `outgoingModel.DocumentId`
  - `outgoingModel.Notes`
- **INotificationService usage:**
  - `NotificationService.ShowError(...)`
  - `NotificationService.ShowSuccess(...)`
- **User property access:**
  - `user.HasPermission(...)` (extension method, not direct property)
- **@rendermode InteractiveServer:**
  - Line 3: `@rendermode InteractiveServer`

### Tasks.razor
- **FilePath:** `src/Components/Pages/Tasks.razor`
- **Types used:**
  - `TaskItem` (from `LegalDocSystem.Models`, inferred from usage)
- **Symbol names in @code:**
  - `private List<TaskItem> tasks = new();`
  - `private List<TaskItem> filteredTasks = new();`
- **Property accesses:**
  - `task.TaskTitle`
  - `task.TaskDescription`
  - `task.AssignedToUser`
  - `task.AssignedToUser.FullName`
  - `task.Status`
  - `task.Priority`
  - `task.DueDate`
  - `task.TaskId`
- **@rendermode InteractiveServer:**
  - Line 2: `@rendermode InteractiveServer`

### Users.razor
- **FilePath:** `src/Components/Pages/Users.razor`
- **Types used:**
  - `User` (from `LegalDocSystem.Models`, inferred from usage)
- **Symbol names in @code:**
  - `private List<User> users = new();`
- **Property accesses:**
  - `user.Username`
  - `user.FullName`
  - `user.Email`
  - `user.Role`
  - `user.IsActive`
  - `user.CreatedAt`
  - `user.LastLogin`
  - `user.UserId`
- **@rendermode InteractiveServer:**
  - Line 2: `@rendermode InteractiveServer`

### DocumentDetails.razor
- **FilePath:** `src/Components/Pages/DocumentDetails.razor`
- **Types used:**
  - `IFileStorageService`
- **Symbol names in @code:**
  - `@inject IFileStorageService FileStorageService`
- **Property accesses:**
  - None (only method calls)
- **IFileStorageService method calls:**
  - `FileStorageService.GetFileAsync(document.FilePath)`
- **@rendermode InteractiveServer:**
  - Line 2: `@rendermode InteractiveServer`

### Documents.razor
- **FilePath:** `src/Components/Pages/Documents.razor`
- **Types used:**
  - None of the specified types (uses `Document` model, which is not in the target list)
- **@rendermode InteractiveServer:**
  - Line 2: `@rendermode InteractiveServer`

---

## Summary

- **SmtpSettings variants found:** 2 (in `LegalDocSystem.Models` and `LegalDocSystem.Services`)
- **Core models found:** All 5 types exist (Incoming, Outgoing, User, TaskItem, OcrQueue)
- **Interfaces found:** Both interfaces exist (INotificationService, IFileStorageService)
- **Services using SmtpSettings:** 3 services (EmailService directly, SharedLinkService and BackgroundJobsService indirectly via IEmailService)
- **Razor components with @rendermode InteractiveServer:** 7 components found
- **Razor components using target types:** 6 components (SmtpSettings, Incoming, Outgoing, Tasks, Users, DocumentDetails)

---

**END OF PHASE 1 REPORT**
