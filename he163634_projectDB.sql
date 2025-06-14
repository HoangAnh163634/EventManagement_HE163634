USE [EventManagementDB]
GO
/****** Object:  Table [dbo].[Events]    Script Date: 07/06/2025 9:01:03 CH ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Events](
	[EventID] [int] IDENTITY(1,1) NOT NULL,
	[OrganizerID] [int] NOT NULL,
	[EventTypeID] [int] NOT NULL,
	[EventName] [nvarchar](200) NOT NULL,
	[Description] [nvarchar](max) NOT NULL,
	[StartDate] [datetime] NOT NULL,
	[EndDate] [datetime] NOT NULL,
	[Location] [nvarchar](255) NOT NULL,
	[IsPublic] [bit] NOT NULL,
	[MaxAttendees] [int] NULL,
	[Status] [nvarchar](20) NOT NULL,
	[CreatedAt] [datetime] NOT NULL,
	[UpdatedAt] [datetime] NULL,
PRIMARY KEY CLUSTERED 
(
	[EventID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  View [dbo].[EventStatusView]    Script Date: 07/06/2025 9:01:03 CH ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE VIEW [dbo].[EventStatusView] AS
SELECT 
    EventID,
    EventName,
    StartDate,
    EndDate,
    Location,
    IsPublic,
    MaxAttendees,
    CASE
        WHEN GETDATE() < StartDate THEN 'Upcoming'
        WHEN GETDATE() BETWEEN StartDate AND EndDate THEN 'Ongoing'
        ELSE 'Past'
    END AS Status,
    OrganizerID,
    EventTypeID
FROM [dbo].[Events];
GO
/****** Object:  Table [dbo].[CalendarSyncs]    Script Date: 07/06/2025 9:01:03 CH ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[CalendarSyncs](
	[SyncID] [int] IDENTITY(1,1) NOT NULL,
	[UserID] [int] NOT NULL,
	[EventID] [int] NOT NULL,
	[GoogleCalendarEventID] [nvarchar](255) NULL,
	[LastSyncedAt] [datetime] NOT NULL,
	[SyncStatus] [nvarchar](20) NOT NULL,
	[ErrorMessage] [nvarchar](max) NULL,
PRIMARY KEY CLUSTERED 
(
	[SyncID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[EventTypes]    Script Date: 07/06/2025 9:01:03 CH ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[EventTypes](
	[EventTypeID] [int] IDENTITY(1,1) NOT NULL,
	[EventTypeName] [nvarchar](100) NOT NULL,
	[Description] [nvarchar](max) NULL,
PRIMARY KEY CLUSTERED 
(
	[EventTypeID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Feedback]    Script Date: 07/06/2025 9:01:03 CH ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Feedback](
	[FeedbackID] [int] IDENTITY(1,1) NOT NULL,
	[RegistrationID] [int] NOT NULL,
	[Rating] [int] NULL,
	[Comments] [nvarchar](max) NULL,
	[SubmittedAt] [datetime] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[FeedbackID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Notifications]    Script Date: 07/06/2025 9:01:03 CH ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Notifications](
	[NotificationID] [int] IDENTITY(1,1) NOT NULL,
	[UserID] [int] NOT NULL,
	[EventID] [int] NULL,
	[NotificationType] [nvarchar](50) NOT NULL,
	[SentAt] [datetime] NOT NULL,
	[Subject] [nvarchar](255) NOT NULL,
	[Body] [nvarchar](max) NOT NULL,
	[Status] [nvarchar](20) NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[NotificationID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[QRCode]    Script Date: 07/06/2025 9:01:03 CH ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[QRCode](
	[QRCodeID] [int] IDENTITY(1,1) NOT NULL,
	[RegistrationID] [int] NOT NULL,
	[QRCodeValue] [nvarchar](255) NOT NULL,
	[GeneratedAt] [datetime] NOT NULL,
	[IsActive] [bit] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[QRCodeID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Registrations]    Script Date: 07/06/2025 9:01:03 CH ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Registrations](
	[RegistrationID] [int] IDENTITY(1,1) NOT NULL,
	[EventID] [int] NOT NULL,
	[AttendeeID] [int] NOT NULL,
	[RegistrationDate] [datetime] NOT NULL,
	[CheckInTime] [datetime] NULL,
	[Status] [nvarchar](20) NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[RegistrationID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Roles]    Script Date: 07/06/2025 9:01:03 CH ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Roles](
	[RoleID] [int] IDENTITY(1,1) NOT NULL,
	[RoleName] [nvarchar](50) NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[RoleID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[SocialShares]    Script Date: 07/06/2025 9:01:03 CH ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[SocialShares](
	[ShareID] [int] IDENTITY(1,1) NOT NULL,
	[EventID] [int] NOT NULL,
	[UserID] [int] NULL,
	[Platform] [nvarchar](50) NOT NULL,
	[SharedAt] [datetime] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[ShareID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Users]    Script Date: 07/06/2025 9:01:03 CH ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Users](
	[UserID] [int] IDENTITY(1,1) NOT NULL,
	[FullName] [nvarchar](100) NOT NULL,
	[Email] [nvarchar](100) NOT NULL,
	[PasswordHash] [nvarchar](255) NOT NULL,
	[PhoneNumber] [nvarchar](20) NULL,
	[RoleID] [int] NOT NULL,
	[CreatedAt] [datetime] NULL,
	[IsActive] [bit] NULL,
PRIMARY KEY CLUSTERED 
(
	[UserID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
SET IDENTITY_INSERT [dbo].[CalendarSyncs] ON 

INSERT [dbo].[CalendarSyncs] ([SyncID], [UserID], [EventID], [GoogleCalendarEventID], [LastSyncedAt], [SyncStatus], [ErrorMessage]) VALUES (1, 6, 3, N'google_event_001', CAST(N'2025-06-02T20:49:49.813' AS DateTime), N'Success', NULL)
INSERT [dbo].[CalendarSyncs] ([SyncID], [UserID], [EventID], [GoogleCalendarEventID], [LastSyncedAt], [SyncStatus], [ErrorMessage]) VALUES (2, 7, 3, N'google_event_002', CAST(N'2025-06-03T20:49:49.813' AS DateTime), N'Success', NULL)
INSERT [dbo].[CalendarSyncs] ([SyncID], [UserID], [EventID], [GoogleCalendarEventID], [LastSyncedAt], [SyncStatus], [ErrorMessage]) VALUES (3, 2, 4, N'google_event_003', CAST(N'2025-06-05T20:49:49.813' AS DateTime), N'Success', NULL)
SET IDENTITY_INSERT [dbo].[CalendarSyncs] OFF
GO
SET IDENTITY_INSERT [dbo].[Events] ON 

INSERT [dbo].[Events] ([EventID], [OrganizerID], [EventTypeID], [EventName], [Description], [StartDate], [EndDate], [Location], [IsPublic], [MaxAttendees], [Status], [CreatedAt], [UpdatedAt]) VALUES (1, 2, 1, N'Tech Conference 2024', N'Annual technology conference featuring latest innovations', CAST(N'2025-05-08T20:49:49.743' AS DateTime), CAST(N'2025-05-09T04:49:49.743' AS DateTime), N'Hanoi Convention Center', 1, 200, N'Past', CAST(N'2025-04-08T20:49:49.743' AS DateTime), CAST(N'2025-05-08T20:49:49.743' AS DateTime))
INSERT [dbo].[Events] ([EventID], [OrganizerID], [EventTypeID], [EventName], [Description], [StartDate], [EndDate], [Location], [IsPublic], [MaxAttendees], [Status], [CreatedAt], [UpdatedAt]) VALUES (2, 2, 2, N'Web Development Workshop', N'Hands-on workshop for web developers', CAST(N'2025-06-07T18:49:49.743' AS DateTime), CAST(N'2025-06-08T00:49:49.743' AS DateTime), N'Tech Hub Hanoi', 1, 50, N'Ongoing', CAST(N'2025-05-23T20:49:49.743' AS DateTime), CAST(N'2025-06-06T20:49:49.743' AS DateTime))
INSERT [dbo].[Events] ([EventID], [OrganizerID], [EventTypeID], [EventName], [Description], [StartDate], [EndDate], [Location], [IsPublic], [MaxAttendees], [Status], [CreatedAt], [UpdatedAt]) VALUES (3, 3, 3, N'Summer Music Festival', N'Annual summer music festival with local artists', CAST(N'2025-06-14T20:49:49.743' AS DateTime), CAST(N'2025-06-15T20:49:49.743' AS DateTime), N'Central Park', 1, 1000, N'Upcoming', CAST(N'2025-05-18T20:49:49.743' AS DateTime), CAST(N'2025-06-07T20:49:49.743' AS DateTime))
INSERT [dbo].[Events] ([EventID], [OrganizerID], [EventTypeID], [EventName], [Description], [StartDate], [EndDate], [Location], [IsPublic], [MaxAttendees], [Status], [CreatedAt], [UpdatedAt]) VALUES (4, 2, 4, N'Company Annual Party', N'Annual celebration for company employees', CAST(N'2025-06-21T20:49:49.743' AS DateTime), CAST(N'2025-06-22T02:49:49.743' AS DateTime), N'Grand Hotel', 0, 100, N'Upcoming', CAST(N'2025-05-28T20:49:49.743' AS DateTime), CAST(N'2025-06-07T20:49:49.743' AS DateTime))
INSERT [dbo].[Events] ([EventID], [OrganizerID], [EventTypeID], [EventName], [Description], [StartDate], [EndDate], [Location], [IsPublic], [MaxAttendees], [Status], [CreatedAt], [UpdatedAt]) VALUES (5, 3, 5, N'Art Exhibition', N'Contemporary art exhibition featuring local artists', CAST(N'2025-06-28T20:49:49.743' AS DateTime), CAST(N'2025-06-30T20:49:49.743' AS DateTime), N'Modern Art Museum', 1, 200, N'Upcoming', CAST(N'2025-06-02T20:49:49.743' AS DateTime), CAST(N'2025-06-07T20:49:49.743' AS DateTime))
SET IDENTITY_INSERT [dbo].[Events] OFF
GO
SET IDENTITY_INSERT [dbo].[EventTypes] ON 

INSERT [dbo].[EventTypes] ([EventTypeID], [EventTypeName], [Description]) VALUES (1, N'Conference', N'Professional conferences and seminars')
INSERT [dbo].[EventTypes] ([EventTypeID], [EventTypeName], [Description]) VALUES (2, N'Workshop', N'Hands-on training and learning sessions')
INSERT [dbo].[EventTypes] ([EventTypeID], [EventTypeName], [Description]) VALUES (3, N'Concert', N'Musical performances and shows')
INSERT [dbo].[EventTypes] ([EventTypeID], [EventTypeName], [Description]) VALUES (4, N'Party', N'Social gatherings and celebrations')
INSERT [dbo].[EventTypes] ([EventTypeID], [EventTypeName], [Description]) VALUES (5, N'Exhibition', N'Art and product showcases')
SET IDENTITY_INSERT [dbo].[EventTypes] OFF
GO
SET IDENTITY_INSERT [dbo].[Feedback] ON 

INSERT [dbo].[Feedback] ([FeedbackID], [RegistrationID], [Rating], [Comments], [SubmittedAt]) VALUES (1, 1, 5, N'Great conference with valuable insights!', CAST(N'2025-05-09T20:49:49.793' AS DateTime))
INSERT [dbo].[Feedback] ([FeedbackID], [RegistrationID], [Rating], [Comments], [SubmittedAt]) VALUES (2, 2, 4, N'Good content but could improve on time management', CAST(N'2025-05-10T20:49:49.793' AS DateTime))
INSERT [dbo].[Feedback] ([FeedbackID], [RegistrationID], [Rating], [Comments], [SubmittedAt]) VALUES (3, 3, 5, N'Excellent speakers and well-organized event', CAST(N'2025-05-11T20:49:49.793' AS DateTime))
SET IDENTITY_INSERT [dbo].[Feedback] OFF
GO
SET IDENTITY_INSERT [dbo].[Notifications] ON 

INSERT [dbo].[Notifications] ([NotificationID], [UserID], [EventID], [NotificationType], [SentAt], [Subject], [Body], [Status]) VALUES (1, 6, 1, N'Confirmation', CAST(N'2025-04-23T20:49:49.803' AS DateTime), N'Tech Conference Registration', N'Thank you for registering!', N'Sent')
INSERT [dbo].[Notifications] ([NotificationID], [UserID], [EventID], [NotificationType], [SentAt], [Subject], [Body], [Status]) VALUES (2, 6, 2, N'Reminder', CAST(N'2025-06-07T17:49:49.803' AS DateTime), N'Web Workshop Reminder', N'Join us today!', N'Sent')
INSERT [dbo].[Notifications] ([NotificationID], [UserID], [EventID], [NotificationType], [SentAt], [Subject], [Body], [Status]) VALUES (3, 6, 3, N'Confirmation', CAST(N'2025-06-02T20:49:49.803' AS DateTime), N'Music Festival Registration', N'Thank you for registering!', N'Sent')
INSERT [dbo].[Notifications] ([NotificationID], [UserID], [EventID], [NotificationType], [SentAt], [Subject], [Body], [Status]) VALUES (4, 7, 3, N'Confirmation', CAST(N'2025-06-03T20:49:49.803' AS DateTime), N'Music Festival Registration', N'Thank you for registering!', N'Sent')
INSERT [dbo].[Notifications] ([NotificationID], [UserID], [EventID], [NotificationType], [SentAt], [Subject], [Body], [Status]) VALUES (5, 8, 3, N'Confirmation', CAST(N'2025-06-04T20:49:49.803' AS DateTime), N'Music Festival Registration', N'Thank you for registering!', N'Sent')
SET IDENTITY_INSERT [dbo].[Notifications] OFF
GO
SET IDENTITY_INSERT [dbo].[QRCode] ON 

INSERT [dbo].[QRCode] ([QRCodeID], [RegistrationID], [QRCodeValue], [GeneratedAt], [IsActive]) VALUES (1, 1, N'QR_TECH_CONF_001', CAST(N'2025-04-23T20:49:49.780' AS DateTime), 0)
INSERT [dbo].[QRCode] ([QRCodeID], [RegistrationID], [QRCodeValue], [GeneratedAt], [IsActive]) VALUES (2, 2, N'QR_TECH_CONF_002', CAST(N'2025-04-24T20:49:49.780' AS DateTime), 0)
INSERT [dbo].[QRCode] ([QRCodeID], [RegistrationID], [QRCodeValue], [GeneratedAt], [IsActive]) VALUES (3, 3, N'QR_TECH_CONF_003', CAST(N'2025-04-25T20:49:49.780' AS DateTime), 1)
INSERT [dbo].[QRCode] ([QRCodeID], [RegistrationID], [QRCodeValue], [GeneratedAt], [IsActive]) VALUES (4, 4, N'QR_WEB_DEV_001', CAST(N'2025-05-28T20:49:49.780' AS DateTime), 0)
INSERT [dbo].[QRCode] ([QRCodeID], [RegistrationID], [QRCodeValue], [GeneratedAt], [IsActive]) VALUES (5, 5, N'QR_WEB_DEV_002', CAST(N'2025-05-29T20:49:49.780' AS DateTime), 1)
INSERT [dbo].[QRCode] ([QRCodeID], [RegistrationID], [QRCodeValue], [GeneratedAt], [IsActive]) VALUES (6, 6, N'QR_MUSIC_FEST_001', CAST(N'2025-06-02T20:49:49.780' AS DateTime), 1)
INSERT [dbo].[QRCode] ([QRCodeID], [RegistrationID], [QRCodeValue], [GeneratedAt], [IsActive]) VALUES (7, 7, N'QR_MUSIC_FEST_002', CAST(N'2025-06-03T20:49:49.780' AS DateTime), 1)
INSERT [dbo].[QRCode] ([QRCodeID], [RegistrationID], [QRCodeValue], [GeneratedAt], [IsActive]) VALUES (8, 8, N'QR_MUSIC_FEST_003', CAST(N'2025-06-04T20:49:49.780' AS DateTime), 1)
INSERT [dbo].[QRCode] ([QRCodeID], [RegistrationID], [QRCodeValue], [GeneratedAt], [IsActive]) VALUES (9, 9, N'QR_PARTY_001', CAST(N'2025-06-05T20:49:49.780' AS DateTime), 1)
INSERT [dbo].[QRCode] ([QRCodeID], [RegistrationID], [QRCodeValue], [GeneratedAt], [IsActive]) VALUES (10, 10, N'QR_PARTY_002', CAST(N'2025-06-06T20:49:49.780' AS DateTime), 1)
INSERT [dbo].[QRCode] ([QRCodeID], [RegistrationID], [QRCodeValue], [GeneratedAt], [IsActive]) VALUES (11, 11, N'QR_ART_001', CAST(N'2025-06-07T20:49:49.780' AS DateTime), 1)
SET IDENTITY_INSERT [dbo].[QRCode] OFF
GO
SET IDENTITY_INSERT [dbo].[Registrations] ON 

INSERT [dbo].[Registrations] ([RegistrationID], [EventID], [AttendeeID], [RegistrationDate], [CheckInTime], [Status]) VALUES (1, 1, 6, CAST(N'2025-04-23T20:49:49.753' AS DateTime), CAST(N'2025-05-08T20:49:49.753' AS DateTime), N'CheckedIn')
INSERT [dbo].[Registrations] ([RegistrationID], [EventID], [AttendeeID], [RegistrationDate], [CheckInTime], [Status]) VALUES (2, 1, 7, CAST(N'2025-04-24T20:49:49.753' AS DateTime), CAST(N'2025-05-08T20:49:49.753' AS DateTime), N'CheckedIn')
INSERT [dbo].[Registrations] ([RegistrationID], [EventID], [AttendeeID], [RegistrationDate], [CheckInTime], [Status]) VALUES (3, 1, 8, CAST(N'2025-04-25T20:49:49.753' AS DateTime), NULL, N'Registered')
INSERT [dbo].[Registrations] ([RegistrationID], [EventID], [AttendeeID], [RegistrationDate], [CheckInTime], [Status]) VALUES (4, 2, 6, CAST(N'2025-05-28T20:49:49.753' AS DateTime), CAST(N'2025-06-07T19:49:49.753' AS DateTime), N'CheckedIn')
INSERT [dbo].[Registrations] ([RegistrationID], [EventID], [AttendeeID], [RegistrationDate], [CheckInTime], [Status]) VALUES (5, 2, 7, CAST(N'2025-05-29T20:49:49.753' AS DateTime), NULL, N'Registered')
INSERT [dbo].[Registrations] ([RegistrationID], [EventID], [AttendeeID], [RegistrationDate], [CheckInTime], [Status]) VALUES (6, 3, 6, CAST(N'2025-06-02T20:49:49.753' AS DateTime), NULL, N'Registered')
INSERT [dbo].[Registrations] ([RegistrationID], [EventID], [AttendeeID], [RegistrationDate], [CheckInTime], [Status]) VALUES (7, 3, 7, CAST(N'2025-06-03T20:49:49.753' AS DateTime), NULL, N'Registered')
INSERT [dbo].[Registrations] ([RegistrationID], [EventID], [AttendeeID], [RegistrationDate], [CheckInTime], [Status]) VALUES (8, 3, 8, CAST(N'2025-06-04T20:49:49.753' AS DateTime), NULL, N'Registered')
INSERT [dbo].[Registrations] ([RegistrationID], [EventID], [AttendeeID], [RegistrationDate], [CheckInTime], [Status]) VALUES (9, 4, 6, CAST(N'2025-06-05T20:49:49.753' AS DateTime), NULL, N'Registered')
INSERT [dbo].[Registrations] ([RegistrationID], [EventID], [AttendeeID], [RegistrationDate], [CheckInTime], [Status]) VALUES (10, 4, 7, CAST(N'2025-06-06T20:49:49.753' AS DateTime), NULL, N'Registered')
INSERT [dbo].[Registrations] ([RegistrationID], [EventID], [AttendeeID], [RegistrationDate], [CheckInTime], [Status]) VALUES (11, 5, 6, CAST(N'2025-06-07T20:49:49.753' AS DateTime), NULL, N'Registered')
SET IDENTITY_INSERT [dbo].[Registrations] OFF
GO
SET IDENTITY_INSERT [dbo].[Roles] ON 

INSERT [dbo].[Roles] ([RoleID], [RoleName]) VALUES (1, N'Admin')
INSERT [dbo].[Roles] ([RoleID], [RoleName]) VALUES (3, N'Attendee')
INSERT [dbo].[Roles] ([RoleID], [RoleName]) VALUES (2, N'Organizer')
INSERT [dbo].[Roles] ([RoleID], [RoleName]) VALUES (4, N'Staff')
SET IDENTITY_INSERT [dbo].[Roles] OFF
GO
SET IDENTITY_INSERT [dbo].[SocialShares] ON 

INSERT [dbo].[SocialShares] ([ShareID], [EventID], [UserID], [Platform], [SharedAt]) VALUES (1, 3, 6, N'Facebook', CAST(N'2025-06-02T20:49:49.810' AS DateTime))
INSERT [dbo].[SocialShares] ([ShareID], [EventID], [UserID], [Platform], [SharedAt]) VALUES (2, 3, NULL, N'Twitter', CAST(N'2025-06-03T20:49:49.810' AS DateTime))
INSERT [dbo].[SocialShares] ([ShareID], [EventID], [UserID], [Platform], [SharedAt]) VALUES (3, 4, 7, N'WhatsApp', CAST(N'2025-06-05T20:49:49.810' AS DateTime))
SET IDENTITY_INSERT [dbo].[SocialShares] OFF
GO
SET IDENTITY_INSERT [dbo].[Users] ON 

INSERT [dbo].[Users] ([UserID], [FullName], [Email], [PasswordHash], [PhoneNumber], [RoleID], [CreatedAt], [IsActive]) VALUES (1, N'Admin User', N'admin@example.com', N'240be518fabd2724ddb6f04eeb1da5967448d7e831c08c8fa822809f74c720a9', N'+84123456789', 1, CAST(N'2025-06-07T20:49:49.727' AS DateTime), 1)
INSERT [dbo].[Users] ([UserID], [FullName], [Email], [PasswordHash], [PhoneNumber], [RoleID], [CreatedAt], [IsActive]) VALUES (2, N'John Organizer', N'john@example.com', N'240be518fabd2724ddb6f04eeb1da5967448d7e831c08c8fa822809f74c720a9', N'+84987654321', 2, CAST(N'2025-06-07T20:49:49.727' AS DateTime), 1)
INSERT [dbo].[Users] ([UserID], [FullName], [Email], [PasswordHash], [PhoneNumber], [RoleID], [CreatedAt], [IsActive]) VALUES (3, N'Sarah Organizer', N'sarah@example.com', N'240be518fabd2724ddb6f04eeb1da5967448d7e831c08c8fa822809f74c720a9', N'+84987654322', 2, CAST(N'2025-06-07T20:49:49.727' AS DateTime), 1)
INSERT [dbo].[Users] ([UserID], [FullName], [Email], [PasswordHash], [PhoneNumber], [RoleID], [CreatedAt], [IsActive]) VALUES (4, N'Mike Staff', N'mike@example.com', N'240be518fabd2724ddb6f04eeb1da5967448d7e831c08c8fa822809f74c720a9', N'+84987654323', 4, CAST(N'2025-06-07T20:49:49.727' AS DateTime), 1)
INSERT [dbo].[Users] ([UserID], [FullName], [Email], [PasswordHash], [PhoneNumber], [RoleID], [CreatedAt], [IsActive]) VALUES (5, N'Lisa Staff', N'lisa@example.com', N'240be518fabd2724ddb6f04eeb1da5967448d7e831c08c8fa822809f74c720a9', N'+84987654324', 4, CAST(N'2025-06-07T20:49:49.727' AS DateTime), 1)
INSERT [dbo].[Users] ([UserID], [FullName], [Email], [PasswordHash], [PhoneNumber], [RoleID], [CreatedAt], [IsActive]) VALUES (6, N'Alice Attendee', N'alice@example.com', N'240be518fabd2724ddb6f04eeb1da5967448d7e831c08c8fa822809f74c720a9', N'+84987654325', 3, CAST(N'2025-06-07T20:49:49.727' AS DateTime), 1)
INSERT [dbo].[Users] ([UserID], [FullName], [Email], [PasswordHash], [PhoneNumber], [RoleID], [CreatedAt], [IsActive]) VALUES (7, N'Bob Attendee', N'bob@example.com', N'240be518fabd2724ddb6f04eeb1da5967448d7e831c08c8fa822809f74c720a9', N'+84987654326', 3, CAST(N'2025-06-07T20:49:49.727' AS DateTime), 1)
INSERT [dbo].[Users] ([UserID], [FullName], [Email], [PasswordHash], [PhoneNumber], [RoleID], [CreatedAt], [IsActive]) VALUES (8, N'Charlie Attendee', N'charlie@example.com', N'240be518fabd2724ddb6f04eeb1da5967448d7e831c08c8fa822809f74c720a9', N'+84987654327', 3, CAST(N'2025-06-07T20:49:49.727' AS DateTime), 1)
SET IDENTITY_INSERT [dbo].[Users] OFF
GO
/****** Object:  Index [UQ_User_Event_CalendarSync]    Script Date: 07/06/2025 9:01:03 CH ******/
ALTER TABLE [dbo].[CalendarSyncs] ADD  CONSTRAINT [UQ_User_Event_CalendarSync] UNIQUE NONCLUSTERED 
(
	[UserID] ASC,
	[EventID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_Events_StartDate]    Script Date: 07/06/2025 9:01:03 CH ******/
CREATE NONCLUSTERED INDEX [IX_Events_StartDate] ON [dbo].[Events]
(
	[StartDate] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [UQ_EventTypeName]    Script Date: 07/06/2025 9:01:03 CH ******/
ALTER TABLE [dbo].[EventTypes] ADD  CONSTRAINT [UQ_EventTypeName] UNIQUE NONCLUSTERED 
(
	[EventTypeName] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [UQ_Feedback_RegistrationID]    Script Date: 07/06/2025 9:01:03 CH ******/
ALTER TABLE [dbo].[Feedback] ADD  CONSTRAINT [UQ_Feedback_RegistrationID] UNIQUE NONCLUSTERED 
(
	[RegistrationID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_Notifications_UserID]    Script Date: 07/06/2025 9:01:03 CH ******/
CREATE NONCLUSTERED INDEX [IX_Notifications_UserID] ON [dbo].[Notifications]
(
	[UserID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [UQ_QRCodeValue]    Script Date: 07/06/2025 9:01:03 CH ******/
ALTER TABLE [dbo].[QRCode] ADD  CONSTRAINT [UQ_QRCodeValue] UNIQUE NONCLUSTERED 
(
	[QRCodeValue] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [UQ_RegistrationID]    Script Date: 07/06/2025 9:01:03 CH ******/
ALTER TABLE [dbo].[QRCode] ADD  CONSTRAINT [UQ_RegistrationID] UNIQUE NONCLUSTERED 
(
	[RegistrationID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [UQ_Event_Attendee]    Script Date: 07/06/2025 9:01:03 CH ******/
ALTER TABLE [dbo].[Registrations] ADD  CONSTRAINT [UQ_Event_Attendee] UNIQUE NONCLUSTERED 
(
	[EventID] ASC,
	[AttendeeID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_Registrations_EventID]    Script Date: 07/06/2025 9:01:03 CH ******/
CREATE NONCLUSTERED INDEX [IX_Registrations_EventID] ON [dbo].[Registrations]
(
	[EventID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [UQ_RoleName]    Script Date: 07/06/2025 9:01:03 CH ******/
ALTER TABLE [dbo].[Roles] ADD  CONSTRAINT [UQ_RoleName] UNIQUE NONCLUSTERED 
(
	[RoleName] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [UQ_Email]    Script Date: 07/06/2025 9:01:03 CH ******/
ALTER TABLE [dbo].[Users] ADD  CONSTRAINT [UQ_Email] UNIQUE NONCLUSTERED 
(
	[Email] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
ALTER TABLE [dbo].[CalendarSyncs] ADD  DEFAULT (getdate()) FOR [LastSyncedAt]
GO
ALTER TABLE [dbo].[Events] ADD  DEFAULT ((1)) FOR [IsPublic]
GO
ALTER TABLE [dbo].[Events] ADD  DEFAULT ('Upcoming') FOR [Status]
GO
ALTER TABLE [dbo].[Events] ADD  DEFAULT (getdate()) FOR [CreatedAt]
GO
ALTER TABLE [dbo].[Feedback] ADD  DEFAULT (getdate()) FOR [SubmittedAt]
GO
ALTER TABLE [dbo].[Notifications] ADD  DEFAULT (getdate()) FOR [SentAt]
GO
ALTER TABLE [dbo].[QRCode] ADD  DEFAULT (getdate()) FOR [GeneratedAt]
GO
ALTER TABLE [dbo].[QRCode] ADD  DEFAULT ((1)) FOR [IsActive]
GO
ALTER TABLE [dbo].[Registrations] ADD  DEFAULT (getdate()) FOR [RegistrationDate]
GO
ALTER TABLE [dbo].[Registrations] ADD  DEFAULT ('Registered') FOR [Status]
GO
ALTER TABLE [dbo].[SocialShares] ADD  DEFAULT (getdate()) FOR [SharedAt]
GO
ALTER TABLE [dbo].[Users] ADD  DEFAULT (getdate()) FOR [CreatedAt]
GO
ALTER TABLE [dbo].[Users] ADD  DEFAULT ((1)) FOR [IsActive]
GO
ALTER TABLE [dbo].[CalendarSyncs]  WITH CHECK ADD  CONSTRAINT [FK_CalendarSyncs_Events] FOREIGN KEY([EventID])
REFERENCES [dbo].[Events] ([EventID])
GO
ALTER TABLE [dbo].[CalendarSyncs] CHECK CONSTRAINT [FK_CalendarSyncs_Events]
GO
ALTER TABLE [dbo].[CalendarSyncs]  WITH CHECK ADD  CONSTRAINT [FK_CalendarSyncs_Users] FOREIGN KEY([UserID])
REFERENCES [dbo].[Users] ([UserID])
GO
ALTER TABLE [dbo].[CalendarSyncs] CHECK CONSTRAINT [FK_CalendarSyncs_Users]
GO
ALTER TABLE [dbo].[Events]  WITH CHECK ADD  CONSTRAINT [FK_Events_EventTypes] FOREIGN KEY([EventTypeID])
REFERENCES [dbo].[EventTypes] ([EventTypeID])
GO
ALTER TABLE [dbo].[Events] CHECK CONSTRAINT [FK_Events_EventTypes]
GO
ALTER TABLE [dbo].[Events]  WITH CHECK ADD  CONSTRAINT [FK_Events_Users_Organizer] FOREIGN KEY([OrganizerID])
REFERENCES [dbo].[Users] ([UserID])
GO
ALTER TABLE [dbo].[Events] CHECK CONSTRAINT [FK_Events_Users_Organizer]
GO
ALTER TABLE [dbo].[Feedback]  WITH CHECK ADD  CONSTRAINT [FK_Feedback_Registrations] FOREIGN KEY([RegistrationID])
REFERENCES [dbo].[Registrations] ([RegistrationID])
GO
ALTER TABLE [dbo].[Feedback] CHECK CONSTRAINT [FK_Feedback_Registrations]
GO
ALTER TABLE [dbo].[Notifications]  WITH CHECK ADD  CONSTRAINT [FK_Notifications_Events] FOREIGN KEY([EventID])
REFERENCES [dbo].[Events] ([EventID])
GO
ALTER TABLE [dbo].[Notifications] CHECK CONSTRAINT [FK_Notifications_Events]
GO
ALTER TABLE [dbo].[Notifications]  WITH CHECK ADD  CONSTRAINT [FK_Notifications_Users] FOREIGN KEY([UserID])
REFERENCES [dbo].[Users] ([UserID])
GO
ALTER TABLE [dbo].[Notifications] CHECK CONSTRAINT [FK_Notifications_Users]
GO
ALTER TABLE [dbo].[QRCode]  WITH CHECK ADD  CONSTRAINT [FK_QRCode_Registrations] FOREIGN KEY([RegistrationID])
REFERENCES [dbo].[Registrations] ([RegistrationID])
GO
ALTER TABLE [dbo].[QRCode] CHECK CONSTRAINT [FK_QRCode_Registrations]
GO
ALTER TABLE [dbo].[Registrations]  WITH CHECK ADD  CONSTRAINT [FK_Registrations_Events] FOREIGN KEY([EventID])
REFERENCES [dbo].[Events] ([EventID])
GO
ALTER TABLE [dbo].[Registrations] CHECK CONSTRAINT [FK_Registrations_Events]
GO
ALTER TABLE [dbo].[Registrations]  WITH CHECK ADD  CONSTRAINT [FK_Registrations_Users_Attendee] FOREIGN KEY([AttendeeID])
REFERENCES [dbo].[Users] ([UserID])
GO
ALTER TABLE [dbo].[Registrations] CHECK CONSTRAINT [FK_Registrations_Users_Attendee]
GO
ALTER TABLE [dbo].[SocialShares]  WITH CHECK ADD  CONSTRAINT [FK_SocialShares_Events] FOREIGN KEY([EventID])
REFERENCES [dbo].[Events] ([EventID])
GO
ALTER TABLE [dbo].[SocialShares] CHECK CONSTRAINT [FK_SocialShares_Events]
GO
ALTER TABLE [dbo].[SocialShares]  WITH CHECK ADD  CONSTRAINT [FK_SocialShares_Users] FOREIGN KEY([UserID])
REFERENCES [dbo].[Users] ([UserID])
GO
ALTER TABLE [dbo].[SocialShares] CHECK CONSTRAINT [FK_SocialShares_Users]
GO
ALTER TABLE [dbo].[Users]  WITH CHECK ADD  CONSTRAINT [FK_Users_Roles] FOREIGN KEY([RoleID])
REFERENCES [dbo].[Roles] ([RoleID])
GO
ALTER TABLE [dbo].[Users] CHECK CONSTRAINT [FK_Users_Roles]
GO
ALTER TABLE [dbo].[CalendarSyncs]  WITH CHECK ADD  CONSTRAINT [CK_SyncStatus] CHECK  (([SyncStatus]='Pending' OR [SyncStatus]='Failed' OR [SyncStatus]='Success'))
GO
ALTER TABLE [dbo].[CalendarSyncs] CHECK CONSTRAINT [CK_SyncStatus]
GO
ALTER TABLE [dbo].[Events]  WITH CHECK ADD  CONSTRAINT [CK_Event_Dates] CHECK  (([EndDate]>[StartDate]))
GO
ALTER TABLE [dbo].[Events] CHECK CONSTRAINT [CK_Event_Dates]
GO
ALTER TABLE [dbo].[Events]  WITH CHECK ADD  CONSTRAINT [CK_EventName] CHECK  ((len([EventName])>=(3)))
GO
ALTER TABLE [dbo].[Events] CHECK CONSTRAINT [CK_EventName]
GO
ALTER TABLE [dbo].[Events]  WITH CHECK ADD  CONSTRAINT [CK_Status] CHECK  (([Status]='Upcoming' OR [Status]='Ongoing' OR [Status]='Past' OR [Status]='Cancelled' OR [Status]='Draft'))
GO
ALTER TABLE [dbo].[Events] CHECK CONSTRAINT [CK_Status]
GO
ALTER TABLE [dbo].[Feedback]  WITH CHECK ADD  CONSTRAINT [CK_Rating] CHECK  (([Rating] IS NULL OR [Rating]>=(1) AND [Rating]<=(5)))
GO
ALTER TABLE [dbo].[Feedback] CHECK CONSTRAINT [CK_Rating]
GO
ALTER TABLE [dbo].[Notifications]  WITH CHECK ADD  CONSTRAINT [CK_Notification_Status] CHECK  (([Status]='Pending' OR [Status]='Failed' OR [Status]='Sent'))
GO
ALTER TABLE [dbo].[Notifications] CHECK CONSTRAINT [CK_Notification_Status]
GO
ALTER TABLE [dbo].[Notifications]  WITH CHECK ADD  CONSTRAINT [CK_NotificationType] CHECK  (([NotificationType]='AccountVerification' OR [NotificationType]='EventCancellation' OR [NotificationType]='EventUpdate' OR [NotificationType]='ThankYou' OR [NotificationType]='Reminder' OR [NotificationType]='Confirmation'))
GO
ALTER TABLE [dbo].[Notifications] CHECK CONSTRAINT [CK_NotificationType]
GO
ALTER TABLE [dbo].[Registrations]  WITH CHECK ADD  CONSTRAINT [CK_Registration_Status] CHECK  (([Status]='CancelledByOrganizer' OR [Status]='CancelledByUser' OR [Status]='CheckedIn' OR [Status]='Registered'))
GO
ALTER TABLE [dbo].[Registrations] CHECK CONSTRAINT [CK_Registration_Status]
GO
ALTER TABLE [dbo].[Roles]  WITH CHECK ADD  CONSTRAINT [CK_RoleName] CHECK  (([RoleName]='Staff' OR [RoleName]='Attendee' OR [RoleName]='Organizer' OR [RoleName]='Admin'))
GO
ALTER TABLE [dbo].[Roles] CHECK CONSTRAINT [CK_RoleName]
GO
ALTER TABLE [dbo].[SocialShares]  WITH CHECK ADD  CONSTRAINT [CK_Platform] CHECK  (([Platform]='Other' OR [Platform]='LinkedIn' OR [Platform]='WhatsApp' OR [Platform]='Twitter' OR [Platform]='Facebook'))
GO
ALTER TABLE [dbo].[SocialShares] CHECK CONSTRAINT [CK_Platform]
GO
ALTER TABLE [dbo].[Users]  WITH CHECK ADD  CONSTRAINT [CK_Email] CHECK  (([Email] like '%_@__%.__%'))
GO
ALTER TABLE [dbo].[Users] CHECK CONSTRAINT [CK_Email]
GO
ALTER TABLE [dbo].[Users]  WITH CHECK ADD  CONSTRAINT [CK_PhoneNumber] CHECK  (([PhoneNumber] IS NULL OR [PhoneNumber] like '[0-9]%' OR [PhoneNumber] like '+[0-9]%'))
GO
ALTER TABLE [dbo].[Users] CHECK CONSTRAINT [CK_PhoneNumber]
GO
/****** Object:  Trigger [dbo].[CheckFutureStartDate]    Script Date: 07/06/2025 9:01:03 CH ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TRIGGER [dbo].[CheckFutureStartDate]
ON [dbo].[Events]
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1 FROM inserted WHERE StartDate < GETDATE() AND Status NOT IN ('Past', 'Cancelled'))
    BEGIN
        RAISERROR ('New or updated events must have StartDate in the future unless Status is Past or Cancelled.', 16, 1);
        ROLLBACK;
    END;
END;
GO
ALTER TABLE [dbo].[Events] ENABLE TRIGGER [CheckFutureStartDate]
GO
/****** Object:  Trigger [dbo].[CheckMaxAttendees]    Script Date: 07/06/2025 9:01:03 CH ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TRIGGER [dbo].[CheckMaxAttendees]
ON [dbo].[Registrations]
AFTER INSERT
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @EventID INT, @MaxAttendees INT, @CurrentCount INT;
    SELECT @EventID = EventID FROM inserted;
    SELECT @MaxAttendees = MaxAttendees FROM [dbo].[Events] WHERE EventID = @EventID;
    SELECT @CurrentCount = COUNT(*) FROM [dbo].[Registrations] 
    WHERE EventID = @EventID AND Status IN ('Registered', 'CheckedIn');
    IF @MaxAttendees IS NOT NULL AND @CurrentCount > @MaxAttendees
    BEGIN
        RAISERROR ('Registration count exceeds MaxAttendees limit.', 16, 1);
        ROLLBACK;
    END;
END;
GO
ALTER TABLE [dbo].[Registrations] ENABLE TRIGGER [CheckMaxAttendees]
GO
