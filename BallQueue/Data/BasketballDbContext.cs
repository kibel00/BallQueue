using Microsoft.EntityFrameworkCore;
using BallQueue.Models;

namespace BallQueue.Data;

/// <summary>
/// Entity Framework Core DbContext for the basketball queue system.
/// Manages all database operations for players, games, teams, payments, and sessions.
/// </summary>
public class BasketballDbContext : DbContext
{
    /// <summary>
    /// Creates a new instance of the database context with connection options.
    /// </summary>
    public BasketballDbContext(DbContextOptions<BasketballDbContext> options)
        : base(options)
    {
    }

    /// <summary>
    /// Table for sessions (groups of games within a day/time period).
    /// </summary>
    public DbSet<Session> Sessions { get; set; } = null!;

    /// <summary>
    /// Table for players registered in the system.
    /// </summary>
    public DbSet<Player> Players { get; set; } = null!;

    /// <summary>
    /// Table for games played.
    /// </summary>
    public DbSet<Game> Games { get; set; } = null!;

    /// <summary>
    /// Table for teams (pairs of teams make up each game).
    /// </summary>
    public DbSet<Team> Teams { get; set; } = null!;

    /// <summary>
    /// Table for payment records.
    /// </summary>
    public DbSet<Payment> Payments { get; set; } = null!;

