using System;
using Avalonia;
using Avalonia.Media;
using Projektanker.Icons.Avalonia;
using Projektanker.Icons.Avalonia.FontAwesome;

namespace FileOrganizer;

internal sealed class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
    {
        IconProvider.Current
            .Register<FontAwesomeIconProvider>();

        return AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
    }
}

public static class AvaloniaAppBuilderExtensions
{
    private static string DefaultFontFamily => "avares://FileOrganizer/Assets#Source Han Sans CN";

    public static AppBuilder WithSourceHanSansCNFont(this AppBuilder builder)
    {
        return builder.With(new FontManagerOptions
        {
            DefaultFamilyName = DefaultFontFamily,
            FontFallbacks = new[] { new FontFallback { FontFamily = new FontFamily(DefaultFontFamily) } }
        });
    }
}