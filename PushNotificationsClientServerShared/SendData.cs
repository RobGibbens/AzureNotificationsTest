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
		/// Platforms to send to. This allows demonstrating tag expressions.
		/// NULL will send to all platforms.
		/// </summary>
		public Platform[] TargetPlatforms
		{
			get;
			set;
		}

		/// <summary>
		/// Sets the message template that will be used. If not set, a random template will be used.
		/// </summary>
		public NotificationTemplate Template
		{
			get;
			set;
		}

		public override string ToString() => $"[{nameof(SendData)}] Sender = {this.SenderId}, Message = {this.Message}";
	}
}
