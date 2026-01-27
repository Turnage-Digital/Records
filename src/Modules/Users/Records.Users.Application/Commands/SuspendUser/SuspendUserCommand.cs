using MediatR;
using Records.Core.Domain.ValueObjects;

namespace Records.Users.Application.Commands.SuspendUser;

public sealed record SuspendUserCommand(
    UlidId UserId,
    string? Reason,
    DateTimeOffset SuspendedAt
) : IRequest;