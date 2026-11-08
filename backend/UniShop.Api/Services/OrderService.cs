using Microsoft.EntityFrameworkCore;
using UniShop.Api.Contracts;
using UniShop.Api.Domain;
using UniShop.Api.Persistence;

namespace UniShop.Api.Services;

public sealed class OrderService(AppDbContext dbContext) : IOrderService
{
    public async Task<IReadOnlyList<DeliveryOptionDto>> GetDeliveryOptionsAsync(string userId, CancellationToken cancellationToken)
    {
        var cart = await dbContext.Carts.AsNoTracking()
            .Where(item => item.UserId == userId)
            .SelectMany(item => item.Items.Select(cartItem => new { cartItem.ProductId, cartItem.Product.IsActive }))
            .ToListAsync(cancellationToken);

        if (cart.Count == 0 || cart.Any(item => !item.IsActive))
        {
            return [];
        }

        var productIds = cart.Select(item => item.ProductId).ToArray();

        return await dbContext.DeliveryMethods.AsNoTracking()
            .Where(method => method.Products.Count(product => productIds.Contains(product.Id)) == productIds.Length)
            .OrderBy(method => method.Price)
            .ThenBy(method => method.Name)
            .Select(method => new DeliveryOptionDto(method.Id, method.Name, method.Price, method.EstimatedDays))
            .ToListAsync(cancellationToken);
    }

    public async Task<OrderDto?> CreateOrderAsync(
        string userId,
        CreateOrderRequest request,
        CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var address = await dbContext.Addresses.AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == request.AddressId && item.UserId == userId, cancellationToken);
        if (address is null)
        {
            return null;
        }

        var cart = await dbContext.Carts
            .Include(item => item.Items)
            .ThenInclude(item => item.Product)
            .ThenInclude(product => product.DeliveryMethods)
            .SingleOrDefaultAsync(item => item.UserId == userId, cancellationToken);
        if (cart is null || cart.Items.Count == 0)
        {
            throw new ShoppingConflictException("Your cart is empty.");
        }

        if (cart.Items.Any(item => !item.Product.IsActive))
        {
            throw new ShoppingConflictException("Your cart contains an unavailable product. Remove it before checkout.");
        }

        if (!Enum.IsDefined(request.PaymentMethod))
        {
            throw new ShoppingConflictException("Select a valid payment method.");
        }

        var deliveryMethod = await dbContext.DeliveryMethods.AsNoTracking()
            .SingleOrDefaultAsync(method => method.Id == request.DeliveryMethodId, cancellationToken);
        if (deliveryMethod is null || cart.Items.Any(item => item.Product.DeliveryMethods.All(method => method.Id != deliveryMethod.Id)))
        {
            throw new ShoppingConflictException("The selected delivery method is not available for every product in your cart.");
        }

        var subtotal = cart.Items.Sum(item => item.Product.Price * item.Quantity);
        var deliveryCost = deliveryMethod.Price;
        var order = new Order
        {
            UserId = userId,
            CreatedAt = DateTimeOffset.UtcNow,
            Status = OrderStatus.Processing,
            ContactName = request.Contact.FullName.Trim(),
            ContactEmail = request.Contact.Email.Trim(),
            ContactPhone = request.Contact.Phone.Trim(),
            ShippingAddressLabel = address.Label,
            ShippingFullName = address.FullName,
            ShippingPhone = address.Phone,
            ShippingStreet = address.Street,
            ShippingCity = address.City,
            ShippingCounty = address.County,
            ShippingPostalCode = address.PostalCode,
            ShippingCountry = address.Country,
            DeliveryMethodId = deliveryMethod.Id,
            DeliveryMethodName = deliveryMethod.Name,
            DeliveryMethodPrice = deliveryMethod.Price,
            PaymentMethod = request.PaymentMethod,
            Subtotal = subtotal,
            DeliveryCost = deliveryCost,
            Total = subtotal + deliveryCost,
            Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim()
        };

        foreach (var cartItem in cart.Items.OrderBy(item => item.ProductId))
        {
            var productName = cartItem.Product.Name;
            var affectedRows = await dbContext.Products
                .Where(product => product.Id == cartItem.ProductId && product.IsActive && product.Stock >= cartItem.Quantity)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(product => product.Stock, product => product.Stock - cartItem.Quantity)
                    .SetProperty(product => product.UpdatedAt, DateTimeOffset.UtcNow), cancellationToken);

            if (affectedRows == 0)
            {
                throw new ShoppingConflictException($"Insufficient stock for {productName}; the order was not placed.");
            }

