-- Set admin role for user 6902973013
-- UserRole enum: Student=1, Moderator=2, Admin=3, SuperAdmin=4
UPDATE Users SET Role = 3 WHERE TelegramId = 6902973013;

-- Verify
SELECT TelegramId, FirstName, Username, Role FROM Users WHERE TelegramId = 6902973013;
