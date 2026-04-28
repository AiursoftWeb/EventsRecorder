using Aiursoft.Scanner.Abstractions;
using Aiursoft.EventsRecorder.Configuration;
using Aiursoft.EventsRecorder.Controllers;
using Aiursoft.EventsRecorder.Entities;
using Aiursoft.EventsRecorder.Services.Authentication;
using Aiursoft.EventsRecorder.Services.FileStorage;
using Aiursoft.UiStack.Layout;
using Aiursoft.UiStack.Navigation;
using Aiursoft.UiStack.Views.Shared.Components.FooterMenu;
using Aiursoft.UiStack.Views.Shared.Components.LanguagesDropdown;
using Aiursoft.UiStack.Views.Shared.Components.MegaMenu;
using Aiursoft.UiStack.Views.Shared.Components.Navbar;
using Aiursoft.UiStack.Views.Shared.Components.SideAdvertisement;
using Aiursoft.UiStack.Views.Shared.Components.Sidebar;
using Aiursoft.UiStack.Views.Shared.Components.SideLogo;
using Aiursoft.UiStack.Views.Shared.Components.SideMenu;
using Aiursoft.UiStack.Views.Shared.Components.UserDropdown;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using System.Diagnostics.CodeAnalysis;

namespace Aiursoft.EventsRecorder.Services;

