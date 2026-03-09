namespace VinhKhanhAudioGuide.Mobile.Data;

public static class SampleData
{
    public static List<Models.Category> GetCategories()
    {
        return new List<Models.Category>
        {
            new() { Id = "1", Name = "Di tích lịch sử", Icon = "heritage_icon", Description = "Các di tích lịch sử, văn hóa quan trọng" },
            new() { Id = "2", Name = "Bảo tàng", Icon = "museum_icon", Description = "Các bảo tàng trưng bày hiện vật" },
            new() { Id = "3", Name = "Chùa chiền", Icon = "temple_icon", Description = "Các ngôi chùa cổ kính" },
            new() { Id = "4", Name = "Công viên", Icon = "park_icon", Description = "Công viên và khu vui chơi" },
            new() { Id = "5", Name = "Làng nghề", Icon = "craft_icon", Description = "Các làng nghề truyền thống" },
            new() { Id = "6", Name = "Ẩm thực", Icon = "food_icon", Description = "Điểm ẩm thực đặc sản" }
        };
    }

    public static List<Models.Location> GetLocations()
    {
        return new List<Models.Location>
        {
            // Di tích lịch sử
            new()
            {
                Id = "loc_001",
                Name = "Chùa Một Cột",
                Description = "Chùa Một Cột hay còn gọi là Diên Hựu tự, là một trong những ngôi chùa có kiến trúc độc đáo nhất Việt Nam. Chùa được xây dựng năm 1049 dưới triều vua Lý Thái Tông, theo truyền thuyết về giấc mơ của nhà vua được Phật Bà Quan Âm dẫn lên đài sen.",
                ImageUrl = "chua_mot_cot",
                Address = "Phố Chùa Một Cột, Đội Cấn, Ba Đình, Hà Nội",
                Latitude = 21.0359,
                Longitude = 105.8344,
                Duration = 15,
                CategoryId = "3",
                AudioGuides = GetChuaMotCotAudioGuides()
            },
            new()
            {
                Id = "loc_002",
                Name = "Văn Miếu - Quốc Tử Giám",
                Description = "Văn Miếu được xây dựng từ năm 1070 dưới triều Lý Thánh Tông để thờ Khổng Tử. Năm 1076, vua Lý Nhân Tông cho lập Quốc Tử Giám - trường đại học đầu tiên của Việt Nam.",
                ImageUrl = "van_mieu",
                Address = "58 Quốc Tử Giám, Văn Miếu, Đống Đa, Hà Nội",
                Latitude = 21.0286,
                Longitude = 105.8354,
                Duration = 45,
                CategoryId = "1",
                AudioGuides = GetVanMieuAudioGuides()
            },
            new()
            {
                Id = "loc_003",
                Name = "Hoàng Thành Thăng Long",
                Description = "Di sản văn hóa thế giới UNESCO, Hoàng Thành Thăng Long là quần thể di tích gắn liền với lịch sử kinh thành Thăng Long - Đông Kinh và Hà Nội ngày nay. Đây là trung tâm quyền lực của Việt Nam trong suốt 13 thế kỷ.",
                ImageUrl = "hoang_thanh",
                Address = "19C Hoàng Diệu, Điện Biên, Ba Đình, Hà Nội",
                Latitude = 21.0355,
                Longitude = 105.8412,
                Duration = 60,
                CategoryId = "1",
                AudioGuides = GetHoangThanhAudioGuides()
            },
            new()
            {
                Id = "loc_004",
                Name = "Lăng Chủ tịch Hồ Chí Minh",
                Description = "Lăng Chủ tịch Hồ Chí Minh là nơi an nghỉ của Chủ tịch Hồ Chí Minh, vị lãnh tụ vĩ đại của dân tộc Việt Nam. Công trình được xây dựng từ năm 1973 đến 1975 tại Quảng trường Ba Đình lịch sử.",
                ImageUrl = "lang_bac",
                Address = "Số 2 Hùng Vương, Điện Biên, Ba Đình, Hà Nội",
                Latitude = 21.0369,
                Longitude = 105.8346,
                Duration = 30,
                CategoryId = "1",
                AudioGuides = GetLangBacAudioGuides()
            },
            new()
            {
                Id = "loc_005",
                Name = "Nhà tù Hỏa Lò",
                Description = "Nhà tù Hỏa Lò được người Pháp xây dựng từ năm 1886-1901 để giam giữ các chiến sĩ cách mạng Việt Nam. Nơi đây đã chứng kiến nhiều sự kiện lịch sử quan trọng và là minh chứng cho tinh thần đấu tranh bất khuất của dân tộc.",
                ImageUrl = "hoa_lo",
                Address = "1 Hoả Lò, Trần Hưng Đạo, Hoàn Kiếm, Hà Nội",
                Latitude = 21.0254,
                Longitude = 105.8467,
                Duration = 40,
                CategoryId = "2",
                AudioGuides = GetHoaLoAudioGuides()
            },
            new()
            {
                Id = "loc_006",
                Name = "Hồ Hoàn Kiếm",
                Description = "Hồ Hoàn Kiếm hay còn gọi là Hồ Gươm, nằm ở trung tâm Hà Nội, gắn liền với truyền thuyết vua Lê Lợi trả gươm thần cho rùa vàng. Đây là biểu tượng văn hóa, lịch sử của thủ đô.",
                ImageUrl = "ho_guom",
                Address = "Hồ Hoàn Kiếm, Hoàn Kiếm, Hà Nội",
                Latitude = 21.0288,
                Longitude = 105.8525,
                Duration = 25,
                CategoryId = "4",
                AudioGuides = GetHoGuomAudioGuides()
            },
            new()
            {
                Id = "loc_007",
                Name = "Bảo tàng Dân tộc học Việt Nam",
                Description = "Bảo tàng Dân tộc học Việt Nam là nơi lưu giữ và giới thiệu văn hóa của 54 dân tộc Việt Nam. Với hơn 25.000 hiện vật và 15.000 bức ảnh, đây là kho tàng văn hóa vô giá.",
                ImageUrl = "bao_tang_dan_toc",
                Address = "Nguyễn Văn Huyên, Quan Hoa, Cầu Giấy, Hà Nội",
                Latitude = 21.0402,
                Longitude = 105.7984,
                Duration = 90,
                CategoryId = "2",
                AudioGuides = GetBaoTangDanTocAudioGuides()
            },
            new()
            {
                Id = "loc_008",
                Name = "Chùa Trấn Quốc",
                Description = "Chùa Trấn Quốc là một trong những ngôi chùa cổ nhất Việt Nam, được xây dựng vào thế kỷ thứ 6 dưới thời nhà Lý Nam Đế. Chùa nằm trên đảo nhỏ phía đông Hồ Tây.",
                ImageUrl = "chua_tran_quoc",
                Address = "Thanh Niên, Yên Phụ, Tây Hồ, Hà Nội",
                Latitude = 21.0478,
                Longitude = 105.8362,
                Duration = 20,
                CategoryId = "3",
                AudioGuides = GetChuaTranQuocAudioGuides()
            },
            new()
            {
                Id = "loc_009",
                Name = "Phố cổ Hà Nội",
                Description = "Phố cổ Hà Nội hay còn gọi là khu 36 phố phường, là khu vực lịch sử nằm ở phía bắc Hồ Hoàn Kiếm. Mỗi phố chuyên bán một mặt hàng riêng, tạo nên nét đặc trưng văn hóa độc đáo.",
                ImageUrl = "pho_co",
                Address = "Khu phố cổ, Hoàn Kiếm, Hà Nội",
                Latitude = 21.0340,
                Longitude = 105.8500,
                Duration = 60,
                CategoryId = "1",
                AudioGuides = GetPhoCoAudioGuides()
            },
            new()
            {
                Id = "loc_010",
                Name = "Làng gốm Bát Tràng",
                Description = "Làng gốm Bát Tràng có lịch sử hơn 700 năm, nổi tiếng với nghề làm gốm sứ truyền thống. Đây là nơi du khách có thể tham quan lò gốm, trải nghiệm làm gốm và mua sắm các sản phẩm thủ công.",
                ImageUrl = "bat_trang",
                Address = "Xã Bát Tràng, Gia Lâm, Hà Nội",
                Latitude = 21.0123,
                Longitude = 105.9145,
                Duration = 120,
                CategoryId = "5",
                AudioGuides = GetBatTrangAudioGuides()
            }
        };
    }

