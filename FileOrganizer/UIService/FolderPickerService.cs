using Avalonia.Controls;
using Avalonia.Platform.Storage;
using FileOrganizer.Views;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FileOrganizer.UIService;

public class FolderPickerService(MainWindow window) : IFolderPickerService
{
    public async Task<IReadOnlyList<IStorageFolder>> OpenFolderPickerAsync(FolderPickerOpenOptions options)
    {
        var topLevel = TopLevel.GetTopLevel(window);
        if (topLevel is not null)
        {
            return await topLevel.StorageProvider.OpenFolderPickerAsync(options);
        }
        return new List<IStorageFolder>();
    }
}