using System.ComponentModel.DataAnnotations;

namespace VinhKhanhAudioGuide.Web.Models;

public class AppUser
{
    [Key]
    [MaxLength(100)]
    public string Id { get; set; } = string.Empty; // ID tạo ra khi quét QR code

    [MaxLength(200)]
    public string ScannedQrCode { get; set; } = string.Empty; // Mã QR đã quét để vào app

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public bool IsActive { get; set; } = true;

    // Navigations
    public ICollection<ListeningHistory> ListeningHistories { get; set; } = new List<ListeningHistory>();
    public ICollection<Feedback> Feedbacks { get; set; } = new List<Feedback>();
}
