using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Persistence.Configurations
{
    public class BuildingConfiguration : IEntityTypeConfiguration<Building>
    {
        public void Configure(EntityTypeBuilder<Building> builder)
        {
            builder.ToTable("Buildings");
            builder.HasKey(b => b.Id);

            builder.Property(b => b.Name).HasMaxLength(256).IsRequired();
            builder.Property(b => b.Province).HasMaxLength(128);
            builder.Property(b => b.City).HasMaxLength(128).IsRequired();
            builder.Property(b => b.District).HasMaxLength(128).IsRequired();
            builder.Property(b => b.Ward).HasMaxLength(128).IsRequired();
            builder.Property(b => b.Address).HasMaxLength(512).IsRequired();
            builder.Property(b => b.Latitude).HasColumnType("decimal(9,6)");
            builder.Property(b => b.Longitude).HasColumnType("decimal(9,6)");
            builder.Property(b => b.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            builder.Property(b => b.IsDeleted).HasDefaultValue(false);
            // Bổ sung các cấu hình Decimal này vào bên trong hàm Configure(EntityTypeBuilder<Building> builder)
            builder.Property(b => b.ElectricityPrice).HasColumnType("decimal(18, 2)");
            builder.Property(b => b.WaterPrice).HasColumnType("decimal(18, 2)");
            builder.Property(b => b.InternetPrice).HasColumnType("decimal(18, 2)");
            builder.Property(b => b.GarbagePrice).HasColumnType("decimal(18, 2)");
            builder.HasOne(b => b.Owner)
                .WithMany(u => u.Buildings)
                .HasForeignKey(b => b.OwnerId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.HasIndex(b => new { b.City, b.District, b.Ward })
                .HasDatabaseName("IX_Buildings_Location");
        }
    }
}
