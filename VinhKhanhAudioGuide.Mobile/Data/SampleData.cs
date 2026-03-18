namespace VinhKhanhAudioGuide.Mobile.Data;

using VinhKhanhAudioGuide.Mobile.Models;

public static class SampleData
{
    public static List<Category> GetCategories()
    {
        return new List<Category>
        {
            new() { Id = "1", Name = "Món nước", Icon = "fa-bowl-food", Description = "Bún, mì, cháo và các món nước đặc trưng Vĩnh Khánh" },
            new() { Id = "2", Name = "Hải sản", Icon = "fa-fish", Description = "Ốc, sò, tôm, cua đậm vị khu ẩm thực đêm" },
            new() { Id = "3", Name = "Món nướng", Icon = "fa-fire-burner", Description = "Các món nướng than hoa tại phố ẩm thực" },
            new() { Id = "4", Name = "Ăn vặt", Icon = "fa-cookie-bite", Description = "Món ăn nhanh, gỏi cuốn, phá lấu, bánh tráng" },
            new() { Id = "5", Name = "Lẩu đêm", Icon = "fa-hot-tub-person", Description = "Các quán lẩu phục vụ buổi tối" },
            new() { Id = "6", Name = "Tráng miệng & Cà phê", Icon = "fa-mug-hot", Description = "Chè, sữa chua, cà phê phục vụ sau bữa chính" }
        };
    }

