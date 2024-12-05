using System;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using NejPortalBackend.Application.Common.Interfaces;
using NejPortalBackend.Application.Common.Models;
using NejPortalBackend.Domain.Entities;

namespace NejPortalBackend.Web.Hubs;

public class NotificationHub : Hub
{
    private readonly INotificationService _notificationService;

    public NotificationHub(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }
    // Method to send a notification to a specific user
    public async Task SendNotification(string userId, string message)
    {
        await _notificationService.SendNotificationAsync(userId, message);
    }

    // Mark a single notification as read
    public async Task MarkAsRead(int notificationId)
    {
        var userId = Context.UserIdentifier; // Gets the current user's ID
        if (userId != null)
        {
            await _notificationService.MarkNotificationAsReadAsync(notificationId, userId);
        }
    }

    // Mark all notifications as read for the current user
    public async Task MarkAllAsRead()
    {
        var userId = Context.UserIdentifier; // Gets the current user's ID
        if (userId != null)
        {
            await _notificationService.MarkAllNotificationsAsReadAsync(userId);
        }
    }

    // Get all unread notifications for the current user
    public async Task<List<NotificationDto>> GetUnreadNotifications()
    {
        var userId = Context.UserIdentifier; // Gets the current user's ID
        return userId != null
            ? await _notificationService.GetUnreadNotificationsAsync(userId)
            : new List<NotificationDto>();
    }
}

