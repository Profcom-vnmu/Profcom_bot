namespace StudentUnionBot.Domain.Enums;

/// <summary>
/// Represents the current state of user conversation with the bot
/// </summary>
public enum UserConversationState
{
    /// <summary>
    /// No active conversation, waiting for commands
    /// </summary>
    Idle = 0,

    /// <summary>
    /// Waiting for appeal category selection
    /// </summary>
    WaitingAppealCategory = 1,

    /// <summary>
    /// Waiting for appeal subject/title input
    /// </summary>
    WaitingAppealSubject = 2,

    /// <summary>
    /// Waiting for appeal message/description
    /// </summary>
    WaitingAppealMessage = 3,

    /// <summary>
    /// Waiting for appeal confirmation
    /// </summary>
    WaitingAppealConfirmation = 4,

    /// <summary>
    /// Waiting for email address input for verification
    /// </summary>
    WaitingEmailInput = 5,

    /// <summary>
    /// Waiting for email verification code input
    /// </summary>
    WaitingVerificationCode = 6,

    /// <summary>
    /// Waiting for custom close reason input (admin only)
    /// </summary>
    WaitingCloseReason = 7,

    /// <summary>
    /// Waiting for full name input
    /// </summary>
    WaitingFullNameInput = 8,

    /// <summary>
    /// Waiting for faculty input
    /// </summary>
    WaitingFacultyInput = 9,

    /// <summary>
    /// Waiting for course input
    /// </summary>
    WaitingCourseInput = 10,

    /// <summary>
    /// Waiting for group input
    /// </summary>
    WaitingGroupInput = 11,

    /// <summary>
    /// Waiting for admin reply message to appeal
    /// </summary>
    WaitingAdminReply = 12,

    /// <summary>
    /// Waiting for broadcast audience selection (admin only)
    /// </summary>
    WaitingBroadcastAudience = 13,

    /// <summary>
    /// Waiting for broadcast message text input (admin only)
    /// </summary>
    WaitingBroadcastMessage = 14,

    /// <summary>
    /// Waiting for broadcast confirmation (admin only)
    /// </summary>
    WaitingBroadcastConfirmation = 15,

    /// <summary>
    /// Waiting for custom email list input for broadcast (admin only)
    /// </summary>
    WaitingBroadcastCustomEmails = 16,

    /// <summary>
    /// Creating news: waiting for title input (admin only)
    /// </summary>
    CreatingNewsTitle = 17,

    /// <summary>
    /// Creating news: waiting for content input (admin only)
    /// </summary>
    CreatingNewsContent = 18,

    /// <summary>
    /// Editing news: waiting for new title input (admin only)
    /// </summary>
    EditingNewsTitle = 19,

    /// <summary>
    /// Editing news: waiting for new content input (admin only)
    /// </summary>
    EditingNewsContent = 20,

    /// <summary>
    /// Creating event: waiting for title input (admin only)
    /// </summary>
    CreatingEventTitle = 21,

    /// <summary>
    /// Creating event: waiting for description input (admin only)
    /// </summary>
    CreatingEventDescription = 22,

    /// <summary>
    /// Creating event: waiting for location input (admin only)
    /// </summary>
    CreatingEventLocation = 23,

    /// <summary>
    /// Creating event: waiting for date/time input (admin only)
    /// </summary>
    CreatingEventDateTime = 24
}
