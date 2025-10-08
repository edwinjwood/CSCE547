using System;
using System.Globalization;
using DirtBikePark.Cli.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace DirtBikePark.Cli.Infrastructure.Persistence;

public sealed class DirtBikeParkDbContext : DbContext
{
    public DirtBikeParkDbContext(DbContextOptions<DirtBikeParkDbContext> options)
        : base(options)
    {
    }

    public DbSet<ParkRecord> Parks => Set<ParkRecord>();
    public DbSet<ParkAvailabilityRecord> ParkAvailability => Set<ParkAvailabilityRecord>();
    public DbSet<BookingRecord> Bookings => Set<BookingRecord>();
    public DbSet<BookingReservedDateRecord> BookingReservedDates => Set<BookingReservedDateRecord>();
    public DbSet<CartRecord> Carts => Set<CartRecord>();
    public DbSet<CartItemRecord> CartItems => Set<CartItemRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var dateConverter = new ValueConverter<DateOnly, string>(
            value => value.ToString("O", CultureInfo.InvariantCulture),
            value => DateOnly.Parse(value, CultureInfo.InvariantCulture));

        modelBuilder.Entity<ParkRecord>(entity =>
        {
            entity.ToTable("parks");
            entity.HasKey(p => p.Id);
            entity.Property(p => p.Name).IsRequired();
            entity.Property(p => p.Description).IsRequired();
            entity.Property(p => p.Location).IsRequired();
            entity.Property(p => p.GuestLimit).IsRequired();
            entity.Property(p => p.AvailableGuestCapacity).IsRequired();
            entity.Property(p => p.PricePerGuestPerDayAmount).HasColumnType("REAL");
            entity.Property(p => p.PricePerGuestPerDayCurrency).IsRequired();
            entity.Property(p => p.CreatedAtUtc).IsRequired();
            entity.Property(p => p.LastModifiedUtc).IsRequired();

            entity.HasMany(p => p.Availability)
                .WithOne(a => a.Park)
                .HasForeignKey(a => a.ParkId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(p => p.Bookings)
                .WithOne(b => b.Park)
                .HasForeignKey(b => b.ParkId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ParkAvailabilityRecord>(entity =>
        {
            entity.ToTable("park_availability");
            entity.HasKey(pa => new { pa.ParkId, pa.Date });
            entity.Property(pa => pa.Date).HasConversion(dateConverter);
        });

        modelBuilder.Entity<BookingRecord>(entity =>
        {
            entity.ToTable("bookings");
            entity.HasKey(b => b.Id);
            entity.Property(b => b.GuestName).IsRequired();
            entity.Property(b => b.Guests).IsRequired();
            entity.Property(b => b.DayCount).IsRequired();
            entity.Property(b => b.Status).HasConversion<string>().IsRequired();
            entity.Property(b => b.GuestCategory).HasConversion<string>().IsRequired();
            entity.Property(b => b.PricePerDayAmount).HasColumnType("REAL");
            entity.Property(b => b.PricePerDayCurrency).IsRequired();
            entity.Property(b => b.StartDate).HasConversion(dateConverter).IsRequired();
            entity.Property(b => b.CreatedAtUtc).IsRequired();

            entity.HasMany(b => b.ReservedDates)
                .WithOne(rd => rd.Booking)
                .HasForeignKey(rd => rd.BookingId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<BookingReservedDateRecord>(entity =>
        {
            entity.ToTable("booking_reserved_dates");
            entity.HasKey(rd => new { rd.BookingId, rd.Date });
            entity.Property(rd => rd.Date).HasConversion(dateConverter);
        });

        modelBuilder.Entity<CartRecord>(entity =>
        {
            entity.ToTable("carts");
            entity.HasKey(c => c.Id);
            entity.Property(c => c.CreatedAtUtc).IsRequired();
            entity.Property(c => c.LastUpdatedUtc).IsRequired();

            entity.HasMany(c => c.Items)
                .WithOne(i => i.Cart)
                .HasForeignKey(i => i.CartId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CartItemRecord>(entity =>
        {
            entity.ToTable("cart_items");
            entity.HasKey(i => new { i.CartId, i.BookingId });
            entity.Property(i => i.ParkName).IsRequired();
            entity.Property(i => i.Quantity).IsRequired();
            entity.Property(i => i.UnitPriceAmount).HasColumnType("REAL");
            entity.Property(i => i.UnitPriceCurrency).IsRequired();
        });
    }
}
