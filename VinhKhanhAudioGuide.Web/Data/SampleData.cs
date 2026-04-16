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
                    new() { Id = "ag_001_3", Title = "Introduction", Description = "About Bun Mam Vinh Khanh", AudioUrl = "https://res.cloudinary.com/dex6q1cqh/video/upload/f_mp3/v1776329636/audio/audio/ag_001_3_en.mp3", Duration = 4, LocationId = "loc_001", Language = "en", GeneratedFromTts = true, TtsSourceText = "Welcome to Bun Mam Vinh Khanh, a beloved noodle shop in District 4, Ho Chi Minh City. Bun Mam is a rich and flavorful fermented fish noodle soup originating from the Mekong Delta. The broth is deeply aromatic, topped with succulent shrimp, roast pork, and duck meatballs. This is a must-try dish for anyone exploring Vietnamese street food culture." },
                    new() { Id = "ag_001_4", Title = "Food Discovery", Description = "Taste of Bun Mam", AudioUrl = "https://res.cloudinary.com/dex6q1cqh/video/upload/f_mp3/v1776329640/audio/audio/ag_001_4_en.mp3", Duration = 4, LocationId = "loc_001", Language = "en", GeneratedFromTts = true, TtsSourceText = "The secret to great Bun Mam lies in its broth — a perfectly balanced blend of fermented fish sauce, lemongrass, and fresh herbs. Each bowl comes with a generous variety of toppings including grilled pork, shrimp, eggplant, and fresh vegetables. The combination creates a symphony of sweet, savory, and umami flavors that defines southern Vietnamese cuisine." },
                    new() { Id = "ag_001_5", Title = "介紹", Description = "關於 Vĩnh Khánh 的 Bún Mắm", AudioUrl = "https://res.cloudinary.com/dex6q1cqh/video/upload/f_mp3/v1776329679/audio/audio/ag_001_5_zh.mp3", Duration = 4, LocationId = "loc_001", Language = "zh", GeneratedFromTts = true, TtsSourceText = "歡迎來到 Vĩnh Khánh 的 Bún Mắm 名店。這道來自湄公河三角洲的湯麵以發酵魚露熬成濃郁湯頭，香氣豐富，搭配鮮蝦、燒肉與鴨肉丸，是探索越南街頭美食時一定要品嚐的一碗。" },
                    new() { Id = "ag_001_6", Title = "美食探索", Description = "Bún Mắm 的風味", AudioUrl = "https://res.cloudinary.com/dex6q1cqh/video/upload/f_mp3/v1776329682/audio/audio/ag_001_6_zh.mp3", Duration = 4, LocationId = "loc_001", Language = "zh", GeneratedFromTts = true, TtsSourceText = "Bún Mắm 的靈魂在於湯底，魚露、香茅與香草交織出層次鮮明的味 道。每一碗都能吃到豐富配料，包括燒肉、鮮蝦、茄子與新鮮蔬菜，甜、鹹、鮮與旨味在口中完美融合。" }
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
                    new() { Id = "ag_002_3", Title = "Introduction", Description = "About Banh Xeo", AudioUrl = "https://res.cloudinary.com/dex6q1cqh/video/upload/f_mp3/v1776329644/audio/audio/ag_002_3_en.mp3", Duration = 4, LocationId = "loc_002", Language = "en", GeneratedFromTts = true, TtsSourceText = "Discover the crispy delight of Banh Xeo, a traditional Vietnamese crepe from the Mekong Delta. Cooked in a cast iron pan, this golden crepe is filled with shrimp, pork, bean sprouts, and served with an abundance of fresh herbs and lettuce for wrapping." },
                    new() { Id = "ag_002_4", Title = "Food Discovery", Description = "Taste of Banh Xeo", AudioUrl = "https://res.cloudinary.com/dex6q1cqh/video/upload/f_mp3/v1776329648/audio/audio/ag_002_4_en.mp3", Duration = 4, LocationId = "loc_002", Language = "en", GeneratedFromTts = true, TtsSourceText = "The name Banh Xeo comes from the sizzling sound the batter makes when it hits the hot pan. The perfect Banh Xeo is ultra-crispy on the outside with a savory filling inside. Dip it in the sweet and tangy fish sauce, wrap it with fresh herbs, and enjoy an explosion of textures and flavors." },
                    new() { Id = "ag_002_5", Title = "介紹", Description = "關於 Bánh Xèo", AudioUrl = "https://res.cloudinary.com/dex6q1cqh/video/upload/f_mp3/v1776329685/audio/audio/ag_002_5_zh.mp3", Duration = 4, LocationId = "loc_002", Language = "zh", GeneratedFromTts = true, TtsSourceText = "來認識酥脆可口的 Bánh Xèo，這是來自湄公河三角洲的越南經典煎餅。金黃的餅皮在鐵鍋中煎得滋滋作響，內餡有蝦、豬肉與豆芽，再搭配大量新鮮香草和生菜一起包著吃。" },
                    new() { Id = "ag_002_6", Title = "美食探索", Description = "Bánh Xèo 的滋味", AudioUrl = "https://res.cloudinary.com/dex6q1cqh/video/upload/f_mp3/v1776329688/audio/audio/ag_002_6_zh.mp3", Duration = 4, LocationId = "loc_002", Language = "zh", GeneratedFromTts = true, TtsSourceText = "Bánh Xèo 這個名字來自麵糊落入熱鍋時發出的吱吱聲。完美的 Bánh Xèo 外皮極致酥脆，內餡鹹香飽滿，沾上酸甜魚露，再用香草包裹，就是層次豐富又令人上癮的一口。" }
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
                    new() { Id = "ag_003_3", Title = "Introduction", Description = "About Banh Trang Tron", AudioUrl = "https://res.cloudinary.com/dex6q1cqh/video/upload/f_mp3/v1776329651/audio/audio/ag_003_3_en.mp3", Duration = 3, LocationId = "loc_003", Language = "en", GeneratedFromTts = true, TtsSourceText = "Banh Trang Tron is one of Vietnam's most popular street snacks. It's a mix of shredded rice paper tossed with dried beef, quail eggs, green mango, herbs, and a spicy-sweet-sour dressing. Simple yet incredibly addictive, this snack captures the essence of Vietnamese street food." },
                    new() { Id = "ag_003_4", Title = "介紹", Description = "關於 Bánh Tráng Trộn", AudioUrl = "https://res.cloudinary.com/dex6q1cqh/video/upload/f_mp3/v1776329691/audio/audio/ag_003_4_zh.mp3", Duration = 3, LocationId = "loc_003", Language = "zh", GeneratedFromTts = true, TtsSourceText = "Bánh Tráng Trộn 是越南最受歡迎的街頭零食之一。它把切絲米紙 與牛肉乾、鵪鶉蛋、青芒果、香草和酸甜微辣的醬汁拌在一起，簡單卻極度令人上癮，充分展現越南街頭小吃的魅力。" }
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
                    new() { Id = "ag_004_3", Title = "Introduction", Description = "About Grilled Skewers", AudioUrl = "https://res.cloudinary.com/dex6q1cqh/video/upload/f_mp3/v1776329655/audio/audio/ag_004_3_en.mp3", Duration = 3, LocationId = "loc_004", Language = "en", GeneratedFromTts = true, TtsSourceText = "Grilled skewers are a beloved street food in Vinh Khanh. From marinated meats and sausages to okra and mushrooms, everything is charcoal-grilled to perfection. The smoky aroma and vibrant flavors make this a favorite among locals and visitors alike." },
                    new() { Id = "ag_004_4", Title = "介紹", Description = "關於烤串", AudioUrl = "https://res.cloudinary.com/dex6q1cqh/video/upload/f_mp3/v1776329694/audio/audio/ag_004_4_zh.mp3", Duration = 3, LocationId = "loc_004", Language = "zh", GeneratedFromTts = true, TtsSourceText = "Vĩnh Khánh 的烤串是當地非常受歡迎的街頭美食。從醃製肉串、香腸到秋葵與蘑菇，所有食材都在炭火上烤得恰到好處，煙燻香氣與鮮明風味讓人一試難忘。" }
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
                    new() { Id = "ag_005_3", Title = "Introduction", Description = "About Pork Rib Porridge", AudioUrl = "https://res.cloudinary.com/dex6q1cqh/video/upload/f_mp3/v1776329659/audio/audio/ag_005_3_en.mp3", Duration = 3, LocationId = "loc_005", Language = "en", GeneratedFromTts = true, TtsSourceText = "Late-night pork rib porridge is the ultimate comfort food on Vinh Khanh street. The rice porridge is slow-cooked to silky perfection, topped with tender pork ribs, crispy fried dough sticks, and a generous sprinkle of pepper. It's the perfect warm bowl for a cool Saigon evening." },
                    new() { Id = "ag_005_4", Title = "介紹", Description = "關於豬肋粥", AudioUrl = "https://res.cloudinary.com/dex6q1cqh/video/upload/f_mp3/v1776329697/audio/audio/ag_005_4_zh.mp3", Duration = 3, LocationId = "loc_005", Language = "zh", GeneratedFromTts = true, TtsSourceText = "深夜的豬肋粥是 Vĩnh Khánh 最療癒的一碗。米粥慢火熬煮至細滑 綿密，再鋪上軟嫩豬肋骨、酥脆油條與胡椒，溫熱又舒服，很適合涼爽的西貢夜晚。" }
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
                    new() { Id = "ag_006_3", Title = "Introduction", Description = "About Late-Night Pho", AudioUrl = "https://res.cloudinary.com/dex6q1cqh/video/upload/f_mp3/v1776329663/audio/audio/ag_006_3_en.mp3", Duration = 3, LocationId = "loc_006", Language = "en", GeneratedFromTts = true, TtsSourceText = "Pho is Vietnam's most iconic dish, and enjoying it late at night in Vinh Khanh is a special experience. The clear beef bone broth is simmered for hours, creating a deeply flavorful and aromatic soup. Topped with sliced beef, fresh herbs, and a squeeze of lime — this is Saigon street food at its finest." },
                    new() { Id = "ag_006_4", Title = "介紹", Description = "關於深夜河粉", AudioUrl = "https://res.cloudinary.com/dex6q1cqh/video/upload/f_mp3/v1776329700/audio/audio/ag_006_4_zh.mp3", Duration = 3, LocationId = "loc_006", Language = "zh", GeneratedFromTts = true, TtsSourceText = "河粉是越南最具代表性的料理之一，而在 Vĩnh Khánh 的夜晚品嚐 更是一種特別體驗。清澈的牛骨湯熬煮數小時，味道濃厚又帶香氣，再加上牛肉片、新鮮香草與一點檸檬，就是最道地的西貢街頭滋味。" }
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
                    new() { Id = "ag_007_3", Title = "Introduction", Description = "About Garlic Butter Snails", AudioUrl = "https://res.cloudinary.com/dex6q1cqh/video/upload/f_mp3/v1776329666/audio/audio/ag_007_3_en.mp3", Duration = 3, LocationId = "loc_007", Language = "en", GeneratedFromTts = true, TtsSourceText = "Vinh Khanh street is famous for its seafood, and garlic butter snails are a crowd favorite. Sea snails are stir-fried with fragrant garlic and rich butter, creating an irresistible dish. Pair it with a crusty baguette to soak up the flavorful sauce — pure indulgence on a Saigon night." },
                    new() { Id = "ag_007_4", Title = "介紹", Description = "關於蒜香奶油螺", AudioUrl = "https://res.cloudinary.com/dex6q1cqh/video/upload/f_mp3/v1776329703/audio/audio/ag_007_4_zh.mp3", Duration = 3, LocationId = "loc_007", Language = "zh", GeneratedFromTts = true, TtsSourceText = "Vĩnh Khánh 的海鮮很有名，而蒜香奶油螺更是人氣招牌。海螺與蒜末、奶油一起快炒，香氣十足，配上一塊法棍沾醬更是一絕，讓人感受到滿滿的西貢夜生活魅力。" }
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
                    new() { Id = "ag_008_3", Title = "Introduction", Description = "About Salt & Chili Grilled Prawns", AudioUrl = "https://res.cloudinary.com/dex6q1cqh/video/upload/f_mp3/v1776329669/audio/audio/ag_008_3_en.mp3", Duration = 3, LocationId = "loc_008", Language = "en", GeneratedFromTts = true, TtsSourceText = "Fresh tiger prawns marinated with salt and chili, grilled over charcoal until the shells are beautifully charred and the meat is perfectly juicy. This street seafood dish is a highlight of Vinh Khanh, combining simple seasoning with top-quality ingredients for an unforgettable taste." },
                    new() { Id = "ag_008_4", Title = "介紹", Description = "關於鹽辣烤蝦", AudioUrl = "https://res.cloudinary.com/dex6q1cqh/video/upload/f_mp3/v1776329706/audio/audio/ag_008_4_zh.mp3", Duration = 3, LocationId = "loc_008", Language = "zh", GeneratedFromTts = true, TtsSourceText = "新鮮虎蝦以鹽和辣椒醃製後，用炭火烤到蝦殼微焦、蝦肉鮮嫩多汁 。這道街頭海鮮料理是 Vĩnh Khánh 的亮點之一，調味簡單卻能把食材鮮味發揮到極致。" }
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
                    new() { Id = "ag_009_3", Title = "Introduction", Description = "About Bubble Tea", AudioUrl = "https://res.cloudinary.com/dex6q1cqh/video/upload/f_mp3/v1776329673/audio/audio/ag_009_3_en.mp3", Duration = 3, LocationId = "loc_009", Language = "en", GeneratedFromTts = true, TtsSourceText = "Bubble tea has become a cultural phenomenon in Vietnam, and this shop in Vinh Khanh offers a wide variety of flavors and toppings. From classic brown sugar boba to cheese foam tea, there's something for every palate. A refreshing treat after exploring the vibrant street food scene." },
                    new() { Id = "ag_009_4", Title = "介紹", Description = "關於珍珠奶茶", AudioUrl = "https://res.cloudinary.com/dex6q1cqh/video/upload/f_mp3/v1776329798/audio/audio/ag_009_4_zh.mp3", Duration = 3, LocationId = "loc_009", Language = "zh", GeneratedFromTts = true, TtsSourceText = "珍珠奶茶在越南已成為很受歡迎的飲品文化，而 Vĩnh Khánh 的這 家店提供多種口味與配料。從經典黑糖珍珠到奶蓋茶，無論喜歡哪一種風格，都能在這裡找到適合自己的那一杯。" }
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
                    new() { Id = "ag_010_3", Title = "Introduction", Description = "About Vietnamese Iced Coffee", AudioUrl = "https://res.cloudinary.com/dex6q1cqh/video/upload/f_mp3/v1776329676/audio/audio/ag_010_3_en.mp3", Duration = 3, LocationId = "loc_010", Language = "en", GeneratedFromTts = true, TtsSourceText = "Vietnamese iced coffee, or Ca Phe Sua Da, is an essential part of Saigon's daily life. Strong dark roast coffee is brewed through a traditional phin filter, dripping slowly over a layer of sweet condensed milk. Poured over ice, it's the perfect balance of bold, bitter, and sweet — a true taste of Vietnam." },
                    new() { Id = "ag_010_4", Title = "介紹", Description = "關於越南冰咖啡", AudioUrl = "https://res.cloudinary.com/dex6q1cqh/video/upload/f_mp3/v1776329801/audio/audio/ag_010_4_zh.mp3", Duration = 3, LocationId = "loc_010", Language = "zh", GeneratedFromTts = true, TtsSourceText = "越南冰咖啡，也就是 Cà Phê Sữa Đá，是西貢日常生活中不可或缺 的一部分。深焙咖啡透過傳統濾壺慢慢滴落在煉乳上，再加入冰塊，形成濃郁、微苦又帶甜味的完美平衡。" }
                }
            }
        };

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
            new()
            {
                Id = "daily",
                Name = "10.000đ/ngày",
                Description = "Một ngày sử dụng. Phù hợp khi bạn muốn trải nghiệm nhanh trong một ngày, tối ưu cho khách ghé ngắn.",
                Price = 10000m,
                Currency = "VND",
                DurationDays = 1,
                IsActive = true,
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
                IsActive = true,
                CreatedAtUtc = SeedBaseUtc.AddMinutes(5)
            }
        };
    }

}