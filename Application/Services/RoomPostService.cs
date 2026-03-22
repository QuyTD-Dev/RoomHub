using Application.DTOs.RoomPosts;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Domain.Entities;

namespace Application.Services
{
    public class RoomPostService : IRoomPostService
    {
        private readonly IRoomPostRepository _repository;
        private readonly ICloudinaryService _cloudinaryService;
        private readonly IFavoriteRoomRepository _favoriteRepo;
        public RoomPostService(IRoomPostRepository repository, ICloudinaryService cloudinaryService, IFavoriteRoomRepository favoriteRepo)
        {
            _repository = repository;
            _cloudinaryService = cloudinaryService;
            _favoriteRepo = favoriteRepo;
        }

        public async Task<IEnumerable<RoomListViewModel>> GetAllRoomsAsync(string? currentUserId = null)
        {
            var rooms = await _repository.GetAllActiveAsync();

            // Lấy danh sách ID phòng đã tim nếu user đã đăng nhập
            List<int> favoriteRoomIds = new List<int>();
            if (!string.IsNullOrEmpty(currentUserId))
            {
                favoriteRoomIds = await _favoriteRepo.GetFavoriteRoomIdsAsync(currentUserId);
            }

            return rooms.Select(r => new RoomListViewModel
            {
                Id = r.Id,
                Title = r.Title,
                BasePrice = r.BasePrice,
                SurfaceArea = r.SurfaceArea,
                Address = r.Floor?.Building?.Address ?? "Chưa cập nhật",
                Status = r.Status,
                CreatedAt = r.CreatedAt,
                RoomNumber = r.RoomNumber,
                RoomType = r.RoomType,
                AmenityCount = r.RoomAmenities.Count,

                LandlordId = r.LandlordId ?? string.Empty,
                IsFavorite = favoriteRoomIds.Contains(r.Id)
            });
        }

        public async Task<IEnumerable<RoomListViewModel>> GetMyRoomsAsync(string landlordId)
        {
            var rooms = await _repository.GetByLandlordIdAsync(landlordId);
            return rooms.Select(r => new RoomListViewModel
            {
                Id = r.Id,
                Title = r.Title,
                BasePrice = r.BasePrice,
                SurfaceArea = r.SurfaceArea,
                Address = r.Floor?.Building?.Address ?? "Chưa cập nhật",
                Status = r.Status,
                CreatedAt = r.CreatedAt,
                RoomNumber = r.RoomNumber,
                RoomType = r.RoomType,
                AmenityCount = r.RoomAmenities.Count
            });
        }

        // Hàm lấy dữ liệu khởi tạo Form
        public async Task<CreateRoomViewModel> GetCreateViewModelAsync(string landlordId)
        {
            var unpublishedRooms = await _repository.GetUnpublishedRoomsByLandlordIdAsync(landlordId);
            return new CreateRoomViewModel
            {
                AvailableRooms = unpublishedRooms
            };
        }

        public async Task<EditRoomViewModel> GetEditViewModelAsync(int id, string landlordId)
        {
            var room = await _repository.GetByIdAsync(id);
            if (room == null || room.LandlordId != landlordId)
                throw new UnauthorizedAccessException("You do not have permission to edit this room.");

            var floors = await _repository.GetFloorsByLandlordIdAsync(landlordId);
            var amenities = await _repository.GetAllAmenitiesAsync();

            return new EditRoomViewModel
            {
                Id = room.Id,
                Title = room.Title,
                RoomNumber = room.RoomNumber,
                RoomType = room.RoomType,
                BasePrice = room.BasePrice,
                SurfaceArea = room.SurfaceArea,
                MaxCapacity = room.MaxCapacity,
                Description = room.Description,
                IsFurnished = room.IsFurnished,
                FloorId = room.FloorId,
                Status = room.Status,
                SelectedAmenityIds = room.RoomAmenities.Select(ra => ra.AmenityId).ToList(),

                AvailableFloors = floors.ToList(),
                AvailableAmenities = amenities.ToList(),
                
                ExistingPhotos = room.RoomPhotos.OrderBy(p => p.DisplayOrder).Select(p => new RoomPhotoViewModel
                {
                    Id = p.Id,
                    Url = p.Url,
                    IsMain = p.IsMain
                }).ToList()
            };
        }

        public async Task<RoomDetailsViewModel> GetRoomDetailsAsync(int id)
        {
            var room = await _repository.GetByIdAsync(id);
            if (room == null)
            {
                throw new KeyNotFoundException("Room not found");
            }

            var photos = room.RoomPhotos.OrderBy(p => p.DisplayOrder).Select(p => new RoomPhotoViewModel
            {
                Id = p.Id,
                Url = p.Url,
                IsMain = p.IsMain
            }).ToList();

            if (!photos.Any())
            {
                photos.Add(new RoomPhotoViewModel { Id = 0, Url = "https://images.unsplash.com/photo-1522708323590-d24dbb6b0267?w=800&q=80", IsMain = true });
            }

            var address = room.Floor?.Building?.Address ?? "Chưa cập nhật";
            var locationDetails = "";
            if (room.Floor?.Building != null)
            {
                locationDetails = $"{room.Floor.Building.Ward}, {room.Floor.Building.District}, {room.Floor.Building.City}";
            }

            // Calculate joined years
            var joinedYears = 0;
            if (room.Landlord != null)
            {
                joinedYears = DateTime.UtcNow.Year - room.Landlord.CreatedAt.Year;
                if (joinedYears == 0) joinedYears = 1;
            }

            return new RoomDetailsViewModel
            {
                Id = room.Id,
                Title = room.Title,
                Description = room.Description ?? "Không có mô tả chi tiết.",
                BasePrice = room.BasePrice,
                SurfaceArea = room.SurfaceArea,
                Address = address,
                LocationDetails = locationDetails,
                Status = room.Status,
                CreatedAt = room.CreatedAt,
                UpdatedAt = room.UpdatedAt,
                RoomNumber = room.RoomNumber,
                RoomType = room.RoomType,
                IsFurnished = room.IsFurnished,
                MaxCapacity = room.MaxCapacity,
                FloorNumber = room.Floor?.FloorNumber ?? 0,
                Photos = photos,
                DepositAmount = room.Deposits?.FirstOrDefault()?.Amount ?? (room.BasePrice), // Approximate 1-month deposit if no exact deposit object
                Amenities = room.RoomAmenities?
                .Where(ra => ra.Amenity != null) // Lọc bỏ những bản ghi không có Amenity
                .Select(ra => new AmenityViewModel
                {
                    Id = ra.Amenity.Id,
                    Name = ra.Amenity.Name,
                    IconUrl = ra.Amenity.IconUrl
                }).ToList() ?? new List<AmenityViewModel>(),
                LandlordId = room.LandlordId,
                LandlordName = room.Landlord?.FullName ?? "Landlord",
                LandlordAvatarUrl = room.Landlord?.AvatarUrl ?? "https://ui-avatars.com/api/?name=Landlord&background=random",
                LandlordPhone = room.Landlord?.PhoneNumber,
                LandlordJoinedYears = joinedYears
            };
        }

