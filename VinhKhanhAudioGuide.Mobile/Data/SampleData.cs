namespace VinhKhanhAudioGuide.Mobile.Data;

using VinhKhanhAudioGuide.Mobile.Models;

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
        var locations = new List<Location>
        {
            new()
            {
                Id = "loc_001",
                Name = "Bún mắm Vĩnh Khánh",
                Description = "Đậm đà hương vị miền Tây, nước lèo thơm phức với đa dạng topping tôm, heo quay, chả vịt.",
                ImageUrl = "bun_mam.jpg",
                Address = "Vĩnh Khánh, Q4, TP.HCM",
                Latitude = 10.7595,
                Longitude = 106.7038,
                Duration = 30,
                CategoryId = "1"
            },
            new()
            {
                Id = "loc_002",
                Name = "Bánh xèo miền Tây",
                Description = "Giòn rụm, ăn kèm rau sống phong phú. Bánh xèo đúc chảo gang truyền thống.",
                ImageUrl = "banh_xeo.jpg",
                Address = "Vĩnh Khánh, Q4, TP.HCM",
                Latitude = 10.7611,
                Longitude = 106.7051,
                Duration = 25,
                CategoryId = "1"
            },
            new()
            {
                Id = "loc_003",
                Name = "Bánh tráng trộn",
                Description = "Chua cay mặn ngọt hài hòa, topping khô bò, trứng cút, xoài băm cực kỳ hấp dẫn.",
                ImageUrl = "banh_trang_tron.jpg",
                Address = "Vĩnh Khánh, Q4, TP.HCM",
                Latitude = 10.7608,
                Longitude = 106.7046,
                Duration = 15,
                CategoryId = "2"
            },
            new()
            {
                Id = "loc_004",
                Name = "Xiên que nướng",
                Description = "Đa dạng topping hấp dẫn từ thịt xiên, xúc xích, đậu bắp nướng than hoa nóng hổi.",
                ImageUrl = "tom_nuong_muoi_ot.jpg",
                Address = "Vĩnh Khánh, Q4, TP.HCM",
                Latitude = 10.7599,
                Longitude = 106.7029,
                Duration = 20,
                CategoryId = "2"
            },
            new()
            {
                Id = "loc_005",
                Name = "Cháo sườn đêm",
                Description = "Cháo sườn nóng hổi, thịt sụn mềm nhừ, rắc thêm tiêu và quẩy giòn tan.",
                ImageUrl = "chao_suon.jpg",
                Address = "Vĩnh Khánh, Q4, TP.HCM",
                Latitude = 10.7604,
                Longitude = 106.7041,
                Duration = 20,
                CategoryId = "3"
            },
            new()
            {
                Id = "loc_006",
                Name = "Phở khuya",
                Description = "Phở đậm vị, nước dùng thanh ngọt nấu từ xương bò, phục vụ khách đi chơi khuya.",
                ImageUrl = "pho.jpg",
                Address = "Vĩnh Khánh, Q4, TP.HCM",
                Latitude = 10.7601,
                Longitude = 106.7036,
                Duration = 30,
                CategoryId = "3"
            },
            new()
            {
                Id = "loc_007",
                Name = "Ốc xào bơ tỏi",
                Description = "Thơm béo, đậm vị bơ tỏi, chấm bánh mì cực cuốn tại phố ốc Vĩnh Khánh.",
                ImageUrl = "oc_xao_bo_toi.jpg",
                Address = "Vĩnh Khánh, Q4, TP.HCM",
                Latitude = 10.7597,
                Longitude = 106.7032,
                Duration = 35,
                CategoryId = "4"
            },
            new()
            {
                Id = "loc_008",
                Name = "Tôm nướng muối ớt",
                Description = "Tôm sú tươi rói, tẩm ướp muối ớt cay nhẹ, nướng xém vỏ thơm lừng.",
                ImageUrl = "tom_nuong_muoi_ot.jpg",
                Address = "Vĩnh Khánh, Q4, TP.HCM",
                Latitude = 10.7614,
                Longitude = 106.7056,
                Duration = 40,
                CategoryId = "4"
            },
            new()
            {
                Id = "loc_009",
                Name = "Trà sữa trân châu",
                Description = "Trà sữa vị ngọt béo, đa dạng các loại topping từ trân châu đường đen đến thạch phô mai.",
                ImageUrl = "ca_phe_sua_da.jpg",
                Address = "Vĩnh Khánh, Q4, TP.HCM",
                Latitude = 10.7609,
                Longitude = 106.7049,
                Duration = 15,
                CategoryId = "5"
            },
            new()
            {
                Id = "loc_010",
                Name = "Cà phê sữa đá",
                Description = "Cà phê pha phin truyền thống, sữa đặc thơm béo, món giải khát không thể thiếu ở Sài Gòn.",
                ImageUrl = "ca_phe_sua_da.jpg",
                Address = "Vĩnh Khánh, Q4, TP.HCM",
                Latitude = 10.7593,
                Longitude = 106.7026,
                Duration = 20,
                CategoryId = "5"
            }
        };

        var audioGuides = GetAudioGuides();
        foreach (var location in locations)
        {
            location.AudioGuides = audioGuides.Where(a => a.LocationId == location.Id).ToList();
        }

        var favoriteIds = new[] { "loc_001", "loc_003", "loc_007" };
        foreach (var location in locations.Where(l => favoriteIds.Contains(l.Id)))
        {
            location.IsFavorite = true;
        }

        return locations;
    }

    public static List<AudioGuide> GetAudioGuides()
    {
        var guides = new List<AudioGuide>
        {
            CreateAudioGuide("ag_001_1", "Giới thiệu quán", "Thông tin và câu chuyện bún mắm", "https://res.cloudinary.com/dex6q1cqh/video/upload/f_mp3/v1773934751/gioithieu_bunmam_l8un47.wav", 3, "loc_001", "vi"),
            CreateAudioGuide("ag_001_2", "Khám phá ẩm thực", "Hương vị bún mắm", "https://res.cloudinary.com/dex6q1cqh/video/upload/f_mp3/v1773934753/amthuc_bunmam_wen2s0.wav", 4, "loc_001", "vi"),

            CreateAudioGuide("ag_002_1", "Giới thiệu quán", "Thông tin bánh xèo", "https://res.cloudinary.com/dex6q1cqh/video/upload/f_mp3/v1773934750/gioithieu_banhxeo_l94n9t.wav", 3, "loc_002", "vi"),
            CreateAudioGuide("ag_002_2", "Khám phá ẩm thực", "Hương vị bánh xèo", "https://res.cloudinary.com/dex6q1cqh/video/upload/f_mp3/v1773934750/amthuc_banhxeo_vld0dx.wav", 4, "loc_002", "vi"),

            CreateAudioGuide("ag_003_1", "Giới thiệu quán", "Thông tin bánh tráng", "https://res.cloudinary.com/dex6q1cqh/video/upload/f_mp3/v1773934750/gioithieu_banhxeo_l94n9t.wav", 3, "loc_003", "vi"),
            CreateAudioGuide("ag_003_2", "Khám phá ẩm thực", "Thưởng thức bánh tráng", "https://res.cloudinary.com/dex6q1cqh/video/upload/f_mp3/v1773934748/amthuc_banhtrang_qglacj.wav", 4, "loc_003", "vi"),

            CreateAudioGuide("ag_004_1", "Giới thiệu quán", "Câu chuyện xiên que", "https://res.cloudinary.com/dex6q1cqh/video/upload/f_mp3/v1773934748/gioithieu_xienque_miulom.wav", 3, "loc_004", "vi"),
            CreateAudioGuide("ag_004_2", "Khám phá ẩm thực", "Hương vị xiên nướng", "https://res.cloudinary.com/dex6q1cqh/video/upload/f_mp3/v1773934746/amthuc_xienque_z8vgny.wav", 4, "loc_004", "vi"),

            CreateAudioGuide("ag_005_1", "Giới thiệu quán", "Thông tin cháo sườn", "https://res.cloudinary.com/dex6q1cqh/video/upload/f_mp3/v1773934744/gioithieu_chaosuon_hxsvpg.wav", 3, "loc_005", "vi"),
            CreateAudioGuide("ag_005_2", "Khám phá ẩm thực", "Hương vị cháo sườn", "https://res.cloudinary.com/dex6q1cqh/video/upload/f_mp3/v1773934747/amthuc_chaosuon_ijo2ja.wav", 4, "loc_005", "vi"),

            CreateAudioGuide("ag_006_1", "Giới thiệu quán", "Câu chuyện quán phở", "https://res.cloudinary.com/dex6q1cqh/video/upload/f_mp3/v1773934748/gioithieu_quanpho_qvkgit.wav", 3, "loc_006", "vi"),
            CreateAudioGuide("ag_006_2", "Khám phá ẩm thực", "Hương vị phở đêm", "https://res.cloudinary.com/dex6q1cqh/video/upload/f_mp3/v1773934745/amthuc_pho_f8ou5l.wav", 4, "loc_006", "vi"),

            CreateAudioGuide("ag_007_1", "Giới thiệu quán", "Khám phá quán ốc", "https://res.cloudinary.com/dex6q1cqh/video/upload/f_mp3/v1773934745/amthuc_oc_vqm14u.wav", 3, "loc_007", "vi"),
            CreateAudioGuide("ag_007_2", "Khám phá ẩm thực", "Hương vị ốc xào", "https://res.cloudinary.com/dex6q1cqh/video/upload/f_mp3/v1773934745/amthuc_oc_vqm14u.wav", 4, "loc_007", "vi"),

            CreateAudioGuide("ag_008_1", "Giới thiệu quán", "Hải sản tôm nướng", "https://res.cloudinary.com/dex6q1cqh/video/upload/f_mp3/v1773934744/gioithieu_tom_bvmu5a.wav", 3, "loc_008", "vi"),
            CreateAudioGuide("ag_008_2", "Khám phá ẩm thực", "Món tôm cay nồng", "https://res.cloudinary.com/dex6q1cqh/video/upload/f_mp3/v1773934744/amthuc_tom_vwvgix.wav", 4, "loc_008", "vi"),

            CreateAudioGuide("ag_009_1", "Giới thiệu quán", "Tiệm trà sữa", "https://res.cloudinary.com/dex6q1cqh/video/upload/f_mp3/v1773934747/gioithieu_trasua_aj0syw.wav", 3, "loc_009", "vi"),
            CreateAudioGuide("ag_009_2", "Khám phá ẩm thực", "Đồ uống trà sữa", "https://res.cloudinary.com/dex6q1cqh/video/upload/f_mp3/v1773934745/douong_trasua_jasd5g.wav", 4, "loc_009", "vi"),

            CreateAudioGuide("ag_010_1", "Giới thiệu quán", "Góc cà phê", "https://res.cloudinary.com/dex6q1cqh/video/upload/f_mp3/v1773934743/gioithieu_caphep_cyhjy6.wav", 3, "loc_010", "vi"),
            CreateAudioGuide("ag_010_2", "Khám phá ẩm thực", "Thưởng thức cà phê", "https://res.cloudinary.com/dex6q1cqh/video/upload/f_mp3/v1773934743/douong_caphe_edzvvl.wav", 4, "loc_010", "vi")
        };

        foreach (var guide in guides)
        {
            EnsureAudioScriptData(guide);
        }

        return guides;
    }

    private static AudioGuide CreateAudioGuide(string id, string title, string description, string audioUrl, int duration, string locationId, string language)
    {
        var totalSeconds = Math.Max(60, duration * 60);
        var segmentDuration = Math.Max(20, totalSeconds / 3);

        var scriptSegments = new List<AudioScriptSegment>
        {
            new()
            {
                Id = 1,
                AudioGuideId = id,
                StartTimeSeconds = 0,
                EndTimeSeconds = segmentDuration,
                ScriptText = $"Mở đầu: {title}. {description}."
            },
            new()
            {
                Id = 2,
                AudioGuideId = id,
                StartTimeSeconds = segmentDuration,
                EndTimeSeconds = segmentDuration * 2,
                ScriptText = $"Nội dung chính: khám phá điểm nhấn tại {title.ToLowerInvariant()}."
            },
            new()
            {
                Id = 3,
                AudioGuideId = id,
                StartTimeSeconds = segmentDuration * 2,
                EndTimeSeconds = totalSeconds,
                ScriptText = "Kết thúc: cảm ơn bạn đã lắng nghe, hãy tiếp tục khám phá POI kế tiếp."
            }
        };

        return new AudioGuide
        {
            Id = id,
            Title = title,
            Description = description,
            AudioUrl = audioUrl,
            CloudinaryAudioUrl = audioUrl,
            CloudinaryPublicId = ToCloudinaryAudioPublicId(audioUrl),
            TranscriptText = string.Join(" ", scriptSegments.Select(s => s.ScriptText.Trim())),
            Duration = duration,
            LocationId = locationId,
            Language = language,
            ScriptSegments = scriptSegments
        };
    }

    private static void EnsureAudioScriptData(AudioGuide guide)
    {
        if (guide.ScriptSegments == null)
        {
            guide.ScriptSegments = new List<AudioScriptSegment>();
        }

        if (guide.ScriptSegments.Count == 0)
        {
            var totalSeconds = Math.Max(60, guide.Duration * 60);
            guide.ScriptSegments.Add(new AudioScriptSegment
            {
                Id = 1,
                AudioGuideId = guide.Id,
                StartTimeSeconds = 0,
                EndTimeSeconds = totalSeconds,
                ScriptText = $"{guide.Title}. {guide.Description}."
            });
        }

        if (string.IsNullOrWhiteSpace(guide.TranscriptText))
        {
            guide.TranscriptText = string.Join(" ",
                guide.ScriptSegments
                    .OrderBy(s => s.StartTimeSeconds)
                    .Select(s => s.ScriptText.Trim())
                    .Where(s => !string.IsNullOrWhiteSpace(s)));
        }
    }

    private static string ToCloudinaryAudioPublicId(string audioUrl)
    {
        var fileName = audioUrl.Split('/').LastOrDefault() ?? string.Empty;
        var dotIndex = fileName.LastIndexOf('.');
        var baseName = dotIndex > 0 ? fileName[..dotIndex] : fileName;

        return string.IsNullOrWhiteSpace(baseName) ? string.Empty : $"audio/{baseName}";
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
                Description = "Trải nghiệm ẩm thực về đêm tại khu phố nhộn nhịp, ấm dạ với các món ăn nóng hổi.",
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
                Description = "Thưởng thức món ngon truyền thống mang đậm hương vị miền Tây Nam Bộ giữa lòng Sài Gòn.",
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
                Description = "Combo nhẹ nhàng cho giới trẻ, nhâm nhi ăn vặt, uống trà sữa tại Vĩnh Khánh.",
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
