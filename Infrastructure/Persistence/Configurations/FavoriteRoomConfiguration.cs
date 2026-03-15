using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations
{
    public class FavoriteRoomConfiguration : IEntityTypeConfiguration<FavoriteRoom>
    {
        public void Configure(EntityTypeBuilder<FavoriteRoom> builder)
        {
            // Tên bảng trong Database
            builder.ToTable("FavoriteRooms");

            // Thiết lập Khóa chính kép (Composite Key)
            builder.HasKey(fr => new { fr.UserId, fr.RoomId });

            // Cấu hình Khóa ngoại tới ApplicationUser
            builder.HasOne(fr => fr.User)
                .WithMany()
                .HasForeignKey(fr => fr.UserId)
                .OnDelete(DeleteBehavior.Cascade); // Xóa user thì xóa luôn danh sách yêu thích

            // Cấu hình Khóa ngoại tới Room
            builder.HasOne(fr => fr.Room)
                .WithMany()
                .HasForeignKey(fr => fr.RoomId)
                .OnDelete(DeleteBehavior.Cascade); // Xóa phòng thì xóa luôn dữ liệu yêu thích của phòng đó
        }
    }
}