        public async Task PublishRoomAsync(CreateRoomViewModel model, string landlordId)
        {
            var room = await _repository.GetByIdAsync(model.SelectedRoomId);
            if (room == null || room.LandlordId != landlordId) throw new Exception("Phòng không hợp lệ.");

            room.Title = model.Title;
            room.Description = model.Description;
            room.IsPublished = true;
            room.UpdatedAt = DateTime.UtcNow;

            if (model.Photos != null && model.Photos.Any())
            {
                foreach (var photo in model.Photos)
                {
                    var uploadResult = await _cloudinaryService.UploadImageAsync(photo, "room_photos");
                    var roomPhoto = new RoomPhoto
                    {
                        RoomId = room.Id,
                        Url = uploadResult.Url,
                        PublicId = uploadResult.PublicId ?? "N/A", // Map thêm PublicId
                        IsMain = room.RoomPhotos.Count == 0, // Ảnh đầu tiên cho làm ảnh bìa

                        // [ĐÃ SỬA]: Dùng UploadedAt thay vì CreatedAt
                        UploadedAt = DateTime.UtcNow
                    };
                    room.RoomPhotos.Add(roomPhoto);
                }
            }

            await _repository.UpdateAsync(room);
        }



        public async Task UpdateRoomAsync(EditRoomViewModel model, string currentUserId)
        {
            var room = await _repository.GetByIdAsync(model.Id);

            if (room == null)
                throw new KeyNotFoundException("Room not found");

            if (room.LandlordId != currentUserId)
                throw new UnauthorizedAccessException("You do not have permission to edit this room.");

            room.Title = model.Title;
            room.RoomNumber = model.RoomNumber;
            room.RoomType = model.RoomType;
            room.BasePrice = model.BasePrice;
            room.SurfaceArea = model.SurfaceArea;
            room.MaxCapacity = model.MaxCapacity;
            room.Description = model.Description;
            room.IsFurnished = model.IsFurnished;
            room.FloorId = model.FloorId;
            room.Status = model.Status;
            room.UpdatedAt = DateTime.UtcNow;

            // Update Amenities
            room.RoomAmenities.Clear();
            if (model.SelectedAmenityIds != null && model.SelectedAmenityIds.Any())
            {
                foreach (var amenityId in model.SelectedAmenityIds)
                {
                    room.RoomAmenities.Add(new RoomAmenity { RoomId = room.Id, AmenityId = amenityId });
                }
            }

            // Remove deleted photos
            if (model.DeletePhotoIds != null && model.DeletePhotoIds.Any())
            {
                var photosToDelete = room.RoomPhotos.Where(p => model.DeletePhotoIds.Contains(p.Id)).ToList();
                foreach (var photo in photosToDelete)
                {
                    await _cloudinaryService.DeleteImageAsync(photo.PublicId);
                    room.RoomPhotos.Remove(photo);
                }
            }

            // Upload new photos
            if (model.NewPhotos != null && model.NewPhotos.Any())
            {
                int order = room.RoomPhotos.Any() ? room.RoomPhotos.Max(p => p.DisplayOrder) + 1 : 0;
                foreach (var file in model.NewPhotos)
                {
                    var uploadResult = await _cloudinaryService.UploadImageAsync(file);
                    if (!string.IsNullOrEmpty(uploadResult.Url))
                    {
                        room.RoomPhotos.Add(new RoomPhoto
                        {
                            Url = uploadResult.Url,
                            PublicId = uploadResult.PublicId,
                            IsMain = !room.RoomPhotos.Any(), // If no photos exist, new one is main
                            DisplayOrder = order,
                            UploadedAt = DateTime.UtcNow
                        });
                        order++;
                    }
                }
            }

            await _repository.UpdateAsync(room);
        }

        public async Task DeleteRoomAsync(int id, string currentUserId)
        {
            var room = await _repository.GetByIdAsync(id);

            if (room == null)
                throw new KeyNotFoundException("Room not found");

            if (room.LandlordId != currentUserId)
                throw new UnauthorizedAccessException("You do not have permission to delete this room.");

            // Delete photos from Cloudinary
            if (room.RoomPhotos != null && room.RoomPhotos.Any())
            {
                foreach (var photo in room.RoomPhotos)
                {
                    await _cloudinaryService.DeleteImageAsync(photo.PublicId);
                }
            }

            await _repository.DeleteAsync(room);
        }
    }
}