            order.Items.Add(new OrderItem
            {
                ProductId = cartItem.ProductId,
                ProductName = productName,
                UnitPrice = cartItem.Product.Price,
                Quantity = cartItem.Quantity,
                LineTotal = cartItem.Product.Price * cartItem.Quantity
            });
        }

        dbContext.Orders.Add(order);
        await dbContext.SaveChangesAsync(cancellationToken);

        order.Payment = new Payment
        {
            OrderId = order.Id,
            Method = order.PaymentMethod,
            Status = PaymentStatus.Pending,
            Amount = order.Total,
            Currency = "RON",
            CreatedAt = DateTimeOffset.UtcNow
        };

        dbContext.CartItems.RemoveRange(cart.Items);
        cart.Items.Clear();
        cart.UpdatedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return ToDto(order, order.Payment.Status);
    }

    public async Task<OrderDto?> GetOrderAsync(string userId, int orderId, CancellationToken cancellationToken)
    {
        var order = await dbContext.Orders.AsNoTracking()
            .Include(item => item.Items)
            .Include(item => item.Payment)
            .SingleOrDefaultAsync(item => item.Id == orderId && item.UserId == userId, cancellationToken);
        return order is null ? null : ToDto(order, order.Payment.Status);
    }

    public async Task<PagedResult<OrderListDto>> GetOrdersAsync(
        string userId,
        OrderHistoryQuery query,
        CancellationToken cancellationToken)
    {
        var orders = dbContext.Orders.AsNoTracking().Where(order => order.UserId == userId);
        if (query.Status is OrderStatus status)
        {
            orders = orders.Where(order => order.Status == status);
        }

        var totalCount = await orders.CountAsync(cancellationToken);
        var page = Math.Max(query.Page ?? 1, 1);
        var pageSize = Math.Clamp(query.PageSize ?? 10, 1, 50);
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        var skip = (long)(page - 1) * pageSize;
        var items = skip >= totalCount
            ? []
            : await orders
                .OrderByDescending(order => order.CreatedAt)
                .ThenByDescending(order => order.Id)
                .Skip((int)skip)
                .Take(pageSize)
                .Select(order => new OrderListDto(
                    order.Id,
                    order.ContactEmail,
                    order.CreatedAt,
                    order.Status,
                    order.Items.Sum(item => item.Quantity),
                    order.Total,
                    order.PaymentMethod,
                    order.Payment.Status))
                .ToListAsync(cancellationToken);

        return new PagedResult<OrderListDto>(items, page, pageSize, totalCount, totalPages);
    }

    public async Task<OrderDto?> CancelOrderAsync(string userId, int orderId, CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var order = await dbContext.Orders
            .Include(item => item.Items)
            .Include(item => item.Payment)
            .SingleOrDefaultAsync(item => item.Id == orderId && item.UserId == userId, cancellationToken);
        if (order is null)
        {
            return null;
        }
        if (order.Status != OrderStatus.Processing)
        {
            throw new ShoppingConflictException("Only processing orders can be cancelled by the customer.");
        }

        var claimed = await dbContext.Orders
            .Where(item => item.Id == orderId && item.UserId == userId && item.Status == OrderStatus.Processing)
            .ExecuteUpdateAsync(setters => setters.SetProperty(item => item.Status, OrderStatus.Cancelled), cancellationToken);
        if (claimed == 0)
        {
            throw new ShoppingConflictException("The order status changed; refresh the order before trying again.");
        }

        foreach (var item in order.Items)
        {
            await dbContext.Products
                .Where(product => product.Id == item.ProductId)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(product => product.Stock, product => product.Stock + item.Quantity)
                    .SetProperty(product => product.UpdatedAt, DateTimeOffset.UtcNow), cancellationToken);
        }

        order.Status = OrderStatus.Cancelled;
        if (order.Payment.Status == PaymentStatus.Paid)
        {
            order.Payment.Status = PaymentStatus.Refunded;
        }
        else if (order.Payment.Status == PaymentStatus.Pending)
        {
            order.Payment.Status = PaymentStatus.Failed;
        }
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return ToDto(order, order.Payment.Status);
    }

    internal static OrderDto ToDto(Order order, PaymentStatus paymentStatus) => new(
        order.Id,
        order.CreatedAt,
        order.Status,
        order.ContactName,
        order.ContactEmail,
        order.ContactPhone,
        order.ShippingAddressLabel,
        order.ShippingFullName,
        order.ShippingPhone,
        order.ShippingStreet,
        order.ShippingCity,
        order.ShippingCounty,
        order.ShippingPostalCode,
        order.ShippingCountry,
        order.DeliveryMethodName,
        order.DeliveryMethodPrice,
        order.PaymentMethod,
        paymentStatus,
        order.Subtotal,
        order.DeliveryCost,
        order.Total,
        order.Notes,
        order.Items.OrderBy(item => item.ProductName)
            .Select(item => new OrderItemDto(item.ProductId, item.ProductName, item.UnitPrice, item.Quantity, item.LineTotal))
            .ToArray());
}