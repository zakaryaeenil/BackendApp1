using System;
using NejPortalBackend.Domain.Entities;

namespace NejPortalBackend.Application.Common.Models;

public class NotificationDto
{
    public string? Id { get; init; }
    public string? UserId { get; init; } // The ID of the user receiving the notification
    public string? Message { get; init; }
    public bool IsRead { get; init; }
    public DateTimeOffset CreatedAt { get; init; }

    private class Mapping : Profile
    {
        public Mapping()
        {
            CreateMap<Notification, NotificationDto>()
                .ForMember(d => d.CreatedAt, opt => opt.MapFrom(s => s.Created));

        }
    }
}

