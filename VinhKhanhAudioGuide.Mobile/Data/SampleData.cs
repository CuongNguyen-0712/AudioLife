namespace VinhKhanhAudioGuide.Mobile.Data;

using VinhKhanhAudioGuide.Mobile.Models;

public static class SampleData
{
    public static List<Category> GetCategories()
    {
        
        return new List<Models.Category>
        {
            new() { Id = "1", Name = "Đặc sản", Icon = "specialty.svg" },
            new() { Id = "2", Name = "Ăn vặt", Icon = "snack.svg" },
            new() { Id = "3", Name = "Ăn đêm", Icon = "night_food.svg" },
            new() { Id = "4", Name = "Hải sản", Icon = "seafood.svg" },
            new() { Id = "5", Name = "Đồ uống", Icon = "drink.svg" },
            
        };
    }

    //Location
    public static List<Models.Location> GetLocations()
    {
        var locations = new List<Models.Location>
    {
        // ===== ĐẶC SẢN (1) =====
        new() { Id = "loc_001", Name = "Bún mắm Vĩnh Khánh", Description = "Đậm đà hương vị miền Tây", ImageUrl = "bun_mam.jpg", Address = "Vĩnh Khánh, Q4", Duration = 30, CategoryId = "1" },
        new() { Id = "loc_002", Name = "Bánh xèo miền Tây", Description = "Giòn rụm, ăn kèm rau sống", ImageUrl = "banh_xeo.jpg", Address = "Vĩnh Khánh, Q4", Duration = 25, CategoryId = "1" },

        // ===== ĂN VẶT (2) =====
        new() { Id = "loc_003", Name = "Bánh tráng trộn", Description = "Chua cay mặn ngọt", ImageUrl = "banh_trang_tron.jpg", Address = "Vĩnh Khánh, Q4", Duration = 15, CategoryId = "2" },
        new() { Id = "loc_004", Name = "Xiên que nướng", Description = "Đa dạng topping hấp dẫn", ImageUrl = "tom_nuong_muoi_ot.jpg", Address = "Vĩnh Khánh, Q4", Duration = 20, CategoryId = "2" },

        // ===== ĂN ĐÊM (3) =====
        new() { Id = "loc_005", Name = "Cháo sườn đêm", Description = "Nóng hổi, dễ ăn", ImageUrl = "chao_suon.jpg", Address = "Vĩnh Khánh, Q4", Duration = 20, CategoryId = "3" },
        new() { Id = "loc_006", Name = "Phở khuya", Description = "Đậm vị, phục vụ khuya", ImageUrl = "pho.jpg", Address = "Vĩnh Khánh, Q4", Duration = 30, CategoryId = "3" },

        // ===== HẢI SẢN (4) =====
        new() { Id = "loc_007", Name = "Ốc xào bơ tỏi", Description = "Thơm béo, đậm vị", ImageUrl = "oc_xao_bo_toi.jpg", Address = "Vĩnh Khánh, Q4", Duration = 35, CategoryId = "4" },
        new() { Id = "loc_008", Name = "Tôm nướng muối ớt", Description = "Cay nhẹ, thơm lừng", ImageUrl = "tom_nuong_muoi_ot.jpg", Address = "Vĩnh Khánh, Q4", Duration = 40, CategoryId = "4" },

        // ===== ĐỒ UỐNG (5) =====
        new() { Id = "loc_009", Name = "Trà sữa trân châu", Description = "Ngọt béo, topping đa dạng", ImageUrl = "ca_phe_sua_da.jpg", Address = "Vĩnh Khánh, Q4", Duration = 15, CategoryId = "5" },
        new() { Id = "loc_010", Name = "Cà phê sữa đá", Description = "Đậm vị truyền thống", ImageUrl = "ca_phe_sua_da.jpg", Address = "Vĩnh Khánh, Q4", Duration = 20, CategoryId = "5" }
    };

        // Attach audio guides to each location for convenience
        var audioGuides = GetAudioGuides();
        foreach (var loc in locations)
        {
            loc.AudioGuides = audioGuides.Where(a => a.LocationId == loc.Id).ToList();
        }

        // Mark a few sample favorites so FavoriteLocations show up initially
        var favIds = new[] { "loc_001", "loc_003", "loc_007" };
        foreach (var loc in locations.Where(l => favIds.Contains(l.Id)))
        {
            loc.IsFavorite = true;
        }

        return locations;
    }