    private static List<Models.AudioGuide> GetChuaMotCotAudioGuides()
    {
        return new List<Models.AudioGuide>
        {
            new() { Id = "ag_001_1", Title = "Lịch sử chùa Một Cột", Description = "Tìm hiểu về nguồn gốc và lịch sử xây dựng", AudioUrl = "chua_mot_cot_history.mp3", Duration = 5, LocationId = "loc_001", Language = "vi" },
            new() { Id = "ag_001_2", Title = "Kiến trúc độc đáo", Description = "Khám phá kiến trúc hoa sen độc nhất vô nhị", AudioUrl = "chua_mot_cot_architecture.mp3", Duration = 4, LocationId = "loc_001", Language = "vi" },
            new() { Id = "ag_001_3", Title = "Ý nghĩa tâm linh", Description = "Ý nghĩa phong thủy và tâm linh của chùa", AudioUrl = "chua_mot_cot_spiritual.mp3", Duration = 6, LocationId = "loc_001", Language = "vi" }
        };
    }

    private static List<Models.AudioGuide> GetVanMieuAudioGuides()
    {
        return new List<Models.AudioGuide>
        {
            new() { Id = "ag_002_1", Title = "Tổng quan Văn Miếu", Description = "Giới thiệu chung về khu di tích", AudioUrl = "van_mieu_overview.mp3", Duration = 8, LocationId = "loc_002", Language = "vi" },
            new() { Id = "ag_002_2", Title = "Khu Văn Miếu", Description = "Khu vực thờ Khổng Tử và các bậc danh nho", AudioUrl = "van_mieu_temple.mp3", Duration = 10, LocationId = "loc_002", Language = "vi" },
            new() { Id = "ag_002_3", Title = "Khu Quốc Tử Giám", Description = "Trường đại học đầu tiên của Việt Nam", AudioUrl = "van_mieu_school.mp3", Duration = 12, LocationId = "loc_002", Language = "vi" },
            new() { Id = "ag_002_4", Title = "82 Bia Tiến sĩ", Description = "Bia đá ghi danh các tiến sĩ qua các triều đại", AudioUrl = "van_mieu_stele.mp3", Duration = 8, LocationId = "loc_002", Language = "vi" },
            new() { Id = "ag_002_5", Title = "Giếng Thiên Quang", Description = "Giếng thiêng của Văn Miếu", AudioUrl = "van_mieu_well.mp3", Duration = 5, LocationId = "loc_002", Language = "vi" }
        };
    }

