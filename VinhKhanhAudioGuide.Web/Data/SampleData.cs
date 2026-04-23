namespace VinhKhanhAudioGuide.Web.Data;

using VinhKhanhAudioGuide.Web.Models;

public static class SampleData
{
    private const string LocalUserId = "local_user";
    private static readonly DateTime SeedBaseUtc = new(2026, 4, 13, 7, 0, 0, DateTimeKind.Utc);

    public static List<Category> GetCategories()
    {
        return new List<Category>
        {
            new() { Id = "1", Name = "Đặc sản", Icon = "specialty.svg", Description = "Các món đặc sản đặc trưng mang hương vị truyền thống." },
            new() { Id = "2", Name = "Ăn vặt", Icon = "snack.svg", Description = "Bánh tráng, xiên que và các món ăn chơi hấp dẫn." },
            new() { Id = "3", Name = "Ăn đêm", Icon = "night_food.svg", Description = "Cháo sườn, phở và các món phục vụ khuya." },
            new() { Id = "4", Name = "Hải sản", Icon = "seafood.svg", Description = "Ốc, tôm, cua, mực tươi sống nướng, xào." },
            new() { Id = "5", Name = "Đồ uống", Icon = "drink.svg", Description = "Cà phê, trà sữa và đồ uống giải khát." }
        };
    }

