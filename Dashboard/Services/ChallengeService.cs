using System.Net;
using Core.Validation;
using Data.Challenges;
using Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Dashboard.Services;

public class ChallengeService : IBasicCrudService<ProgrammingChallenge, int>
{
    private readonly ILogger<ChallengeService> _logger;
    private readonly SocialDbContext _context;

    public ChallengeService(ILogger<ChallengeService> logger, SocialDbContext context)
    {
        _logger = logger;
        _context = context;
    }

    /// <inheritdoc cref="IBasicCrudService{TEntity,TPrimaryKey}.GetAllAsync()"/>
    public async Task<IList<ProgrammingChallenge>> GetAllAsync()
     => await _context.ProgrammingChallenges
        .Include(x=>x.Tests)
        .ToListAsync();

    /// <inheritdoc cref="IBasicCrudService{TEntity,TPrimaryKey}.GetAllAsync(System.Linq.IQueryable{TEntity})"/>
    public async Task<IList<ProgrammingChallenge>> GetAllAsync(Predicate<ProgrammingChallenge> predicate)
        => await _context.ProgrammingChallenges
            .Include(x => x.Tests)
            .Where(x => predicate(x))
            .ToListAsync();

    /// <inheritdoc cref="IBasicCrudService{TEntity,TPrimaryKey}.GetAsync(TPrimaryKey)"/>
    public async Task<ProgrammingChallenge?> GetAsync(int id)
        => await _context.ProgrammingChallenges.Include(x => x.Tests).FirstOrDefaultAsync(x=>x.Id == id);

    /// <inheritdoc cref="IBasicCrudService{TEntity,TPrimaryKey}.GetAsync(System.Linq.IQueryable{TEntity})"/>
    public async Task<ProgrammingChallenge?> GetAsync(Predicate<ProgrammingChallenge> predicate)
        => await _context.ProgrammingChallenges
            .Include(x => x.Tests)
            .FirstOrDefaultAsync(x => predicate(x));

    /// <inheritdoc cref="IBasicCrudService{TEntity,TPrimaryKey}.CreateAsync"/>
    public Task<ProgrammingChallenge> CreateAsync(ProgrammingChallenge entity)
    {
        _context.ProgrammingChallenges.Add(entity);
        return Task.FromResult(entity);
    }

    /// <inheritdoc cref="IBasicCrudService{TEntity,TPrimaryKey}.UpdateAsync"/>
    public Task<ResultOf> UpdateAsync(ProgrammingChallenge entity)
    {
        try
        {
            _context.ProgrammingChallenges.Update(entity);
            return Task.FromResult(ResultOf.Success());
        }
        catch (Exception ex)
        {
            _logger.LogError("Error occurred while updating {TypeName}. {Error}",
                nameof(ProgrammingChallenge), ex.Message);
            return Task.FromResult(ResultOf.Error(ex.Message, HttpStatusCode.InternalServerError));
        }
    }

    /// <inheritdoc cref="IBasicCrudService{TEntity,TPrimaryKey}.DeleteAsync"/>
    public async Task<ResultOf> DeleteAsync(int id)
    {
        try
        {
            var result = await GetAsync(id);
            if (result is null)
                return ResultOf.NotFound();
            _context.ProgrammingChallenges.Remove(result);
            return ResultOf.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError("Error occurred while deleting record with Id: {Id}. {Error}",
                id, ex.Message);
            return ResultOf.Error(ex.Message, HttpStatusCode.InternalServerError);
        }
    }

    /// <inheritdoc cref="IBasicCrudService{TEntity,TPrimaryKey}.DeleteAllAsync"/>
    public async Task<ResultOf> DeleteAllAsync(Predicate<ProgrammingChallenge> predicate)
    {
        try
        {
            var results = await GetAllAsync(predicate);
            _context.ProgrammingChallenges.RemoveRange(results);
            return ResultOf.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError("Error occurred while deleting {TypeName} records",
                nameof(ProgrammingChallenge));
            return ResultOf.Error(ex.Message, HttpStatusCode.InternalServerError);
        }
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}