using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace EventManagement.Models.ViewModels;

public class EventViewModel : IValidatableObject
{
    public int EventId { get; set; }

    [Required(ErrorMessage = "Tên sự kiện là bắt buộc")]
    [StringLength(200, ErrorMessage = "Tên sự kiện không được vượt quá 200 ký tự")]
    [Display(Name = "Tên sự kiện")]
    public string EventName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Mô tả là bắt buộc")]
    [Display(Name = "Mô tả")]
    public string Description { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ngày bắt đầu là bắt buộc")]
    [Display(Name = "Ngày bắt đầu")]
    [DataType(DataType.DateTime)]
    public DateTime StartDate { get; set; }

    [Required(ErrorMessage = "Ngày kết thúc là bắt buộc")]
    [Display(Name = "Ngày kết thúc")]
    [DataType(DataType.DateTime)]
    public DateTime EndDate { get; set; }

    [Required(ErrorMessage = "Địa điểm là bắt buộc")]
    [StringLength(255, ErrorMessage = "Địa điểm không được vượt quá 255 ký tự")]
    [Display(Name = "Địa điểm")]
    public string Location { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "Địa chỉ không được vượt quá 500 ký tự")]
    [Display(Name = "Địa chỉ chi tiết")]
    public string? Address { get; set; }

    [Display(Name = "Công khai")]
    public bool IsPublic { get; set; } = true;

    [Required(ErrorMessage = "Mức độ riêng tư là bắt buộc")]
    [Display(Name = "Mức độ riêng tư")]
    public string PrivacyLevel { get; set; } = "Public";

    [Display(Name = "Số lượng người tham gia tối đa")]
    [Range(1, int.MaxValue, ErrorMessage = "Số lượng người tham gia phải lớn hơn 0")]
    public int? MaxAttendees { get; set; }

    [Required(ErrorMessage = "Trạng thái là bắt buộc")]
    [Display(Name = "Trạng thái")]
    public string Status { get; set; } = "Upcoming";

    [Display(Name = "Hạn đăng ký")]
    [DataType(DataType.DateTime)]
    public DateTime? RegistrationDeadline { get; set; }

    [Display(Name = "Giá vé")]
    [Range(0, double.MaxValue, ErrorMessage = "Giá vé không được âm")]
    [DataType(DataType.Currency)]
    public decimal? Price { get; set; }

    [Display(Name = "Đơn vị tiền")]
    [StringLength(3)]
    public string Currency { get; set; } = "VND";

    [Required(ErrorMessage = "Loại sự kiện là bắt buộc")]
    [Display(Name = "Loại sự kiện")]
    public int EventTypeId { get; set; }

    [Display(Name = "Ảnh banner")]
    [StringLength(500)]
    [DataType(DataType.ImageUrl)]
    public string? BannerImageUrl { get; set; }

    [Display(Name = "Tags")]
    [StringLength(500)]
    public string? Tags { get; set; }

    // Navigation properties for display
    public string? EventTypeName { get; set; }
    public string? OrganizerName { get; set; }
    public int? TotalRegistrations { get; set; }
    public int? TotalCheckedIn { get; set; }
    public decimal? AverageRating { get; set; }
    public int TotalFeedbacks { get; set; }

    // For file upload
    [Display(Name = "Banner Image")]
    public IFormFile? BannerImage { get; set; }

    // For dropdown lists
    public List<EventType>? EventTypes { get; set; }
    public List<string>? PrivacyLevels { get; set; } = new() { "Public", "Private", "Invitation Only" };
    public List<string>? Statuses { get; set; } = new() { "Upcoming", "Ongoing", "Completed", "Cancelled" };
    public List<string>? Currencies { get; set; } = new() { "VND", "USD" };

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        // Trim các trường text
        if (!string.IsNullOrEmpty(EventName)) EventName = EventName.Trim();
        if (!string.IsNullOrEmpty(Description)) Description = Description.Trim();
        if (!string.IsNullOrEmpty(Location)) Location = Location.Trim();
        if (!string.IsNullOrEmpty(Address)) Address = Address.Trim();
        if (!string.IsNullOrEmpty(Tags)) Tags = Tags.Trim();

        // Validate tên sự kiện: không chỉ số, không ký tự đặc biệt, không consecutive spaces
        if (!string.IsNullOrEmpty(EventName))
        {
            if (string.IsNullOrWhiteSpace(EventName))
                yield return new ValidationResult("Tên sự kiện không được để trống hoặc chỉ chứa khoảng trắng.", new[] { nameof(EventName) });
            else if (EventName.Length < 5)
                yield return new ValidationResult("Tên sự kiện phải có ít nhất 5 ký tự.", new[] { nameof(EventName) });
            else if (EventName.Contains("  "))
                yield return new ValidationResult("Tên sự kiện không được chứa nhiều khoảng trắng liên tiếp.", new[] { nameof(EventName) });
            else if (!System.Text.RegularExpressions.Regex.IsMatch(EventName, @"^[a-zA-ZÀ-ỹ0-9\s.,-]+$"))
                yield return new ValidationResult("Tên sự kiện chỉ được chứa chữ, số, khoảng trắng và một số ký tự ., -", new[] { nameof(EventName) });
        }

        // Validate mô tả
        if (string.IsNullOrWhiteSpace(Description))
            yield return new ValidationResult("Mô tả không được để trống hoặc chỉ chứa khoảng trắng.", new[] { nameof(Description) });
        else if (Description.Length < 10)
            yield return new ValidationResult("Mô tả phải có ít nhất 10 ký tự.", new[] { nameof(Description) });

        // Validate location
        if (string.IsNullOrWhiteSpace(Location))
            yield return new ValidationResult("Địa điểm không được để trống hoặc chỉ chứa khoảng trắng.", new[] { nameof(Location) });
        else if (Location.Length < 3)
            yield return new ValidationResult("Địa điểm phải có ít nhất 3 ký tự.", new[] { nameof(Location) });
        else if (Location.Contains("  "))
            yield return new ValidationResult("Địa điểm không được chứa nhiều khoảng trắng liên tiếp.", new[] { nameof(Location) });

        // Validate tags (nếu có)
        if (!string.IsNullOrEmpty(Tags))
        {
            if (Tags.Length > 200)
                yield return new ValidationResult("Tags không được vượt quá 200 ký tự.", new[] { nameof(Tags) });
            if (Tags.Contains(",,") || Tags.Contains(", ,"))
                yield return new ValidationResult("Tags không được chứa dấu phẩy liên tiếp hoặc dấu phẩy kèm khoảng trắng.", new[] { nameof(Tags) });
        }

        if (EndDate <= StartDate)
            yield return new ValidationResult("Ngày kết thúc phải sau ngày bắt đầu.", new[] { nameof(EndDate) });
        if (RegistrationDeadline.HasValue && RegistrationDeadline.Value >= StartDate)
            yield return new ValidationResult("Hạn đăng ký phải trước ngày bắt đầu.", new[] { nameof(RegistrationDeadline) });
        if (!MaxAttendees.HasValue || MaxAttendees.Value < 1)
            yield return new ValidationResult("Số lượng người tham gia phải lớn hơn 0.", new[] { nameof(MaxAttendees) });
        if (StartDate < DateTime.Now)
            yield return new ValidationResult("Ngày bắt đầu không được ở quá khứ.", new[] { nameof(StartDate) });
    }
} 