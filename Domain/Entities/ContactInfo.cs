namespace StudentUnionBot.Domain.Entities;

/// <summary>
/// Контактна інформація профспілкового комітету
/// </summary>
public class ContactInfo
{
    public int Id { get; private set; }
    
    /// <summary>
    /// Назва контакту (посада, відділ)
    /// </summary>
    public string Title { get; private set; } = string.Empty;
    
    /// <summary>
    /// Тип контакту
    /// </summary>
    public ContactType Type { get; private set; }
    
    /// <summary>
    /// ПІБ контактної особи
    /// </summary>
    public string? PersonName { get; private set; }
    
    /// <summary>
    /// Посада контактної особи
    /// </summary>
    public string? Position { get; private set; }
    
    /// <summary>
    /// Номер телефону
    /// </summary>
    public string? PhoneNumber { get; private set; }
    
    /// <summary>
    /// Email адреса
    /// </summary>
    public string? Email { get; private set; }
    
    /// <summary>
    /// Telegram username (без @)
    /// </summary>
    public string? TelegramUsername { get; private set; }
    
    /// <summary>
    /// Адреса офісу
    /// </summary>
    public string? Address { get; private set; }
    
    /// <summary>
    /// Номер кабінету
    /// </summary>
    public string? OfficeNumber { get; private set; }
    
    /// <summary>
    /// Години роботи
    /// </summary>
    public string? WorkingHours { get; private set; }
    
    /// <summary>
    /// Додаткова інформація
    /// </summary>
    public string? Description { get; private set; }
    
    /// <summary>
    /// Порядок відображення (менше = вище)
    /// </summary>
    public int DisplayOrder { get; private set; }
    
    /// <summary>
    /// Чи активний контакт
    /// </summary>
    public bool IsActive { get; private set; }
    
    /// <summary>
    /// Дата створення
    /// </summary>
    public DateTime CreatedAt { get; private set; }
    
    /// <summary>
    /// Дата останнього оновлення
    /// </summary>
    public DateTime UpdatedAt { get; private set; }
    
    // Приватний конструктор для EF Core
    private ContactInfo() { }
    
    /// <summary>
    /// Фабричний метод для створення контактної інформації
    /// </summary>
    public static ContactInfo Create(
        string title,
        ContactType type,
        string? personName = null,
        string? position = null,
        string? phoneNumber = null,
        string? email = null,
        string? telegramUsername = null,
        string? address = null,
        string? officeNumber = null,
        string? workingHours = null,
        string? description = null,
        int displayOrder = 0)
    {
        var contact = new ContactInfo
        {
            Title = title,
            Type = type,
            PersonName = personName,
            Position = position,
            PhoneNumber = phoneNumber,
            Email = email,
            TelegramUsername = telegramUsername,
            Address = address,
            OfficeNumber = officeNumber,
            WorkingHours = workingHours,
            Description = description,
            DisplayOrder = displayOrder,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        
        return contact;
    }
    
    public void Update(
        string title,
        ContactType type,
        string? personName = null,
        string? position = null,
        string? phoneNumber = null,
        string? email = null,
        string? telegramUsername = null,
        string? address = null,
        string? officeNumber = null,
        string? workingHours = null,
        string? description = null)
    {
        Title = title;
        Type = type;
        PersonName = personName;
        Position = position;
        PhoneNumber = phoneNumber;
        Email = email;
        TelegramUsername = telegramUsername;
        Address = address;
        OfficeNumber = officeNumber;
        WorkingHours = workingHours;
        Description = description;
        UpdatedAt = DateTime.UtcNow;
    }
    
    public void SetDisplayOrder(int order)
    {
        DisplayOrder = order;
        UpdatedAt = DateTime.UtcNow;
    }
    
    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }
    
    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }
}

/// <summary>
/// Тип контактної інформації
/// </summary>
public enum ContactType
{
    /// <summary>
    /// Головний офіс/приймальня
    /// </summary>
    MainOffice = 1,
    
    /// <summary>
    /// Голова профкому
    /// </summary>
    Chairman = 2,
    
    /// <summary>
    /// Заступник голови
    /// </summary>
    ViceChairman = 3,
    
    /// <summary>
    /// Відділ соціального захисту
    /// </summary>
    SocialProtection = 4,
    
    /// <summary>
    /// Культурно-масовий відділ
    /// </summary>
    CulturalDepartment = 5,
    
    /// <summary>
    /// Спортивний відділ
    /// </summary>
    SportsDepartment = 6,
    
    /// <summary>
    /// Юридична підтримка
    /// </summary>
    Legal = 7,
    
    /// <summary>
    /// Гаряча лінія
    /// </summary>
    Hotline = 8,
    
    /// <summary>
    /// Інший контакт
    /// </summary>
    Other = 99
}
