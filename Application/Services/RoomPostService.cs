using Application.DTOs.RoomPosts;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Domain.Entities;

namespace Application.Services
{
    public class RoomPostService : IRoomPostService
    {
        private readonly IRoomPostRepository _repository;

        public RoomPostService(IRoomPostRepository repository)
        {
            _repository = repository;
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

        public async Task<CreateRoomViewModel> GetCreateViewModelAsync(string landlordId)
        {
            var floors = await _repository.GetFloorsByLandlordIdAsync(landlordId);
            var amenities = await _repository.GetAllAmenitiesAsync();

            return new CreateRoomViewModel
            {
                AvailableFloors = floors.ToList(),
                AvailableAmenities = amenities.ToList()
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
                AvailableAmenities = amenities.ToList()
            };
        }

        public async Task<RoomDetailsViewModel> GetRoomDetailsAsync(int id)
        {
            var room = await _repository.GetByIdAsync(id);
            if (room == null)
            {
                throw new KeyNotFoundException("Room not found");
            }

            // Simple parse of JSON array for photos. Since we don't have JSON library ready mapped, 
            // string.IsNullOrWhiteSpace check and split
            var photos = new List<string>();
            if (!string.IsNullOrWhiteSpace(room.Photos))
            {
                // Just for testing/fallback, assuming it could be comma-separated or json
                photos = room.Photos.Replace("[", "").Replace("]", "").Replace("\"", "").Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();
            }
            if (!photos.Any())
            {
                photos.Add("https://images.unsplash.com/photo-1522708323590-d24dbb6b0267?w=800&q=80");
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

        public async Task CreateRoomAsync(CreateRoomViewModel model, string landlordId)
        {
            var room = new Room
            {
                LandlordId = landlordId,
                Title = model.Title,
                RoomNumber = model.RoomNumber,
                RoomType = model.RoomType,
                BasePrice = model.BasePrice,
                SurfaceArea = model.SurfaceArea,
                MaxCapacity = model.MaxCapacity,
                Description = model.Description,
                IsFurnished = model.IsFurnished,
                FloorId = model.FloorId,
                Status = model.Status,
                CreatedAt = DateTime.UtcNow
            };

            if (model.SelectedAmenityIds != null && model.SelectedAmenityIds.Any())
            {
                foreach (var amenityId in model.SelectedAmenityIds)
                {
                    room.RoomAmenities.Add(new RoomAmenity { AmenityId = amenityId });
                }
            }

            await _repository.AddAsync(room);
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

            await _repository.UpdateAsync(room);
        }

        public async Task DeleteRoomAsync(int id, string currentUserId)
        {
            var room = await _repository.GetByIdAsync(id);

            if (room == null)
                throw new KeyNotFoundException("Room not found");

            if (room.LandlordId != currentUserId)
                throw new UnauthorizedAccessException("You do not have permission to delete this room.");

            await _repository.DeleteAsync(room);
        }
    }
}