    public static List<Location> GetLocations()
    {
        return new List<Location>
        {
            new()
            {
                Id = "loc_001",
                Name = "Ốc Oanh Vĩnh Khánh",
                Description = "Quán ốc lâu năm nổi tiếng tại phố ẩm thực Vĩnh Khánh với các món ốc xào bơ tỏi, ốc len xào dừa và sò điệp nướng mỡ hành.",
                ImageUrl = "/images/locations/oc_oanh.jpg",
                Address = "534 Vĩnh Khánh, Phường 8, Quận 4, TP.HCM",
                Latitude = 10.7595,
                Longitude = 106.7038,
                Duration = 35,
                CategoryId = "2",
                AudioGuides = CreateAudioGuides("loc_001", "001", "Ốc hương xào bơ tỏi")
            },
            new()
            {
                Id = "loc_002",
                Name = "Bún bò O Xíu",
                Description = "Quán bún bò đậm vị miền Trung, nổi bật với nước dùng cay nhẹ, topping đầy đặn và phục vụ xuyên tối.",
                ImageUrl = "/images/locations/bun_bo_o_xiu.jpg",
                Address = "212 Vĩnh Khánh, Phường 10, Quận 4, TP.HCM",
                Latitude = 10.7611,
                Longitude = 106.7051,
                Duration = 25,
                CategoryId = "1",
                AudioGuides = CreateAudioGuides("loc_002", "002", "Bún bò đặc biệt")
            },
            new()
            {
                Id = "loc_003",
                Name = "Bạch tuộc nướng Sáu Lửa",
                Description = "Bạch tuộc nướng sa tế, mực nướng muối ớt và các món nướng than hoa rất được ưa chuộng vào buổi tối.",
                ImageUrl = "/images/locations/bach_tuoc_nuong.jpg",
                Address = "181 Vĩnh Khánh, Phường 10, Quận 4, TP.HCM",
                Latitude = 10.7608,
                Longitude = 106.7046,
                Duration = 30,
                CategoryId = "3",
                AudioGuides = CreateAudioGuides("loc_003", "003", "Bạch tuộc nướng sa tế")
            },
            new()
            {
                Id = "loc_004",
                Name = "Sò nướng mỡ hành 14",
                Description = "Quầy hải sản bình dân chuyên sò nướng mỡ hành, nghêu hấp sả và ốc móng tay xào rau muống.",
                ImageUrl = "/images/locations/so_nuong_14.jpg",
                Address = "14 Vĩnh Khánh, Phường 8, Quận 4, TP.HCM",
                Latitude = 10.7599,
                Longitude = 106.7029,
                Duration = 28,
                CategoryId = "2",
                AudioGuides = CreateAudioGuides("loc_004", "004", "Sò nướng mỡ hành")
            },
            new()
            {
                Id = "loc_005",
                Name = "Phá lấu bò Cô Thắm",
                Description = "Món phá lấu bò ăn kèm bánh mì nóng giòn, nước chấm me đặc trưng, rất phù hợp khám phá về đêm.",
                ImageUrl = "/images/locations/pha_lau_co_tham.jpg",
                Address = "128 Vĩnh Khánh, Phường 10, Quận 4, TP.HCM",
                Latitude = 10.7604,
                Longitude = 106.7041,
                Duration = 20,
                CategoryId = "4",
                AudioGuides = CreateAudioGuides("loc_005", "005", "Phá lấu bò truyền thống")
            },
            new()
            {
                Id = "loc_006",
                Name = "Gỏi cuốn Cô Ba",
                Description = "Gỏi cuốn tôm thịt, bò bía và các món cuốn thanh vị, phục vụ nhanh cho khách đi bộ tham quan phố ẩm thực.",
                ImageUrl = "/images/locations/goi_cuon_co_ba.jpg",
                Address = "96 Vĩnh Khánh, Phường 10, Quận 4, TP.HCM",
                Latitude = 10.7601,
                Longitude = 106.7036,
                Duration = 18,
                CategoryId = "4",
                AudioGuides = CreateAudioGuides("loc_006", "006", "Gỏi cuốn tôm thịt")
            },
            new()
            {
                Id = "loc_007",
                Name = "Lẩu hải sản Chị Mười",
                Description = "Quán lẩu đêm nổi tiếng với lẩu hải sản chua cay, topping tươi và không khí nhộn nhịp.",
                ImageUrl = "/images/locations/lau_hai_san_chi_muoi.jpg",
                Address = "66 Vĩnh Khánh, Phường 8, Quận 4, TP.HCM",
                Latitude = 10.7597,
                Longitude = 106.7032,
                Duration = 55,
                CategoryId = "5",
                AudioGuides = CreateAudioGuides("loc_007", "007", "Lẩu hải sản chua cay")
            },
            new()
            {
                Id = "loc_008",
                Name = "Chè khúc bạch Vĩnh Khánh",
                Description = "Quán chè đêm với khúc bạch, tàu hũ, sâm bổ lượng, phù hợp kết thúc hành trình ẩm thực.",
                ImageUrl = "/images/locations/che_khuc_bach.jpg",
                Address = "310 Vĩnh Khánh, Phường 10, Quận 4, TP.HCM",
                Latitude = 10.7614,
                Longitude = 106.7056,
                Duration = 15,
                CategoryId = "6",
                AudioGuides = CreateAudioGuides("loc_008", "008", "Chè khúc bạch")
            },
            new()
            {
                Id = "loc_009",
                Name = "Cà phê vợt đêm 1975",
                Description = "Không gian cà phê vợt hoài cổ, phục vụ cà phê sữa đậm và bánh ngọt nhẹ cuối buổi tối.",
                ImageUrl = "/images/locations/ca_phe_vot.jpg",
                Address = "197 Vĩnh Khánh, Phường 9, Quận 4, TP.HCM",
                Latitude = 10.7609,
                Longitude = 106.7049,
                Duration = 22,
                CategoryId = "6",
                AudioGuides = CreateAudioGuides("loc_009", "009", "Cà phê vợt truyền thống")
            },
            new()
            {
                Id = "loc_010",
                Name = "Bánh tráng nướng Dì Lan",
                Description = "Quầy bánh tráng nướng, trứng nướng và xiên nướng phù hợp nhóm khách trẻ tại tuyến phố đêm.",
                ImageUrl = "/images/locations/banh_trang_nuong.jpg",
                Address = "45 Vĩnh Khánh, Phường 8, Quận 4, TP.HCM",
                Latitude = 10.7593,
                Longitude = 106.7026,
                Duration = 18,
                CategoryId = "4",
                AudioGuides = CreateAudioGuides("loc_010", "010", "Bánh tráng nướng đặc biệt")
            }
        };
    }

