using CampusGo.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace CampusGo.Web.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Vehicle> Vehicles => Set<Vehicle>();
    public DbSet<Trip> Trips => Set<Trip>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<Rating> Ratings => Set<Rating>();
    public DbSet<ChatRoom> ChatRooms => Set<ChatRoom>();
    public DbSet<Message> Messages => Set<Message>();
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<Notification> Notifications => Set<Notification>();

    // Override the DbContext method called OnModelCreating() to specify additional rules.
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // User
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(u => u.UserId);
            entity.Property(u => u.FullName).IsRequired().HasMaxLength(150);
            entity.Property(u => u.Email).IsRequired().HasMaxLength(255); // Configures column property
            entity.HasIndex(u => u.Email).IsUnique(); // Creating an index for emails to query users by their emails and enforce uniqueness
            entity.Property(u => u.StudentId).IsRequired().HasMaxLength(50);
            entity.Property(u => u.Role).HasConversion<string>();
        });

        // Vehicle — one user can own many vehicles
        modelBuilder.Entity<Vehicle>(entity =>
        {
            entity.HasKey(v => v.VehicleId);
            entity.HasIndex(v => v.PlateNumber).IsUnique(); // Make sure that plate numbers do not repeat
            entity.Property(v => v.Type).HasConversion<string>();
            entity.HasOne(v => v.Owner)
                .WithMany(u => u.Vehicles)
                .HasForeignKey(v => v.UserId);
        });

        // Trip — disambiguating the two relationships User has to Trip/Booking
        modelBuilder.Entity<Trip>(entity =>
        {
            entity.HasKey(t => t.TripId);
            entity.HasOne(t => t.Driver)
                .WithMany(u => u.TripsAsDriver)
                .HasForeignKey(t => t.DriverId)
                .OnDelete(DeleteBehavior.Restrict); // don't cascade-delete a user's trip history

            entity.HasOne(t => t.Vehicle)
                .WithMany()
                .HasForeignKey(t => t.VehicleId);
        });

        // Booking
        modelBuilder.Entity<Booking>(entity =>
        {
            entity.HasKey(b => b.BookingId);
            entity.HasOne(b => b.Trip)
                .WithMany(t => t.Bookings)
                .HasForeignKey(b => b.TripId);

            entity.HasOne(b => b.Rider)
                .WithMany(u => u.BookingsAsRider)
                .HasForeignKey(b => b.RiderId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.Property(b => b.Status)
                .HasConversion<string>();
        });

        // Rating — two FKs to User (rater and ratee) need explicit config
        modelBuilder.Entity<Rating>(entity =>
        {
            entity.HasKey(r => r.RatingId);
            entity.HasOne(r => r.Booking)
                .WithMany(b => b.Ratings)
                .HasForeignKey(r => r.BookingId);

            entity.HasOne(r => r.Rater)
                .WithMany()
                .HasForeignKey(r => r.RaterId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(r => r.Ratee)
                .WithMany()
                .HasForeignKey(r => r.RateeId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ChatRoom
        modelBuilder.Entity<ChatRoom>(entity =>
        {
            entity.HasKey(c => c.ChatRoomId);
            entity.HasOne(c => c.Trip)
                .WithOne(t => t.ChatRoom)
                .HasForeignKey<ChatRoom>(c => c.TripId);
        });

        modelBuilder.Entity<Message>(entity =>
        {
            entity.HasKey(m => m.MessageId);
            entity.HasOne(m => m.ChatRoom)
                .WithMany(c => c.Messages)
                .HasForeignKey(m => m.ChatRoomId);

            entity.HasOne(m => m.Sender)
                .WithMany()
                .HasForeignKey(m => m.SenderId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Transaction
        modelBuilder.Entity<Transaction>(entity =>
        {
            entity.HasKey(t => t.TransactionId);
            entity.HasOne(t => t.Booking)
                .WithOne()
                .HasForeignKey<Transaction>(t => t.BookingId);

            entity.Property(t => t.Type).HasConversion<string>();
            entity.Property(t => t.Status).HasConversion<string>();
        });

        // Notification
        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(n => n.NotificationId);
            entity.HasOne(n => n.User)
                .WithMany(u => u.Notifications)
                .HasForeignKey(n => n.UserId);

            entity.Property(n => n.Type).HasConversion<string>();
        });
    }
}