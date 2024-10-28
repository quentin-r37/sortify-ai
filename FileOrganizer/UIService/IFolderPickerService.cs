using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;

namespace FileOrganizer.UIService;

public interface IFolderPickerService
{
    Task<IReadOnlyList<IStorageFolder>> OpenFolderPickerAsync(FolderPickerOpenOptions options);
}