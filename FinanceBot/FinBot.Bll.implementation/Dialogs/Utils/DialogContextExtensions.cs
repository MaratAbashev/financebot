using FinBot.Domain.Models;

namespace FinBot.Bll.Implementation.Dialogs.Utils;

public static class DialogContextExtensions
{
    public static bool TryGetData<T>(this DialogContext dialogContext, string key, out T data) where T : IConvertible?
    {
        data = default!;
    
        if (dialogContext.DialogStorage == null
            || !dialogContext.DialogStorage.TryGetValue(key, out var boxedData))
        {
            return false;
        }

        try
        {
            if (boxedData is T directData)
            {
                data = directData;
                return true;
            }
        
            data = (T)Convert.ChangeType(boxedData, typeof(T));
            return true;
        }
        catch
        {
            return false;
        }
    }
}