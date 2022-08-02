using Core.Validation;
using Data;

namespace Dashboard.Services;

/// <summary>
/// Basic contract required to perform Crud operations for our database models
/// </summary>
/// <typeparam name="TEntity">Entity type that shall be managed</typeparam>
/// <typeparam name="TPrimaryKey">Primary key of <typeparamref name="TEntity"/>, will help shape certain methods</typeparam>
public interface IBasicCrudService<TEntity, TPrimaryKey>
    where TEntity : class, IEntityWithTypedId<TPrimaryKey>
{
    /// <summary>
    /// Get all records of <typeparamref name="TEntity"/>
    /// </summary>
    /// <returns>Collection of <typeparamref name="TEntity"/></returns>
    Task<IList<TEntity>> GetAllAsync();
    
    /// <summary>
    /// Gets all records of <typeparamref name="TEntity"/> that fits the criteria defined by
    /// <paramref name="predicate"/>
    /// </summary>
    /// <param name="predicate">Filter to apply when querying database</param>
    /// <returns>Collection of <typeparamref name="TEntity"/> that meets criteria</returns>
    Task<IList<TEntity>> GetAllAsync(Predicate<TEntity> predicate);

    /// <summary>
    /// Retrieves a record from database with matching <paramref name="id"/>
    /// </summary>
    /// <param name="id">Primary key to search for on <typeparamref name="TEntity"/></param>
    /// <returns><typeparamref name="TEntity"/> if found, otherwise null</returns>
    Task<TEntity?> GetAsync(TPrimaryKey id);
    
    /// <summary>
    /// Retrieves a record from database that fits <paramref name="filter"/>. If more than one record
    /// is found, it returns the first item from result set.
    /// </summary>
    /// <param name="predicate">Filter to apply when querying the database</param>
    /// <returns><typeparamref name="TEntity"/> if found, otherwise null.</returns>
    Task<TEntity?> GetAsync(Predicate<TEntity> predicate);

    /// <summary>
    /// Adds a new <typeparam name="TEntity"></typeparam> to the database
    /// </summary>
    /// <param name="entity"><typeparamref name="TEntity"/> that should get added</param>
    /// <returns><paramref name="entity"/> with the assigned primary key</returns>
    Task<TEntity> CreateAsync(TEntity entity);
    
    /// <summary>
    /// Updates the <paramref name="entity"/> record in the database
    /// </summary>
    /// <param name="entity"><typeparamref name="TEntity"/> that should get updated</param>
    /// <returns>A <see cref="ResultOf"/> instance with success, or failure if something didn't work</returns>
    Task<ResultOf> UpdateAsync(TEntity entity);

    /// <summary>
    /// Deletes a <typeparamref name="TEntity"/> from database with matching Id
    /// </summary>
    /// <param name="id">Delete record with matching Id</param>
    /// <returns><see cref="ResultOf"/> with success if item was deleted. Or failure if something came up</returns>
    Task<ResultOf> DeleteAsync(TPrimaryKey id);
    
    /// <summary>
    /// Deletes a <typeparamref name="TEntity"/> from database that fits the criteria defined in <paramref name="predicate"/>
    /// </summary>
    /// <param name="predicate">Filter to apply when querying the database</param>
    /// <returns><see cref="ResultOf"/> with success if things were able to be deleted. Otherwise, failure if something bad happened</returns>
    Task<ResultOf> DeleteAllAsync(Predicate<TEntity> predicate);

    /// <summary>
    /// Enables the manual saving of database context.
    /// </summary>
    Task SaveChangesAsync();
}