    private static List<Models.AudioGuide> GetHoangThanhAudioGuides()
    {
        return new List<Models.AudioGuide>
        {
            new() { Id = "ag_003_1", Title = "Lịch sử Hoàng Thành", Description = "13 thế kỷ lịch sử kinh đô", AudioUrl = "hoang_thanh_history.mp3", Duration = 15, LocationId = "loc_003", Language = "vi" },
            new() { Id = "ag_003_2", Title = "Cột cờ Hà Nội", Description = "Biểu tượng của thủ đô", AudioUrl = "hoang_thanh_flagtower.mp3", Duration = 8, LocationId = "loc_003", Language = "vi" },
            new() { Id = "ag_003_3", Title = "Đoan Môn", Description = "Cổng chính vào Hoàng Thành", AudioUrl = "hoang_thanh_gate.mp3", Duration = 10, LocationId = "loc_003", Language = "vi" },
            new() { Id = "ag_003_4", Title = "Điện Kính Thiên", Description = "Nơi vua thiết triều", AudioUrl = "hoang_thanh_palace.mp3", Duration = 12, LocationId = "loc_003", Language = "vi" },
            new() { Id = "ag_003_5", Title = "Khu khảo cổ 18 Hoàng Diệu", Description = "Những phát hiện khảo cổ quan trọng", AudioUrl = "hoang_thanh_archaeology.mp3", Duration = 15, LocationId = "loc_003", Language = "vi" }
        };
    }

    private static List<Models.AudioGuide> GetLangBacAudioGuides()
    {
        return new List<Models.AudioGuide>
        {
            new() { Id = "ag_004_1", Title = "Cuộc đời Bác Hồ", Description = "Tóm tắt cuộc đời vĩ đại của Người", AudioUrl = "lang_bac_life.mp3", Duration = 10, LocationId = "loc_004", Language = "vi" },
            new() { Id = "ag_004_2", Title = "Kiến trúc Lăng", Description = "Ý nghĩa kiến trúc công trình", AudioUrl = "lang_bac_architecture.mp3", Duration = 8, LocationId = "loc_004", Language = "vi" },
            new() { Id = "ag_004_3", Title = "Quảng trường Ba Đình", Description = "Nơi Bác đọc Tuyên ngôn Độc lập", AudioUrl = "lang_bac_square.mp3", Duration = 7, LocationId = "loc_004", Language = "vi" },
            new() { Id = "ag_004_4", Title = "Hướng dẫn tham quan", Description = "Quy định và lộ trình tham quan", AudioUrl = "lang_bac_guide.mp3", Duration = 5, LocationId = "loc_004", Language = "vi" }
        };
    }

