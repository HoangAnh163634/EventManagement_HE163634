using Google.Apis.Auth.OAuth2;
using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Auth.OAuth2.Flows;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace EventManagement.Services
{
    public class GoogleCalendarService
    {
        private readonly IConfiguration _configuration;
        private readonly string[] _scopes;
        private readonly string _clientId;
        private readonly string _clientSecret;
        private readonly string _redirectUri;

        public GoogleCalendarService(IConfiguration configuration)
        {
            _configuration = configuration;
            _clientId = _configuration["GoogleCalendar:ClientId"]!;
            _clientSecret = _configuration["GoogleCalendar:ClientSecret"]!;
            _redirectUri = _configuration["GoogleCalendar:RedirectUri"]!;
            _scopes = _configuration["GoogleCalendar:Scopes"]!.Split(' ');
        }

        public string GetAuthorizationUrl(string state)
        {
            var clientSecrets = new ClientSecrets
            {
                ClientId = _clientId,
                ClientSecret = _clientSecret
            };
            var codeFlow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
            {
                ClientSecrets = clientSecrets,
                Scopes = _scopes
            });
            var url = codeFlow.CreateAuthorizationCodeRequest(_redirectUri).Build().ToString();
            if (!string.IsNullOrEmpty(state))
            {
                url += (url.Contains("?") ? "&" : "?") + $"state={Uri.EscapeDataString(state)}";
            }
            return url;
        }

        public async Task<TokenResponse> ExchangeCodeForTokenAsync(string code, CancellationToken cancellationToken = default)
        {
            var clientSecrets = new ClientSecrets
            {
                ClientId = _clientId,
                ClientSecret = _clientSecret
            };
            var codeFlow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
            {
                ClientSecrets = clientSecrets,
                Scopes = _scopes
            });
            return await codeFlow.ExchangeCodeForTokenAsync("user", code, _redirectUri, CancellationToken.None);
        }

        public CalendarService GetCalendarService(string accessToken, string refreshToken)
        {
            var credential = GoogleCredential.FromAccessToken(accessToken);
            return new CalendarService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = "EventManagement"
            });
        }

        public async Task<Event> CreateGoogleEventAsync(string accessToken, Event googleEvent)
        {
            var credential = GoogleCredential.FromAccessToken(accessToken);
            var service = new CalendarService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = "EventManagement"
            });
            var createdEvent = await service.Events.Insert(googleEvent, "primary").ExecuteAsync();
            return createdEvent;
        }

        public async Task DeleteGoogleEventAsync(string accessToken, string refreshToken, string eventId)
        {
            var service = GetCalendarService(accessToken, refreshToken);
            var request = service.Events.Delete("primary", eventId);
            await request.ExecuteAsync();
        }

        public async Task<Event> UpdateGoogleEventAsync(string accessToken, string refreshToken, string eventId, Event @event)
        {
            var service = GetCalendarService(accessToken, refreshToken);
            var request = service.Events.Update(@event, "primary", eventId);
            return await request.ExecuteAsync();
        }
    }
} 