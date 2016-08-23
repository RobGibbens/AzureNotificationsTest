using System;
using System.Collections.Generic;

using Xamarin.Forms;
using PushNotificationsClientServerShared;
using System.Linq;
using PropertyChanged;

namespace PushNotificationApp
{
	[ImplementPropertyChanged]
	public partial class StatusPage : ContentPage
	{
		public StatusPage ()
		{
			this.Padding = new Thickness(0, Device.OnPlatform(20, 0, 0), 0, 0);
			this.BindingContext = this;
			InitializeComponent ();
		}

		public string UniqueId
		{
			get;
			set;
		}

		public string DeviceToken
		{
			get;
			set;
		}

		public string DeviceName
		{
			get;
			set;
		}

		protected override void OnAppearing ()
		{
			base.OnAppearing ();
			this.UniqueId = App.UniqueDeviceId;
			this.DeviceToken = App.DeviceToken;
			this.DeviceName = App.DeviceName;
		}

		async void HandleRegisterUpdateClicked (object sender, System.EventArgs e)
		{
			this.IsBusy = true;
			App.DeviceName = this.DeviceName;
			try
			{
				await ((App)App.Current).RegisterDeviceAsync();
			}
			finally
			{
				this.IsBusy = false;
			}
		}

		async void HandleUnregisterClicked (object sender, System.EventArgs e)
		{
			if(string.IsNullOrWhiteSpace(App.UniqueDeviceId))
			{
				this.DisplayAlert("Unregistering failed", "Unique device ID not set - cannot unregister.", "OK");
				return;
			}

			this.IsBusy = true;
			try
			{
				var deviceInfo = await App.PushManager.UnregisterDeviceAsync(App.UniqueDeviceId);
				this.DisplayAlert("Unregistering suceeded", "Device sucessfully unregistered", "OK");
			}
			catch(Exception ex)
			{
				this.DisplayAlert("Error unregistering", ex.Message, "OK");
			}

			finally
			{
				this.IsBusy = false;
			}
		}
	}
}

