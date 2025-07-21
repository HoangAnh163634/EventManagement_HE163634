using System.ComponentModel.DataAnnotations;

namespace EventManagement.Models.ViewModels;

public class RegistrationViewModel
{
    public int RegistrationId { get; set; }

    public int EventId { get; set; }

    public int AttendeeId { get; set; }

    [Display(Name = "Ngày đăng ký")]
    public DateTime RegistrationDate { get; set; }

    [Display(Name = "Yêu cầu đặc biệt")]
    [StringLength(1000, ErrorMessage = "Yêu cầu đặc biệt không được vượt quá 1000 ký tự")]
    public string? SpecialRequests { get; set; }

    [Display(Name = "Trạng thái")]
    public string Status { get; set; } = "Registered";

    // Navigation properties for display
    public string? EventName { get; set; }
    public string? AttendeeName { get; set; }
    public string? AttendeeEmail { get; set; }
    public DateTime? EventStartDate { get; set; }
    public DateTime? EventEndDate { get; set; }
    public string? EventLocation { get; set; }
    public decimal? EventPrice { get; set; }
    public string? EventCurrency { get; set; }

    // For QR code
    public string? QrCodeValue { get; set; }
    public string? QrCodeImageUrl { get; set; }

    // For check-in
    [Display(Name = "Thời gian check-in")]
    public DateTime? CheckInTime { get; set; }

    [Display(Name = "Phương thức check-in")]
    public string? CheckInMethod { get; set; }

    [Display(Name = "Địa điểm check-in")]
    public string? CheckInLocation { get; set; }

    // For cancellation
    [Display(Name = "Lý do hủy")]
    [StringLength(500, ErrorMessage = "Lý do hủy không được vượt quá 500 ký tự")]
    public string? CancellationReason { get; set; }

    // For feedback
    public bool HasFeedback { get; set; }
    public int? FeedbackRating { get; set; }
    public string? FeedbackComments { get; set; }
} 