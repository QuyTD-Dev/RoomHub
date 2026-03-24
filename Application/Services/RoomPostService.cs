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

        public async Task<PaginatedList<RoomListViewModel>> GetAllRoomsAsync(string? keyword = null, string? province = null, Domain.Enums.RoomType? roomType = null, decimal? minPrice = null, decimal? maxPrice = null, string? district = null, string? sortBy = null, int pageIndex = 1, int pageSize = 9)
        {
            var (rooms, totalCount) = await _repository.PaginatedSearchAsync(keyword, province, roomType, minPrice, maxPrice, district, sortBy, pageIndex, pageSize);

            var items = rooms.Select(r => new RoomListViewModel
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
                // Lấy ảnh chính hoặc ảnh đầu tiên
                MainPhotoUrl = r.RoomPhotos?.FirstOrDefault(p => p.IsMain)?.Url ?? r.RoomPhotos?.OrderBy(p => p.DisplayOrder).FirstOrDefault()?.Url
            }).ToList();

            return new PaginatedList<RoomListViewModel>(items, totalCount, pageIndex, pageSize);
        }

        public async Task<IEnumerable<RoomSuggestionDto>> GetSuggestionsAsync(string keyword, string? province = null, int maxResults = 6)
        {
            if (string.IsNullOrWhiteSpace(keyword)) return Enumerable.Empty<RoomSuggestionDto>();

            var rooms = await _repository.SearchAsync(keyword, province);

            return rooms.Take(maxResults).Select(r =>
            {
                var mainPhoto = r.RoomPhotos?.FirstOrDefault(p => p.IsMain)?.Url
                                ?? r.RoomPhotos?.OrderBy(p => p.DisplayOrder).FirstOrDefault()?.Url
                                ?? "https://images.unsplash.com/photo-1522708323590-d24dbb6b0267?w=400&q=70";

                return new RoomSuggestionDto
                {
                    Id = r.Id,
                    Title = r.Title,
                    BasePrice = r.BasePrice,
                    MainPhotoUrl = mainPhoto,
                    Address = r.Floor?.Building?.Address ?? "Chưa cập nhật",
                    RoomType = r.RoomType
                };
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

        // =========================
        // PUBLIC BROWSING
        // =========================

        public async Task<IEnumerable<RoomListViewModel>> GetPublicRoomsAsync()
        {
            var rooms = await _repository.GetAvailableRoomsAsync();
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

        public async Task<RoomDetailsViewModel> GetPublicRoomDetailsAsync(int id)
        {
            var room = await _repository.GetRoomDetailsByIdAsync(id);
            if (room == null)
                throw new KeyNotFoundException("Room not found");

            var photoUrls = new List<RoomPhotoViewModel>();
            if (room.RoomPhotos != null && room.RoomPhotos.Any())
            {
                photoUrls = room.RoomPhotos.OrderBy(p => p.DisplayOrder).Select(p => new RoomPhotoViewModel
                {
                    Id = p.Id,
                    Url = p.Url,
                    IsMain = p.IsMain
                }).ToList();
            }
            if (!photoUrls.Any())
            {
                photoUrls.Add(new RoomPhotoViewModel { Id = 0, Url = "https://images.unsplash.com/photo-1522708323590-d24dbb6b0267?w=800&q=80", IsMain = true });
            }

            var address = room.Floor?.Building?.Address ?? "Chưa cập nhật";
            var locationDetails = "";
            if (room.Floor?.Building != null)
            {
                locationDetails = $"{room.Floor.Building.Ward}, {room.Floor.Building.District}, {room.Floor.Building.City}";
            }

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
                Photos = photoUrls,
                DepositAmount = room.Deposits?.FirstOrDefault()?.Amount ?? room.BasePrice,
                Amenities = room.RoomAmenities?
                    .Where(ra => ra.Amenity != null)
                    .Select(ra => new AmenityViewModel
                    {
                        Id = ra.Amenity.Id,
                        Name = ra.Amenity.Name,
                        IconUrl = ra.Amenity.IconUrl
                    }).ToList() ?? new List<AmenityViewModel>(),
                LandlordId = room.LandlordId,
                LandlordName = room.Landlord?.FullName ?? "Landlord",
                LandlordAvatarUrl = room.Landlord?.AvatarUrl ?? $"https://ui-avatars.com/api/?name={room.Landlord?.FullName ?? "L"}&background=FF6B35&color=fff",
                LandlordPhone = room.Landlord?.PhoneNumber,
                LandlordJoinedYears = joinedYears
            };
        }
    }
}
