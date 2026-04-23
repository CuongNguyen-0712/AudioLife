using VinhKhanhAudioGuide.Mobile.Models;
using LocationModel = VinhKhanhAudioGuide.Mobile.Models.Location;
using System.Text.RegularExpressions;

namespace VinhKhanhAudioGuide.Mobile.Services;

internal static class ContentLocalizationMapper
{
    private static readonly Dictionary<string, (string En, string Zh)> CategoryNameMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["1"] = ("Specialties", "特色美食"),
        ["2"] = ("Street Snacks", "街头小吃"),
        ["3"] = ("Late Night", "夜宵"),
        ["4"] = ("Seafood", "海鲜"),
        ["5"] = ("Drinks", "饮品")
    };

    private static readonly Dictionary<string, (string En, string Zh)> CategoryDescriptionMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["1"] = ("Signature dishes with traditional local flavors.", "具有传统风味的本地招牌菜。"),
        ["2"] = ("Rice paper snacks, skewers, and popular street bites.", "拌米纸、烤串和人气街头小食。"),
        ["3"] = ("Warm late-night options served until midnight.", "营业到深夜的暖心夜宵选择。"),
        ["4"] = ("Fresh shellfish and seafood grilled or stir-fried.", "新鲜贝类与海鲜，烧烤或炒制。"),
        ["5"] = ("Coffee, milk tea, and refreshing beverages.", "咖啡、奶茶和清爽饮品。")
    };

    private static readonly Dictionary<string, (string En, string Zh)> LocationNameMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["loc_001"] = ("Vinh Khanh Fermented Fish Noodle Soup", "永庆发酵鱼汤米粉"),
        ["loc_002"] = ("Mekong Crispy Pancake", "湄公河脆饼"),
        ["loc_003"] = ("Mixed Rice Paper", "拌米纸"),
        ["loc_004"] = ("Grilled Skewers", "炭烤串串"),
        ["loc_005"] = ("Late-Night Pork Rib Porridge", "深夜排骨粥"),
        ["loc_006"] = ("Late-Night Pho", "深夜河粉"),
        ["loc_007"] = ("Garlic Butter Stir-Fried Snails", "蒜香黄油炒螺"),
        ["loc_008"] = ("Grilled Chili Salt Shrimp", "椒盐烤虾"),
        ["loc_009"] = ("Bubble Milk Tea", "珍珠奶茶"),
        ["loc_010"] = ("Vietnamese Iced Milk Coffee", "越南冰奶咖")
    };

    private static readonly Dictionary<string, (string En, string Zh)> LocationDescriptionMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["loc_001"] = ("A rich Mekong-style broth with shrimp, roasted pork, and duck sausage toppings.", "浓郁的湄公河风味汤底，配虾、烤猪肉和鸭肉肠。"),
        ["loc_002"] = ("Golden crispy pancake cooked on cast iron, served with fresh herbs.", "铸铁锅现做金黄脆饼，搭配丰富香草。"),
        ["loc_003"] = ("Balanced sweet-sour-spicy flavor with beef jerky, quail eggs, and green mango.", "酸甜辣均衡，配牛肉干、鹌鹑蛋和青芒果。"),
        ["loc_004"] = ("A variety of charcoal-grilled skewers, hot and smoky from the street grill.", "多种炭烤串串，现烤出炉，香气十足。"),
        ["loc_005"] = ("Comforting hot porridge with tender rib meat and crispy crullers.", "热腾腾的排骨粥，肉质软嫩，配酥脆油条。"),
        ["loc_006"] = ("Clear and sweet beef-bone broth pho for night owls.", "清甜牛骨高汤河粉，适合夜间觅食。"),
        ["loc_007"] = ("Savory buttery snails with garlic aroma, perfect with bread.", "蒜香黄油螺肉，咸香浓郁，配面包更佳。"),
        ["loc_008"] = ("Fresh giant shrimp marinated in chili salt and flame-grilled.", "新鲜大虾以椒盐腌制后炙烤，鲜香微辣。"),
        ["loc_009"] = ("Creamy milk tea with assorted toppings like brown sugar pearls and cheese jelly.", "奶香浓郁，配黑糖珍珠和芝士冻等多样配料。"),
        ["loc_010"] = ("Traditional phin coffee mixed with condensed milk over ice.", "传统滴滤咖啡加炼乳与冰块，是西贡经典。")
    };

    private static readonly Dictionary<string, (string En, string Zh)> TourNameMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["tour_001"] = ("Vinh Khanh Food Tour", "永庆美食之旅"),
        ["tour_002"] = ("Saigon Late-Night Eats", "西贡夜宵路线"),
        ["tour_003"] = ("Mekong Specialties Combo", "湄公河特色组合"),
        ["tour_004"] = ("Snacks and Chill", "轻松小吃与茶饮")
    };

    private static readonly Dictionary<string, (string En, string Zh)> TourDescriptionMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["tour_001"] = ("Explore Vinh Khanh street-food paradise with snacks and signature seafood.", "探索永庆街头美食天堂，打卡小吃与招牌海鲜。"),
        ["tour_002"] = ("Experience warm and lively late-night food spots in Saigon.", "体验西贡热闹夜间食街与暖心美食。"),
        ["tour_003"] = ("Taste traditional Mekong flavors in the heart of Saigon.", "在西贡市中心品尝湄公河传统风味。"),
        ["tour_004"] = ("A light route for young foodies: snacks and milk tea stops.", "轻松路线：街头小吃加奶茶休闲打卡。")
    };

    private static readonly Dictionary<string, (string En, string Zh)> AudioTitleMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Giới thiệu quán"] = ("Venue Introduction", "店铺介绍"),
        ["Khám phá ẩm thực"] = ("Food Discovery", "美食探索")
    };

    private static readonly Dictionary<string, string> UrlByGuideAndLanguage = new(StringComparer.OrdinalIgnoreCase)
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

    public static string ToLanguageCode(string? cultureName)
    {
        if (string.IsNullOrWhiteSpace(cultureName))
        {
            return "vi";
        }

        return cultureName.Trim().ToLowerInvariant() switch
        {
            "vi" or "vi-vn" => "vi",
            "en" or "en-us" => "en",
            "zh" or "zh-cn" or "zh-hans" => "zh",
            _ => "vi"
        };
    }

    public static List<Category> LocalizeCategories(IEnumerable<Category> source, string languageCode)
    {
        return source.Select(item => new Category
        {
            Id = item.Id,
            Icon = item.Icon,
            LocationCount = item.LocationCount,
            IsSelected = item.IsSelected,
            Name = TranslateById(item.Id, item.Name, languageCode, CategoryNameMap),
            Description = TranslateById(item.Id, item.Description, languageCode, CategoryDescriptionMap)
        }).ToList();
    }

    public static List<Tour> LocalizeTours(IEnumerable<Tour> source, string languageCode)
    {
        return source.Select(item => new Tour
        {
            Id = item.Id,
            ImageUrl = item.ImageUrl,
            Duration = item.Duration,
            LocationIds = new List<string>(item.LocationIds),
            Price = item.Price,
            IsFeatured = item.IsFeatured,
            Name = TranslateById(item.Id, item.Name, languageCode, TourNameMap),
            Description = TranslateById(item.Id, item.Description, languageCode, TourDescriptionMap)
        }).ToList();
    }

    public static List<LocationModel> LocalizeLocations(
        IEnumerable<LocationModel> source,
        string languageCode,
        ISet<string>? favoriteLocationIds = null)
    {
        return source.Select(item => LocalizeLocation(item, languageCode, favoriteLocationIds)).ToList();
    }

    public static LocationModel LocalizeLocation(
        LocationModel source,
        string languageCode,
        ISet<string>? favoriteLocationIds = null)
    {
        return new LocationModel
        {
            Id = source.Id,
            Name = TranslateById(source.Id, source.Name, languageCode, LocationNameMap),
            Description = TranslateById(source.Id, source.Description, languageCode, LocationDescriptionMap),
            ImageUrl = source.ImageUrl,
            Address = source.Address,
            Latitude = source.Latitude,
            Longitude = source.Longitude,
            Priority = source.Priority,
            DetectionRadiusMeters = source.DetectionRadiusMeters,
            Duration = source.Duration,
            CategoryId = source.CategoryId,
            CategoryName = TranslateById(source.CategoryId, source.CategoryName, languageCode, CategoryNameMap),
            AudioGuides = LocalizeAudioGuides(source.AudioGuides, languageCode, source.Id),
            IsFavorite = favoriteLocationIds?.Contains(source.Id) == true || source.IsFavorite
        };
    }

    public static List<AudioGuide> LocalizeAudioGuides(
        IEnumerable<AudioGuide> source,
        string languageCode,
        string? locationId = null)
    {
        var guides = source.ToList();
        if (guides.Count == 0)
        {
            return new List<AudioGuide>();
        }

        var normalized = NormalizeLanguageCode(languageCode);
        if (normalized == "vi")
        {
            return guides.Select(CloneAudioGuide).ToList();
        }

        var exact = guides.Where(guide => NormalizeLanguageCode(guide.Language) == normalized).ToList();
        if (exact.Count > 0)
        {
            return exact.Select(guide => BuildLocalizedAudioGuide(guide, normalized, locationId)).ToList();
        }

        var fallback = guides
            .Where(guide => NormalizeLanguageCode(guide.Language) == "vi")
            .DefaultIfEmpty(guides[0])
            .Select(guide => BuildLocalizedAudioGuide(guide, normalized, locationId))
            .ToList();

        return fallback;
    }

    private static AudioGuide BuildLocalizedAudioGuide(AudioGuide source, string languageCode, string? locationId)
    {
        var normalizedLang = NormalizeLanguageCode(languageCode);
        var localizedId = $"{source.Id}_{normalizedLang}";
        
        // Try to find real audio URL for this language
        string audioUrl = source.AudioUrl;
        if (UrlByGuideAndLanguage.TryGetValue(localizedId, out var mappedUrl))
        {
            audioUrl = mappedUrl;
        }

        var title = ResolveLocalizedAudioTitle(source, normalizedLang);
        var description = ResolveLocalizedAudioDescription(source, title, normalizedLang);

        var guide = CloneAudioGuide(source);
        guide.Id = localizedId;
        guide.Language = normalizedLang;
        guide.Title = title;
        guide.Description = description;
        guide.AudioUrl = audioUrl;
        guide.CloudinaryAudioUrl = audioUrl;
        guide.CloudinaryPublicId = ToCloudinaryPublicId(audioUrl);

        guide.ScriptSegments = ShouldRebuildScriptSegments(source, normalizedLang)
            ? BuildLocalizedScriptSegments(source, title, description, normalizedLang)
            : source.ScriptSegments.Select(segment => new AudioScriptSegment
            {
                Id = segment.Id,
                AudioGuideId = localizedId,
                StartTimeSeconds = segment.StartTimeSeconds,
                EndTimeSeconds = segment.EndTimeSeconds,
                ScriptText = segment.ScriptText
            }).ToList();
        guide.TranscriptText = string.Join(" ", guide.ScriptSegments.Select(segment => segment.ScriptText));
        return guide;
    }

    private static string ToCloudinaryPublicId(string audioUrl)
    {
        if (string.IsNullOrWhiteSpace(audioUrl)) return string.Empty;
        var fileName = audioUrl.Split('/').LastOrDefault() ?? string.Empty;
        var dotIndex = fileName.LastIndexOf('.');
        var baseName = dotIndex > 0 ? fileName[..dotIndex] : fileName;
        return string.IsNullOrWhiteSpace(baseName) ? string.Empty : $"audio/{baseName}";
    }

    private static bool ShouldRebuildScriptSegments(AudioGuide source, string languageCode)
    {
        if (source.ScriptSegments == null || source.ScriptSegments.Count == 0)
        {
            return true;
        }

        var normalizedLanguage = NormalizeLanguageCode(languageCode);
        var sourceLanguage = NormalizeLanguageCode(source.Language);
        if (!string.Equals(sourceLanguage, normalizedLanguage, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return source.ScriptSegments.Any(segment => LooksLikeVietnamese(segment.ScriptText));
    }

    private static string ResolveLocalizedAudioTitle(AudioGuide source, string languageCode)
    {
        if (!LooksLikeVietnamese(source.Title)
            && string.Equals(NormalizeLanguageCode(source.Language), NormalizeLanguageCode(languageCode), StringComparison.OrdinalIgnoreCase))
        {
            return source.Title;
        }

        return TranslateAudioTitle(source.Title, languageCode);
    }

    private static string ResolveLocalizedAudioDescription(AudioGuide source, string title, string languageCode)
    {
        if (!LooksLikeVietnamese(source.Description)
            && string.Equals(NormalizeLanguageCode(source.Language), NormalizeLanguageCode(languageCode), StringComparison.OrdinalIgnoreCase))
        {
            return source.Description;
        }

        return BuildLocalizedAudioDescription(title, string.Empty, languageCode);
    }

    private static bool LooksLikeVietnamese(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        const string accentedVietnameseCharsPattern = "[àáạảãâầấậẩẫăằắặẳẵèéẹẻẽêềếệểễìíịỉĩòóọỏõôồốộổỗơờớợởỡùúụủũưừứựửữỳýỵỷỹđ]";
        if (Regex.IsMatch(text, accentedVietnameseCharsPattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            return true;
        }

        const string commonVietnamesePhrasesPattern = "\\b(mở đầu|nội dung chính|kết thúc|cảm ơn|khám phá|tiếp tục|bạn đã|địa điểm)\\b";
        return Regex.IsMatch(text, commonVietnamesePhrasesPattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    }

    private static List<AudioScriptSegment> BuildLocalizedScriptSegments(
        AudioGuide source,
        string title,
        string description,
        string languageCode)
    {
        var totalSeconds = Math.Max(60, source.Duration * 60);
        var segmentDuration = Math.Max(20, totalSeconds / 3);

        var middleText = languageCode switch
        {
            "en" => $"Main content: key highlights of {title.ToLowerInvariant()}.",
            "zh" => $"主要内容：带你了解《{title}》的核心亮点。",
            _ => $"Nội dung chính: khám phá điểm nhấn tại {title.ToLowerInvariant()}."
        };

        var endingText = languageCode switch
        {
            "en" => "Ending: thank you for listening. Continue to the next point of interest.",
            "zh" => "结尾：感谢收听，继续探索下一个兴趣点。",
            _ => "Kết thúc: cảm ơn bạn đã lắng nghe, hãy tiếp tục khám phá POI kế tiếp."
        };

        var openingText = languageCode switch
        {
            "en" => $"Opening: {title}. {description}",
            "zh" => $"开场：{title}。{description}",
            _ => $"Mở đầu: {title}. {description}"
        };

        return new List<AudioScriptSegment>
        {
            new()
            {
                Id = 1,
                AudioGuideId = source.Id,
                StartTimeSeconds = 0,
                EndTimeSeconds = segmentDuration,
                ScriptText = openingText
            },
            new()
            {
                Id = 2,
                AudioGuideId = source.Id,
                StartTimeSeconds = segmentDuration,
                EndTimeSeconds = segmentDuration * 2,
                ScriptText = middleText
            },
            new()
            {
                Id = 3,
                AudioGuideId = source.Id,
                StartTimeSeconds = segmentDuration * 2,
                EndTimeSeconds = totalSeconds,
                ScriptText = endingText
            }
        };
    }

    private static string BuildLocalizedAudioDescription(string title, string locationName, string languageCode)
    {
        return languageCode switch
        {
            "en" => string.IsNullOrWhiteSpace(locationName)
                ? $"Audio guide for {title}."
                : $"Audio story about {locationName}.",
            "zh" => string.IsNullOrWhiteSpace(locationName)
                ? $"《{title}》音频导览。"
                : $"关于{locationName}的音频故事。",
            _ => string.IsNullOrWhiteSpace(locationName)
                ? $"Nội dung audio cho {title}."
                : $"Nội dung audio về {locationName}."
        };
    }

    private static string TranslateAudioTitle(string sourceTitle, string languageCode)
    {
        if (languageCode == "vi")
        {
            return sourceTitle;
        }

        if (AudioTitleMap.TryGetValue(sourceTitle, out var value))
        {
            return languageCode == "en" ? value.En : value.Zh;
        }

        return languageCode switch
        {
            "en" => "Audio Guide",
            "zh" => "音频导览",
            _ => sourceTitle
        };
    }

    private static AudioGuide CloneAudioGuide(AudioGuide source)
    {
        return new AudioGuide
        {
            Id = source.Id,
            Title = source.Title,
            Description = source.Description,
            AudioUrl = source.AudioUrl,
            CloudinaryAudioUrl = source.CloudinaryAudioUrl,
            CloudinaryPublicId = source.CloudinaryPublicId,
            TranscriptText = source.TranscriptText,
            Duration = source.Duration,
            LocationId = source.LocationId,
            Language = source.Language,
            ScriptSegments = source.ScriptSegments.Select(segment => new AudioScriptSegment
            {
                Id = segment.Id,
                AudioGuideId = segment.AudioGuideId,
                StartTimeSeconds = segment.StartTimeSeconds,
                EndTimeSeconds = segment.EndTimeSeconds,
                ScriptText = segment.ScriptText
            }).ToList()
        };
    }

    private static string TranslateById(
        string id,
        string currentValue,
        string languageCode,
        IReadOnlyDictionary<string, (string En, string Zh)> map)
    {
        if (NormalizeLanguageCode(languageCode) == "vi")
        {
            return currentValue;
        }

        if (!map.TryGetValue(id, out var value))
        {
            return currentValue;
        }

        return NormalizeLanguageCode(languageCode) == "en" ? value.En : value.Zh;
    }

    private static string NormalizeLanguageCode(string? languageCode)
    {
        if (string.IsNullOrWhiteSpace(languageCode))
        {
            return "vi";
        }

        return languageCode.Trim().ToLowerInvariant() switch
        {
            "vi" or "vi-vn" => "vi",
            "en" or "en-us" => "en",
            "zh" or "zh-cn" or "zh-hans" => "zh",
            _ => "vi"
        };
    }
}