    /// <summary>
    /// Configures model relationships and constraints.
    /// </summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ========== SESSION ==========
        modelBuilder.Entity<Session>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Name).HasMaxLength(255);
            entity.HasMany(e => e.Players)
                .WithOne(p => p.Session)
                .HasForeignKey(p => p.SessionId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(e => e.Games)
                .WithOne(g => g.Session)
                .HasForeignKey(g => g.SessionId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(e => e.Payments)
                .WithOne(p => p.Session)
                .HasForeignKey(p => p.SessionId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => e.StartDateTime);
        });

        // ========== PLAYER ==========
        modelBuilder.Entity<Player>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
            entity.Property(e => e.ArrivalNumber).IsRequired();
            entity.Property(e => e.CurrentStatus).IsRequired();
            entity.Property(e => e.AmountPaid).HasPrecision(10, 2);
            entity.HasIndex(e => new { e.SessionId, e.ArrivalNumber }).IsUnique();
            entity.HasIndex(e => e.CurrentStatus);
            entity.HasIndex(e => e.HasPaid);
        });

        // ========== GAME ==========
        modelBuilder.Entity<Game>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Number).IsRequired();
            entity.Property(e => e.Status).IsRequired();
            entity.Property(e => e.Winner);
            entity.Ignore(e => e.TeamA);
            entity.Ignore(e => e.TeamB);
            entity.HasOne(e => e.Referee)
                .WithMany()
                .HasForeignKey(e => e.RefereeId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(e => e.Scorer)
                .WithMany()
                .HasForeignKey(e => e.ScorerId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasIndex(e => new { e.SessionId, e.Number }).IsUnique();
            entity.HasIndex(e => e.Status);
        });

        // ========== TEAM ==========
        modelBuilder.Entity<Team>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Side).IsRequired();
            entity.Property(e => e.PlayerIdsJson).IsRequired().HasMaxLength(2000);
            entity.Ignore(e => e.Game);
            entity.HasIndex(e => new { e.GameId, e.Side }).IsUnique();
        });

        // ========== PAYMENT ==========
        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Amount).HasPrecision(10, 2).IsRequired();
            entity.Property(e => e.Notes).HasMaxLength(500);
            entity.HasOne(e => e.Player)
                .WithMany()
                .HasForeignKey(e => e.PlayerId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => new { e.PlayerId, e.PaymentDateTime });
            entity.HasIndex(e => e.PaymentDateTime);
        });
    }

    /// <summary>
    /// Initial database creation with default schema.
    /// Call this once during app initialization.
    /// </summary>
    public async Task InitializeDatabaseAsync()
    {
        try
        {
            await Database.EnsureCreatedAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Database initialization error: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Rebuilds legacy game tables that used mutual Game/Team foreign keys.
    /// The original shape cannot be inserted by EF Core because each new row
    /// requires the other row to exist first. Session, player, and payment data
    /// are not touched.
    /// </summary>
    public void RemoveLegacyCircularTeamForeignKeys()
    {
        if (!HasForeignKey("Games", "Teams") && !HasForeignKey("Teams", "Games"))
            return;

        Database.ExecuteSqlRaw("PRAGMA foreign_keys = OFF;");
        using var transaction = Database.BeginTransaction();

        try
        {
            Database.ExecuteSqlRaw("""
                CREATE TABLE "__Teams" (
                    "Id" TEXT NOT NULL CONSTRAINT "PK___Teams" PRIMARY KEY,
                    "Side" INTEGER NOT NULL,
                    "GameId" TEXT NOT NULL,
                    "PlayerIdsJson" TEXT NOT NULL
                );
                """);
            Database.ExecuteSqlRaw("""
                INSERT INTO "__Teams" ("Id", "Side", "GameId", "PlayerIdsJson")
                SELECT "Id", "Side", "GameId", "PlayerIdsJson" FROM "Teams";
                """);
            Database.ExecuteSqlRaw("""DROP TABLE "Teams";""");
            Database.ExecuteSqlRaw("""ALTER TABLE "__Teams" RENAME TO "Teams";""");
            Database.ExecuteSqlRaw("""CREATE UNIQUE INDEX "IX_Teams_GameId_Side" ON "Teams" ("GameId", "Side");""");

            Database.ExecuteSqlRaw("""
                CREATE TABLE "__Games" (
                    "Id" TEXT NOT NULL CONSTRAINT "PK___Games" PRIMARY KEY,
                    "Number" INTEGER NOT NULL,
                    "Status" INTEGER NOT NULL,
                    "StartDateTime" TEXT NOT NULL,
                    "EndDateTime" TEXT NULL,
                    "TeamAId" TEXT NOT NULL,
                    "TeamBId" TEXT NOT NULL,
                    "RefereeId" TEXT NULL,
                    "ScorerId" TEXT NULL,
                    "Winner" INTEGER NULL,
                    "SessionId" TEXT NOT NULL,
                    CONSTRAINT "FK___Games_Sessions_SessionId" FOREIGN KEY ("SessionId") REFERENCES "Sessions" ("Id") ON DELETE CASCADE,
                    CONSTRAINT "FK___Games_Players_RefereeId" FOREIGN KEY ("RefereeId") REFERENCES "Players" ("Id") ON DELETE SET NULL,
                    CONSTRAINT "FK___Games_Players_ScorerId" FOREIGN KEY ("ScorerId") REFERENCES "Players" ("Id") ON DELETE SET NULL
                );
                """);
            Database.ExecuteSqlRaw("""
                INSERT INTO "__Games" ("Id", "Number", "Status", "StartDateTime", "EndDateTime", "TeamAId", "TeamBId", "RefereeId", "ScorerId", "Winner", "SessionId")
                SELECT "Id", "Number", "Status", "StartDateTime", "EndDateTime", "TeamAId", "TeamBId", "RefereeId", "ScorerId", "Winner", "SessionId" FROM "Games";
                """);
            Database.ExecuteSqlRaw("""DROP TABLE "Games";""");
            Database.ExecuteSqlRaw("""ALTER TABLE "__Games" RENAME TO "Games";""");
            Database.ExecuteSqlRaw("""CREATE UNIQUE INDEX "IX_Games_SessionId_Number" ON "Games" ("SessionId", "Number");""");
            Database.ExecuteSqlRaw("""CREATE INDEX "IX_Games_Status" ON "Games" ("Status");""");

            transaction.Commit();
        }
        finally
        {
            Database.ExecuteSqlRaw("PRAGMA foreign_keys = ON;");
        }
    }

    private bool HasForeignKey(string tableName, string targetTable)
    {
        var connection = Database.GetDbConnection();
        var shouldClose = connection.State != System.Data.ConnectionState.Open;

        if (shouldClose)
            connection.Open();

        try
        {
            using var command = connection.CreateCommand();
            command.CommandText = $"PRAGMA foreign_key_list('{tableName}');";
            using var reader = command.ExecuteReader();

            while (reader.Read())
            {
                if (string.Equals(reader[2]?.ToString(), targetTable, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }
        finally
        {
            if (shouldClose)
                connection.Close();
        }
    }
}
