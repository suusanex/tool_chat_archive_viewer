using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Serilog;
using Serilog.Events;

namespace ChatArchiveViewer.App;

public partial class App : Application
{
    private Window? window;

    public App()
    {
        InitializeComponent();

        Host = Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder()
            .ConfigureServices(
                (_, services) =>
                {
                    services.AddSingleton(AppLaunchOptions.Parse(Environment.GetCommandLineArgs()));
                    services.AddSingleton<IWindowProvider, WindowProvider>();
                    services.AddSingleton<IBundledSampleLocator>(_ => new BundledSampleLocator(AppContext.BaseDirectory));
                    services.AddSingleton<IAppSettingsService, AppSettingsService>();
                    services.AddSingleton<IExternalLauncher, ExternalLauncher>();
                    services.AddSingleton<ArchiveSessionService>();
                    services.AddSingleton<IArchiveSessionService>(sp => sp.GetRequiredService<ArchiveSessionService>());
                    services.AddSingleton<IConversationDayMessageCountSource>(sp => sp.GetRequiredService<ArchiveSessionService>());
                    services.AddSingleton<IConversationDateCountService, ConversationDateCountService>();
                    services.AddSingleton<IArchiveOpenService, ArchiveOpenService>();
                    services.AddSingleton<IArchiveWorkflowService, ArchiveWorkflowService>();

                    services.AddSingleton<IArchiveFormatRegistry, ArchiveFormatRegistry>();
                    services.AddSingleton<IArchiveLoadService, ArchiveLoadService>();
                    services.AddSingleton<SearchService>();
                    services.AddSingleton<DateFilterService>();
                    services.AddSingleton<IArchiveFormatProvider, SlackFormatProvider>();

                    services.AddSingleton<ArchiveOverviewViewModel>();
                    services.AddSingleton<MessageListViewModel>();
                    services.AddSingleton<ArchiveBrowseViewModel>();
                    services.AddSingleton<SearchViewModel>();
                    services.AddSingleton<AboutViewModel>();
                    services.AddSingleton<SettingsViewModel>();
                    services.AddSingleton<MainPageViewModel>();

                    services.AddTransient<Views.MainPage>();
                    services.AddTransient<Views.BrowsePage>();
                    services.AddTransient<Views.MainWindow>();
                    services.AddTransient<Views.AboutPage>();
                    services.AddTransient<Views.SettingsPage>();
                    services.AddTransient<Views.SearchPage>();
                })
            .ConfigureLogging(
                logging =>
                {
                    logging.ClearProviders();
                    var appDataRoot = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                        "ChatArchiveViewer");
                    Directory.CreateDirectory(appDataRoot);
                    var logDirectory = Path.Combine(appDataRoot, "Logs");
                    Directory.CreateDirectory(logDirectory);
                    var logPath = Path.Combine(logDirectory, "app-.log");
                    var logger = new LoggerConfiguration()
                        .MinimumLevel.Information()
                        .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                        .WriteTo.File(logPath, rollingInterval: RollingInterval.Day, retainedFileCountLimit: 14)
                        .CreateLogger();
                    logging.AddSerilog(logger, dispose: true);
                })
            .Build();
    }

    public IHost Host { get; }

    public Window? MainWindow => window;

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        _ = args;
        window ??= Host.Services.GetRequiredService<Views.MainWindow>();
        ApplyWindowIdentity(window);

        var windowProvider = Host.Services.GetRequiredService<IWindowProvider>();
        windowProvider.CurrentWindow = window;

        window.Activate();
        ApplyWindowIdentity(window);
    }

    private static void ApplyWindowIdentity(Window window)
    {
        ArgumentNullException.ThrowIfNull(window);
        window.Title = AppIdentity.AppName;
        window.AppWindow.Title = AppIdentity.AppName;
    }
}
