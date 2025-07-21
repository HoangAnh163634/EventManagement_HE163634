using System;
using System.Collections.Generic;

namespace EventManagement.Models.ViewModels;

public class AdminDashboardViewModel
{
    public int TotalUsers { get; set; }
    public int NewUsersToday { get; set; }
    public int NewUsersThisWeek { get; set; }
    public int NewUsersThisMonth { get; set; }
    public int TotalEvents { get; set; }
    public int NewEventsToday { get; set; }
    public int NewEventsThisWeek { get; set; }
    public int NewEventsThisMonth { get; set; }
    public int TotalRegistrations { get; set; }
    public int NewRegistrationsToday { get; set; }
    public int NewRegistrationsThisWeek { get; set; }
    public int NewRegistrationsThisMonth { get; set; }
    public int TotalFeedbacks { get; set; }
    public int NewFeedbacksToday { get; set; }
    public int NewFeedbacksThisWeek { get; set; }
    public int NewFeedbacksThisMonth { get; set; }
    public int UpcomingEvents { get; set; }
    public int OngoingEvents { get; set; }
    public int CompletedEvents { get; set; }
    public int CancelledEvents { get; set; }
    public Dictionary<string, int> EventStatusDistribution { get; set; } = new();
    public Dictionary<string, int> UserRoleDistribution { get; set; } = new();
    public List<ChartDataPoint> UserGrowth { get; set; } = new();
    public List<ChartDataPoint> EventGrowth { get; set; } = new();
    public List<ChartDataPoint> RegistrationGrowth { get; set; } = new();
    public List<TopEventItem> TopEvents { get; set; } = new();
    public List<TopOrganizerItem> TopOrganizers { get; set; } = new();
}

public class ChartDataPoint
{
    public DateTime Date { get; set; }
    public int Value { get; set; }
}

public class TopEventItem
{
    public int EventId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public int RegistrationCount { get; set; }
}

public class TopOrganizerItem
{
    public int UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public int EventCount { get; set; }
} 