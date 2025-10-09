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
    WaitingGroupInput = 11
}