    public static List<Location> GetLocations()
    {
        var locations = new List<Location>
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
                Priority = 95,
                DetectionRadiusMeters = 120,
                Duration = 30,
                CategoryId = "1",
                AudioGuides = new List<AudioGuide>
                {
                    new() { Id = "ag_001_1", Title = "Giới thiệu quán", Description = "Thông tin và câu chuyện bún mắm", AudioUrl = "https://res.cloudinary.com/dex6q1cqh/video/upload/f_mp3/v1773934751/gioithieu_bunmam_l8un47.wav", Duration = 3, LocationId = "loc_001", Language = "vi" },
                    new() { Id = "ag_001_2", Title = "Khám phá ẩm thực", Description = "Hương vị bún mắm", AudioUrl = "https://res.cloudinary.com/dex6q1cqh/video/upload/f_mp3/v1773934753/amthuc_bunmam_wen2s0.wav", Duration = 4, LocationId = "loc_001", Language = "vi" },
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
                Priority = 90,
                DetectionRadiusMeters = 100,
                Duration = 25,
                CategoryId = "1",
                AudioGuides = new List<AudioGuide>
                {
                    new() { Id = "ag_002_1", Title = "Giới thiệu quán", Description = "Thông tin bánh xèo", AudioUrl = "https://res.cloudinary.com/dex6q1cqh/video/upload/f_mp3/v1773934750/gioithieu_banhxeo_l94n9t.wav", Duration = 3, LocationId = "loc_002", Language = "vi" },
                    new() { Id = "ag_002_2", Title = "Khám phá ẩm thực", Description = "Hương vị bánh xèo", AudioUrl = "https://res.cloudinary.com/dex6q1cqh/video/upload/f_mp3/v1773934750/amthuc_banhxeo_vld0dx.wav", Duration = 4, LocationId = "loc_002", Language = "vi" },
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
                Priority = 86,
                DetectionRadiusMeters = 85,
                Duration = 15,
                CategoryId = "2",
                AudioGuides = new List<AudioGuide>
                {
                    new() { Id = "ag_003_1", Title = "Giới thiệu quán", Description = "Thông tin bánh tráng", AudioUrl = "https://res.cloudinary.com/dex6q1cqh/video/upload/f_mp3/v1773934750/gioithieu_banhtrang_ujedfy.wav", Duration = 3, LocationId = "loc_003", Language = "vi" },
                    new() { Id = "ag_003_2", Title = "Khám phá ẩm thực", Description = "Thưởng thức bánh tráng", AudioUrl = "https://res.cloudinary.com/dex6q1cqh/video/upload/f_mp3/v1773934748/amthuc_banhtrang_qglacj.wav", Duration = 4, LocationId = "loc_003", Language = "vi" },
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
                Priority = 80,
                DetectionRadiusMeters = 90,
                Duration = 20,
                CategoryId = "2",
                AudioGuides = new List<AudioGuide>
                {
                    new() { Id = "ag_004_1", Title = "Giới thiệu quán", Description = "Câu chuyện xiên que", AudioUrl = "https://res.cloudinary.com/dex6q1cqh/video/upload/f_mp3/v1773934748/gioithieu_xienque_miulom.wav", Duration = 3, LocationId = "loc_004", Language = "vi" },
                    new() { Id = "ag_004_2", Title = "Khám phá ẩm thực", Description = "Hương vị xiên nướng", AudioUrl = "https://res.cloudinary.com/dex6q1cqh/video/upload/f_mp3/v1773934746/amthuc_xienque_z8vgny.wav", Duration = 4, LocationId = "loc_004", Language = "vi" },
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
                Priority = 84,
                DetectionRadiusMeters = 95,
                Duration = 20,
                CategoryId = "3",
                AudioGuides = new List<AudioGuide>
                {
                    new() { Id = "ag_005_1", Title = "Giới thiệu quán", Description = "Thông tin cháo sườn", AudioUrl = "https://res.cloudinary.com/dex6q1cqh/video/upload/f_mp3/v1773934744/gioithieu_chaosuon_hxsvpg.wav", Duration = 3, LocationId = "loc_005", Language = "vi" },
                    new() { Id = "ag_005_2", Title = "Khám phá ẩm thực", Description = "Hương vị cháo sườn", AudioUrl = "https://res.cloudinary.com/dex6q1cqh/video/upload/f_mp3/v1773934747/amthuc_chaosuon_ijo2ja.wav", Duration = 4, LocationId = "loc_005", Language = "vi" },
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
                Priority = 88,
                DetectionRadiusMeters = 105,
                Duration = 30,
                CategoryId = "3",
                AudioGuides = new List<AudioGuide>
                {
                    new() { Id = "ag_006_1", Title = "Giới thiệu quán", Description = "Câu chuyện quán phở", AudioUrl = "https://res.cloudinary.com/dex6q1cqh/video/upload/f_mp3/v1773934748/gioithieu_quanpho_qvkgit.wav", Duration = 3, LocationId = "loc_006", Language = "vi" },
                    new() { Id = "ag_006_2", Title = "Khám phá ẩm thực", Description = "Hương vị phở đêm", AudioUrl = "https://res.cloudinary.com/dex6q1cqh/video/upload/f_mp3/v1773934745/amthuc_pho_f8ou5l.wav", Duration = 4, LocationId = "loc_006", Language = "vi" },
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
                Priority = 92,
                DetectionRadiusMeters = 110,
                Duration = 35,
                CategoryId = "4",
                AudioGuides = new List<AudioGuide>
                {
                    new() { Id = "ag_007_1", Title = "Giới thiệu quán", Description = "Khám phá quán ốc", AudioUrl = "https://res.cloudinary.com/dex6q1cqh/video/upload/f_mp3/v1773934745/amthuc_oc_vqm14u.wav", Duration = 3, LocationId = "loc_007", Language = "vi" },
                    new() { Id = "ag_007_2", Title = "Khám phá ẩm thực", Description = "Hương vị ốc xào", AudioUrl = "https://res.cloudinary.com/dex6q1cqh/video/upload/f_mp3/v1773934745/amthuc_oc_vqm14u.wav", Duration = 4, LocationId = "loc_007", Language = "vi" },
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
                Priority = 91,
                DetectionRadiusMeters = 115,
                Duration = 40,
                CategoryId = "4",
                AudioGuides = new List<AudioGuide>
                {
                    new() { Id = "ag_008_1", Title = "Giới thiệu quán", Description = "Hải sản tôm nướng", AudioUrl = "https://res.cloudinary.com/dex6q1cqh/video/upload/f_mp3/v1773934744/gioithieu_tom_bvmu5a.wav", Duration = 3, LocationId = "loc_008", Language = "vi" },
                    new() { Id = "ag_008_2", Title = "Khám phá ẩm thực", Description = "Món tôm cay nồng", AudioUrl = "https://res.cloudinary.com/dex6q1cqh/video/upload/f_mp3/v1773934744/amthuc_tom_vwvgix.wav", Duration = 4, LocationId = "loc_008", Language = "vi" },
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
                Priority = 75,
                DetectionRadiusMeters = 80,
                Duration = 15,
                CategoryId = "5",
                AudioGuides = new List<AudioGuide>
                {
                    new() { Id = "ag_009_1", Title = "Giới thiệu quán", Description = "Tiệm trà sữa", AudioUrl = "https://res.cloudinary.com/dex6q1cqh/video/upload/f_mp3/v1773934747/gioithieu_trasua_aj0syw.wav", Duration = 3, LocationId = "loc_009", Language = "vi" },
                    new() { Id = "ag_009_2", Title = "Khám phá ẩm thực", Description = "Đồ uống trà sữa", AudioUrl = "https://res.cloudinary.com/dex6q1cqh/video/upload/f_mp3/v1773934745/douong_trasua_jasd5g.wav", Duration = 4, LocationId = "loc_009", Language = "vi" },
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
                Priority = 78,
                DetectionRadiusMeters = 85,
                Duration = 20,
                CategoryId = "5",
                AudioGuides = new List<AudioGuide>
                {
                    new() { Id = "ag_010_1", Title = "Giới thiệu quán", Description = "Góc cà phê", AudioUrl = "https://res.cloudinary.com/dex6q1cqh/video/upload/f_mp3/v1773934743/gioithieu_caphep_cyhjy6.wav", Duration = 3, LocationId = "loc_010", Language = "vi" },
                    new() { Id = "ag_010_2", Title = "Khám phá ẩm thực", Description = "Thưởng thức cà phê", AudioUrl = "https://res.cloudinary.com/dex6q1cqh/video/upload/f_mp3/v1773934743/douong_caphe_edzvvl.wav", Duration = 4, LocationId = "loc_010", Language = "vi" },
                }
            }
        };

        AddMultilingualAudioGuides(locations);
        NormalizeAudioGuideSeedData(locations);
        AlignLocationsWithMobileSample(locations);
        return locations;
    }

    public static List<Tour> GetTours()
    {
        var tours = new List<Tour>
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

        AlignToursWithMobileSample(tours);
        return tours;
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
                CreatedAtUtc = SeedBaseUtc
            },
            new()
            {
                Username = "admin.poi.01",
                Password = "Poi@123",
                DisplayName = "Admin POI - Khu Trung Tâm",
                Role = "PoiAdmin",
                IsActive = true,
                CreatedAtUtc = SeedBaseUtc.AddMinutes(1)
            },
            new()
            {
                Username = "admin.poi.02",
                Password = "Poi@123",
                DisplayName = "Admin POI - Khu Mở Rộng",
                Role = "PoiAdmin",
                IsActive = true,
                CreatedAtUtc = SeedBaseUtc.AddMinutes(2)
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

    public static List<PoiChangeRequest> GetSamplePoiChangeRequests()
    {
        return new List<PoiChangeRequest>
        {
            new()
            {
                Id = Guid.Parse("a4ac4b80-06fe-4ba4-8ee5-f9ce15d7ae7f"),
                SubmittedByUsername = "admin.poi.01",
                SubmittedByName = "Admin POI - Khu Trung Tâm",
                LocationId = "loc_003",
                LocationName = "Bánh tráng trộn",
                Topic = "Nội dung audio",
                Title = "Cập nhật transcript tiếng Việt",
                Details = "Đề xuất làm rõ hơn phần giới thiệu món và lịch sử quán.",
                TargetType = PoiChangeTargetType.AudioGuide,
                TargetEntityId = "ag_003_1",
                ChangeSetJson = "{\"Fields\":{\"Title\":\"Giới thiệu quán\",\"TranscriptText\":\"Nội dung mới\"}}",
                Status = PoiChangeRequestStatus.Pending,
                SubmittedAtUtc = SeedBaseUtc.AddDays(-2)
            },
            new()
            {
                Id = Guid.Parse("fdfb3d17-9d5f-47f7-a9cf-bccd1e3f4fca"),
                SubmittedByUsername = "admin.poi.02",
                SubmittedByName = "Admin POI - Khu Mở Rộng",
                LocationId = "loc_008",
                LocationName = "Tôm nướng muối ớt",
                Topic = "Thông tin địa điểm",
                Title = "Cập nhật mô tả địa điểm",
                Details = "Bổ sung thông tin giờ mở cửa và lưu ý món đặc trưng.",
                TargetType = PoiChangeTargetType.Location,
                TargetEntityId = "loc_008",
                ChangeSetJson = "{\"Fields\":{\"Description\":\"Mô tả đã cập nhật\"}}",
                Status = PoiChangeRequestStatus.Approved,
                SubmittedAtUtc = SeedBaseUtc.AddDays(-5),
                UpdatedAtUtc = SeedBaseUtc.AddDays(-4),
                UpdatedBy = "admin.system",
                ReviewNote = "Nội dung hợp lệ, đã duyệt."
            }
        };
    }

    public static List<ListeningHistorySeed> GetListeningHistorySeeds()
    {
        return new List<ListeningHistorySeed>
        {
            new()
            {
                Id = "h1",
                UserId = LocalUserId,
                AudioGuideId = "ag_001_1",
                LocationId = "loc_001",
                AudioTitle = "Giới thiệu quán",
                LocationName = "Bún mắm Vĩnh Khánh",
                LocationImageUrl = "bun_mam.jpg",
                AudioDuration = 3,
                Progress = 0.8M,
                ListenedSeconds = 144,
                IsCompleted = false,
                LastListenedAtUtc = SeedBaseUtc.AddHours(-2)
            },
            new()
            {
                Id = "h2",
                UserId = LocalUserId,
                AudioGuideId = "ag_002_1",
                LocationId = "loc_002",
                AudioTitle = "Giới thiệu quán",
                LocationName = "Bánh xèo miền Tây",
                LocationImageUrl = "banh_xeo.jpg",
                AudioDuration = 3,
                Progress = 1.0M,
                ListenedSeconds = 180,
                IsCompleted = true,
                LastListenedAtUtc = SeedBaseUtc.AddHours(-5)
            },
            new()
            {
                Id = "h3",
                UserId = LocalUserId,
                AudioGuideId = "ag_007_1",
                LocationId = "loc_007",
                AudioTitle = "Giới thiệu quán",
                LocationName = "Ốc xào bơ tỏi",
                LocationImageUrl = "oc_xao_bo_toi.jpg",
                AudioDuration = 3,
                Progress = 0.45M,
                ListenedSeconds = 81,
                IsCompleted = false,
                LastListenedAtUtc = SeedBaseUtc.AddDays(-1)
            },
            new()
            {
                Id = "h4",
                UserId = LocalUserId,
                AudioGuideId = "ag_006_1",
                LocationId = "loc_006",
                AudioTitle = "Giới thiệu quán",
                LocationName = "Phở khuya",
                LocationImageUrl = "pho.png",
                AudioDuration = 3,
                Progress = 1.0M,
                ListenedSeconds = 180,
                IsCompleted = true,
                LastListenedAtUtc = SeedBaseUtc.AddDays(-2)
            }
        };
    }

    private static void NormalizeAudioGuideSeedData(IEnumerable<Location> locations)
    {
        foreach (var location in locations)
        {
            foreach (var guide in location.AudioGuides)
            {
                guide.LocationId = location.Id;

                if (string.IsNullOrWhiteSpace(guide.CloudinaryAudioUrl))
                {
                    guide.CloudinaryAudioUrl = string.IsNullOrWhiteSpace(guide.AudioUrl)
                        ? null
                        : guide.AudioUrl;
                }

                if (string.IsNullOrWhiteSpace(guide.CloudinaryPublicId))
                {
                    guide.CloudinaryPublicId = ToCloudinaryPublicId(guide.AudioUrl);
                }

                if (guide.ScriptSegments.Count == 0)
                {
                    guide.ScriptSegments = BuildDefaultSegments(guide);
                }

                if (string.IsNullOrWhiteSpace(guide.TranscriptText))
                {
                    guide.TranscriptText = string.Join(" ", guide.ScriptSegments
                        .OrderBy(segment => segment.StartTimeSeconds)
                        .Select(segment => segment.ScriptText.Trim())
                        .Where(segment => !string.IsNullOrWhiteSpace(segment)));
                }
            }
        }
    }

    private static void AlignLocationsWithMobileSample(IEnumerable<Location> locations)
    {
        var imageByLocationId = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["loc_001"] = "bun_mam.jpg",
            ["loc_002"] = "banh_xeo.jpg",
            ["loc_003"] = "banh_trang_tron.jpg",
            ["loc_004"] = "tom_nuong_muoi_ot.jpg",
            ["loc_005"] = "chao_suon_dem.jpg",
            ["loc_006"] = "pho.png",
            ["loc_007"] = "oc_xao_bo_toi.jpg",
            ["loc_008"] = "tom_nuong_muoi_ot.jpg",
            ["loc_009"] = "ca_phe_sua_da.jpg",
            ["loc_010"] = "ca_phe_sua_da.jpg"
        };

        foreach (var location in locations)
        {
            if (imageByLocationId.TryGetValue(location.Id, out var imageName))
            {
                location.ImageUrl = imageName;
            }

            location.AudioGuides = location.AudioGuides
                .OrderBy(guide => guide.Id)
                .ToList();
        }
    }

    private static void AlignToursWithMobileSample(IEnumerable<Tour> tours)
    {
        var imageByTourId = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["tour_001"] = "foodtour_vinhkhanh.jpg",
            ["tour_002"] = "an_dem.jpg",
            ["tour_003"] = "dac_san_mien_tay.jpg",
            ["tour_004"] = "an_vat.jpg"
        };

        foreach (var tour in tours)
        {
            if (imageByTourId.TryGetValue(tour.Id, out var imageName))
            {
                tour.ImageUrl = imageName;
            }
        }
    }

    private static List<AudioScriptSegment> BuildDefaultSegments(AudioGuide guide)
    {
        var totalSeconds = Math.Max(60, guide.Duration * 60);
        var segmentDuration = Math.Max(20, totalSeconds / 3);

        return new List<AudioScriptSegment>
        {
            new()
            {
                AudioGuideId = guide.Id,
                StartTimeSeconds = 0,
                EndTimeSeconds = segmentDuration,
                ScriptText = $"Mở đầu: {guide.Title}. {guide.Description}."
            },
            new()
            {
                AudioGuideId = guide.Id,
                StartTimeSeconds = segmentDuration,
                EndTimeSeconds = segmentDuration * 2,
                ScriptText = $"Nội dung chính: khám phá điểm nhấn tại {guide.Title.ToLowerInvariant()}."
            },
            new()
            {
                AudioGuideId = guide.Id,
                StartTimeSeconds = segmentDuration * 2,
                EndTimeSeconds = totalSeconds,
                ScriptText = "Kết thúc: cảm ơn bạn đã lắng nghe, hãy tiếp tục khám phá POI kế tiếp."
            }
        };
    }

    private static string? ToCloudinaryPublicId(string? audioUrl)
    {
        if (string.IsNullOrWhiteSpace(audioUrl))
        {
            return null;
        }

        var fileName = audioUrl.Split('/').LastOrDefault() ?? string.Empty;
        var dotIndex = fileName.LastIndexOf('.');
        var baseName = dotIndex > 0 ? fileName[..dotIndex] : fileName;
        return string.IsNullOrWhiteSpace(baseName) ? null : $"audio/{baseName}";
    }

    private static void AddMultilingualAudioGuides(IEnumerable<Location> locations)
    {
        var urlByGuideAndLanguage = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["ag_001_1_en"] = "https://res.cloudinary.com/dex6q1cqh/video/upload/f_mp3/v1776939858/audio/seed_ag_001_1_en_293310f4a816486bb50b0f411d404d0e.mp3",
            ["ag_001_1_zh"] = "https://res.cloudinary.com/dex6q1cqh/video/upload/f_mp3/v1776939860/audio/seed_ag_001_1_zh_de51bed61a884344a278563601b71fcb.mp3",
            ["ag_001_2_en"] = "https://res.cloudinary.com/dex6q1cqh/video/upload/f_mp3/v1776939863/audio/seed_ag_001_2_en_4de6f22d2df7472aa9b9a06dc6fb429d.mp3",
            ["ag_001_2_zh"] = "https://res.cloudinary.com/dex6q1cqh/video/upload/f_mp3/v1776939865/audio/seed_ag_001_2_zh_3bd5f726732a44ac988b39ff1a398e43.mp3",
            ["ag_002_1_en"] = "https://res.cloudinary.com/dex6q1cqh/video/upload/f_mp3/v1776939868/audio/seed_ag_002_1_en_f7993908b82f4103906dd54f5b58ca4d.mp3",
            ["ag_002_1_zh"] = "https://res.cloudinary.com/dex6q1cqh/video/upload/f_mp3/v1776939871/audio/seed_ag_002_1_zh_d4d103171ed547b894fc21fc65e76026.mp3",
            ["ag_002_2_en"] = "https://res.cloudinary.com/dex6q1cqh/video/upload/f_mp3/v1776939873/audio/seed_ag_002_2_en_288706f117ab4d8a931bc2dd19871200.mp3",
            ["ag_002_2_zh"] = "https://res.cloudinary.com/dex6q1cqh/video/upload/f_mp3/v1776939876/audio/seed_ag_002_2_zh_674e6cad8aa1445bb93ebef857e23708.mp3",
            ["ag_003_1_en"] = "https://res.cloudinary.com/dex6q1cqh/video/upload/f_mp3/v1776939879/audio/seed_ag_003_1_en_6a411c26f5ec453f8c326c3382347350.mp3",
            ["ag_003_1_zh"] = "https://res.cloudinary.com/dex6q1cqh/video/upload/f_mp3/v1776939881/audio/seed_ag_003_1_zh_650e159a7de14b38b13ce9f9978247a4.mp3",
            ["ag_003_2_en"] = "https://res.cloudinary.com/dex6q1cqh/video/upload/f_mp3/v1776939883/audio/seed_ag_003_2_en_0e0b988be835495dbd94d8dca94e6418.mp3",
            ["ag_003_2_zh"] = "https://res.cloudinary.com/dex6q1cqh/video/upload/f_mp3/v1776939885/audio/seed_ag_003_2_zh_8fa4cacf95a64b20939b67a58158016f.mp3",
            ["ag_004_1_en"] = "https://res.cloudinary.com/dex6q1cqh/video/upload/f_mp3/v1776939887/audio/seed_ag_004_1_en_2e55168b80374ba49e6c770047f64f99.mp3",
            ["ag_004_1_zh"] = "https://res.cloudinary.com/dex6q1cqh/video/upload/f_mp3/v1776939889/audio/seed_ag_004_1_zh_93b21a86c01e46989ce2ea22d6ce2cfe.mp3",
            ["ag_004_2_en"] = "https://res.cloudinary.com/dex6q1cqh/video/upload/f_mp3/v1776939891/audio/seed_ag_004_2_en_a812530bfdd04e09832b05d24013cdd1.mp3",
            ["ag_004_2_zh"] = "https://res.cloudinary.com/dex6q1cqh/video/upload/f_mp3/v1776939893/audio/seed_ag_004_2_zh_3a2666309121466ba284c9f04d65fb4a.mp3",
            ["ag_005_1_en"] = "https://res.cloudinary.com/dex6q1cqh/video/upload/f_mp3/v1776939896/audio/seed_ag_005_1_en_634b6c9f2ce342e69ade959b1a6ff797.mp3",
            ["ag_005_1_zh"] = "https://res.cloudinary.com/dex6q1cqh/video/upload/f_mp3/v1776939898/audio/seed_ag_005_1_zh_0412003617a846efbec0c882b1025c08.mp3",
            ["ag_005_2_en"] = "https://res.cloudinary.com/dex6q1cqh/video/upload/f_mp3/v1776939901/audio/seed_ag_005_2_en_4b34771321c54c3ab7a3ffa302e422ae.mp3",
            ["ag_005_2_zh"] = "https://res.cloudinary.com/dex6q1cqh/video/upload/f_mp3/v1776939904/audio/seed_ag_005_2_zh_63f6b5ffedd14a87bd712aab51dfd666.mp3",
            ["ag_006_1_en"] = "https://res.cloudinary.com/dex6q1cqh/video/upload/f_mp3/v1776939906/audio/seed_ag_006_1_en_e05930d5ddf1484ab92506d8414630d7.mp3",
            ["ag_006_1_zh"] = "https://res.cloudinary.com/dex6q1cqh/video/upload/f_mp3/v1776939909/audio/seed_ag_006_1_zh_c40b3252e54747d88ff27abceff34012.mp3",
            ["ag_006_2_en"] = "https://res.cloudinary.com/dex6q1cqh/video/upload/f_mp3/v1776939911/audio/seed_ag_006_2_en_b23ad2821a77459694a4f4988ea3f062.mp3",
            ["ag_006_2_zh"] = "https://res.cloudinary.com/dex6q1cqh/video/upload/f_mp3/v1776939914/audio/seed_ag_006_2_zh_6f69344ddc9948469bfc51f22e3ec2f3.mp3",
            ["ag_007_1_en"] = "https://res.cloudinary.com/dex6q1cqh/video/upload/f_mp3/v1776939916/audio/seed_ag_007_1_en_7871d27def8f41a29f7995164137a321.mp3",
            ["ag_007_1_zh"] = "https://res.cloudinary.com/dex6q1cqh/video/upload/f_mp3/v1776939918/audio/seed_ag_007_1_zh_c350154adfb849a9ac7785ba0d764a40.mp3",
            ["ag_007_2_en"] = "https://res.cloudinary.com/dex6q1cqh/video/upload/f_mp3/v1776939920/audio/seed_ag_007_2_en_e5280cacde824c209d41603340df5c5e.mp3",
            ["ag_007_2_zh"] = "https://res.cloudinary.com/dex6q1cqh/video/upload/f_mp3/v1776939922/audio/seed_ag_007_2_zh_392ac72e10d94a48b20a2091c22205bb.mp3",
            ["ag_008_1_en"] = "https://res.cloudinary.com/dex6q1cqh/video/upload/f_mp3/v1776939924/audio/seed_ag_008_1_en_99b62b8bde984e1c99132812d22a9ddb.mp3",
            ["ag_008_1_zh"] = "https://res.cloudinary.com/dex6q1cqh/video/upload/f_mp3/v1776939926/audio/seed_ag_008_1_zh_1d018daeec654ad19ce768b25a402690.mp3",
            ["ag_008_2_en"] = "https://res.cloudinary.com/dex6q1cqh/video/upload/f_mp3/v1776939928/audio/seed_ag_008_2_en_2ac9e11c65f046dbab19df2ef3ced616.mp3",
            ["ag_008_2_zh"] = "https://res.cloudinary.com/dex6q1cqh/video/upload/f_mp3/v1776939930/audio/seed_ag_008_2_zh_f910f7288ae344b2be54d25b59eaf7a1.mp3",
            ["ag_009_1_en"] = "https://res.cloudinary.com/dex6q1cqh/video/upload/f_mp3/v1776939932/audio/seed_ag_009_1_en_8959accf9ac84caea8be55d477a8e66f.mp3",
            ["ag_009_1_zh"] = "https://res.cloudinary.com/dex6q1cqh/video/upload/f_mp3/v1776939934/audio/seed_ag_009_1_zh_3c5d28c81bf5411a845c92cc5e4b9819.mp3",
            ["ag_009_2_en"] = "https://res.cloudinary.com/dex6q1cqh/video/upload/f_mp3/v1776939937/audio/seed_ag_009_2_en_cc54a7acd9f84f50998faf98929c4fa6.mp3",
            ["ag_009_2_zh"] = "https://res.cloudinary.com/dex6q1cqh/video/upload/f_mp3/v1776939939/audio/seed_ag_009_2_zh_b8ba5c4cfda9466b88ca2e7ccb3252d4.mp3",
            ["ag_010_1_en"] = "https://res.cloudinary.com/dex6q1cqh/video/upload/f_mp3/v1776939941/audio/seed_ag_010_1_en_10f9d40ceb13472d9843c058646fc4b3.mp3",
            ["ag_010_1_zh"] = "https://res.cloudinary.com/dex6q1cqh/video/upload/f_mp3/v1776939944/audio/seed_ag_010_1_zh_881b401a72b1438a9abeca0c1caff93c.mp3",
            ["ag_010_2_en"] = "https://res.cloudinary.com/dex6q1cqh/video/upload/f_mp3/v1776939946/audio/seed_ag_010_2_en_e3bce913740d4a8b86c3a588ea7b37ca.mp3",
            ["ag_010_2_zh"] = "https://res.cloudinary.com/dex6q1cqh/video/upload/f_mp3/v1776939948/audio/seed_ag_010_2_zh_55a9aabfb0af434fb116d739b8693de5.mp3"
        };

        foreach (var location in locations)
        {
            var vietnameseGuides = location.AudioGuides
                .Where(guide => string.Equals(guide.Language, "vi", StringComparison.OrdinalIgnoreCase))
                .OrderBy(guide => guide.Id)
                .ToList();

            foreach (var baseGuide in vietnameseGuides)
            {
                foreach (var language in new[] { "en", "zh" })
                {
                    var localizedId = $"{baseGuide.Id}_{language}";
                    if (location.AudioGuides.Any(guide => string.Equals(guide.Id, localizedId, StringComparison.OrdinalIgnoreCase)))
                    {
                        continue;
                    }

                    if (!urlByGuideAndLanguage.TryGetValue(localizedId, out var audioUrl))
                    {
                        continue;
                    }

                    var isIntro = baseGuide.Title.Contains("Giới thiệu", StringComparison.OrdinalIgnoreCase);
                    var transcript = language == "en"
                        ? BuildEnglishTranscript(location.Name, baseGuide.Title, baseGuide.Description, isIntro)
                        : BuildChineseTranscript(location.Name, baseGuide.Title, baseGuide.Description, isIntro);

                    location.AudioGuides.Add(new AudioGuide
                    {
                        Id = localizedId,
                        Title = language == "en"
                            ? (isIntro ? "Restaurant Introduction" : "Food Exploration")
                            : (isIntro ? "店铺介绍" : "美食探索"),
                        Description = language == "en"
                            ? (isIntro ? "Overview and story of this POI" : "Flavor highlights and tasting guidance")
                            : (isIntro ? "该兴趣点的店铺与背景介绍" : "风味亮点与品尝建议"),
                        AudioUrl = audioUrl,
                        CloudinaryAudioUrl = audioUrl,
                        CloudinaryPublicId = ToCloudinaryPublicId(audioUrl),
                        Duration = Math.Max(baseGuide.Duration, 2),
                        LocationId = location.Id,
                        Language = language,
                        GeneratedFromTts = true,
                        TtsSourceText = transcript,
                        TranscriptText = transcript,
                        ScriptSegments = new List<AudioScriptSegment>()
                    });
                }
            }
        }
    }

    private static string BuildEnglishTranscript(string locationName, string title, string description, bool isIntro)
    {
        var topic = isIntro ? "restaurant introduction" : "food exploration";
        return $"Welcome to {locationName}. This section is about {topic}. {description} At this stop, we focus on ingredients, cooking rhythm, and serving style that shape the final flavor. Start by noticing the aroma, then texture, then aftertaste. If this is your first visit, choose a medium portion and pair it with herbs or a light drink to balance richness. Take your time between bites and compare how each topping changes sweetness, saltiness, and depth. Local street food is best enjoyed slowly, with attention to small details from broth to garnish. Thank you for listening, and continue to the next point of interest for another culinary story.";
    }

    private static string BuildChineseTranscript(string locationName, string title, string description, bool isIntro)
    {
        var topic = isIntro ? "店铺介绍" : "美食探索";
        return $"欢迎来到{locationName}。本段主题是{topic}。{description} 在这个站点，我们会从食材选择、烹饪火候与上桌方式三个角度，理解这道街头美食的魅力。建议你先闻香气，再感受口感，最后体会回味层次。若是第一次来，可以先点中等分量，并搭配清爽饮品或新鲜香草，让味道更平衡。慢慢品尝时，留意每种配料如何改变甜、咸、鲜与香的比例。真正的在地美食体验，不只在一口满足，也在细节观察。感谢收听，下一站我们继续探索更多本地风味故事。";
    }

    public sealed class ListeningHistorySeed
    {
        public string Id { get; set; } = string.Empty;
        public string AudioGuideId { get; set; } = string.Empty;
        public string LocationId { get; set; } = string.Empty;
        public string AudioTitle { get; set; } = string.Empty;
        public string LocationName { get; set; } = string.Empty;
        public string LocationImageUrl { get; set; } = string.Empty;
        public int AudioDuration { get; set; }
        public decimal Progress { get; set; }
        public int ListenedSeconds { get; set; }
        public bool IsCompleted { get; set; }
        public DateTime LastListenedAtUtc { get; set; }
        public string UserId { get; set; } = string.Empty;
    }

    // ========== Payment Package Sample Data ==========

    public static List<PaymentPackage> GetPaymentPackages()
    {
        return new List<PaymentPackage>
        {
            // --- User Packages ---
            new()
            {
                Id = "daily",
                Name = "10.000đ/ngày",
                Description = "Một ngày sử dụng. Phù hợp khi bạn muốn trải nghiệm nhanh trong một ngày, tối ưu cho khách ghé ngắn.",
                Price = 10000m,
                Currency = "VND",
                DurationDays = 1,
                TargetType = "User",
                IsActive = true,
                DefaultPoiPriority = 0,
                CreatedAtUtc = SeedBaseUtc
            },
            new()
            {
                Id = "full-tour",
                Name = "29.000đ/full tour",
                Description = "Một lần thanh toán. Mở khóa toàn bộ tour, phù hợp khi bạn muốn nghe trọn vẹn nội dung đã quét.",
                Price = 29000m,
                Currency = "VND",
                DurationDays = 90,
                TargetType = "User",
                IsActive = true,
                DefaultPoiPriority = 0,
                CreatedAtUtc = SeedBaseUtc.AddMinutes(5)
            },

            // --- Admin POI Packages ---
            new()
            {
                Id = "starter",
                Name = "Gói Khởi Động",
                Description = "Phù hợp cho quán/cửa hàng mới muốn trải nghiệm hệ thống. Quản lý tối đa 2 điểm POI, upload nội dung âm thanh cơ bản và xem thống kê lượt nghe hàng tuần.",
                Price = 99_000m,
                Currency = "VND",
                DurationDays = 30,
                TargetType = "Admin",
                IsActive = true,
                DefaultPoiPriority = 100,
                CreatedAtUtc = SeedBaseUtc.AddMinutes(10)
            },
            new()
            {
                Id = "standard",
                Name = "Gói Tiêu Chuẩn",
                Description = "Dành cho đơn vị kinh doanh ổn định. Quản lý tối đa 5 điểm POI, hỗ trợ đa ngôn ngữ (Việt-Anh-Trung), dashboard thống kê thời gian thực và ưu tiên hỗ trợ kỹ thuật.",
                Price = 249_000m,
                Currency = "VND",
                DurationDays = 30,
                TargetType = "Admin",
                IsActive = true,
                DefaultPoiPriority = 200,
                CreatedAtUtc = SeedBaseUtc.AddMinutes(15)
            },
            new()
            {
                Id = "pro",
                Name = "Gói Chuyên Nghiệp",
                Description = "Giải pháp toàn diện cho chuỗi nhà hàng / khu ẩm thực. Không giới hạn POI, TTS tự động (text-to-speech), báo cáo doanh thu nâng cao và API tích hợp hệ thống POS.",
                Price = 599_000m,
                Currency = "VND",
                DurationDays = 90,
                TargetType = "Admin",
                IsActive = true,
                DefaultPoiPriority = 500,
                CreatedAtUtc = SeedBaseUtc.AddMinutes(20)
            },
            new()
            {
                Id = "enterprise",
                Name = "Gói Doanh Nghiệp",
                Description = "Dành cho ban quản lý khu phố, trung tâm thương mại hoặc địa điểm du lịch quy mô lớn. Bao gồm toàn bộ tính năng Pro, SLA 99.9%, onboarding tận nơi và tùy chỉnh branding riêng.",
                Price = 1_299_000m,
                Currency = "VND",
                DurationDays = 365,
                TargetType = "Admin",
                IsActive = true,
                DefaultPoiPriority = 1000,
                CreatedAtUtc = SeedBaseUtc.AddMinutes(25)
            }
        };
    }


}