    private static List<Models.AudioGuide> GetHoaLoAudioGuides()
    {
        return new List<Models.AudioGuide>
        {
            new() { Id = "ag_005_1", Title = "Lịch sử nhà tù", Description = "Từ thời Pháp thuộc đến nay", AudioUrl = "hoa_lo_history.mp3", Duration = 10, LocationId = "loc_005", Language = "vi" },
            new() { Id = "ag_005_2", Title = "Các chiến sĩ cách mạng", Description = "Những người con anh hùng", AudioUrl = "hoa_lo_heroes.mp3", Duration = 12, LocationId = "loc_005", Language = "vi" },
            new() { Id = "ag_005_3", Title = "Không gian giam giữ", Description = "Mô tả các phòng giam", AudioUrl = "hoa_lo_cells.mp3", Duration = 8, LocationId = "loc_005", Language = "vi" },
            new() { Id = "ag_005_4", Title = "Di tích còn lại", Description = "Những gì còn được bảo tồn", AudioUrl = "hoa_lo_remains.mp3", Duration = 10, LocationId = "loc_005", Language = "vi" }
        };
    }

    private static List<Models.AudioGuide> GetHoGuomAudioGuides()
    {
        return new List<Models.AudioGuide>
        {
            new() { Id = "ag_006_1", Title = "Truyền thuyết Hồ Gươm", Description = "Câu chuyện vua Lê trả gươm", AudioUrl = "ho_guom_legend.mp3", Duration = 8, LocationId = "loc_006", Language = "vi" },
            new() { Id = "ag_006_2", Title = "Tháp Rùa", Description = "Biểu tượng giữa lòng hồ", AudioUrl = "ho_guom_turtle_tower.mp3", Duration = 5, LocationId = "loc_006", Language = "vi" },
            new() { Id = "ag_006_3", Title = "Đền Ngọc Sơn", Description = "Ngôi đền trên đảo Ngọc", AudioUrl = "ho_guom_temple.mp3", Duration = 7, LocationId = "loc_006", Language = "vi" },
            new() { Id = "ag_006_4", Title = "Cầu Thê Húc", Description = "Cầu đỏ nối vào đền", AudioUrl = "ho_guom_bridge.mp3", Duration = 5, LocationId = "loc_006", Language = "vi" }
        };
    }

    private static List<Models.AudioGuide> GetBaoTangDanTocAudioGuides()
    {
        return new List<Models.AudioGuide>
        {
            new() { Id = "ag_007_1", Title = "Tổng quan bảo tàng", Description = "Giới thiệu về bảo tàng", AudioUrl = "dan_toc_overview.mp3", Duration = 10, LocationId = "loc_007", Language = "vi" },
            new() { Id = "ag_007_2", Title = "Người Kinh", Description = "Văn hóa dân tộc đa số", AudioUrl = "dan_toc_kinh.mp3", Duration = 15, LocationId = "loc_007", Language = "vi" },
            new() { Id = "ag_007_3", Title = "Các dân tộc Tây Bắc", Description = "Thái, Mường, Mông, Dao...", AudioUrl = "dan_toc_northwest.mp3", Duration = 20, LocationId = "loc_007", Language = "vi" },
            new() { Id = "ag_007_4", Title = "Các dân tộc Tây Nguyên", Description = "Ê Đê, Gia Rai, Ba Na...", AudioUrl = "dan_toc_highland.mp3", Duration = 20, LocationId = "loc_007", Language = "vi" },
            new() { Id = "ag_007_5", Title = "Khu trưng bày ngoài trời", Description = "Nhà sàn và kiến trúc dân gian", AudioUrl = "dan_toc_outdoor.mp3", Duration = 25, LocationId = "loc_007", Language = "vi" }
        };
    }

    private static List<Models.AudioGuide> GetChuaTranQuocAudioGuides()
    {
        return new List<Models.AudioGuide>
        {
            new() { Id = "ag_008_1", Title = "Lịch sử chùa Trấn Quốc", Description = "Ngôi chùa cổ nhất Hà Nội", AudioUrl = "tran_quoc_history.mp3", Duration = 7, LocationId = "loc_008", Language = "vi" },
            new() { Id = "ag_008_2", Title = "Kiến trúc và cảnh quan", Description = "Vẻ đẹp bên Hồ Tây", AudioUrl = "tran_quoc_architecture.mp3", Duration = 6, LocationId = "loc_008", Language = "vi" },
            new() { Id = "ag_008_3", Title = "Cây Bồ Đề", Description = "Cây chiết từ Bồ Đề Đạo Tràng", AudioUrl = "tran_quoc_tree.mp3", Duration = 5, LocationId = "loc_008", Language = "vi" }
        };
    }

