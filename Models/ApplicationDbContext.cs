using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace DetailTrack.Models;

public partial class ApplicationDbContext : DbContext
{
    public ApplicationDbContext()
    {
    }

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Comment> Comments { get; set; }

    public virtual DbSet<MachineModel> MachineModels { get; set; }

    public virtual DbSet<MachineType> MachineTypes { get; set; }

    public virtual DbSet<Request> Requests { get; set; }

    public virtual DbSet<RequestFile> RequestFiles { get; set; }

    public virtual DbSet<RequestHistory> RequestHistories { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<Specialization> Specializations { get; set; }

    public virtual DbSet<ToolRequest> ToolRequests { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<Workshop> Workshops { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Server=localhost\\SQLEXPRESS;Database=DetailTrackDB; Integrated Security=True; TrustServerCertificate=True;");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Comment>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Comments__3214EC07F5E921C3");

            entity.HasIndex(e => e.RequestId, "IX_Comments_RequestId");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");

            entity.HasOne(d => d.Request).WithMany(p => p.Comments)
                .HasForeignKey(d => d.RequestId)
                .HasConstraintName("FK_Comments_Request");

            entity.HasOne(d => d.ToolRequest).WithMany(p => p.Comments)
                .HasForeignKey(d => d.ToolRequestId)
                .HasConstraintName("FK_Comments_ToolRequest");

            entity.HasOne(d => d.User).WithMany(p => p.Comments)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Comments_User");
        });

        modelBuilder.Entity<MachineModel>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__MachineM__3214EC07319320D9");

            entity.Property(e => e.Name).HasMaxLength(100);

