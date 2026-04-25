using AGONECompliance.Domain;
using AGONECompliance.Shared;
using Microsoft.EntityFrameworkCore;

namespace AGONECompliance.Data;

public sealed class ComplianceDbContext(DbContextOptions<ComplianceDbContext> options) : DbContext(options)
{
    public DbSet<EvaluationWorkspace> EvaluationWorkspaces => Set<EvaluationWorkspace>();
    public DbSet<UploadedDocument> UploadedDocuments => Set<UploadedDocument>();
    public DbSet<DocumentPageBlob> DocumentPageBlobs => Set<DocumentPageBlob>();
    public DbSet<ComplianceRule> ComplianceRules => Set<ComplianceRule>();
    public DbSet<EvaluationRun> EvaluationRuns => Set<EvaluationRun>();
    public DbSet<EvaluationResult> EvaluationResults => Set<EvaluationResult>();
    public DbSet<EvaluationRunRule> EvaluationRunRules => Set<EvaluationRunRule>();
    public DbSet<BackgroundJobRun> BackgroundJobRuns => Set<BackgroundJobRun>();
    public DbSet<PromptTemplate> PromptTemplates => Set<PromptTemplate>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema("compliance");

        modelBuilder.Entity<EvaluationWorkspace>(entity =>
        {
            entity.HasIndex(x => x.Name);
            entity.Property(x => x.Name).HasMaxLength(256);
            entity.Property(x => x.Description).HasMaxLength(2048);
            entity.Property(x => x.Status).HasMaxLength(64);
        });

        modelBuilder.Entity<UploadedDocument>(entity =>
        {
            entity.HasIndex(x => x.Type);
            entity.HasIndex(x => new { x.EvaluationWorkspaceId, x.Type });
            entity.Property(x => x.OriginalFileName).HasMaxLength(512);
            entity.Property(x => x.ContentType).HasMaxLength(256);
            entity.Property(x => x.BlobPath).HasMaxLength(1024);
            entity.Property(x => x.FullTextBlobPath).HasMaxLength(1024);
            entity.Property(x => x.ParsedJsonBlobPath).HasMaxLength(1024);
            entity.HasOne(x => x.EvaluationWorkspace)
                .WithMany(x => x.Documents)
                .HasForeignKey(x => x.EvaluationWorkspaceId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<DocumentPageBlob>(entity =>
        {
            entity.HasIndex(x => new { x.DocumentId, x.PageNumber }).IsUnique();
            entity.Property(x => x.BlobPath).HasMaxLength(1024);
            entity.Property(x => x.ExtractedText).HasMaxLength(4000);
            entity.HasOne(x => x.EvaluationWorkspace)
                .WithMany()
                .HasForeignKey(x => x.EvaluationWorkspaceId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Document)
                .WithMany()
                .HasForeignKey(x => x.DocumentId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ComplianceRule>(entity =>
        {
            entity.HasIndex(x => new { x.EvaluationWorkspaceId, x.Code }).IsUnique();
            entity.HasIndex(x => new { x.EvaluationWorkspaceId, x.IsActive });
            entity.Property(x => x.Code).HasMaxLength(128);
            entity.Property(x => x.Title).HasMaxLength(1024);
            entity.Property(x => x.Reference).HasMaxLength(512);
            entity.Property(x => x.ClassificationCategory).HasMaxLength(64);
            entity.Property(x => x.ActionParty).HasMaxLength(64);
            entity.HasOne(x => x.EvaluationWorkspace)
                .WithMany(x => x.Rules)
                .HasForeignKey(x => x.EvaluationWorkspaceId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PromptTemplate>(entity =>
        {
            entity.HasIndex(x => new { x.TemplateType, x.Version }).IsUnique();
            entity.HasIndex(x => x.IsActive);
            entity.Property(x => x.TemplateType).HasMaxLength(128);
            entity.Property(x => x.Name).HasMaxLength(256);
        });

        modelBuilder.Entity<EvaluationRun>(entity =>
        {
            entity.HasIndex(x => new { x.EvaluationWorkspaceId, x.Status });
            entity.HasOne(x => x.ProspectusDocument)
                .WithMany()
                .HasForeignKey(x => x.ProspectusDocumentId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.EvaluationWorkspace)
                .WithMany(x => x.EvaluationRuns)
                .HasForeignKey(x => x.EvaluationWorkspaceId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<EvaluationRunRule>(entity =>
        {
            entity.HasIndex(x => new { x.EvaluationRunId, x.RuleId }).IsUnique();
            entity.HasOne(x => x.EvaluationRun)
                .WithMany()
                .HasForeignKey(x => x.EvaluationRunId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<BackgroundJobRun>(entity =>
        {
            entity.HasIndex(x => new { x.EvaluationWorkspaceId, x.Status, x.JobType });
            entity.HasIndex(x => x.Status);
            entity.Property(x => x.JobType).HasMaxLength(64);
            entity.Property(x => x.Status).HasMaxLength(32);
            entity.Property(x => x.Message).HasMaxLength(1024);
            entity.Property(x => x.FailureReason).HasMaxLength(2048);
            entity.Property(x => x.RelatedRuleGenerationRequestId);
            entity.HasOne(x => x.EvaluationWorkspace)
                .WithMany()
                .HasForeignKey(x => x.EvaluationWorkspaceId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<EvaluationResult>(entity =>
        {
            entity.HasIndex(x => new { x.EvaluationRunId, x.RuleId }).IsUnique();
            entity.Property(x => x.ConfidenceScore).HasColumnType("decimal(5,4)");
            entity.HasOne(x => x.EvaluationRun)
                .WithMany(x => x.Results)
                .HasForeignKey(x => x.EvaluationRunId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Rule)
                .WithMany()
                .HasForeignKey(x => x.RuleId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
