using System;
using MediatR;
using NejPortalBackend.Application.Common.Models;

namespace NejPortalBackend.Application.Common.Interfaces;

public interface INotificationService
{

    Task SendNotificationAsync(string userId, string message, CancellationToken cancellationToken = default);
    Task MarkNotificationAsReadAsync(int notificationId, string userId, CancellationToken cancellationToken = default);
    Task MarkAllNotificationsAsReadAsync(string userId, CancellationToken cancellationToken = default);
    Task<List<NotificationDto>> GetUnreadNotificationsAsync(string userId);

}