            entity.HasOne(d => d.MachineType).WithMany(p => p.MachineModels)
                .HasForeignKey(d => d.MachineTypeId)
                .HasConstraintName("FK_MachineModels_MachineType");
        });

        modelBuilder.Entity<MachineType>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__MachineT__3214EC079F4AAFDC");

            entity.Property(e => e.Name).HasMaxLength(100);
        });

        modelBuilder.Entity<Request>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Requests__3214EC0724D69DEB");

            entity.HasIndex(e => e.CreatedAt, "IX_Requests_CreatedAt");

            entity.HasIndex(e => e.DetailName, "IX_Requests_DetailName");

            entity.HasIndex(e => e.MachineTypeId, "IX_Requests_MachineTypeId");

            entity.HasIndex(e => e.ProgrammerId, "IX_Requests_ProgrammerId");

            entity.HasIndex(e => e.SetupTechnicianId, "IX_Requests_SetupTechnicianId");

            entity.HasIndex(e => e.Status, "IX_Requests_Status");

            entity.HasIndex(e => e.TechnologistId, "IX_Requests_TechnologistId");

            entity.HasIndex(e => e.WorkshopId, "IX_Requests_WorkshopId");

            entity.HasIndex(e => e.RequestNumber, "UQ__Requests__9ADA6BE0634A9BAD").IsUnique();

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.DetailName).HasMaxLength(200);
            entity.Property(e => e.RequestNumber).HasMaxLength(50);
            entity.Property(e => e.Status).HasMaxLength(50);
            entity.Property(e => e.Type).HasMaxLength(20);

            entity.HasOne(d => d.Constructor).WithMany(p => p.RequestConstructors)
                .HasForeignKey(d => d.ConstructorId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Requests_Constructor");

            entity.HasOne(d => d.MachineModel).WithMany(p => p.Requests)
                .HasForeignKey(d => d.MachineModelId)
                .HasConstraintName("FK_Requests_MachineModel");

            entity.HasOne(d => d.MachineType).WithMany(p => p.Requests)
                .HasForeignKey(d => d.MachineTypeId)
                .HasConstraintName("FK_Requests_MachineType");

            entity.HasOne(d => d.Programmer).WithMany(p => p.RequestProgrammers)
                .HasForeignKey(d => d.ProgrammerId)
                .HasConstraintName("FK_Requests_Programmer");

            entity.HasOne(d => d.SetupTechnician).WithMany(p => p.RequestSetupTechnicians)
                .HasForeignKey(d => d.SetupTechnicianId)
                .HasConstraintName("FK_Requests_SetupTech");

            entity.HasOne(d => d.Technologist).WithMany(p => p.RequestTechnologists)
                .HasForeignKey(d => d.TechnologistId)
                .HasConstraintName("FK_Requests_Technologist");

            entity.HasOne(d => d.Workshop).WithMany(p => p.Requests)
                .HasForeignKey(d => d.WorkshopId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_Requests_Workshop");
        });

        modelBuilder.Entity<RequestFile>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__RequestF__3214EC075ECB60F5");

            entity.HasIndex(e => e.RequestId, "IX_RequestFiles_RequestId");

            entity.Property(e => e.FileName).HasMaxLength(255);
            entity.Property(e => e.FilePath).HasMaxLength(500);
            entity.Property(e => e.FileType).HasMaxLength(50);
            entity.Property(e => e.UploadedAt).HasDefaultValueSql("(getdate())");

            entity.HasOne(d => d.Request).WithMany(p => p.RequestFiles)
                .HasForeignKey(d => d.RequestId)
                .HasConstraintName("FK_RequestFiles_Request");

            entity.HasOne(d => d.ToolRequest).WithMany(p => p.RequestFiles)
                .HasForeignKey(d => d.ToolRequestId)
                .HasConstraintName("FK_RequestFiles_ToolRequest");

            entity.HasOne(d => d.UploadedBy).WithMany(p => p.RequestFiles)
                .HasForeignKey(d => d.UploadedById)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_RequestFiles_Uploader");
        });

        modelBuilder.Entity<RequestHistory>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__RequestH__3214EC07F536F39A");

            entity.ToTable("RequestHistory");

            entity.HasIndex(e => e.RequestId, "IX_RequestHistory_RequestId");

            entity.Property(e => e.ChangedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.NewStatus).HasMaxLength(50);
            entity.Property(e => e.OldStatus).HasMaxLength(50);

            entity.HasOne(d => d.ChangedBy).WithMany(p => p.RequestHistories)
                .HasForeignKey(d => d.ChangedById)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_RequestHistory_ChangedBy");

            entity.HasOne(d => d.Request).WithMany(p => p.RequestHistories)
                .HasForeignKey(d => d.RequestId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_RequestHistory_Request");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Roles__3214EC07AE40A726");

            entity.HasIndex(e => e.Name, "UQ__Roles__737584F679CCEFD3").IsUnique();

            entity.Property(e => e.Name).HasMaxLength(50);
        });

        modelBuilder.Entity<Specialization>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Speciali__3214EC07967CE5CB");

            entity.Property(e => e.Name).HasMaxLength(100);
        });

        modelBuilder.Entity<ToolRequest>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__ToolRequ__3214EC07ACF9F8BE");

            entity.HasIndex(e => e.MainRequestId, "IX_ToolRequests_MainRequestId");

            entity.HasIndex(e => e.Status, "IX_ToolRequests_Status");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.Status).HasMaxLength(50);
            entity.Property(e => e.ToolName).HasMaxLength(200);

            entity.HasOne(d => d.AssignedEngineer).WithMany(p => p.ToolRequestAssignedEngineers)
                .HasForeignKey(d => d.AssignedEngineerId)
                .HasConstraintName("FK_ToolRequests_Engineer");

            entity.HasOne(d => d.MainRequest).WithMany(p => p.ToolRequests)
                .HasForeignKey(d => d.MainRequestId)
                .HasConstraintName("FK_ToolRequests_MainRequest");

            entity.HasOne(d => d.RequestedBy).WithMany(p => p.ToolRequestRequestedBies)
                .HasForeignKey(d => d.RequestedById)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ToolRequests_RequestedBy");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Users__3214EC071A032766");

            entity.HasIndex(e => e.Login, "UQ__Users__5E55825B050F0477").IsUnique();

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.FullName).HasMaxLength(150);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.Login).HasMaxLength(50);
            entity.Property(e => e.PasswordHash).HasMaxLength(256);

            entity.HasOne(d => d.Role).WithMany(p => p.Users)
                .HasForeignKey(d => d.RoleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Users_Role");

            entity.HasOne(d => d.Specialization).WithMany(p => p.Users)
                .HasForeignKey(d => d.SpecializationId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Users_Spec");

            entity.HasOne(d => d.Workshop).WithMany(p => p.Users)
                .HasForeignKey(d => d.WorkshopId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Users_Workshop");
        });

        modelBuilder.Entity<Workshop>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Workshop__3214EC0781435372");

            entity.Property(e => e.Name).HasMaxLength(100);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