    private static List<AudioGuide> CreateAudioGuides(string locationId, string code, string featuredDish)
    {
        return new List<AudioGuide>
        {
            new() 
            { 
                Id = $"ag_{code}_1", 
                Title = $"Giới thiệu {featuredDish}", 
                Description = "Tổng quan món đặc trưng và phong cách phục vụ", 
                AudioUrl = $"{locationId}_intro.mp3", 
                Duration = 4, 
                LocationId = locationId, 
                Language = "vi",
                ScriptSegments = new List<AudioScriptSegment>
                {
                    new() { StartTimeSeconds = 0, EndTimeSeconds = 2, ScriptText = $"Chào mừng bạn đến với {featuredDish}." },
                    new() { StartTimeSeconds = 2, EndTimeSeconds = 4, ScriptText = "Đây là một món ăn rất đặc trưng." }
                },
                ListeningHistories = new List<ListeningHistory>
                {
                    new() { UserId = "user_1", ListenedSeconds = 4, IsCompleted = true, LastListenedAt = DateTime.UtcNow.AddDays(-1) }
                }
            },
            new() 
            { 
                Id = $"ag_{code}_2", 
                Title = "Hành trình nguyên liệu", 
                Description = "Nguồn gốc nguyên liệu và cách chọn hải sản/topping tươi", 
                AudioUrl = $"{locationId}_ingredients.mp3", 
                Duration = 5, 
                LocationId = locationId, 
                Language = "vi",
                ScriptSegments = new List<AudioScriptSegment>
                {
                    new() { StartTimeSeconds = 0, EndTimeSeconds = 3, ScriptText = "Nguyên liệu được chọn lọc kỹ càng từ vùng biển tươi ngon." },
                    new() { StartTimeSeconds = 3, EndTimeSeconds = 5, ScriptText = "Hải sản luôn đảm bảo chất lượng cao nhất." }
                }
            },
            new() 
            { 
                Id = $"ag_{code}_3", 
                Title = "Bí quyết thưởng thức", 
                Description = "Gợi ý nước chấm, mức cay và thứ tự món để trải nghiệm trọn vẹn", 
                AudioUrl = $"{locationId}_tips.mp3", 
                Duration = 4, 
                LocationId = locationId, 
                Language = "vi",
                ScriptSegments = new List<AudioScriptSegment>
                {
                    new() { StartTimeSeconds = 0, EndTimeSeconds = 2, ScriptText = "Nước chấm đặc biệt làm tăng thêm hương vị." },
                    new() { StartTimeSeconds = 2, EndTimeSeconds = 4, ScriptText = "Bạn nên ăn kèm với một chút rau thơm." }
                }
            }
        };
    }

    public static List<Tour> GetTours()
    {
        return new List<Tour>
        {
            new()
            {
                Id = "tour_001",
                Name = "Food Walk Vĩnh Khánh cơ bản",
                Description = "Hành trình nhập môn gồm món nước, hải sản và ăn vặt nổi tiếng tại tuyến phố ẩm thực Vĩnh Khánh.",
                ImageUrl = "/images/tours/tour_vinhkhanh_basic.jpg",
                Duration = 150,
                LocationIds = new List<string> { "loc_002", "loc_001", "loc_006" },
                Price = 0,
                IsFeatured = true
            },
            new()
            {
                Id = "tour_002",
                Name = "Hải sản đêm Vĩnh Khánh",
                Description = "Tập trung các điểm hải sản và nướng than hoa được yêu thích nhất vào khung giờ tối.",
                ImageUrl = "/images/tours/tour_vinhkhanh_seafood.jpg",
                Duration = 180,
                LocationIds = new List<string> { "loc_001", "loc_004", "loc_003", "loc_007" },
                Price = 0,
                IsFeatured = true
            },
            new()
            {
                Id = "tour_003",
                Name = "Ăn vặt và tráng miệng",
                Description = "Tour nhẹ dành cho nhóm trẻ: phá lấu, bánh tráng nướng, chè và cà phê vợt đêm.",
                ImageUrl = "/images/tours/tour_vinhkhanh_snack.jpg",
                Duration = 120,
                LocationIds = new List<string> { "loc_005", "loc_010", "loc_008", "loc_009" },
                Price = 0,
                IsFeatured = false
            },
            new()
            {
                Id = "tour_004",
                Name = "Combo gia đình cuối tuần",
                Description = "Lộ trình cân bằng món chính, món nướng và điểm tráng miệng phù hợp gia đình có trẻ nhỏ.",
                ImageUrl = "/images/tours/tour_vinhkhanh_family.jpg",
                Duration = 210,
                LocationIds = new List<string> { "loc_002", "loc_003", "loc_007", "loc_008" },
                Price = 0,
                IsFeatured = false
            }
        };
    }

    public static Location? GetLocationById(string id)
    {
        return GetLocations().FirstOrDefault(l => l.Id == id);
    }

    public static Tour? GetTourById(string id)
    {
        return GetTours().FirstOrDefault(t => t.Id == id);
    }

    public static Category? GetCategoryById(string id)
    {
        return GetCategories().FirstOrDefault(c => c.Id == id);
    }

    public static List<Location> GetLocationsByCategory(string categoryId)
    {
        return GetLocations().Where(l => l.CategoryId == categoryId).ToList();
    }

    public static List<Location> GetLocationsByIds(List<string> ids)
    {
        return GetLocations().Where(l => ids.Contains(l.Id)).ToList();
    }

    public static List<Tour> GetFeaturedTours()
    {
        return GetTours().Where(t => t.IsFeatured).ToList();
    }
}
