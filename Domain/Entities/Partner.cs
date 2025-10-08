using StudentUnionBot.Domain.Enums;

namespace StudentUnionBot.Domain.Entities;

/// <summary>
/// Партнер студентського профспілкового комітету
/// </summary>
public class Partner
{
    public int Id { get; private set; }
    
    /// <summary>
    /// Назва партнера
    /// </summary>
    public string Name { get; private set; } = string.Empty;
    
    /// <summary>
    /// Тип партнера
    /// </summary>
    public PartnerType Type { get; private set; }
    
    /// <summary>
    /// Короткий опис партнера
    /// </summary>
    public string? Description { get; private set; }
    
    /// <summary>
    /// Знижка або бонус для студентів (текстом)
    /// </summary>
    public string? DiscountInfo { get; private set; }
    
    /// <summary>
    /// Відсоток знижки (якщо фіксований)
    /// </summary>
    public int? DiscountPercent { get; private set; }
    
    /// <summary>
    /// Промокод для знижки
    /// </summary>
    public string? PromoCode { get; private set; }
    
    /// <summary>
    /// Веб-сайт партнера
    /// </summary>
    public string? Website { get; private set; }
    
    /// <summary>
    /// Адреса закладу
    /// </summary>
    public string? Address { get; private set; }
    
    /// <summary>
    /// Номер телефону
    /// </summary>
    public string? PhoneNumber { get; private set; }
    
    /// <summary>
    /// Instagram профіль
    /// </summary>
    public string? Instagram { get; private set; }
    
    /// <summary>
    /// Facebook сторінка
    /// </summary>
    public string? Facebook { get; private set; }
    
    /// <summary>
    /// Telegram канал/група
    /// </summary>
    public string? Telegram { get; private set; }
    
    /// <summary>
    /// ID фото/логотипу в Telegram
    /// </summary>
    public string? LogoFileId { get; private set; }
    
    /// <summary>
    /// Умови отримання знижки
    /// </summary>
    public string? TermsAndConditions { get; private set; }
    
    /// <summary>
    /// Термін дії партнерства (null = безстроково)
    /// </summary>
    public DateTime? ValidUntil { get; private set; }
    
    /// <summary>
    /// Чи активне партнерство
    /// </summary>
    public bool IsActive { get; private set; }
    
    /// <summary>
    /// Чи рекомендований партнер (показується у топі)
    /// </summary>
    public bool IsFeatured { get; private set; }
    
    /// <summary>
    /// Порядок відображення
    /// </summary>
    public int DisplayOrder { get; private set; }
    
    /// <summary>
    /// Кількість переглядів
    /// </summary>
    public int ViewCount { get; private set; }
    
    /// <summary>
    /// Дата створення
    /// </summary>
    public DateTime CreatedAt { get; private set; }
    
    /// <summary>
    /// Дата останнього оновлення
    /// </summary>
    public DateTime UpdatedAt { get; private set; }
    
    // Приватний конструктор для EF Core
    private Partner() { }
    
    /// <summary>
    /// Фабричний метод для створення партнера
    /// </summary>
    public static Partner Create(
        string name,
        PartnerType type,
        string? description = null,
        string? discountInfo = null,
        int? discountPercent = null,
        string? promoCode = null,
        string? website = null,
        string? address = null,
        string? phoneNumber = null,
        string? instagram = null,
        string? facebook = null,
        string? telegram = null,
        string? logoFileId = null,
        string? termsAndConditions = null,
        DateTime? validUntil = null,
        int displayOrder = 0)
    {
        var partner = new Partner
        {
            Name = name,
            Type = type,
            Description = description,
            DiscountInfo = discountInfo,
            DiscountPercent = discountPercent,
            PromoCode = promoCode,
            Website = website,
            Address = address,
            PhoneNumber = phoneNumber,
            Instagram = instagram,
            Facebook = facebook,
            Telegram = telegram,
            LogoFileId = logoFileId,
            TermsAndConditions = termsAndConditions,
            ValidUntil = validUntil,
            IsActive = true,
            IsFeatured = false,
            DisplayOrder = displayOrder,
            ViewCount = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        
        return partner;
    }
    
    public void Update(
        string name,
        PartnerType type,
        string? description = null,
        string? discountInfo = null,
        int? discountPercent = null,
        string? promoCode = null,
        string? website = null,
        string? address = null,
        string? phoneNumber = null,
        string? instagram = null,
        string? facebook = null,
        string? telegram = null,
        string? logoFileId = null,
        string? termsAndConditions = null,
        DateTime? validUntil = null)
    {
        Name = name;
        Type = type;
        Description = description;
        DiscountInfo = discountInfo;
        DiscountPercent = discountPercent;
        PromoCode = promoCode;
        Website = website;
        Address = address;
        PhoneNumber = phoneNumber;
        Instagram = instagram;
        Facebook = facebook;
        Telegram = telegram;
        LogoFileId = logoFileId;
        TermsAndConditions = termsAndConditions;
        ValidUntil = validUntil;
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
    
    public void Feature()
    {
        IsFeatured = true;
        UpdatedAt = DateTime.UtcNow;
    }
    
    public void Unfeature()
    {
        IsFeatured = false;
        UpdatedAt = DateTime.UtcNow;
    }
    
    public void SetDisplayOrder(int order)
    {
        DisplayOrder = order;
        UpdatedAt = DateTime.UtcNow;
    }
    
    public void IncrementViewCount()
    {
        ViewCount++;
    }
    
    public bool IsValid()
    {
        return IsActive && (!ValidUntil.HasValue || ValidUntil.Value > DateTime.UtcNow);
    }
}