    private static List<Models.AudioGuide> GetPhoCoAudioGuides()
    {
        return new List<Models.AudioGuide>
        {
            new() { Id = "ag_009_1", Title = "36 phố phường", Description = "Nguồn gốc tên các phố", AudioUrl = "pho_co_streets.mp3", Duration = 15, LocationId = "loc_009", Language = "vi" },
            new() { Id = "ag_009_2", Title = "Phố Hàng Bạc", Description = "Phố của thợ kim hoàn", AudioUrl = "pho_co_hang_bac.mp3", Duration = 8, LocationId = "loc_009", Language = "vi" },
            new() { Id = "ag_009_3", Title = "Phố Hàng Mã", Description = "Phố đồ chơi và đồ thờ", AudioUrl = "pho_co_hang_ma.mp3", Duration = 8, LocationId = "loc_009", Language = "vi" },
            new() { Id = "ag_009_4", Title = "Đình và đền phố cổ", Description = "Di tích tâm linh khu phố cổ", AudioUrl = "pho_co_temples.mp3", Duration = 10, LocationId = "loc_009", Language = "vi" },
            new() { Id = "ag_009_5", Title = "Ẩm thực phố cổ", Description = "Các món ngon nổi tiếng", AudioUrl = "pho_co_food.mp3", Duration = 12, LocationId = "loc_009", Language = "vi" }
        };
    }

    private static List<Models.AudioGuide> GetBatTrangAudioGuides()
    {
        return new List<Models.AudioGuide>
        {
            new() { Id = "ag_010_1", Title = "Lịch sử làng gốm", Description = "700 năm nghề gốm", AudioUrl = "bat_trang_history.mp3", Duration = 10, LocationId = "loc_010", Language = "vi" },
            new() { Id = "ag_010_2", Title = "Quy trình làm gốm", Description = "Từ đất sét đến thành phẩm", AudioUrl = "bat_trang_process.mp3", Duration = 15, LocationId = "loc_010", Language = "vi" },
            new() { Id = "ag_010_3", Title = "Các sản phẩm đặc trưng", Description = "Bát, đĩa, lọ hoa...", AudioUrl = "bat_trang_products.mp3", Duration = 10, LocationId = "loc_010", Language = "vi" },
            new() { Id = "ag_010_4", Title = "Trải nghiệm làm gốm", Description = "Hướng dẫn tự tay nặn gốm", AudioUrl = "bat_trang_experience.mp3", Duration = 8, LocationId = "loc_010", Language = "vi" }
        };
    }

    public static List<Models.Tour> GetTours()
    {
        return new List<Models.Tour>
        {
            new()
            {
                Id = "tour_001",
                Name = "Hà Nội một ngày",
                Description = "Khám phá những điểm đến nổi tiếng nhất Hà Nội trong một ngày",
                ImageUrl = "tour_hanoi_oneday",
                Duration = 480,
                LocationIds = new List<string> { "loc_002", "loc_004", "loc_006", "loc_001" },
                Price = 0,
                IsFeatured = true
            },
            new()
            {
                Id = "tour_002",
                Name = "Di sản văn hóa",
                Description = "Hành trình qua các di sản văn hóa lịch sử quan trọng",
                ImageUrl = "tour_heritage",
                Duration = 360,
                LocationIds = new List<string> { "loc_003", "loc_002", "loc_005" },
                Price = 0,
                IsFeatured = true
            },
            new()
            {
                Id = "tour_003",
                Name = "Tâm linh Hà Nội",
                Description = "Tham quan các ngôi chùa cổ kính trong thành phố",
                ImageUrl = "tour_spiritual",
                Duration = 240,
                LocationIds = new List<string> { "loc_001", "loc_008", "loc_006" },
                Price = 0,
                IsFeatured = false
            },
            new()
            {
                Id = "tour_004",
                Name = "Làng nghề truyền thống",
                Description = "Khám phá làng gốm Bát Tràng và văn hóa thủ công",
                ImageUrl = "tour_craft",
                Duration = 300,
                LocationIds = new List<string> { "loc_010" },
                Price = 0,
                IsFeatured = false
            }
        };
    }
}
