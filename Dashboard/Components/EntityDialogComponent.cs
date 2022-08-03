using Data;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Dashboard.Components;

public class EntityDialogComponent<TEntity, TPrimaryKey> : ComponentBase
    where TEntity : class, IEntityWithTypedId<TPrimaryKey>, new()
{
    [Inject] protected ISnackbar Snackbar { get; set; }
    [CascadingParameter] private MudDialogInstance Dialog { get; set; }
    [Parameter] public string? InvalidErrorMessage { get; set; }

    protected override void OnInitialized()
    {
        base.OnInitialized();
        if (string.IsNullOrEmpty(InvalidErrorMessage))
            InvalidErrorMessage = "You have some errors to fix";
    }

    protected bool IsValid;
    protected string[] Errors = Array.Empty<string>();
    
    protected TEntity Model = new();

    protected virtual void Cancel() => Dialog.Cancel();
    
    protected virtual void Submit()
    {
        if (!IsValid)
        {
            Snackbar.Add(InvalidErrorMessage, Severity.Error);
            return;
        }

        Dialog.Close(DialogResult.Ok(Model));
    }

    private void BuildModelEditor()
    {
        
    }
}