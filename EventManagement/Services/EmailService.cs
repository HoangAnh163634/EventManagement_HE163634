using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using MimeKit.Text;

namespace EventManagement.Services;

public class EmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<bool> SendEmailAsync(string toEmail, string subject, string htmlBody, string? toName = null)
    {
        try
        {
            var emailSettings = _configuration.GetSection("EmailSettings");
            var smtpServer = emailSettings["SmtpServer"];
            var smtpPort = int.Parse(emailSettings["SmtpPort"]!);
            var senderName = emailSettings["SenderName"];
            var senderEmail = emailSettings["SenderEmail"];
            var password = emailSettings["Password"];

            var email = new MimeMessage();
            email.From.Add(new MailboxAddress(senderName, senderEmail));
            email.To.Add(new MailboxAddress(toName ?? toEmail, toEmail));
            email.Subject = subject;
            email.Body = new TextPart(TextFormat.Html) { Text = htmlBody };

            using var smtp = new SmtpClient();
            await smtp.ConnectAsync(smtpServer, smtpPort, SecureSocketOptions.StartTls);
            await smtp.AuthenticateAsync(senderEmail, password);
            await smtp.SendAsync(email);
            await smtp.DisconnectAsync(true);

            _logger.LogInformation("Email sent successfully to {Email}", toEmail);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Email}", toEmail);
            return false;
        }
    }

    public async Task<bool> SendWelcomeEmailAsync(string toEmail, string fullName, string verificationToken)
    {
        var subject = "Chào mừng đến với Event Management System!";
        var verificationUrl = $"{_configuration["BaseUrl"]}/Account/VerifyEmail?token={verificationToken}";
        
        var htmlBody = $@"
            <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; background-color: #1a1a1a; color: #ffffff; border-radius: 10px; overflow: hidden;'>
                <div style='background: linear-gradient(135deg, #d4af37, #f4e4bc); padding: 30px; text-align: center;'>
                    <h1 style='color: #1a1a1a; margin: 0; font-size: 28px;'>🎉 Chào mừng {fullName}!</h1>
                </div>
                
                <div style='padding: 30px;'>
                    <h2 style='color: #d4af37; margin-top: 0;'>Cảm ơn bạn đã đăng ký!</h2>
                    <p style='font-size: 16px; line-height: 1.6; margin-bottom: 25px;'>
                        Chúng tôi rất vui khi bạn tham gia vào cộng đồng Event Management System. 
                        Để hoàn tất quá trình đăng ký, vui lòng xác thực email của bạn.
                    </p>
                    
                    <div style='text-align: center; margin: 30px 0;'>
                        <a href='{verificationUrl}' 
                           style='display: inline-block; background-color: #d4af37; color: #1a1a1a; 
                                  padding: 15px 30px; text-decoration: none; border-radius: 5px; 
                                  font-weight: bold; font-size: 16px;'>
                            ✅ Xác Thực Email
                        </a>
                    </div>
                    
                    <p style='font-size: 14px; color: #cccccc; margin-top: 25px;'>
                        Nếu bạn không thể click vào nút trên, copy và paste link sau vào trình duyệt:<br>
                        <span style='word-break: break-all; color: #d4af37;'>{verificationUrl}</span>
                    </p>
                    
                    <hr style='border: none; border-top: 1px solid #333; margin: 30px 0;'>
                    
                    <p style='font-size: 14px; color: #999; text-align: center; margin: 0;'>
                        Event Management System - HE163634<br>
                        Email này được gửi tự động, vui lòng không reply.
                    </p>
                </div>
            </div>";

        return await SendEmailAsync(toEmail, subject, htmlBody, fullName);
    }

    public async Task<bool> SendEventNotificationAsync(string toEmail, string fullName, string eventTitle, string eventDate, string eventLocation)
    {
        var subject = $"🎪 Nhắc nhở: Sự kiện '{eventTitle}' sắp diễn ra!";
        
        var htmlBody = $@"
            <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; background-color: #1a1a1a; color: #ffffff; border-radius: 10px; overflow: hidden;'>
                <div style='background: linear-gradient(135deg, #d4af37, #f4e4bc); padding: 30px; text-align: center;'>
                    <h1 style='color: #1a1a1a; margin: 0; font-size: 24px;'>🎪 Sự Kiện Sắp Diễn Ra</h1>
                </div>
                
                <div style='padding: 30px;'>
                    <h2 style='color: #d4af37; margin-top: 0;'>Xin chào {fullName}!</h2>
                    <p style='font-size: 16px; line-height: 1.6;'>
                        Chúng tôi muốn nhắc nhở bạn về sự kiện mà bạn đã đăng ký:
                    </p>
                    
                    <div style='background-color: #2a2a2a; padding: 20px; border-radius: 8px; border-left: 4px solid #d4af37; margin: 20px 0;'>
                        <h3 style='color: #d4af37; margin: 0 0 10px 0;'>📅 {eventTitle}</h3>
                        <p style='margin: 5px 0; font-size: 14px;'><strong>🕒 Thời gian:</strong> {eventDate}</p>
                        <p style='margin: 5px 0; font-size: 14px;'><strong>📍 Địa điểm:</strong> {eventLocation}</p>
                    </div>
                    
                    <p style='font-size: 16px; line-height: 1.6; color: #cccccc;'>
                        Hãy chuẩn bị sẵn sàng và đừng quên tham gia đúng giờ nhé! 🎉
                    </p>
                    
                    <hr style='border: none; border-top: 1px solid #333; margin: 30px 0;'>
                    
                    <p style='font-size: 14px; color: #999; text-align: center; margin: 0;'>
                        Event Management System - HE163634<br>
                        Email này được gửi tự động, vui lòng không reply.
                    </p>
                </div>
            </div>";

        return await SendEmailAsync(toEmail, subject, htmlBody, fullName);
    }

    public async Task<bool> SendPasswordResetEmailAsync(string toEmail, string fullName, string resetToken)
    {
        var subject = "🔐 Yêu cầu đặt lại mật khẩu";
        var resetUrl = $"{_configuration["BaseUrl"]}/Account/ResetPassword?token={resetToken}";
        
        var htmlBody = $@"
            <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; background-color: #1a1a1a; color: #ffffff; border-radius: 10px; overflow: hidden;'>
                <div style='background: linear-gradient(135deg, #d4af37, #f4e4bc); padding: 30px; text-align: center;'>
                    <h1 style='color: #1a1a1a; margin: 0; font-size: 24px;'>🔐 Đặt Lại Mật Khẩu</h1>
                </div>
                
                <div style='padding: 30px;'>
                    <h2 style='color: #d4af37; margin-top: 0;'>Xin chào {fullName}!</h2>
                    <p style='font-size: 16px; line-height: 1.6;'>
                        Chúng tôi nhận được yêu cầu đặt lại mật khẩu cho tài khoản của bạn.
                    </p>
                    
                    <div style='text-align: center; margin: 30px 0;'>
                        <a href='{resetUrl}' 
                           style='display: inline-block; background-color: #d4af37; color: #1a1a1a; 
                                  padding: 15px 30px; text-decoration: none; border-radius: 5px; 
                                  font-weight: bold; font-size: 16px;'>
                            🔑 Đặt Lại Mật Khẩu
                        </a>
                    </div>
                    
                    <div style='background-color: #2a2a2a; padding: 15px; border-radius: 8px; border-left: 4px solid #ff6b6b; margin: 20px 0;'>
                        <p style='margin: 0; font-size: 14px; color: #ffcccc;'>
                            ⚠️ <strong>Lưu ý:</strong> Link này sẽ hết hạn sau 1 giờ. 
                            Nếu bạn không yêu cầu đặt lại mật khẩu, vui lòng bỏ qua email này.
                        </p>
                    </div>
                    
                    <hr style='border: none; border-top: 1px solid #333; margin: 30px 0;'>
                    
                    <p style='font-size: 14px; color: #999; text-align: center; margin: 0;'>
                        Event Management System - HE163634<br>
                        Email này được gửi tự động, vui lòng không reply.
                    </p>
                </div>
            </div>";

        return await SendEmailAsync(toEmail, subject, htmlBody, fullName);
    }
} 