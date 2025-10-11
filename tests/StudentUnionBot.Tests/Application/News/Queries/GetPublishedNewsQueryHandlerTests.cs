using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using StudentUnionBot.Application.News.DTOs;
using StudentUnionBot.Application.News.Queries.GetPublishedNews;
using StudentUnionBot.Core.Results;
using StudentUnionBot.Domain.Enums;
using StudentUnionBot.Domain.Interfaces;
using Xunit;

namespace StudentUnionBot.Tests.Application.News.Queries;

public class GetPublishedNewsQueryHandlerTests
{
    private readonly Mock<INewsRepository> _newsRepositoryMock;
    private readonly Mock<IStudentUnionCacheService> _cacheServiceMock;
    private readonly Mock<ILogger<GetPublishedNewsQueryHandler>> _loggerMock;
    private readonly GetPublishedNewsQueryHandler _handler;

    public GetPublishedNewsQueryHandlerTests()
    {
        _newsRepositoryMock = new Mock<INewsRepository>();
        _cacheServiceMock = new Mock<IStudentUnionCacheService>();
        _loggerMock = new Mock<ILogger<GetPublishedNewsQueryHandler>>();

        _handler = new GetPublishedNewsQueryHandler(
            _newsRepositoryMock.Object,
            _cacheServiceMock.Object,
            _loggerMock.Object
        );
    }