public class ViewModelArgsInjector(
    IStringLocalizer<ViewModelArgsInjector> localizer,
    StorageService storageService,
    NavigationState<Startup> navigationState,
    IAuthorizationService authorizationService,
    IOptions<AppSettings> appSettings,
    GlobalSettingsService globalSettingsService,
    SignInManager<User> signInManager) : IScopedDependency
{

    [ExcludeFromCodeCoverage]
    // ReSharper disable once UnusedMember.Local
    private void _useless_for_localizer()
    {
        // Titles, navbar strings.
        _ = localizer["Features"];
        _ = localizer["Index"];
        _ = localizer["Directory"];
        _ = localizer["Users"];
        _ = localizer["Roles"];
        _ = localizer["Administration"];
        _ = localizer["System"];
        _ = localizer["Info"];
        _ = localizer["Manage"];
        _ = localizer["Login"];
        _ = localizer["System Info"];
        _ = localizer["Create User"];
        _ = localizer["User Details"];
        _ = localizer["Edit User"];
        _ = localizer["Delete User"];
        _ = localizer["Create Role"];
        _ = localizer["Role Details"];
        _ = localizer["Edit Role"];
        _ = localizer["Delete Role"];
        _ = localizer["Change Profile"];
        _ = localizer["Change Avatar"];
        _ = localizer["Change Password"];
        _ = localizer["Home"];
        _ = localizer["Settings"];
        _ = localizer["Profile Settings"];
        _ = localizer["Personal"];
        _ = localizer["Unauthorized"];
        _ = localizer["Error"];
        _ = localizer["Permissions"];
        _ = localizer["Background Jobs"];
        _ = localizer["Global Settings"];

        _ = localizer["Access Denied"];
        _ = localizer["Bad Request"];
        _ = localizer["Dashboard"];
        _ = localizer["Internal Server Error"];
        _ = localizer["Lockout"];
        _ = localizer["Not Found"];
        _ = localizer["Permission Details"];
        _ = localizer["Register"];
    
        _ = localizer["Create Event Type"];
        _ = localizer["Delete Event Type"];
        _ = localizer["Edit Event Type"];
        _ = localizer["Event Type Details"];
        _ = localizer["Event Types"];
        _ = localizer["Events"];
        _ = localizer["My Records"];

        // Plugins nav group
        _ = localizer["Insights"];
        _ = localizer["My Plugins"];

        // Plugins index page
        _ = localizer["Plugins calculate derived metrics from your event records. Configure a plugin to activate it."];
        _ = localizer["Enabled"];
        _ = localizer["Not configured"];
        _ = localizer["Configure this plugin to start seeing metrics."];
        _ = localizer["No data yet. Start recording events to see metrics."];
        _ = localizer["Configure"];
        _ = localizer["Disable"];
        _ = localizer["Are you sure you want to disable this plugin?"];

        // Plugins configure page
        _ = localizer["Configure Plugin"];
        _ = localizer["Event Type"];
        _ = localizer["-- Select Event Type --"];
        _ = localizer["No event types found."];
        _ = localizer["Create one first."];
        _ = localizer["Numeric Field"];
        _ = localizer["-- Select Numeric Field --"];
        _ = localizer["No numeric fields found for the selected event type."];
        _ = localizer["Select the numeric field that this plugin should analyze."];
        _ = localizer["Save Changes"];
        _ = localizer["Enable Plugin"];
        _ = localizer["Cancel"];
        _ = localizer["Available Metrics"];
        _ = localizer["Metric"];
        _ = localizer["Unit"];
        _ = localizer["Description"];

        // Plugin names
        _ = localizer["Abstinence Score"];
        _ = localizer["Exercise Analytics"];
        _ = localizer["Habit Streak"];
        _ = localizer["Mood Tracker"];
        _ = localizer["Sleep Analysis"];
        _ = localizer["Weight Trend"];

        // Plugin descriptions
        _ = localizer["Tracks self-control for any behavior you want to reduce. Score recovers +10/day (max 100) and halves on each event."];
        _ = localizer["Computes Fitness (42-day load), Fatigue (7-day load), and Form using the CTL/ATL training-load model. Supports multiple sources."];
        _ = localizer["Tracks your daily habit streak, all-time longest streak, and 30-day completion rate. Any day with at least one record counts as a completed day."];
        _ = localizer["Tracks mood trends with an exponential moving average (\u03b1=0.2), 7/30-day averages, and 30-day volatility (standard deviation)."];
        _ = localizer["Analyzes sleep duration and consistency. Tracks your average sleep time and bedtime stability over the past 30 days."];
        _ = localizer["Tracks body weight with moving averages and a weekly trend direction computed via linear regression over the past 30 days."];

        // Metric names
        _ = localizer["Days Since Last Event"];
        _ = localizer["Fitness (CTL)"];
        _ = localizer["Fatigue (ATL)"];
        _ = localizer["Form (CTL \u2212 ATL)"];
        _ = localizer["Current Streak"];
        _ = localizer["Longest Streak"];
        _ = localizer["30-Day Completion Rate"];
        _ = localizer["Mood EMA"];
        _ = localizer["7-Day Average"];
        _ = localizer["30-Day Average"];
        _ = localizer["30-Day Volatility"];
        _ = localizer["Average Sleep Duration"];
        _ = localizer["Bedtime Stability"];
        _ = localizer["Latest Weight"];
        _ = localizer["Weekly Trend"];

        // Metric units
        _ = localizer["pts"];
        _ = localizer["days"];
        _ = localizer["kcal/session"];
        _ = localizer["%"];
        _ = localizer["hrs"];
        _ = localizer["hrs \u03c3"];
        _ = localizer["kg"];
        _ = localizer["kg/week"];

        // Metric descriptions
        _ = localizer["Recovers +10/day (max 100). Each event: \u00f72."];
        _ = localizer["Days elapsed since the most recent recorded event."];
        _ = localizer["Average calories per session over the past 42 days."];
        _ = localizer["Average calories per session over the past 7 days."];
        _ = localizer["Fitness minus Fatigue. Positive = fresh; negative = fatigued."];
        _ = localizer["Consecutive days with at least one recorded event."];
        _ = localizer["All-time longest consecutive streak."];
        _ = localizer["Percentage of days in the past 30 days with at least one record."];
        _ = localizer["Exponential moving average of mood (\u03b1=0.2)."];
        _ = localizer["Average mood over the past 7 days."];
        _ = localizer["Average mood over the past 30 days."];
        _ = localizer["Standard deviation of mood over the past 30 days."];
        _ = localizer["Average sleep duration over the past 30 days."];
        _ = localizer["Standard deviation of sleep duration over the past 30 days."];
        _ = localizer["Most recent recorded weight."];
        _ = localizer["Average weight over the past 7 days."];
        _ = localizer["Average weight over the past 30 days."];
        _ = localizer["Rate of change over 30 days. Positive = gaining, negative = losing."];
    }

    public void InjectSimple(
        HttpContext context,
        UiStackLayoutViewModel toInject)
    {
        toInject.PageTitle = localizer[toInject.PageTitle ?? "View"];
        toInject.AppName = globalSettingsService.GetSettingValueAsync(SettingsMap.ProjectName).GetAwaiter().GetResult();
        toInject.Theme = UiTheme.Light;
        toInject.SidebarTheme = UiSidebarTheme.Default;
        toInject.Layout = UiLayout.Fluid;
        toInject.ContentNoPadding = true;
    }

    public void Inject(
        HttpContext context,
        UiStackLayoutViewModel toInject)
    {
        var preferDarkTheme = context.Request.Cookies[ThemeController.ThemeCookieKey] == true.ToString();
        var projectName = globalSettingsService.GetSettingValueAsync(SettingsMap.ProjectName).GetAwaiter().GetResult();
        var brandName = globalSettingsService.GetSettingValueAsync(SettingsMap.BrandName).GetAwaiter().GetResult();
        var brandHomeUrl = globalSettingsService.GetSettingValueAsync(SettingsMap.BrandHomeUrl).GetAwaiter().GetResult();
        toInject.PageTitle = localizer[toInject.PageTitle ?? "View"];
        toInject.AppName = projectName;
        toInject.Theme = preferDarkTheme ? UiTheme.Dark : UiTheme.Light;
        toInject.SidebarTheme = preferDarkTheme ? UiSidebarTheme.Dark : UiSidebarTheme.Default;
        toInject.Layout = UiLayout.Fluid;
        toInject.FooterMenu = new FooterMenuViewModel
        {
            AppBrand = new Link { Text = brandName, Href = brandHomeUrl },
            Links =
            [
                new Link { Text = localizer["Home"], Href = "/" },
                new Link { Text = "Aiursoft", Href = "https://www.aiursoft.com" },
            ]
        };
        toInject.Navbar = new NavbarViewModel
        {
            ThemeSwitchApiCallEndpoint = "/api/switch-theme"
        };

        var currentViewingController = context.GetRouteValue("controller")?.ToString();
        var navGroupsForView = new List<NavGroup>();

        foreach (var groupDef in navigationState.NavMap)
        {
            var itemsForView = new List<CascadedSideBarItem>();
            foreach (var itemDef in groupDef.Items)
            {
                var linksForView = new List<CascadedLink>();
                foreach (var linkDef in itemDef.Links)
                {
                    bool isVisible;
                    if (string.IsNullOrEmpty(linkDef.RequiredPolicy))
                    {
                        isVisible = true;
                    }
                    else
                    {
                        var authResult = authorizationService.AuthorizeAsync(context.User, linkDef.RequiredPolicy).GetAwaiter().GetResult();
                        isVisible = authResult.Succeeded;
                    }

                    if (isVisible)
                    {
                        linksForView.Add(new CascadedLink
                        {
                            Href = linkDef.Href,
                            Text = localizer[linkDef.Text]
                        });
                    }
                }

                if (linksForView.Any())
                {
                    itemsForView.Add(new CascadedSideBarItem
                    {
                        UniqueId = itemDef.UniqueId,
                        Text = localizer[itemDef.Text],
                        LucideIcon = itemDef.Icon,
                        IsActive = linksForView.Any(l =>
                        {
                            // Extract controller name from href (e.g., "/Manage/Index" -> "Manage")
                            var hrefController = l.Href.TrimStart('/').Split('/').FirstOrDefault();
                            // Exact match to avoid false positives like "Manage" matching "ManagePayroll"
                            return string.Equals(hrefController, currentViewingController, StringComparison.OrdinalIgnoreCase);
                        }),
                        Links = linksForView
                    });
                }
            }

            if (itemsForView.Any())
            {
                navGroupsForView.Add(new NavGroup
                {
                    Name = localizer[groupDef.Name],
                    Items = itemsForView.Select(t => (SideBarItem)t).ToList()
                });
            }
        }

        toInject.Sidebar = new SidebarViewModel
        {
            SideLogo = new SideLogoViewModel
            {
                AppName = projectName,
                LogoUrl = GetLogoUrl(context).GetAwaiter().GetResult(),
                Href = "/"
            },
            SideMenu = new SideMenuViewModel
            {
                Groups = navGroupsForView
            }
        };

        var currentCulture = context.Features
            .Get<IRequestCultureFeature>()?
            .RequestCulture.Culture.Name; // zh-CN

        // ReSharper disable once RedundantNameQualifier
        var suppportedCultures = Aiursoft.WebTools.OfficialPlugins.LocalizationPlugin.SupportedCultures
            .Select(c => new LanguageSelection
            {
                Link = $"/Culture/Set?culture={c.Key}&returnUrl={context.Request.Path}",
                Name = c.Value // 中文 - 中国
            })
            .ToArray();

        // ReSharper disable once RedundantNameQualifier
        toInject.Navbar.LanguagesDropdown = new LanguagesDropdownViewModel
        {
            Languages = suppportedCultures,
            SelectedLanguage = new LanguageSelection
            {
                Name = Aiursoft.WebTools.OfficialPlugins.LocalizationPlugin.SupportedCultures[currentCulture ?? "en-US"],
                Link = "#",
            }
        };

        if (signInManager.IsSignedIn(context.User))
        {
            var avatarPath = context.User.Claims.First(c => c.Type == UserClaimsPrincipalFactory.AvatarClaimType)
                .Value;
            toInject.Navbar.UserDropdown = new UserDropdownViewModel
            {
                UserName = context.User.Claims.First(c => c.Type == UserClaimsPrincipalFactory.DisplayNameClaimType).Value,
                UserAvatarUrl = $"{storageService.RelativePathToInternetUrl(avatarPath)}?w=100&square=true",
                IconLinkGroups =
                [
                    new IconLinkGroup
                    {
                        Links =
                        [
                            new IconLink { Icon = "user", Text = localizer["Profile"], Href = "/Manage" },
                        ]
                    },
                    new IconLinkGroup
                    {
                        Links =
                        [
                            new IconLink { Icon = "log-out", Text = localizer["Sign out"], Href = "/Account/Logoff" }
                        ]
                    }
                ]
            };
        }
        else
        {
            toInject.Sidebar.SideAdvertisement = new SideAdvertisementViewModel
            {
                Title = localizer["Login"],
                Description = localizer["Login to get access to all features."],
                Href = "/Account/Login",
                ButtonText = localizer["Login"]
            };

            var allowRegister = appSettings.Value.Local.AllowRegister;
            var links = new List<IconLink>
            {
                new()
                {
                    Text = localizer["Login"],
                    Href = "/Account/Login",
                    Icon = "user"
                }
            };
            if (allowRegister && appSettings.Value.LocalEnabled)
            {
                links.Add(new IconLink
                {
                    Text = localizer["Register"],
                    Href = "/Account/Register",
                    Icon = "user-plus"
                });
            }
            toInject.Navbar.UserDropdown = new UserDropdownViewModel
            {
                UserName = localizer["Click to login"],
                UserAvatarUrl = string.Empty,
                IconLinkGroups =
                [
                    new IconLinkGroup
                    {
                        Links = links.ToArray()
                    }
                ]
            };
        }
    }


    private async Task<string> GetLogoUrl(HttpContext context)
    {
        var logoPath = await globalSettingsService.GetSettingValueAsync(SettingsMap.ProjectLogo);
        if (string.IsNullOrWhiteSpace(logoPath))
        {
            return "/logo.svg";
        }
        return storageService.RelativePathToInternetUrl(logoPath, context);
    }
}