    //Audio Guide
    public static List<Models.AudioGuide> GetAudioGuides()
    {
        return new List<Models.AudioGuide>
    {
        // ===== loc_001 - BÚN MẮM =====
        new() { Id = "ag_001_1", Title = "Giới thiệu quán", Description = "Quán bún mắm lâu năm tại Vĩnh Khánh", AudioUrl = "bun_mam_place.mp3", Duration = 3, LocationId = "loc_001", Language = "vi" },
        new() { Id = "ag_001_2", Title = "Khám phá ẩm thực", Description = "Bún mắm đậm đà hương vị miền Tây", AudioUrl = "bun_mam_food.mp3", Duration = 4, LocationId = "loc_001", Language = "vi" },

        // ===== loc_002 - BÁNH XÈO =====
        new() { Id = "ag_002_1", Title = "Giới thiệu quán", Description = "Quán bánh xèo giòn rụm, đông khách", AudioUrl = "banh_xeo_place.mp3", Duration = 3, LocationId = "loc_002", Language = "vi" },
        new() { Id = "ag_002_2", Title = "Khám phá ẩm thực", Description = "Bánh xèo miền Tây ăn kèm rau sống", AudioUrl = "banh_xeo_food.mp3", Duration = 4, LocationId = "loc_002", Language = "vi" },

        // ===== loc_003 - BÁNH TRÁNG =====
        new() { Id = "ag_003_1", Title = "Giới thiệu quán", Description = "Quầy ăn vặt quen thuộc giới trẻ", AudioUrl = "banh_trang_place.mp3", Duration = 2, LocationId = "loc_003", Language = "vi" },
        new() { Id = "ag_003_2", Title = "Khám phá ẩm thực", Description = "Bánh tráng trộn chua cay hấp dẫn", AudioUrl = "banh_trang_food.mp3", Duration = 3, LocationId = "loc_003", Language = "vi" },

        // ===== loc_004 - XIÊN QUE =====
        new() { Id = "ag_004_1", Title = "Giới thiệu quán", Description = "Quán xiên que nhộn nhịp buổi tối", AudioUrl = "xien_que_place.mp3", Duration = 2, LocationId = "loc_004", Language = "vi" },
        new() { Id = "ag_004_2", Title = "Khám phá ẩm thực", Description = "Xiên nướng đa dạng, thơm ngon", AudioUrl = "xien_que_food.mp3", Duration = 3, LocationId = "loc_004", Language = "vi" },

        // ===== loc_005 - CHÁO SƯỜN =====
        new() { Id = "ag_005_1", Title = "Giới thiệu quán", Description = "Quán cháo đêm quen thuộc", AudioUrl = "chao_suon_place.mp3", Duration = 3, LocationId = "loc_005", Language = "vi" },
        new() { Id = "ag_005_2", Title = "Khám phá ẩm thực", Description = "Cháo sườn nóng hổi, dễ ăn", AudioUrl = "chao_suon_food.mp3", Duration = 3, LocationId = "loc_005", Language = "vi" },

        // ===== loc_006 - PHỞ =====
        new() { Id = "ag_006_1", Title = "Giới thiệu quán", Description = "Quán phở phục vụ khuya", AudioUrl = "pho_place.mp3", Duration = 3, LocationId = "loc_006", Language = "vi" },
        new() { Id = "ag_006_2", Title = "Khám phá ẩm thực", Description = "Phở đậm vị truyền thống", AudioUrl = "pho_food.mp3", Duration = 4, LocationId = "loc_006", Language = "vi" },

        // ===== loc_007 - ỐC =====
        new() { Id = "ag_007_1", Title = "Giới thiệu quán", Description = "Quán ốc lâu năm nổi tiếng", AudioUrl = "oc_place.mp3", Duration = 3, LocationId = "loc_007", Language = "vi" },
        new() { Id = "ag_007_2", Title = "Khám phá ẩm thực", Description = "Ốc xào bơ tỏi thơm béo", AudioUrl = "oc_food.mp3", Duration = 4, LocationId = "loc_007", Language = "vi" },

        // ===== loc_008 - TÔM =====
        new() { Id = "ag_008_1", Title = "Giới thiệu quán", Description = "Quán hải sản tươi sống", AudioUrl = "tom_place.mp3", Duration = 3, LocationId = "loc_008", Language = "vi" },
        new() { Id = "ag_008_2", Title = "Khám phá ẩm thực", Description = "Tôm nướng muối ớt đậm vị", AudioUrl = "tom_food.mp3", Duration = 4, LocationId = "loc_008", Language = "vi" },

        // ===== loc_009 - TRÀ SỮA =====
        new() { Id = "ag_009_1", Title = "Giới thiệu quán", Description = "Quán trà sữa quen thuộc", AudioUrl = "tra_sua_place.mp3", Duration = 2, LocationId = "loc_009", Language = "vi" },
        new() { Id = "ag_009_2", Title = "Khám phá ẩm thực", Description = "Trà sữa topping đa dạng", AudioUrl = "tra_sua_food.mp3", Duration = 3, LocationId = "loc_009", Language = "vi" },

        // ===== loc_010 - CÀ PHÊ =====
        new() { Id = "ag_010_1", Title = "Giới thiệu quán", Description = "Quán cà phê vỉa hè đặc trưng", AudioUrl = "ca_phe_place.mp3", Duration = 3, LocationId = "loc_010", Language = "vi" },
        new() { Id = "ag_010_2", Title = "Khám phá ẩm thực", Description = "Cà phê sữa đá đậm đà", AudioUrl = "ca_phe_food.mp3", Duration = 3, LocationId = "loc_010", Language = "vi" }
    };
    }