    [Fact]
    public async Task Handle_ReturnsPublishedNews_WhenCacheIsEmpty()
    {
        // Arrange
        var query = new GetPublishedNewsQuery
        {
            Category = null,
            PageNumber = 1,
            PageSize = 10,
            OnlyPinned = false
        };

        var newsEntities = new List<StudentUnionBot.Domain.Entities.News>
        {
            StudentUnionBot.Domain.Entities.News.Create("Новина 1", "Контент 1", NewsCategory.Important, 12345, "Автор 1", publishImmediately: true),
            StudentUnionBot.Domain.Entities.News.Create("Новина 2", "Контент 2", NewsCategory.Education, 12346, "Автор 2", publishImmediately: true)
        };

        _cacheServiceMock
            .Setup(x => x.GetNewsListAsync<NewsListDto>(1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync((NewsListDto?)null);

        _newsRepositoryMock
            .Setup(x => x.GetPublishedNewsAsync(null, false, 1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(newsEntities);

        _newsRepositoryMock
            .Setup(x => x.GetPublishedNewsCountAsync(null, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);

        _cacheServiceMock
            .Setup(x => x.SetNewsListAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<NewsListDto>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Items.Should().HaveCount(2);
        result.Value.TotalCount.Should().Be(2);
        result.Value.PageNumber.Should().Be(1);
        result.Value.PageSize.Should().Be(10);
        result.Value.Items.First().Title.Should().Be("Новина 1");
        
        _newsRepositoryMock.Verify(x => x.GetPublishedNewsAsync(null, false, 1, 10, It.IsAny<CancellationToken>()), Times.Once);
        _cacheServiceMock.Verify(x => x.SetNewsListAsync(1, 10, It.IsAny<NewsListDto>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ReturnsCachedData_WhenCacheHit()
    {
        // Arrange
        var query = new GetPublishedNewsQuery
        {
            PageNumber = 1,
            PageSize = 10
        };

        var cachedData = new NewsListDto
        {
            Items = new List<NewsDto>
            {
                new() { Id = 1, Title = "Cached News", Content = "Cached Content", Category = NewsCategory.Important }
            },
            TotalCount = 1,
            PageNumber = 1,
            PageSize = 10
        };

        _cacheServiceMock
            .Setup(x => x.GetNewsListAsync<NewsListDto>(1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(cachedData);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(cachedData);
        result.Value.Items.Should().HaveCount(1);
        result.Value.Items.First().Title.Should().Be("Cached News");
        
        // Репозиторій не повинен викликатися при cache hit
        _newsRepositoryMock.Verify(x => x.GetPublishedNewsAsync(It.IsAny<NewsCategory?>(), It.IsAny<bool>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_FiltersNewsByCategory_WhenCategorySpecified()
    {
        // Arrange
        var query = new GetPublishedNewsQuery
        {
            Category = NewsCategory.Education,
            PageNumber = 1,
            PageSize = 5
        };

        var newsEntities = new List<StudentUnionBot.Domain.Entities.News>
        {
            StudentUnionBot.Domain.Entities.News.Create("Освітня новина", "Контент", NewsCategory.Education, 12345, "Автор", publishImmediately: true)
        };

        _cacheServiceMock
            .Setup(x => x.GetNewsListAsync<NewsListDto>(1, 5, It.IsAny<CancellationToken>()))
            .ReturnsAsync((NewsListDto?)null);

        _newsRepositoryMock
            .Setup(x => x.GetPublishedNewsAsync(NewsCategory.Education, false, 1, 5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(newsEntities);

        _newsRepositoryMock
            .Setup(x => x.GetPublishedNewsCountAsync(NewsCategory.Education, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(1);
        result.Value.Items.First().Category.Should().Be(NewsCategory.Education);
        
        _newsRepositoryMock.Verify(x => x.GetPublishedNewsAsync(NewsCategory.Education, false, 1, 5, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ReturnsOnlyPinnedNews_WhenOnlyPinnedIsTrue()
    {
        // Arrange
        var query = new GetPublishedNewsQuery
        {
            OnlyPinned = true,
            PageNumber = 1,
            PageSize = 10
        };

        var pinnedNews = StudentUnionBot.Domain.Entities.News.Create("Закріплена новина", "Важливий контент", NewsCategory.Urgent, 12345, "Автор", publishImmediately: true);
        pinnedNews.Pin();

        var newsEntities = new List<StudentUnionBot.Domain.Entities.News> { pinnedNews };

        _cacheServiceMock
            .Setup(x => x.GetNewsListAsync<NewsListDto>(1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync((NewsListDto?)null);

        _newsRepositoryMock
            .Setup(x => x.GetPublishedNewsAsync(null, true, 1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(newsEntities);

        _newsRepositoryMock
            .Setup(x => x.GetPublishedNewsCountAsync(null, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(1);
        
        _newsRepositoryMock.Verify(x => x.GetPublishedNewsAsync(null, true, 1, 10, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ReturnsEmptyList_WhenNoNewsFound()
    {
        // Arrange
        var query = new GetPublishedNewsQuery
        {
            PageNumber = 1,
            PageSize = 10
        };

        _cacheServiceMock
            .Setup(x => x.GetNewsListAsync<NewsListDto>(1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync((NewsListDto?)null);

        _newsRepositoryMock
            .Setup(x => x.GetPublishedNewsAsync(null, false, 1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<StudentUnionBot.Domain.Entities.News>());

        _newsRepositoryMock
            .Setup(x => x.GetPublishedNewsCountAsync(null, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().BeEmpty();
        result.Value.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task Handle_ReturnsPaginatedResults_WhenMultiplePagesExist()
    {
        // Arrange
        var query = new GetPublishedNewsQuery
        {
            PageNumber = 2,
            PageSize = 5
        };

        var newsEntities = new List<StudentUnionBot.Domain.Entities.News>
        {
            StudentUnionBot.Domain.Entities.News.Create("Новина 6", "Контент 6", NewsCategory.Important, 12345, "Автор", publishImmediately: true),
            StudentUnionBot.Domain.Entities.News.Create("Новина 7", "Контент 7", NewsCategory.Important, 12345, "Автор", publishImmediately: true)
        };

        _cacheServiceMock
            .Setup(x => x.GetNewsListAsync<NewsListDto>(2, 5, It.IsAny<CancellationToken>()))
            .ReturnsAsync((NewsListDto?)null);

        _newsRepositoryMock
            .Setup(x => x.GetPublishedNewsAsync(null, false, 2, 5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(newsEntities);

        _newsRepositoryMock
            .Setup(x => x.GetPublishedNewsCountAsync(null, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(12); // Всього 12 новин, 3 сторінки по 5

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.PageNumber.Should().Be(2);
        result.Value.PageSize.Should().Be(5);
        result.Value.TotalCount.Should().Be(12);
        result.Value.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_ReturnsFailure_WhenRepositoryThrowsException()
    {
        // Arrange
        var query = new GetPublishedNewsQuery();

        _cacheServiceMock
            .Setup(x => x.GetNewsListAsync<NewsListDto>(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((NewsListDto?)null);

        _newsRepositoryMock
            .Setup(x => x.GetPublishedNewsAsync(It.IsAny<NewsCategory?>(), It.IsAny<bool>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database connection error"));

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Виникла помилка при завантаженні новин");
    }
}
