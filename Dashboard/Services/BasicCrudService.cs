using System.Net;
using Core.Validation;
using Data;
using Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Dashboard.Services;

public class BasicCrudService<TEntity, TPrimaryKey> : IBasicCrudService<TEntity, TPrimaryKey>
    where TEntity : class, IEntityWithTypedId<TPrimaryKey>
{
    protected readonly SocialDbContext Context;
    protected readonly DbSet<TEntity> Table;
    private readonly ILogger<BasicCrudService<TEntity, TPrimaryKey>> _logger;
    
    public BasicCrudService(SocialDbContext context, ILogger<BasicCrudService<TEntity, TPrimaryKey>> logger)
    {
        Context = context;
        _logger = logger;
        Table = context.Set<TEntity>();
    }

    public virtual async Task<IList<TEntity>> GetAllAsync()
        => await Table.ToListAsync();

    public virtual async Task<IList<TEntity>> GetAllAsync(Predicate<TEntity> predicate)
        => await Table.Where(x => predicate(x)).ToListAsync();

    public virtual async Task<TEntity?> GetAsync(TPrimaryKey id)
        => await Table.FirstOrDefaultAsync(x => x.Id!.Equals(id));

    public virtual async Task<TEntity?> GetAsync(Predicate<TEntity> predicate)
        => await Table.FirstOrDefaultAsync(x => predicate(x));

    public virtual Task<TEntity> CreateAsync(TEntity entity)
    {
        Table.Add(entity);
        return Task.FromResult(entity);
    }

    public virtual Task<ResultOf> UpdateAsync(TEntity entity)
    {
        try
        {
            Table.Update(entity);
            return Task.FromResult(ResultOf.Success());
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to update {TypeName}. {Error}", typeof(TEntity).Name, ex);
            return Task.FromResult(ResultOf.Error(ex.Message, HttpStatusCode.InternalServerError));
        }
    }

    public virtual async Task<ResultOf> DeleteAsync(TPrimaryKey id)
    {
        try
        {
            var item = await GetAsync(id);
            if (item is null)
                return ResultOf.NotFound();
            Table.Remove(item);
            return ResultOf.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError("Unable to delete {TypeName} with Id {Id}. {Error}",
                typeof(TEntity).Name,
                id,
                ex);
            return ResultOf.Error(ex.Message, HttpStatusCode.InternalServerError);
        }
    }

    public virtual async Task<ResultOf> DeleteAllAsync(Predicate<TEntity> predicate)
    {
        try
        {
            var items = await GetAllAsync(predicate);
            Table.RemoveRange(items);
            return ResultOf.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError("Unable to delete {TypeName} items", typeof(TEntity).Name);
            return ResultOf.Error(ex.Message, HttpStatusCode.InternalServerError);
        }
    }

    public async Task SaveChangesAsync()
    {
        await Context.SaveChangesAsync();
    }
}