    //Tours
    public static List<Models.Tour> GetTours()
    {
        return new List<Models.Tour>
    {
        new()
        {
            Id = "tour_001",
            Name = "Food Tour Vĩnh Khánh",
            Description = "Khám phá thiên đường ẩm thực đường phố",
            ImageUrl = "foodtour_vinhkhanh.jpg",
            Duration = 180,
            LocationIds = new List<string> { "loc_003", "loc_004", "loc_007", "loc_008" },
            Price = 0,
            IsFeatured = true
        },
        new()
        {
            Id = "tour_002",
            Name = "Ăn đêm Sài Gòn",
            Description = "Trải nghiệm ẩm thực về đêm",
            ImageUrl = "an_dem.jpg",
            Duration = 120,
            LocationIds = new List<string> { "loc_005", "loc_006" },
            Price = 0,
            IsFeatured = true
        },
        new()
        {
            Id = "tour_003",
            Name = "Combo đặc sản miền Tây",
            Description = "Thưởng thức món ngon truyền thống",
            ImageUrl = "dac_san_mien_tay.jpg",
            Duration = 150,
            LocationIds = new List<string> { "loc_001", "loc_002" },
            Price = 0,
            IsFeatured = false
        },
        new()
        {
            Id = "tour_004",
            Name = "Ăn vặt & chill",
            Description = "Combo nhẹ nhàng cho giới trẻ",
            ImageUrl = "an_vat.jpg",
            Duration = 90,
            LocationIds = new List<string> { "loc_003", "loc_004", "loc_009" },
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
