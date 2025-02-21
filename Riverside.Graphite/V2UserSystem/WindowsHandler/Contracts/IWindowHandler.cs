using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using System;
using System.Threading.Tasks;
using Windows.Storage;

namespace Graphite.WindowCore.Contracts;

public interface IWindowHandler
{
    Window MainWindow { get; }
    IntPtr Hwnd { get; }
    AppWindow AppWindow { get; }
    TitleBar TitleBar { get; }
    void Initialize(Window window);
    void SetWindowSize(int width, int height);
    void SetWindowPosition(int x, int y);
    void CenterOnScreen();
    void Maximize();
    void Minimize();
    void Restore();
    void SetWindowStyle(WindowStyle style);
    void SetWindowBackdrop(BackdropType backdropType);
    void SetIcon(string iconPath);
    void SetTitle(string title);
    Task<StorageFile> PickSaveFileAsync(string suggestedFileName = null);
    Task<StorageFile> PickOpenFileAsync(params string[] fileTypes);
    void EnableDragToMove();
    void SaveWindowPosition();
    void RestoreWindowPosition();
    void Cleanup();
}
