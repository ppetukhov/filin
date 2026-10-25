using System.Data.Common;

using Filin.EventStore.AdoNet.Postgres;
using Filin.EventStore.AdoNet.Tests.Internal;
using Filin.Storage;

using Xunit;

namespace Filin.EventStore.AdoNet.Tests;

public class PgDbCommandFactoryTests
{
    private readonly TestDbDataSource _dataSource;
    private readonly TestStorageSerializer _serializer;
    private readonly PgDbCommandFactory _factory;

    public PgDbCommandFactoryTests()
    {
        _dataSource = new TestDbDataSource();
        _serializer = new TestStorageSerializer();
        _factory = new PgDbCommandFactory(_dataSource, _serializer);
    }

    #region CreateAppendEventCommand Tests

    [Fact]
    public void CreateAppendEventCommand_FastPath_GeneratesCorrectCommandTextAndParameters()
    {
        // Arrange
        var eventType = new EventType("UserCreated");
        var tag = new Tag("tenant:1");
        var @event = new DcbEvent
        {
            EventType = eventType,
            Tags = new Tags(tag),
            Payload = new { UserId = 42 }
        };

        var condition = new AppendCondition(
            FailIfEventsMatch: default,
            After: new EventPosition(100)
        );

        const string expectedSql = "SELECT * from dcb_append_event($1,($2,$3,array[$4],array[$5],$6::json)::dcb_event);";

        // Act
        var command = _factory.CreateAppendEventCommand(@event, condition);

        // Assert: 1. Точная проверка всего текста SQL-запроса
        Assert.NotNull(command);
        Assert.Equal(expectedSql, command.CommandText);

        // Assert: 2. Проверка всех параметров
        var parameters = command.Parameters.Cast<DbParameter>().ToList();
        Assert.Equal(6, parameters.Count);
        Assert.Equal(100L, parameters[0].Value);
        Assert.Equal((int)eventType.Hash, parameters[1].Value);
        Assert.Equal("UserCreated", parameters[2].Value);
        Assert.Equal((long)tag.Hash, parameters[3].Value);
        Assert.Equal("tenant:1", parameters[4].Value);
        Assert.Equal("{}", parameters[5].Value);
        Assert.Equal(1, _serializer.SerializeCallCount);
    }

    [Fact]
    public void CreateAppendEventCommand_SlowPath_WhenCacheKeyExceeded_GeneratesDynamicSql()
    {
        // Arrange
        var eventType = new EventType("LargeEvent");
        var tags = CreateLargeTagsCollection(20);
        var @event = new DcbEvent
        {
            EventType = eventType,
            Tags = tags,
            Payload = new { Data = "test" }
        };

        var condition = new AppendCondition(
            FailIfEventsMatch: default,
            After: new EventPosition(50)
        );

        const string expectedSql = "SELECT * from dcb_append_event($1,($2,$3,array[$4,$5,$6,$7,$8,$9,$10,$11,$12,$13,$14,$15,$16,$17,$18,$19,$20,$21,$22,$23],array[$24,$25,$26,$27,$28,$29,$30,$31,$32,$33,$34,$35,$36,$37,$38,$39,$40,$41,$42,$43],$44::json)::dcb_event);";

        // Act
        var command = _factory.CreateAppendEventCommand(@event, condition);

        // Assert: 1
        Assert.NotNull(command);
        Assert.Equal(expectedSql, command.CommandText);

        // Assert: 2
        var parameters = command.Parameters.Cast<DbParameter>().ToList();
        Assert.Equal(44, parameters.Count);
        Assert.Equal(50L, parameters[0].Value);
        Assert.Equal(1, _serializer.SerializeCallCount);
    }

    #endregion

    #region CreateAppendEventsCommand Tests

    [Fact]
    public void CreateAppendEventsCommand_WhenSingleEvent_DelegatesToSingleEventCommand()
    {
        // Arrange
        var eventType = new EventType("OrderPlaced");
        var events = new List<DcbEvent>
        {
            new DcbEvent
            {
                EventType = eventType,
                Tags = default,
                Payload = new { OrderId = 1 }
            }
        };

        var condition = new AppendCondition(default, new EventPosition(10));

        const string expectedSql = "SELECT * from dcb_append_event($1,($2,$3,array[],array[],$4::json)::dcb_event);";

        // Act
        var command = _factory.CreateAppendEventsCommand(events, condition);

        // Assert
        Assert.NotNull(command);
        Assert.Equal(expectedSql, command.CommandText);

        var parameters = command.Parameters.Cast<DbParameter>().ToList();
        Assert.Equal(4, parameters.Count);
        Assert.Equal(10L, parameters[0].Value);
        Assert.Equal((int)eventType.Hash, parameters[1].Value);
        Assert.Equal("OrderPlaced", parameters[2].Value);
        Assert.Equal("{}", parameters[3].Value);
    }

