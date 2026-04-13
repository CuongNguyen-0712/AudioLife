namespace VinhKhanhAudioGuide.Web.Data;

using VinhKhanhAudioGuide.Web.Models;

public static class SampleData
{

    public static List<Category> GetCategories()
    {
        return new List<Category>
        {
            new() { Id = "1", Name = "Đặc sản", Icon = "fa-bowl-food", Description = "Các món đặc sản đặc trưng mang hương vị truyền thống." },
            new() { Id = "2", Name = "Ăn vặt", Icon = "fa-cookie-bite", Description = "Bánh tráng, xiên que và các món ăn chơi hấp dẫn." },
            new() { Id = "3", Name = "Ăn đêm", Icon = "fa-moon", Description = "Cháo sườn, phở và các món phục vụ khuya." },
            new() { Id = "4", Name = "Hải sản", Icon = "fa-fish", Description = "Ốc, tôm, cua, mực tươi sống nướng, xào." },
            new() { Id = "5", Name = "Đồ uống", Icon = "fa-mug-hot", Description = "Cà phê, trà sữa và đồ uống giải khát." }
        };
    }

    public static List<Location> GetLocations()
    {
        return new List<Location>
        {
            // ===== ĐẶC SẢN (1) =====
            new()
            {
                Id = "loc_001",
                Name = "Bún mắm Vĩnh Khánh",
                Description = "Đậm đà hương vị miền Tây, nước lèo thơm phức với đa dạng topping tôm, heo quay, chả vịt.",
                ImageUrl = "/images/locations/bun_mam.jpg",
                Address = "Vĩnh Khánh, Q4, TP.HCM",
                Latitude = 10.7595,
                Longitude = 106.7038,
                Duration = 30,
                CategoryId = "1",
                AudioGuides = new List<AudioGuide>
                {
                    new() { Id = "ag_001_1", Title = "Giới thiệu quán", Description = "Thông tin và câu chuyện bún mắm", AudioUrl = "https://res.cloudinary.com/dex6q1cqh/video/upload/f_mp3/v1773934751/gioithieu_bunmam_l8un47.wav", Duration = 3, LocationId = "loc_001", Language = "vi" },
                    new() { Id = "ag_001_2", Title = "Khám phá ẩm thực", Description = "Hương vị bún mắm", AudioUrl = "https://res.cloudinary.com/dex6q1cqh/video/upload/f_mp3/v1773934753/amthuc_bunmam_wen2s0.wav", Duration = 4, LocationId = "loc_001", Language = "vi" },
                    new() { Id = "ag_001_3", Title = "Introduction", Description = "About Bun Mam Vinh Khanh", AudioUrl = "", Duration = 3, LocationId = "loc_001", Language = "en", GeneratedFromTts = true, TtsSourceText = "Welcome to Bun Mam Vinh Khanh, a beloved noodle shop in District 4, Ho Chi Minh City. Bun Mam is a rich and flavorful fermented fish noodle soup originating from the Mekong Delta. The broth is deeply aromatic, topped with succulent shrimp, roast pork, and duck meatballs. This is a must-try dish for anyone exploring Vietnamese street food culture." },
                    new() { Id = "ag_001_4", Title = "Food Discovery", Description = "Taste of Bun Mam", AudioUrl = "", Duration = 4, LocationId = "loc_001", Language = "en", GeneratedFromTts = true, TtsSourceText = "The secret to great Bun Mam lies in its broth — a perfectly balanced blend of fermented fish sauce, lemongrass, and fresh herbs. Each bowl comes with a generous variety of toppings including grilled pork, shrimp, eggplant, and fresh vegetables. The combination creates a symphony of sweet, savory, and umami flavors that defines southern Vietnamese cuisine." }
                }
            },
            new()
            {
                Id = "loc_002",
                Name = "Bánh xèo miền Tây",
                Description = "Giòn rụm, ăn kèm rau sống phong phú. Bánh xèo đúc chảo gang truyền thống.",
                ImageUrl = "/images/locations/banh_xeo.jpg",
                Address = "Vĩnh Khánh, Q4, TP.HCM",
                Latitude = 10.7611,
                Longitude = 106.7051,
                Duration = 25,
                CategoryId = "1",
                AudioGuides = new List<AudioGuide>
                {
                    new() { Id = "ag_002_1", Title = "Giới thiệu quán", Description = "Thông tin bánh xèo", AudioUrl = "https://res.cloudinary.com/dex6q1cqh/video/upload/f_mp3/v1773934750/gioithieu_banhxeo_l94n9t.wav", Duration = 3, LocationId = "loc_002", Language = "vi" },
                    new() { Id = "ag_002_2", Title = "Khám phá ẩm thực", Description = "Hương vị bánh xèo", AudioUrl = "https://res.cloudinary.com/dex6q1cqh/video/upload/f_mp3/v1773934750/amthuc_banhxeo_vld0dx.wav", Duration = 4, LocationId = "loc_002", Language = "vi" },
                    new() { Id = "ag_002_3", Title = "Introduction", Description = "About Banh Xeo", AudioUrl = "", Duration = 3, LocationId = "loc_002", Language = "en", GeneratedFromTts = true, TtsSourceText = "Discover the crispy delight of Banh Xeo, a traditional Vietnamese crepe from the Mekong Delta. Cooked in a cast iron pan, this golden crepe is filled with shrimp, pork, bean sprouts, and served with an abundance of fresh herbs and lettuce for wrapping." },
                    new() { Id = "ag_002_4", Title = "Food Discovery", Description = "Taste of Banh Xeo", AudioUrl = "", Duration = 4, LocationId = "loc_002", Language = "en", GeneratedFromTts = true, TtsSourceText = "The name Banh Xeo comes from the sizzling sound the batter makes when it hits the hot pan. The perfect Banh Xeo is ultra-crispy on the outside with a savory filling inside. Dip it in the sweet and tangy fish sauce, wrap it with fresh herbs, and enjoy an explosion of textures and flavors." }
                }
            },
            // ===== ĂN VẶT (2) =====
            new()
            {
                Id = "loc_003",
                Name = "Bánh tráng trộn",
                Description = "Chua cay mặn ngọt hài hòa, topping khô bò, trứng cút, xoài băm cực kỳ hấp dẫn.",
                ImageUrl = "/images/locations/banh_trang_tron.jpg",
                Address = "Vĩnh Khánh, Q4, TP.HCM",
                Latitude = 10.7608,
                Longitude = 106.7046,
                Duration = 15,
                CategoryId = "2",
                AudioGuides = new List<AudioGuide>
                {
                    new() { Id = "ag_003_1", Title = "Giới thiệu quán", Description = "Thông tin bánh tráng", AudioUrl = "https://res.cloudinary.com/dex6q1cqh/video/upload/f_mp3/v1773934750/gioithieu_banhtrang_ujedfy.wav", Duration = 3, LocationId = "loc_003", Language = "vi" },
                    new() { Id = "ag_003_2", Title = "Khám phá ẩm thực", Description = "Thưởng thức bánh tráng", AudioUrl = "https://res.cloudinary.com/dex6q1cqh/video/upload/f_mp3/v1773934748/amthuc_banhtrang_qglacj.wav", Duration = 4, LocationId = "loc_003", Language = "vi" },
                    new() { Id = "ag_003_3", Title = "Introduction", Description = "About Banh Trang Tron", AudioUrl = "", Duration = 3, LocationId = "loc_003", Language = "en", GeneratedFromTts = true, TtsSourceText = "Banh Trang Tron is one of Vietnam's most popular street snacks. It's a mix of shredded rice paper tossed with dried beef, quail eggs, green mango, herbs, and a spicy-sweet-sour dressing. Simple yet incredibly addictive, this snack captures the essence of Vietnamese street food." }
                }
            },
            new()
            {
                Id = "loc_004",
                Name = "Xiên que nướng",
                Description = "Đa dạng topping hấp dẫn từ thịt xiên, xúc xích, đậu bắp nướng than hoa nóng hổi.",
                ImageUrl = "/images/locations/tom_nuong_muoi_ot.jpg",
                Address = "Vĩnh Khánh, Q4, TP.HCM",
                Latitude = 10.7599,
                Longitude = 106.7029,
                Duration = 20,
                CategoryId = "2",
                AudioGuides = new List<AudioGuide>
                {
                    new() { Id = "ag_004_1", Title = "Giới thiệu quán", Description = "Câu chuyện xiên que", AudioUrl = "https://res.cloudinary.com/dex6q1cqh/video/upload/f_mp3/v1773934748/gioithieu_xienque_miulom.wav", Duration = 3, LocationId = "loc_004", Language = "vi" },
                    new() { Id = "ag_004_2", Title = "Khám phá ẩm thực", Description = "Hương vị xiên nướng", AudioUrl = "https://res.cloudinary.com/dex6q1cqh/video/upload/f_mp3/v1773934746/amthuc_xienque_z8vgny.wav", Duration = 4, LocationId = "loc_004", Language = "vi" },
                    new() { Id = "ag_004_3", Title = "Introduction", Description = "About Grilled Skewers", AudioUrl = "", Duration = 3, LocationId = "loc_004", Language = "en", GeneratedFromTts = true, TtsSourceText = "Grilled skewers are a beloved street food in Vinh Khanh. From marinated meats and sausages to okra and mushrooms, everything is charcoal-grilled to perfection. The smoky aroma and vibrant flavors make this a favorite among locals and visitors alike." }
                }
            },
            // ===== ĂN ĐÊM (3) =====
            new()
            {
                Id = "loc_005",
                Name = "Cháo sườn đêm",
                Description = "Cháo sườn nóng hổi, thịt sụn mềm nhừ, rắc thêm tiêu và quẩy giòn tan.",
                ImageUrl = "/images/locations/chao_suon.jpg",
                Address = "Vĩnh Khánh, Q4, TP.HCM",
                Latitude = 10.7604,
                Longitude = 106.7041,
                Duration = 20,
                CategoryId = "3",
                AudioGuides = new List<AudioGuide>
                {
                    new() { Id = "ag_005_1", Title = "Giới thiệu quán", Description = "Thông tin cháo sườn", AudioUrl = "https://res.cloudinary.com/dex6q1cqh/video/upload/f_mp3/v1773934744/gioithieu_chaosuon_hxsvpg.wav", Duration = 3, LocationId = "loc_005", Language = "vi" },
                    new() { Id = "ag_005_2", Title = "Khám phá ẩm thực", Description = "Hương vị cháo sườn", AudioUrl = "https://res.cloudinary.com/dex6q1cqh/video/upload/f_mp3/v1773934747/amthuc_chaosuon_ijo2ja.wav", Duration = 4, LocationId = "loc_005", Language = "vi" },
                    new() { Id = "ag_005_3", Title = "Introduction", Description = "About Pork Rib Porridge", AudioUrl = "", Duration = 3, LocationId = "loc_005", Language = "en", GeneratedFromTts = true, TtsSourceText = "Late-night pork rib porridge is the ultimate comfort food on Vinh Khanh street. The rice porridge is slow-cooked to silky perfection, topped with tender pork ribs, crispy fried dough sticks, and a generous sprinkle of pepper. It's the perfect warm bowl for a cool Saigon evening." }
                }
            },
            new()
            {
                Id = "loc_006",
                Name = "Phở khuya",
                Description = "Phở đậm vị, nước dùng thanh ngọt nấu từ xương bò, phục vụ khách đi chơi khuya.",
                ImageUrl = "/images/locations/pho.jpg",
                Address = "Vĩnh Khánh, Q4, TP.HCM",
                Latitude = 10.7601,
                Longitude = 106.7036,
                Duration = 30,
                CategoryId = "3",
                AudioGuides = new List<AudioGuide>
                {
                    new() { Id = "ag_006_1", Title = "Giới thiệu quán", Description = "Câu chuyện quán phở", AudioUrl = "https://res.cloudinary.com/dex6q1cqh/video/upload/f_mp3/v1773934748/gioithieu_quanpho_qvkgit.wav", Duration = 3, LocationId = "loc_006", Language = "vi" },
                    new() { Id = "ag_006_2", Title = "Khám phá ẩm thực", Description = "Hương vị phở đêm", AudioUrl = "https://res.cloudinary.com/dex6q1cqh/video/upload/f_mp3/v1773934745/amthuc_pho_f8ou5l.wav", Duration = 4, LocationId = "loc_006", Language = "vi" },
                    new() { Id = "ag_006_3", Title = "Introduction", Description = "About Late-Night Pho", AudioUrl = "", Duration = 3, LocationId = "loc_006", Language = "en", GeneratedFromTts = true, TtsSourceText = "Pho is Vietnam's most iconic dish, and enjoying it late at night in Vinh Khanh is a special experience. The clear beef bone broth is simmered for hours, creating a deeply flavorful and aromatic soup. Topped with sliced beef, fresh herbs, and a squeeze of lime — this is Saigon street food at its finest." }
                }
            },
            // ===== HẢI SẢN (4) =====
            new()
            {
                Id = "loc_007",
                Name = "Ốc xào bơ tỏi",
                Description = "Thơm béo, đậm vị bơ tỏi, chấm bánh mì cực cuốn tại phố ốc Vĩnh Khánh.",
                ImageUrl = "/images/locations/oc_xao_bo_toi.jpg",
                Address = "Vĩnh Khánh, Q4, TP.HCM",
                Latitude = 10.7597,
                Longitude = 106.7032,
                Duration = 35,
                CategoryId = "4",
                AudioGuides = new List<AudioGuide>
                {
                    new() { Id = "ag_007_1", Title = "Giới thiệu quán", Description = "Khám phá quán ốc", AudioUrl = "https://res.cloudinary.com/dex6q1cqh/video/upload/f_mp3/v1773934745/amthuc_oc_vqm14u.wav", Duration = 3, LocationId = "loc_007", Language = "vi" },
                    new() { Id = "ag_007_2", Title = "Khám phá ẩm thực", Description = "Hương vị ốc xào", AudioUrl = "https://res.cloudinary.com/dex6q1cqh/video/upload/f_mp3/v1773934745/amthuc_oc_vqm14u.wav", Duration = 4, LocationId = "loc_007", Language = "vi" },
                    new() { Id = "ag_007_3", Title = "Introduction", Description = "About Garlic Butter Snails", AudioUrl = "", Duration = 3, LocationId = "loc_007", Language = "en", GeneratedFromTts = true, TtsSourceText = "Vinh Khanh street is famous for its seafood, and garlic butter snails are a crowd favorite. Sea snails are stir-fried with fragrant garlic and rich butter, creating an irresistible dish. Pair it with a crusty baguette to soak up the flavorful sauce — pure indulgence on a Saigon night." }
                }
            },
            new()
            {
                Id = "loc_008",
                Name = "Tôm nướng muối ớt",
                Description = "Tôm sú tươi rói, tẩm ướp muối ớt cay nhẹ, nướng xém vỏ thơm lừng.",
                ImageUrl = "/images/locations/tom_nuong_muoi_ot.jpg",
                Address = "Vĩnh Khánh, Q4, TP.HCM",
                Latitude = 10.7614,
                Longitude = 106.7056,
                Duration = 40,
                CategoryId = "4",
                AudioGuides = new List<AudioGuide>
                {
                    new() { Id = "ag_008_1", Title = "Giới thiệu quán", Description = "Hải sản tôm nướng", AudioUrl = "https://res.cloudinary.com/dex6q1cqh/video/upload/f_mp3/v1773934744/gioithieu_tom_bvmu5a.wav", Duration = 3, LocationId = "loc_008", Language = "vi" },
                    new() { Id = "ag_008_2", Title = "Khám phá ẩm thực", Description = "Món tôm cay nồng", AudioUrl = "https://res.cloudinary.com/dex6q1cqh/video/upload/f_mp3/v1773934744/amthuc_tom_vwvgix.wav", Duration = 4, LocationId = "loc_008", Language = "vi" },
                    new() { Id = "ag_008_3", Title = "Introduction", Description = "About Salt & Chili Grilled Prawns", AudioUrl = "", Duration = 3, LocationId = "loc_008", Language = "en", GeneratedFromTts = true, TtsSourceText = "Fresh tiger prawns marinated with salt and chili, grilled over charcoal until the shells are beautifully charred and the meat is perfectly juicy. This street seafood dish is a highlight of Vinh Khanh, combining simple seasoning with top-quality ingredients for an unforgettable taste." }
                }
            },
            // ===== ĐỒ UỐNG (5) =====
            new()
            {
                Id = "loc_009",
                Name = "Trà sữa trân châu",
                Description = "Trà sữa vị ngọt béo, đa dạng các loại topping từ trân châu đường đen đến thạch phô mai.",
                ImageUrl = "/images/locations/ca_phe_sua_da.jpg",
                Address = "Vĩnh Khánh, Q4, TP.HCM",
                Latitude = 10.7609,
                Longitude = 106.7049,
                Duration = 15,
                CategoryId = "5",
                AudioGuides = new List<AudioGuide>
                {
                    new() { Id = "ag_009_1", Title = "Giới thiệu quán", Description = "Tiệm trà sữa", AudioUrl = "https://res.cloudinary.com/dex6q1cqh/video/upload/f_mp3/v1773934747/gioithieu_trasua_aj0syw.wav", Duration = 3, LocationId = "loc_009", Language = "vi" },
                    new() { Id = "ag_009_2", Title = "Khám phá ẩm thực", Description = "Đồ uống trà sữa", AudioUrl = "https://res.cloudinary.com/dex6q1cqh/video/upload/f_mp3/v1773934745/douong_trasua_jasd5g.wav", Duration = 4, LocationId = "loc_009", Language = "vi" },
                    new() { Id = "ag_009_3", Title = "Introduction", Description = "About Bubble Tea", AudioUrl = "", Duration = 3, LocationId = "loc_009", Language = "en", GeneratedFromTts = true, TtsSourceText = "Bubble tea has become a cultural phenomenon in Vietnam, and this shop in Vinh Khanh offers a wide variety of flavors and toppings. From classic brown sugar boba to cheese foam tea, there's something for every palate. A refreshing treat after exploring the vibrant street food scene." }
                }
            },
            new()
            {
                Id = "loc_010",
                Name = "Cà phê sữa đá",
                Description = "Cà phê pha phin truyền thống, sữa đặc thơm béo, món giải khát không thể thiếu ở Sài Gòn.",
                ImageUrl = "/images/locations/ca_phe_sua_da.jpg",
                Address = "Vĩnh Khánh, Q4, TP.HCM",
                Latitude = 10.7593,
                Longitude = 106.7026,
                Duration = 20,
                CategoryId = "5",
                AudioGuides = new List<AudioGuide>
                {
                    new() { Id = "ag_010_1", Title = "Giới thiệu quán", Description = "Góc cà phê", AudioUrl = "https://res.cloudinary.com/dex6q1cqh/video/upload/f_mp3/v1773934743/gioithieu_caphep_cyhjy6.wav", Duration = 3, LocationId = "loc_010", Language = "vi" },
                    new() { Id = "ag_010_2", Title = "Khám phá ẩm thực", Description = "Thưởng thức cà phê", AudioUrl = "https://res.cloudinary.com/dex6q1cqh/video/upload/f_mp3/v1773934743/douong_caphe_edzvvl.wav", Duration = 4, LocationId = "loc_010", Language = "vi" },
                    new() { Id = "ag_010_3", Title = "Introduction", Description = "About Vietnamese Iced Coffee", AudioUrl = "", Duration = 3, LocationId = "loc_010", Language = "en", GeneratedFromTts = true, TtsSourceText = "Vietnamese iced coffee, or Ca Phe Sua Da, is an essential part of Saigon's daily life. Strong dark roast coffee is brewed through a traditional phin filter, dripping slowly over a layer of sweet condensed milk. Poured over ice, it's the perfect balance of bold, bitter, and sweet — a true taste of Vietnam." }
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
                Name = "Food Tour Vĩnh Khánh",
                Description = "Khám phá thiên đường ẩm thực đường phố với đa dạng các món ăn vặt và hải sản đặc trưng.",
                ImageUrl = "/images/tours/foodtour_vinhkhanh.jpg",
                Duration = 180,
                LocationIds = new List<string> { "loc_003", "loc_004", "loc_007", "loc_008" },
                Price = 0,
                IsFeatured = true
            },
            new()
            {
                Id = "tour_002",
                Name = "Ăn đêm Sài Gòn",
                Description = "Trải nghiệm ẩm thực về đêm tại khu phố nhộn nhịp, ấm dạ với các món ăn nóng hổi.",
                ImageUrl = "/images/tours/an_dem.jpg",
                Duration = 120,
                LocationIds = new List<string> { "loc_005", "loc_006" },
                Price = 0,
                IsFeatured = true
            },
            new()
            {
                Id = "tour_003",
                Name = "Combo đặc sản miền Tây",
                Description = "Thưởng thức món ngon truyền thống mang đậm hương vị miền Tây Nam Bộ giữa lòng Sài Gòn.",
                ImageUrl = "/images/tours/dac_san_mien_tay.jpg",
                Duration = 150,
                LocationIds = new List<string> { "loc_001", "loc_002" },
                Price = 0,
                IsFeatured = false
            },
            new()
            {
                Id = "tour_004",
                Name = "Ăn vặt & chill",
                Description = "Combo nhẹ nhàng cho giới trẻ, nhâm nhi ăn vặt, uống trà sữa tại Vĩnh Khánh.",
                ImageUrl = "/images/tours/an_vat.jpg",
                Duration = 90,
                LocationIds = new List<string> { "loc_003", "loc_004", "loc_009" },
                Price = 0,
                IsFeatured = false
            }
        };
    }

    public static List<AuthUserAccount> GetAuthUserAccounts()
    {
        return new List<AuthUserAccount>
        {
            new()
            {
                Username = "admin.system",
                Password = "Admin@123",
                DisplayName = "Admin Hệ Thống",
                Role = "SystemAdmin",
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow
            },
            new()
            {
                Username = "admin.poi.01",
                Password = "Poi@123",
                DisplayName = "Admin POI - Khu Trung Tâm",
                Role = "PoiAdmin",
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow
            },
            new()
            {
                Username = "admin.poi.02",
                Password = "Poi@123",
                DisplayName = "Admin POI - Khu Mở Rộng",
                Role = "PoiAdmin",
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow
            }
        };
    }

    public static List<PoiAdminLocationAssignment> GetPoiAdminLocationAssignments()
    {
        return new List<PoiAdminLocationAssignment>
        {
            new() { Username = "admin.poi.01", LocationId = "loc_001" },
            new() { Username = "admin.poi.01", LocationId = "loc_002" },
            new() { Username = "admin.poi.01", LocationId = "loc_003" },
            new() { Username = "admin.poi.01", LocationId = "loc_004" },
            new() { Username = "admin.poi.01", LocationId = "loc_005" },
            new() { Username = "admin.poi.02", LocationId = "loc_006" },
            new() { Username = "admin.poi.02", LocationId = "loc_007" },
            new() { Username = "admin.poi.02", LocationId = "loc_008" },
            new() { Username = "admin.poi.02", LocationId = "loc_009" },
            new() { Username = "admin.poi.02", LocationId = "loc_010" }
        };
    }
}