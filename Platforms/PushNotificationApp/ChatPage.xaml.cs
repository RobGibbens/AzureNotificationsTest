using System;
using System.Linq;

using Xamarin.Forms;
using System.Threading;
using System.Collections.ObjectModel;

namespace PushNotificationApp
{
	public class Message
	{
		public bool IsReceived {get; set;}
		public string Text { get; set; }
	}

	public partial class ChatPage : ContentPage
	{
		public ChatPage ()
		{
			this.Padding = new Thickness (0, Device.OnPlatform (20, 0, 0), 0, 0);
			this.BindingContext = this;
			InitializeComponent ();

			this.Messages.Add(new Message
			{
				Text = "Hello!",
				IsReceived = false,
			});

			this.Messages.Add(new Message
			{
				Text = "All good!",
				IsReceived = true,
			});

			MessagingCenter.Subscribe<App, string> (this, App.ReceivedRemoteNotificationMessage, this.OnReceivedRemoteMessage);
		}

		void OnReceivedRemoteMessage (object sender, string message)
		{
			this.Messages.Add(new Message
			{
				Text = message,
				IsReceived = true
			});
			this.lstMessages.ScrollTo(this.Messages.Last(), ScrollToPosition.Start, true);
		}

		public ObservableCollection<Message> Messages { get; } = new ObservableCollection<Message> ();

		async void HandleSendClicked (object sender, EventArgs e)
		{
			if(string.IsNullOrWhiteSpace(App.UniqueDeviceId) || string.IsNullOrWhiteSpace(App.DeviceToken))
			{
				this.DisplayAlert("Cannot send", "Your device seems to be unregistered.", "OK");
				return;
			}

			this.IsBusy = true;
			this.btnSend.IsEnabled = false;

			bool success = await App.PushManager.SendNotificationAsync(App.UniqueDeviceId, this.txtMessage.Text, default(CancellationToken), PushNotificationsClientServerShared.NotificationTemplate.Happy);

			if(success)
			{
				this.Messages.Add(new Message
				{
					Text = this.txtMessage.Text
				});
				this.lstMessages.ScrollTo(this.Messages.Last(), ScrollToPosition.Start, true);
			}
			else
			{
				this.DisplayAlert(string.Empty, "Failed to send message. Is your device registered?", "OK");
			}

			this.IsBusy = false;
			this.btnSend.IsEnabled = true;
		}
	}
}