    [Fact]
    public void CreateAppendEventsCommand_MultipleEvents_FastPath_GeneratesBatchSql()
    {
        // Arrange
        var ev1Type = new EventType("Event1");
        var ev2Type = new EventType("Event2");
        var events = new List<DcbEvent>
        {
            new DcbEvent { EventType = ev1Type, Payload = "p1" },
            new DcbEvent { EventType = ev2Type, Payload = "p2" }
        };

        var condition = new AppendCondition(default, new EventPosition(0));

        const string expectedSql = "SELECT * from dcb_append_events($1,array[($2,$3,array[],array[],$4::json)::dcb_event,($5,$6,array[],array[],$7::json)::dcb_event]::dcb_event[],);";

        // Act
        var command = _factory.CreateAppendEventsCommand(events, condition);

        // Assert
        Assert.NotNull(command);
        Assert.Equal(expectedSql, command.CommandText);

        var parameters = command.Parameters.Cast<DbParameter>().ToList();
        Assert.Equal(7, parameters.Count);

        // $1: position
        Assert.Equal(0L, parameters[0].Value);

        // DcbEvent 1
        Assert.Equal((int)ev1Type.Hash, parameters[1].Value);
        Assert.Equal("Event1", parameters[2].Value);
        Assert.Equal("{}", parameters[3].Value);

        // DcbEvent 2
        Assert.Equal((int)ev2Type.Hash, parameters[4].Value);
        Assert.Equal("Event2", parameters[5].Value);
        Assert.Equal("{}", parameters[6].Value);

        Assert.Equal(2, _serializer.SerializeCallCount);
    }

    #endregion

    #region CreateReadEventsCommand Tests

    [Fact]
    public void CreateReadEventsCommand_FastPath_GeneratesCorrectCommandTextAndParameters()
    {
        // Arrange
        var afterPosition = new EventPosition(500);
        var eventType = new EventType("ItemAdded");
        var query = new DcbQuery(new DcbQueryItem(eventType));
        int limit = 100;

        const string expectedSql = "SELECT * from dcb_read_events($1,VARIADIC array[(array[$2],array[$3],array[]::bigint[],array[]::text[])]::dcb_query_item[]) LIMIT $4;";

        // Act
        var command = _factory.CreateReadEventsCommand(afterPosition, query, limit);

        // Assert: 1
        Assert.NotNull(command);
        Assert.Equal(expectedSql, command.CommandText);

        // Assert: 2
        var parameters = command.Parameters.Cast<DbParameter>().ToList();
        Assert.Equal(4, parameters.Count);

        Assert.Equal(500L, parameters[0].Value);
        Assert.Equal((int)eventType.Hash, parameters[1].Value);
        Assert.Equal("ItemAdded", parameters[2].Value);
        Assert.Equal(100, parameters[3].Value);
    }

    [Fact]
    public void CreateReadEventsCommand_FastPath_WithTags_VerifiesAllParametersInOrder()
    {
        // Arrange
        var afterPosition = new EventPosition(12345);
        var eventType = new EventType("OrderPlaced");
        var tag = new Tag("tenant:sales");
        var tags = new Tags(tag);
        var query = new DcbQuery(new DcbQueryItem([eventType], tags));
        int limit = 25;

        const string expectedSql = "SELECT * from dcb_read_events($1,VARIADIC array[(array[$2],array[$3],array[$4],array[$5])]::dcb_query_item[]) LIMIT $6;";

        // Act
        var command = _factory.CreateReadEventsCommand(afterPosition, query, limit);

        // Assert: 1
        Assert.NotNull(command);
        Assert.Equal(expectedSql, command.CommandText);

        // Assert: 2
        var parameters = command.Parameters.Cast<DbParameter>().ToList();
        Assert.Equal(6, parameters.Count);

        Assert.Equal(12345L, parameters[0].Value);
        Assert.Equal((int)eventType.Hash, parameters[1].Value);
        Assert.Equal("OrderPlaced", parameters[2].Value);
        Assert.Equal((long)tag.Hash, parameters[3].Value);
        Assert.Equal("tenant:sales", parameters[4].Value);
        Assert.Equal(25, parameters[5].Value);
    }

    [Fact]
    public void CreateReadEventsCommand_EmptyQuery_VerifiesParametersAndLimitPlaceholder()
    {
        // Arrange
        var afterPosition = new EventPosition(0);
        var query = default(DcbQuery);
        int limit = 50;

        const string expectedSql = "SELECT * from dcb_read_events($1,) LIMIT $2;";

        // Act
        var command = _factory.CreateReadEventsCommand(afterPosition, query, limit);

        // Assert: 1
        Assert.NotNull(command);
        Assert.Equal(expectedSql, command.CommandText);

        // Assert: 2
        var parameters = command.Parameters.Cast<DbParameter>().ToList();
        Assert.Equal(2, parameters.Count);

        Assert.Equal(0L, parameters[0].Value);
        Assert.Equal(50, parameters[1].Value);
    }

    #endregion

    #region Helpers

    private static Tags CreateLargeTagsCollection(int count)
    {
        var tagList = new Tag[count];
        for (int i = 0; i < count; i++)
        {
            tagList[i] = new Tag($"tag_{i}");
        }
        return new Tags(tagList);
    }

    #endregion
}
