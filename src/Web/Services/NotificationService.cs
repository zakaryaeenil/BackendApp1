using System;
using System.Threading;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using NejPortalBackend.Application.Common.Interfaces;
using NejPortalBackend.Application.Common.Models;
using NejPortalBackend.Domain.Entities;
using NejPortalBackend.Infrastructure.Data;
using NejPortalBackend.Web.Hubs;

namespace NejPortalBackend.Web.Services;

public class NotificationService : INotificationService
{
    private readonly IHubContext<NotificationHub> _hubContext;
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;
    public NotificationService(IHubContext<NotificationHub> hubContext, IApplicationDbContext context, IMapper mapper)
    {
        _hubContext = hubContext;
        _context = context;
        _mapper = mapper;
    }

    // Method to send a notification
    public async Task SendNotificationAsync(string userId, string message, CancellationToken cancellationToken = default)
    {
        // Create a new notification object
        var notification = new Notification
        {
            UserId = userId,
            Message = message,
            IsRead = false
        };

        // Save the notification to the database
        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync(cancellationToken);

        // Send the notification to the user's SignalR connection
        await _hubContext.Clients.User(userId).SendAsync("ReceiveNotification", notification);
    }

    public async Task MarkNotificationAsReadAsync(int notificationId, string userId, CancellationToken cancellationToken = default)
    {
        var notification = await _context.Notifications
            .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId, cancellationToken);
        if (notification != null)
        {
            notification.IsRead = true;
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task MarkAllNotificationsAsReadAsync(string userId, CancellationToken cancellationToken = default)
    {
        var notifications = await _context.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ToListAsync(cancellationToken);

        notifications.ForEach(n => n.IsRead = true);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<List<NotificationDto>> GetUnreadNotificationsAsync(string userId)
    {
        return await _context.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ProjectTo<NotificationDto>(_mapper.ConfigurationProvider)
            .ToListAsync();
    }
}

