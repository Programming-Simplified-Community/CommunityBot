using Blazorise.Extensions;
using Dashboard.Services;
using Data;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Dashboard.Components;

public class EntityOverviewComponent<TEntity, TPrimaryKey> : ComponentBase
    where TEntity : class, IEntityWithTypedId<TPrimaryKey>
{
    #region Required Services
    
    // I know this is weird looking but per blazor docs, and injection requirements - must be a property...
    [Inject] 
    private IBasicCrudService<TEntity, TPrimaryKey> Crud { get; set; }

    [Inject] 
    private ISnackbar _snackbar { get; set; }
    #endregion
    
    public IList<TEntity> Items { get; private set; } = new List<TEntity>();

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        Items = await Crud.GetAllAsync();
    }

    public async Task Create(TEntity entity)
    {
        entity = await Crud.CreateAsync(entity);
        Items.Add(entity);
        StateHasChanged();
    }

    public async Task Update(TEntity entity)
    {
        var response = await Crud.UpdateAsync(entity);

        if (response.IsSuccess)
        {
            _snackbar.Add("Saved", Severity.Success);
            StateHasChanged();
        }
        else
            _snackbar.Add(response.Message, Severity.Error);
    }

    public async Task Delete(TPrimaryKey id)
    {
        var response = await Crud.DeleteAsync(id);

        if (response.IsSuccess)
        {
            var item = Items.FirstOrDefault(x => x.Id.IsEqual(id));
            
            if (item is not null)
            {
                Items.Remove(item);
                StateHasChanged();
            }
            
            _snackbar.Add("Deleted", Severity.Success);
        }
        else
            _snackbar.Add(response.Message, Severity.Error);
    }

    public async Task Delete(Predicate<TEntity> predicate)
    {
        var response = await Crud.DeleteAllAsync(predicate);

        if (response.IsSuccess)
        {
            _snackbar.Add("Deleted", Severity.Success);
            Items = Items.Where(x => !predicate(x)).ToList();
            StateHasChanged();
        }
        else
            _snackbar.Add(response.Message, Severity.Error);
    }

    public async Task FilterItems(Predicate<TEntity> predicate)
    {
        Items = await Crud.GetAllAsync(predicate);
        StateHasChanged();
    }
}