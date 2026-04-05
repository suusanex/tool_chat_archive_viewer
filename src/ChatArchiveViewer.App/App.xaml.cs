using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using ChatArchiveViewer.CloudFetch;
using ChatArchiveViewer.CloudFetch.Abstractions;
using ChatArchiveViewer.CloudFetch.Services;

namespace ChatArchiveViewer.App;

public partial class App : Application
{
    private Window? window;

    public App()
    {
        InitializeComponent();

        Host = Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration(
                (_, configurationBuilder) =>
                {
                    foreach (var appSettingsPath in AppSettingsPathResolver.GetConfigurationFilePaths())
                    {
                        configurationBuilder.AddJsonFile(appSettingsPath, optional: true, reloadOnChange: false);
                    }
                })
            .ConfigureServices(
                (hostContext, services) =>
                {
                    var bootstrapConfigUrl = CloudFetchConstants.GetBootstrapConfigUrl(
                        hostContext.Configuration[CloudFetchConstants.BootstrapConfigUrlConfigurationKey]);
                    var cloudFetchFeatureOptions = new CloudFetchFeatureOptions(bootstrapConfigUrl);

                    services.AddSingleton(AppLaunchOptions.Parse(Environment.GetCommandLineArgs()));
                    services.AddSingleton(cloudFetchFeatureOptions);
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

                    if (cloudFetchFeatureOptions.IsEnabled)
                    {
                        services.AddSingleton<HttpClient>();
                        services.AddSingleton<IBootstrapConfigProvider>(sp =>
                            new BootstrapConfigProvider(
                                sp.GetRequiredService<HttpClient>(),
                                sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<BootstrapConfigProvider>>(),
                                cloudFetchFeatureOptions.BootstrapConfigUrl!));
                        services.AddSingleton<ICloudAuthService, MsalAuthService>();
                        services.AddSingleton<ICloudManifestProvider, CloudManifestProvider>();
                        services.AddSingleton<ICloudArchiveDownloader, CloudArchiveDownloader>();
                        services.AddSingleton<ICacheManager>(sp =>
                            new LocalCacheManager(
                                sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<LocalCacheManager>>(),
                                cacheDirectory: null,
                                bootstrapConfigUrl: cloudFetchFeatureOptions.BootstrapConfigUrl));
                        services.AddSingleton<IHashVerifier, Sha256Verifier>();
                        services.AddSingleton<ICloudFetchOrchestrator, CloudFetchOrchestrator>();
                    }
                    else
                    {
                        services.AddSingleton<ICloudFetchOrchestrator, DisabledCloudFetchOrchestrator>();
                    }

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
                    AppLogging.ConfigureLogging(logging);
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
