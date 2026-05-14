using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using VinhKhanhAudioGuide.Web.Data;
using VinhKhanhAudioGuide.Web.Models.Simulation;
using VinhKhanhAudioGuide.Web.Services.Simulation;

namespace VinhKhanhAudioGuide.Web.Pages.Admin;

public class SimulationModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly IPoiSimulationService _simulationService;

    public SimulationModel(AppDbContext db, IPoiSimulationService simulationService)
    {
        _db = db;
        _simulationService = simulationService;
    }

    // Dữ liệu cho Form chọn POI
    public List<LocationOption> Locations { get; set; } = new();
    public List<AudioOption> AudioGuides { get; set; } = new();

    // Trạng thái batch hiện tại
    public SimulationBatch? CurrentBatch => _simulationService.CurrentBatch;

    public async Task OnGetAsync()
    {
        Locations = await _db.Locations
            .AsNoTracking()
            .OrderBy(l => l.Name)
            .Select(l => new LocationOption { Id = l.Id, Name = l.Name })
            .ToListAsync();

        AudioGuides = await _db.AudioGuides
            .AsNoTracking()
            .OrderBy(a => a.Title)
            .Select(a => new AudioOption
            {
                Id = a.Id,
                Title = a.Title,
                LocationId = a.LocationId,
                DurationMinutes = a.Duration
            })
            .ToListAsync();
    }

    // AJAX endpoint lấy danh sách AudioGuide theo LocationId
    public async Task<IActionResult> OnGetAudiosByLocationAsync(string locationId)
    {
        var audios = await _db.AudioGuides
            .AsNoTracking()
            .Where(a => a.LocationId == locationId)
            .Select(a => new AudioOption
            {
                Id = a.Id,
                Title = a.Title,
                LocationId = a.LocationId,
                DurationMinutes = a.Duration
            })
            .ToListAsync();

        return new JsonResult(audios);
    }

    public class LocationOption
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }

    public class AudioOption
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string LocationId { get; set; } = string.Empty;
        public int DurationMinutes { get; set; }
    }
}
