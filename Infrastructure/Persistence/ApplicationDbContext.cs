using Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using RoomHub.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Persistence
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // Identity extension
        public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

        // Property hierarchy
        public DbSet<Building> Buildings => Set<Building>();
        public DbSet<Floor> Floors => Set<Floor>();
        public DbSet<Amenity> Amenities => Set<Amenity>();

        // Rooms
        public DbSet<Room> Rooms => Set<Room>();
        public DbSet<RoomAmenity> RoomAmenities => Set<RoomAmenity>();
        public DbSet<RoomPhoto> RoomPhotos => Set<RoomPhoto>();

        // Tenant profile
        public DbSet<TenantProfile> TenantProfiles => Set<TenantProfile>();

        // Deposit & Contract
        public DbSet<Deposit> Deposits => Set<Deposit>();
        public DbSet<Contract> Contracts => Set<Contract>();

        // Billing
        public DbSet<UtilityReading> UtilityReadings => Set<UtilityReading>();
        public DbSet<Invoice> Invoices => Set<Invoice>();
        public DbSet<InvoiceItem> InvoiceItems => Set<InvoiceItem>();
        public DbSet<Payment> Payments => Set<Payment>();

        // Maintenance & Services
        public DbSet<MaintenanceTicket> MaintenanceTickets => Set<MaintenanceTicket>();
        public DbSet<Service> Services => Set<Service>();
        public DbSet<ServiceRequest> ServiceRequests => Set<ServiceRequest>();

        // Communication & Reviews
        public DbSet<Message> Messages => Set<Message>();
        public DbSet<Review> Reviews => Set<Review>();
        public DbSet<ReviewViolation> ReviewViolations => Set<ReviewViolation>();

        // Notification & Audit
        public DbSet<Notification> Notifications => Set<Notification>();
        public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

        // AI support
        public DbSet<SearchHistory> SearchHistories => Set<SearchHistory>();
        public DbSet<BookingHistory> BookingHistories => Set<BookingHistory>();

        // System
        public DbSet<SystemSetting> SystemSettings => Set<SystemSetting>();

        public DbSet<FavoriteRoom> FavoriteRooms { get; set; }
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Apply all IEntityTypeConfiguration from this assembly
            builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
        }
    }
}
