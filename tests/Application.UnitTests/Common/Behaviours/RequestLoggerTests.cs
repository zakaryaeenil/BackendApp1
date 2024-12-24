using NejPortalBackend.Application.Common.Behaviours;
using NejPortalBackend.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;
using NejPortalBackend.Application.Operations.Commands.CreateOperation;
using NUnit.Framework;

namespace NejPortalBackend.Application.UnitTests.Common.Behaviours;

public class RequestLoggerTests
{
    private Mock<ILogger<CreateOperationCommand>> _logger = null!;
    private Mock<IUser> _user = null!;
    private Mock<IIdentityService> _identityService = null!;

    [SetUp]
    public void Setup()
    {
        _logger = new Mock<ILogger<CreateOperationCommand>>();
        _user = new Mock<IUser>();
        _identityService = new Mock<IIdentityService>();
    }
}
