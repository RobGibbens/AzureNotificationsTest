using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Networking.PushNotifications;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace PushNotificationApp.UWP
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    sealed partial class App : Application
    {
        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            this.InitializeComponent();
            this.Suspending += OnSuspending;
        }

		public static PushNotificationApp.App formsApp;

        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used such as when the application is launched to open a specific file.
        /// </summary>
        /// <param name="e">Details about the launch request and process.</param>
        protected async override void OnLaunched(LaunchActivatedEventArgs e)
        {
#if DEBUG
            if (System.Diagnostics.Debugger.IsAttached)
            {
                this.DebugSettings.EnableFrameRateCounter = true;
            }
#endif
            Frame rootFrame = Window.Current.Content as Frame;

            // Do not repeat app initialization when the Window already has content,
            // just ensure that the window is active
            if (rootFrame == null)
            {
                // Create a Frame to act as the navigation context and navigate to the first page
                rootFrame = new Frame();

                rootFrame.NavigationFailed += OnNavigationFailed;
				Xamarin.Forms.Forms.Init(e);
				formsApp = new PushNotificationApp.App();

				if (e.PreviousExecutionState == ApplicationExecutionState.Terminated)
                {
                    //TODO: Load state from previously suspended application
                }

                // Place the frame in the current Window
                Window.Current.Content = rootFrame;

				// Get a "push channel" which acts as a token for the devive and also receives messages.
				// The UWP app must be configured for notifications. See: https://azure.microsoft.com/en-us/documentation/articles/app-service-mobile-windows-store-dotnet-get-started-push/
				// Registration is a bit of a nightmare:
				// - The app must be registered at the Windows Dev Center (https://developer.microsoft.com/en-us/windows). This can only be accessed
				//   with a non-corporate account (I used my private Bizspark account). Registration is €14 or free if you redeem an MSDN/Bizspark code.
				// - With the app registered, the UWP project in VS can be right clicked. In the menu "Store" select "Associate App with the Store" and in the
				//   dialog the opens click "Refresh" to see the just created app from the Windows Dev Center and select it.
				// - This will add the app credentials to the UWP project.
				// - To allow Azure to send notification to the app we need the "Package SID" and the "Security Key". The package SID is available from 
				//   https://apps.dev.microsoft.com/. It looks like this:" ms-app://s-1-15-2-2780614805-1053180435-662555083-xxxxxxxx-xxxxxxxxx-295702231-1050728353".
				//   Do not use the package SID from the Windows Dev Center. It is missing the "ms-app://" prefix.
				//   The security is available from https://apps.dev.microsoft.com/ after creating a new password.
				//   Both of these must be pasted into the Notification Services dialog for WNS over at the Azure portal.
				var pushChannel = await PushNotificationChannelManager.CreatePushNotificationChannelForApplicationAsync();
				formsApp.OnNativeRegisteredForRemoteNotifications(pushChannel.Uri);
				pushChannel.PushNotificationReceived += PushChannel_PushNotificationReceived;
            }

            if (e.PrelaunchActivated == false)
            {
                if (rootFrame.Content == null)
                {
                    // When the navigation stack isn't restored navigate to the first page,
                    // configuring the new page by passing required information as a navigation
                    // parameter
                    rootFrame.Navigate(typeof(MainPage), e.Arguments);
                }
                // Ensure the current window is active
                Window.Current.Activate();
            }
        }

		private void PushChannel_PushNotificationReceived(PushNotificationChannel sender, PushNotificationReceivedEventArgs args)
		{
			formsApp.OnNativeReceivedRemoteNotification("Hello!");
		}

		/// <summary>
		/// Invoked when Navigation to a certain page fails
		/// </summary>
		/// <param name="sender">The Frame which failed navigation</param>
		/// <param name="e">Details about the navigation failure</param>
		void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
        }

        /// <summary>
        /// Invoked when application execution is being suspended.  Application state is saved
        /// without knowing whether the application will be terminated or resumed with the contents
        /// of memory still intact.
        /// </summary>
        /// <param name="sender">The source of the suspend request.</param>
        /// <param name="e">Details about the suspend request.</param>
        private void OnSuspending(object sender, SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();
            //TODO: Save application state and stop any background activity
            deferral.Complete();
        }
    }
}
