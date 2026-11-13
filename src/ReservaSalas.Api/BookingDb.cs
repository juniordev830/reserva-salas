using Microsoft.EntityFrameworkCore;
namespace ReservaSalas.Api;
public sealed class Room {
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public int Capacity { get; set; }
    public bool Active { get; set; } = true;
}
public sealed class Booking {
    public int Id { get; set; }
    public int RoomId { get; set; }
    public DateTimeOffset StartsAt { get; set; }
    public DateTimeOffset EndsAt { get; set; }
    public string OwnerId { get; set; } = "";
}
public sealed record RoomInput(string Name, int Capacity);
public sealed record BookingInput(int RoomId, DateTimeOffset StartsAt, DateTimeOffset EndsAt);
public sealed class BookingDb(DbContextOptions<BookingDb> options) : DbContext(options) {
    public DbSet<Room> Rooms => Set<Room>();
    public DbSet<Booking> Bookings => Set<Booking>();
    protected override void OnModelCreating(ModelBuilder b) {
        b.Entity<Room>().Property(x => x.Name).HasMaxLength(120).IsRequired();
        b.Entity<Booking>().Property(x => x.OwnerId).HasMaxLength(200).IsRequired();
        b.Entity<Booking>().HasOne<Room>().WithMany().HasForeignKey(x => x.RoomId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Booking>().HasIndex(x => new { x.RoomId, x.StartsAt, x.EndsAt });
        b.Entity<Booking>().ToTable(t => t.HasCheckConstraint("CK_Booking_Interval", "\"EndsAt\" > \"StartsAt\""));
    }
}