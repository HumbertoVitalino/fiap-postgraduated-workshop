using Fiap.Workshop.Application.Interfaces.Repositories;
using Fiap.Workshop.Domain.Entities;
using Fiap.Workshop.Infrastructure.Repositories.Mappers;
using Microsoft.EntityFrameworkCore;

namespace Fiap.Workshop.Infrastructure.Repositories;

internal sealed class RefreshTokenRepository(AppDbContext context) : Repository<RefreshToken>(context), IRefreshTokenRepository
{
    public override async Task<RefreshToken?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var model = await _context.RefreshTokens
            .AsNoTracking()
            .FirstOrDefaultAsync(rt => rt.Id == id, cancellationToken);

        return model?.MapToDomain();
    }

    public async Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken)
    {
        var model = await _context.RefreshTokens
            .AsNoTracking()
            .FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash, cancellationToken);

        return model?.MapToDomain();
    }

    public async Task<IReadOnlyCollection<RefreshToken>> GetAllActiveByUserIdAsync(Guid userId, CancellationToken cancellationToken)
    {
        var now = DateTime.Now;

        var models = await _context.RefreshTokens
            .AsNoTracking()
            .Where(rt => rt.UserId == userId && rt.RevokedAt == null && rt.ExpiresAt > now)
            .ToListAsync(cancellationToken);

        return models.Select(model => model.MapToDomain()).ToList();
    }

    public override async Task AddAsync(RefreshToken entity, CancellationToken cancellationToken)
    {
        EnqueueEvents(entity);
        await _context.RefreshTokens.AddAsync(entity.MapToModel(), cancellationToken);
    }

    public override void Update(RefreshToken entity)
    {
        EnqueueEvents(entity);
        Upsert(_context.RefreshTokens, entity.MapToModel(), entity.Id);
    }

    public override void Remove(RefreshToken entity) =>
        Delete(_context.RefreshTokens, entity.MapToModel(), entity.Id);
}
