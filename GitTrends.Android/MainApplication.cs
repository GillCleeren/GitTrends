﻿using System;
using Android.App;
using Android.Runtime;
using Autofac;
using Shiny;
using Shiny.Notifications;

namespace GitTrends.Droid
{
    [Application]
    public class MainApplication : Application
    {
        public MainApplication(IntPtr handle, JniHandleOwnership transfer) : base(handle, transfer)
        {
        }

        public override void OnCreate()
        {
            base.OnCreate();

            AndroidOptions.DefaultSmallIconResourceName = nameof(Resource.Drawable.icon);
            AndroidOptions.DefaultColorResourceName = nameof(Resource.Color.colorPrimary);
            AndroidOptions.DefaultChannel = nameof(GitTrends);
            AndroidOptions.DefaultChannelDescription = "GitTrends Notifications";
            AndroidOptions.DefaultLaunchActivityFlags = AndroidActivityFlags.FromBackground;
            AndroidOptions.DefaultNotificationImportance = AndroidNotificationImportance.High;
            AndroidShinyHost.Init(this, platformBuild: services => services.UseNotifications());

            Plugin.CurrentActivity.CrossCurrentActivity.Current.Init(this);

            Xamarin.Essentials.Platform.Init(this);

            FFImageLoading.Forms.Platform.CachedImageRenderer.Init(true);
            FFImageLoading.Forms.Platform.CachedImageRenderer.InitImageViewHandler();
            var ignore = typeof(FFImageLoading.Svg.Forms.SvgCachedImage);
        }

#if !AppStore
        #region UI Test Back Door Methods
        [Preserve, Java.Interop.Export(Mobile.Shared.BackdoorMethodConstants.SetGitHubUser)]
        public async void SetGitHubUser(string accessToken)
        {
            using var scope = ContainerService.Container.BeginLifetimeScope();
            var backdoorService = scope.Resolve<UITestBackdoorService>();

            await backdoorService.SetGitHubUser(accessToken.ToString()).ConfigureAwait(false);
        }

        [Preserve, Java.Interop.Export(Mobile.Shared.BackdoorMethodConstants.TriggerPullToRefresh)]
        public async void TriggerRepositoriesPullToRefresh()
        {
            using var scope = ContainerService.Container.BeginLifetimeScope();
            var backdoorService = scope.Resolve<UITestBackdoorService>();

            await backdoorService.TriggerPullToRefresh().ConfigureAwait(false);
        }

        [Preserve, Java.Interop.Export(Mobile.Shared.BackdoorMethodConstants.GetVisibleCollection)]
        public string GetVisibleCollection()
        {
            using var scope = ContainerService.Container.BeginLifetimeScope();
            var backdoorService = scope.Resolve<UITestBackdoorService>();

            return Newtonsoft.Json.JsonConvert.SerializeObject(backdoorService.GetVisibleCollection());
        }

        [Preserve, Java.Interop.Export(Mobile.Shared.BackdoorMethodConstants.GetCurrentTrendsChartOption)]
        public string GetCurrentTrendsChartOption()
        {
            using var scope = ContainerService.Container.BeginLifetimeScope();
            var backdoorService = scope.Resolve<UITestBackdoorService>();

            return Newtonsoft.Json.JsonConvert.SerializeObject(backdoorService.GetCurrentTrendsChartOption());
        }

        [Preserve, Java.Interop.Export(Mobile.Shared.BackdoorMethodConstants.IsTrendsSeriesVisible)]
        public bool IsTrendsSeriesVisible(string seriesLabel)
        {
            using var scope = ContainerService.Container.BeginLifetimeScope();
            var backdoorService = scope.Resolve<UITestBackdoorService>();

            return backdoorService.IsTrendsSeriesVisible(seriesLabel);
        }
        #endregion
#endif
    }
}
