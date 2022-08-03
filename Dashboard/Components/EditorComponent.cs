using Dashboard.Utility;
using Microsoft.AspNetCore.Components;

namespace Dashboard.Components;

public class EditorComponent : ComponentBase
{
    protected Dictionary<string, PropInfo> Properties = new();
    
    protected void InitializeProperties(Type type)
    {
        Properties = EditorWizard.ExtractPropertyInfo(type);
    }
}