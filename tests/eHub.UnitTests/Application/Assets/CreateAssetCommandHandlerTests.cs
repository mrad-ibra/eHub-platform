using eHub.Application.Assets.Abstractions;
using eHub.Application.Assets.Commands.CreateAsset;
using eHub.Application.Catalog.Abstractions;
using eHub.Application.Common.Context;
using eHub.Application.Common.Persistence;
using eHub.Application.Common.Time;
using eHub.Domain.Assets;
using eHub.Domain.Catalog;

namespace eHub.UnitTests.Application.Assets;

public sealed class CreateAssetCommandHandlerTests
{
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IAssetRepository _assets = Substitute.For<IAssetRepository>();
    private readonly ICategoryRepository _categories = Substitute.For<ICategoryRepository>();
    private readonly ISubCategoryRepository _subCategories = Substitute.For<ISubCategoryRepository>();
    private readonly IClock _clock = Substitute.For<IClock>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly DateTime _now = new(2026, 7, 18, 12, 0, 0, DateTimeKind.Utc);
    private readonly Guid _ownerId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private readonly Guid _categoryId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    public CreateAssetCommandHandlerTests()
    {
        _clock.UtcNow.Returns(_now);
        _currentUser.RequireUserId().Returns(_ownerId);
        _categories.GetByIdAsync(_categoryId, Arg.Any<CancellationToken>())
            .Returns(Category.Create("VEHICLE", "Vehicles", _now));
    }

    [Fact]
    public async Task Handle_CreatesAssetForCurrentUser()
    {
        var handler = new CreateAssetCommandHandler(
            _currentUser,
            _assets,
            _categories,
            _subCategories,
            _clock,
            _unitOfWork);

        var id = await handler.Handle(
            new CreateAssetCommand(_categoryId, "My Asset"),
            CancellationToken.None);

        id.Should().NotBeEmpty();
        await _assets.Received(1).AddAsync(
            Arg.Is<Asset>(a => a.OwnerId == _ownerId && a.Title == "My Asset" && a.CategoryId == _categoryId),
            Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
