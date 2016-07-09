using System.ComponentModel.DataAnnotations;

namespace PushNotificationsClientServerShared
{
	/// <summary>
	/// Contains all data required to send a push message.
	/// </summary>
	public sealed class SendData
    {
		/// <summary>
		/// Device ID of the sender. Required.
		/// </summary>
		[Required]
		public string SenderId { get; set; }

		/// <summary>
		/// Message to send. Required.
		/// </summary>
		[Required]
		public string Message { get; set; }

		/// <summary>
		/// Sets the message template that will be used. If not set, a random template will be used.
		/// </summary>
		public NotificationTemplate Template
		{
			get;
			set;
		}

		/// <summary>
		/// The recipient of the notification. Matched against the device IDs. If NULL, the notification is sent to everybody.
		/// </summary>
		public string RecipientId
		{
			get;
			set;
		}

		public override string ToString() => $"[{nameof(SendData)}] Sender = {this.SenderId}, Message = {this.Message}";
	}
}
