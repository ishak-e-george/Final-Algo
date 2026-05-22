using Microsoft.EntityFrameworkCore;
using RescueFlow.Web.Models.Entities;

namespace RescueFlow.Web.Data;

public class RescueFlowDbContext : DbContext
{
    public RescueFlowDbContext(DbContextOptions<RescueFlowDbContext> options)
        : base(options)
    {
    }

    public DbSet<RescueCase> RescueCases => Set<RescueCase>();
    public DbSet<Station> Stations => Set<Station>();
    public DbSet<RescueVehicle> RescueVehicles => Set<RescueVehicle>();
    public DbSet<Equipment> Equipment => Set<Equipment>();
    public DbSet<VehicleEquipment> VehicleEquipment => Set<VehicleEquipment>();
    public DbSet<Hospital> Hospitals => Set<Hospital>();
    public DbSet<HospitalCapabilityEntity> HospitalCapabilities => Set<HospitalCapabilityEntity>();
    public DbSet<HospitalCapacity> HospitalCapacities => Set<HospitalCapacity>();
    public DbSet<ResponsePlan> ResponsePlans => Set<ResponsePlan>();
    public DbSet<ResponseAssignment> ResponseAssignments => Set<ResponseAssignment>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<DispatchDecisionLog> DispatchDecisionLogs => Set<DispatchDecisionLog>();
    public DbSet<SafetyValidationResult> SafetyValidationResults => Set<SafetyValidationResult>();
    public DbSet<SafetyViolation> SafetyViolations => Set<SafetyViolation>();
    public DbSet<AlgorithmRunLog> AlgorithmRunLogs => Set<AlgorithmRunLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Joint table config
        modelBuilder.Entity<VehicleEquipment>()
            .HasKey(x => new { x.RescueVehicleId, x.EquipmentId });

        modelBuilder.Entity<VehicleEquipment>()
            .HasOne(x => x.RescueVehicle)
            .WithMany(v => v.VehicleEquipment)
            .HasForeignKey(x => x.RescueVehicleId);

        modelBuilder.Entity<VehicleEquipment>()
            .HasOne(x => x.Equipment)
            .WithMany(e => e.VehicleEquipment)
            .HasForeignKey(x => x.EquipmentId);

        // One-to-one Hospital -> HospitalCapacity
        modelBuilder.Entity<Hospital>()
            .HasOne(h => h.Capacity)
            .WithOne(c => c.Hospital)
            .HasForeignKey<HospitalCapacity>(c => c.HospitalId);

        // One-to-one RescueCase -> ResponsePlan
        modelBuilder.Entity<RescueCase>()
            .HasOne(c => c.ResponsePlan)
            .WithOne(p => p.RescueCase)
            .HasForeignKey<ResponsePlan>(p => p.RescueCaseId);

        // Concurrency tokens
        bool isSqlite = Database.ProviderName == "Microsoft.EntityFrameworkCore.Sqlite";

        if (isSqlite)
        {
            modelBuilder.Entity<RescueVehicle>()
                .Property(v => v.RowVersion)
                .IsConcurrencyToken();

            modelBuilder.Entity<HospitalCapacity>()
                .Property(c => c.RowVersion)
                .IsConcurrencyToken();

            modelBuilder.Entity<RescueCase>()
                .Property(c => c.RowVersion)
                .IsConcurrencyToken();
        }
        else
        {
            modelBuilder.Entity<RescueVehicle>()
                .Property(v => v.RowVersion)
                .IsRowVersion();

            modelBuilder.Entity<HospitalCapacity>()
                .Property(c => c.RowVersion)
                .IsRowVersion();

            modelBuilder.Entity<RescueCase>()
                .Property(c => c.RowVersion)
                .IsRowVersion();
        }

        // Active Assignment filtered unique index
        modelBuilder.Entity<ResponseAssignment>()
            .HasIndex(a => a.RescueVehicleId)
            .IsUnique()
            .HasFilter(isSqlite ? "IsActive = 1" : "[IsActive] = 1");
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        UpdateRowVersions();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        UpdateRowVersions();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void UpdateRowVersions()
    {
        if (Database.ProviderName == "Microsoft.EntityFrameworkCore.Sqlite")
        {
            var entries = ChangeTracker.Entries()
                .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

            foreach (var entry in entries)
            {
                var prop = entry.Metadata.FindProperty("RowVersion");
                if (prop != null && prop.ClrType == typeof(byte[]))
                {
                    entry.CurrentValues["RowVersion"] = Guid.NewGuid().ToByteArray();
                }
            }
        }
    }
}
