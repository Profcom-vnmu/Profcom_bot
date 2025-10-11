using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using StudentUnionBot.Application.Events.DTOs;
using StudentUnionBot.Application.Events.Queries.GetUpcomingEvents;
using StudentUnionBot.Core.Results;
using StudentUnionBot.Domain.Enums;
using StudentUnionBot.Domain.Interfaces;
using Xunit;

namespace StudentUnionBot.Tests.Application.Events.Queries;

public class GetUpcomingEventsQueryHandlerTests
{
    private readonly Mock<IEventRepository> _eventRepositoryMock;
    private readonly Mock<IStudentUnionCacheService> _cacheServiceMock;
    private readonly Mock<ILogger<GetUpcomingEventsQueryHandler>> _loggerMock;
    private readonly GetUpcomingEventsQueryHandler _handler;

    public GetUpcomingEventsQueryHandlerTests()
    {
        _eventRepositoryMock = new Mock<IEventRepository>();
        _cacheServiceMock = new Mock<IStudentUnionCacheService>();
        _loggerMock = new Mock<ILogger<GetUpcomingEventsQueryHandler>>();

        _handler = new GetUpcomingEventsQueryHandler(
            _eventRepositoryMock.Object,
            _cacheServiceMock.Object,
            _loggerMock.Object
        );
    }

    [Fact]
    public async Task Handle_ReturnsUpcomingEvents_WhenCacheIsEmpty()
    {
        // Arrange
        var query = new GetUpcomingEventsQuery
        {
            Type = null,
            OnlyFeatured = false,
            PageNumber = 1,
            PageSize = 10
        };

        var eventEntities = new List<StudentUnionBot.Domain.Entities.Event>
        {
            StudentUnionBot.Domain.Entities.Event.Create(
                "Конференція",
                "Наукова конференція",
                EventCategory.Academic,
                EventType.Educational,
                DateTime.UtcNow.AddDays(10),
                12345,
                "Організатор 1",
                publishImmediately: true),
            StudentUnionBot.Domain.Entities.Event.Create(
                "Концерт",
                "Студентський концерт",
                EventCategory.Entertainment,
                EventType.Cultural,
                DateTime.UtcNow.AddDays(15),
                12346,
                "Організатор 2",
                publishImmediately: true)
        };

        _cacheServiceMock
            .Setup(x => x.GetEventsListAsync<EventListDto>(1, 10, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((EventListDto?)null);

        _eventRepositoryMock
            .Setup(x => x.GetUpcomingEventsAsync(null, false, 1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(eventEntities);

        _eventRepositoryMock
            .Setup(x => x.GetUpcomingEventsCountAsync(null, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);

        _cacheServiceMock
            .Setup(x => x.SetEventsListAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<EventListDto>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
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
        result.Value.Items.First().Title.Should().Be("Конференція");
        
        _eventRepositoryMock.Verify(x => x.GetUpcomingEventsAsync(null, false, 1, 10, It.IsAny<CancellationToken>()), Times.Once);
        _cacheServiceMock.Verify(x => x.SetEventsListAsync(1, 10, It.IsAny<EventListDto>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ReturnsCachedData_WhenCacheHit()
    {
        // Arrange
        var query = new GetUpcomingEventsQuery
        {
            PageNumber = 1,
            PageSize = 10
        };

        var cachedData = new EventListDto
        {
            Items = new List<EventDto>
            {
                new() { Id = 1, Title = "Cached Event", Description = "From cache", Type = EventType.Educational }
            },
            TotalCount = 1,
            PageNumber = 1,
            PageSize = 10
        };

        _cacheServiceMock
            .Setup(x => x.GetEventsListAsync<EventListDto>(1, 10, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(cachedData);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(cachedData);
        result.Value.Items.Should().HaveCount(1);
        result.Value.Items.First().Title.Should().Be("Cached Event");
        
        // Репозиторій не повинен викликатися при cache hit
        _eventRepositoryMock.Verify(x => x.GetUpcomingEventsAsync(It.IsAny<EventType?>(), It.IsAny<bool>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_FiltersEventsByType_WhenTypeSpecified()
    {
        // Arrange
        var query = new GetUpcomingEventsQuery
        {
            Type = EventType.Sports,
            PageNumber = 1,
            PageSize = 5
        };

        var eventEntities = new List<StudentUnionBot.Domain.Entities.Event>
        {
            StudentUnionBot.Domain.Entities.Event.Create(
                "Футбольний турнір",
                "Міжфакультетський турнір",
                EventCategory.Sports,
                EventType.Sports,
                DateTime.UtcNow.AddDays(7),
                12345,
                "Організатор",
                publishImmediately: true)
        };

        _cacheServiceMock
            .Setup(x => x.GetEventsListAsync<EventListDto>(1, 5, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((EventListDto?)null);

        _eventRepositoryMock
            .Setup(x => x.GetUpcomingEventsAsync(EventType.Sports, false, 1, 5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(eventEntities);

        _eventRepositoryMock
            .Setup(x => x.GetUpcomingEventsCountAsync(EventType.Sports, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(1);
        result.Value.Items.First().Type.Should().Be(EventType.Sports);
        
        _eventRepositoryMock.Verify(x => x.GetUpcomingEventsAsync(EventType.Sports, false, 1, 5, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ReturnsOnlyFeaturedEvents_WhenOnlyFeaturedIsTrue()
    {
        // Arrange
        var query = new GetUpcomingEventsQuery
        {
            OnlyFeatured = true,
            PageNumber = 1,
            PageSize = 10
        };

        var featuredEvent = StudentUnionBot.Domain.Entities.Event.Create(
            "Важлива подія",
            "Featured event",
            EventCategory.Academic,
            EventType.Educational,
            DateTime.UtcNow.AddDays(5),
            12345,
            "Організатор",
            publishImmediately: true);
        featuredEvent.Feature();

        var eventEntities = new List<StudentUnionBot.Domain.Entities.Event> { featuredEvent };

        _cacheServiceMock
            .Setup(x => x.GetEventsListAsync<EventListDto>(1, 10, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((EventListDto?)null);

        _eventRepositoryMock
            .Setup(x => x.GetUpcomingEventsAsync(null, true, 1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(eventEntities);

        _eventRepositoryMock
            .Setup(x => x.GetUpcomingEventsCountAsync(null, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(1);
        result.Value.Items.First().IsFeatured.Should().BeTrue();
        
        _eventRepositoryMock.Verify(x => x.GetUpcomingEventsAsync(null, true, 1, 10, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ReturnsEmptyList_WhenNoEventsFound()
    {
        // Arrange
        var query = new GetUpcomingEventsQuery
        {
            PageNumber = 1,
            PageSize = 10
        };

        _cacheServiceMock
            .Setup(x => x.GetEventsListAsync<EventListDto>(1, 10, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((EventListDto?)null);

        _eventRepositoryMock
            .Setup(x => x.GetUpcomingEventsAsync(null, false, 1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<StudentUnionBot.Domain.Entities.Event>());

        _eventRepositoryMock
            .Setup(x => x.GetUpcomingEventsCountAsync(null, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().BeEmpty();
        result.Value.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task Handle_ReturnsFailure_WhenRepositoryThrowsException()
    {
        // Arrange
        var query = new GetUpcomingEventsQuery();

        _cacheServiceMock
            .Setup(x => x.GetEventsListAsync<EventListDto>(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((EventListDto?)null);

        _eventRepositoryMock
            .Setup(x => x.GetUpcomingEventsAsync(It.IsAny<EventType?>(), It.IsAny<bool>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database connection failed"));

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Не вдалося завантажити події");
    }
}
