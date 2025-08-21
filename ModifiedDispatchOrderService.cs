using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

public class OrderService
{
    private readonly IOrderRepository _orderRepository;
    private readonly IDeliveryTypeHandlerFactory _deliveryTypeHandlerFactory;
    private readonly ILogger<OrderService> _logger;

    public OrderService(
        IOrderRepository orderRepository,
        IDeliveryTypeHandlerFactory deliveryTypeHandlerFactory,
        ILogger<OrderService> logger)
    {
        _orderRepository = orderRepository;
        _deliveryTypeHandlerFactory = deliveryTypeHandlerFactory;
        _logger = logger;
    }

    public async Task<Result<OrderDto>> DispatchOrder(
        string externalId,
        DispatchOrderRequest request,
        CancellationToken cancellationToken
    )
    {
        var order = await _orderRepository.GetOrderByExternalId(externalId);

        // cannot dispatch in created status, it should be fail and go back to the DLQ
        if (order.Status == OrderStatus.Created)
        {
            _logger.LogWarning("Order : {ExternalId} is still in creation status.", externalId);
            throw new InvalidOperationException("Order is still in creation status.");
        }

        // Update order dispatch status
        order.Dispatch(request.DispatchedTime);

        // Set the total net weight in grams based on the order data
        if (request.Packages != null)
        {
            if (request.Packages.TotalGrossWeightInGrams is null or 0)
                request.Packages.TotalGrossWeightInGrams = order.Packages?.TotalGrossWeightInGrams;
            if (request.Packages.TotalNetWeightInGrams is null or 0)
                request.Packages.TotalNetWeightInGrams = order.Packages?.TotalNetWeightInGrams;
            if (request.Packages.TotalTareWeightInGrams is null or 0)
                request.Packages.TotalTareWeightInGrams = order.Packages?.TotalTareWeightInGrams;

            // Update order packages with request data (including Breakdown)
            order.Packages = request.Packages;
        }

        // Update order metadata with request metadata
        if (request.Metadata != null && request.Metadata.Any())
        {
            // Initialize order metadata if null
            if (order.Metadata == null)
                order.Metadata = new Dictionary<string, object>();

            // Merge request metadata into order metadata
            foreach (var kvp in request.Metadata)
            {
                order.Metadata[kvp.Key] = kvp.Value;
            }
        }

        // Update order categorised items with request data
        if (request.CategorisedItems != null)
        {
            order.CategorisedItems = request.CategorisedItems;
        }

        // Update order in Mongo with all the updated data (including Metadata, CategorisedItems, and Packages.Breakdown)
        await _orderRepository.UpsertOneAsync(order);

        // Get Handler & send Dispatch message
        await _deliveryTypeHandlerFactory
            .GetDeliveryTypeHandler(order.DeliveryProposition)
            .DispatchOrder(externalId, request, order, cancellationToken);

        return new Result<OrderDto>(order.Adapt<OrderDto>());
    }
}

// Repository implementation with UpsertOneAsync method
public class OrderRepository<TEntity> : IOrderRepository<TEntity> where TEntity : class
{
    private readonly IDbContext _dbContext;

    public OrderRepository(IDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task UpsertOneAsync(TEntity entity)
    {
        var tenantCollection = _dbContext.GetCollection<TEntity>();
        var filter = Builders<TEntity>.Filter.Eq(e => e.Id, entity.Id);
        var _ = await tenantCollection.ReplaceOneAsync(
            filter,
            entity,
            new ReplaceOptions { IsUpsert = true }
        );
    }

    public async Task UpdateOneAsync(TEntity entity, UpdateDefinition<TEntity> updateDefinition)
    {
        var tenantCollection = _dbContext.GetCollection<TEntity>();
        var filter = Builders<TEntity>.Filter.Eq(o => o.Id, entity.Id);

        await tenantCollection.UpdateOneAsync(
            filter,
            updateDefinition,
            new UpdateOptions { IsUpsert = true }
        );
    }
}

// Extension method for Dispatch
public static class OrderExtensions
{
    public static void Dispatch(this HomeRunOrder order, DateTimeOffset dispatchTime)
    {
        order.AddNewStatus(
            OrderStatus.Dispatched,
            dispatchTime == default ? DateTimeOffset.UtcNow : dispatchTime
        );
    }
}

/// <summary>
/// Represents a request to Dispatch an order.
/// </summary>
public class DispatchOrderRequest
{
    /// <summary>
    /// This indicates the time when the order was dispatched for delivery
    /// </summary>
    /// <example>2024-12-28T10:00:00+11:00</example>
    public DateTimeOffset DispatchedTime { get; set; }

    /// <summary>
    /// Represents the packages being delivered as part of the order.
    /// </summary>
    public Packages? Packages { get; set; }

    /// <summary>
    /// Represents the packages categorised item.
    /// </summary>
    public List<CategorisedItem>? CategorisedItems { get; set; }

    /// <summary>
    /// Additional metadata associated with the order.
    /// </summary>
    /// <example>{"additionalRequiredShiftTags:quadrant": "X" // NOTE: Change as required if the entity is changing. Not including this key will leave the pre-recorded tag intact}</example>
    [JsonConverter(typeof(MetadataConverter))]
    public IDictionary<string, object>? Metadata { get; set; } = new Dictionary<string, object>();
}