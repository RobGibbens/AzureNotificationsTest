# Push Notifications with Azure Notification Hub

The solution I created is using a custom .NET backend to send push notifications instead of lettings the client apps communicate directly with the Azure Notification Hub. The advantage is that we can expose a tailor-made REST API for our purposes. Also device management can happen centralized on the server. 
To communicate with our backend, the solution contains a PCL which acts as a wrapper around the restful API we expose. It can be used across all platforms.

The app itself is a Xamarin.Forms app that offers chat functions.

The solution can be found on [Github](https://github.com/Krumelur/AzureNotificationsTest).

## Solution structure
The solution has the following projects:
* Forms project (PCL): this communicates with our custom backend but needs additional information from the native projects.
* Native iOS project which registers with Apples push service (APNS) and passes the received device token on to the Forms app which will send it to our backend in order to register the device.
* Native Android project which registers with Google’s Cloud Messaging/Fire Messaging (GCM/FCM) and passes the received device token on to the Forms app which will send it to our backend in order to register the device.
* UWP project which registers with Windows Notification Services (WNS) and passes the received push cannel on to the Forms app for further processing.
* WebAPI 2.0 backend („PushNotificationsBackend“) to communicate with the Azure Notification Hub. It also exposes the REST API for the Forms client.
* Wrapper around the REST API („PushNotificationsClient“) with async helper methods for device registration, message sending etc.
* A shared project („PushNotificationsClientServeShared“) containing classes shared by the client wrapper and the server.
## Notification Hub
Notification Hub itself does not send notifications. It forwards them to the vendor specific servers (APNS, GCM, WNS).
* Access at: https://portal.azure.com
* Create a new Notification Hub project (this is covered in the Lightning Lecture [Push Notifications on iOS](https://university.xamarin.com/lightninglectures/push-notifications-on-ios-with-azure), starting around minute 7:00)
* Important things we need from the portal:
** The „Notification Hub Path“: this is just the name of the hub and can be found on the overview page.
** The connection string: we need the „DefaultFullSharedAccessSignature“, available from the settings -> Access policies
## iOS
We use APNS to register our device. This requires creating a certificate signing request for our app. The process is shown in my lightning lecture about sending push notifications with iOS.
The certificate must be uploaded to the Azure Notification Hub.
If an iOS app wants to register for remote notifications, it has to call `RegisterForRemoteNotifications()` and will eventually receive a device token. All of this is commented in the Github repo inside [AppDelegate.cs](https://github.com/Krumelur/AzureNotificationsTest/blob/master/Platforms/iOS/AppDelegate.cs#L50). 
## Android
Android uses GCM/FCM. The app must be registered in the [„new“ dev portal](https://developers.google.com/mobile/add?platform=android). A valid app name *and* ID must be provided. The registration is done in the style of a wizard and the 2nd step allows adding GCM services and immediately provides the required „Server API Key“ in the format of „AIzaSyDgxTxhgfF9IR8YU5DEUlKs8r3oYNMYr-o“. This API key must be pasted into the Azure Notification Hub under Settings -> Notification Services, where we already uploaded the APNS certificate.

Compared to iOS, the client part is very complicated. Xamarin has a GCM client component. My lightning lecture about sending push notifications with Android shows how to use this. To better understand how the registration process works (and to avoid bugs introduced by additional components), I did the registration manually. Xamarin has great [documentation](https://developer.xamarin.com/guides/cross-platform/application_fundamentals/notifications/android/remote_notifications_in_android/) on this.

In a nutshell:
* Google Play services are used
* Various permissions must be added into the manifest XML
* A receiver/intent filter must be registered
* A [service](https://github.com/Krumelur/AzureNotificationsTest/blob/master/Platforms/Droid/MainActivity.cs#L94) to retrieve/refresh the device token. 
* The service kicks off another service which asynchronously gets the token and passes it to the Forms app for further processing.
* To receive notifications, yet another service has been implemented. It forwards the notification to the Forms app to display it. Alternatively, it could add it into Android’s [notification center](https://github.com/Krumelur/AzureNotificationsTest/blob/master/Platforms/Droid/MyGcmListenerService.cs#L16).
## UWP
The UWP app requires the following capabilities - and I have no idea why it would need the 2nd one…:
* Internet (Client)
* Private networks (Client & Server)

UWP uses „push channels“ to receive notifications. I used this link for first [inspirations](https://azure.microsoft.com/en-us/documentation/articles/app-service-mobile-windows-store-dotnet-get-started-push/).

Registration is a bit of a nightmare:
* The app must be registered at the Windows Dev Center (https://developer.microsoft.com/en-us/windows). This can only be accessed with a non-corporate account (I used my private Bizspark account). Registration is €14 or free if you redeem an MSDN/Bizspark code.
* With the app registered, the UWP project in VS can be right clicked. In the menu "Store" select "Associate App with the Store" and in the dialog the opens click "Refresh" to see the just created app from the Windows Dev Center and select it.
* This will add the app credentials to the UWP project.
* To allow Azure to send notification to the app we need the "Package SID" and the "Security Key". The package SID is available from https://apps.dev.microsoft.com/. It looks like this: "ms-app://s-1-15-2-2780614805-1053180435-662555083-xxxxxxxx-xxxxxxxxx-295702231-1050728353".
Do *not* use the package SID from the Windows Dev Center. It is missing the "ms-app://" prefix.
The security key is available from https://apps.dev.microsoft.com/ *after creating a new „password“*.
Both of these must be pasted into the Notification Services dialog for WNS over at the Azure portal.

The magic happens in the UWP’s App.xaml.cs [file](https://github.com/Krumelur/AzureNotificationsTest/blob/master/PushNotificationApp.UWP/App.xaml.cs#L85).

## Forms App and backend communication
The Forms App contains callbacks for the native platforms to inform about a [received device token](https://github.com/Krumelur/AzureNotificationsTest/blob/master/Platforms/PushNotificationApp/App.xaml.cs#L92) or a received push notification. 

To register a device with Azure/our backend we need:
* A device token/push channel (*Note*: this is not a unique device ID. The token can change over time)
* A unique device ID (during registration, the backend assigns a GUID that must be remembered by the client. I’m using James’ cross platform settings plugin).
* A device name (only required by our backend)

Device information is passed to the backend via a `DeviceInformation` [class](https://github.com/Krumelur/AzureNotificationsTest/blob/master/PushNotificationsClientServerShared/DeviceInformation.cs) that is shared between the client’s REST wrapper and the server.

If no unique device ID is passed by the client, a new device will be registered. Otherwise an existing will be updated.

Registering a device does two things:

* Save the `DeviceInformation` object in a database.
* Create an `Installation` object, provided by [NotificationHubs](https://msdn.microsoft.com/en-us/library/microsoft.azure.notificationhubs.installation.aspx).

The installation object is an alternative to the (soon to be deprecated?) „registration“ model. The differences are discussed in this [document](https://msdn.microsoft.com/en-us/magazine/dn948105.aspx).

There are different ways of sending notifications:
* Platform specific
* Cross platform, using templates

Our backend uses templates. For each device, a set of three templates („happy“, „unhappy“, „neutral“) is registered that contain placeholders for message text. The templates are stored inside the installation object and use different [payloads](https://github.com/Krumelur/AzureNotificationsTest/blob/master/PushNotificationsBackend/Extensions.cs#L67).
Installations also support the idea of „tags“. Tags can be used to send notifications to specific recipients only. There is a query language ([tag expressions](https://azure.microsoft.com/en-us/documentation/articles/notification-hubs-tags-segment-push-message/)) for selecting devices by tag. Our backend uses this to allow selective sending to a specific target platform (iOS, Android or Windows). 

To register templates, UWP requires a special header to be set. This is part of the installation class which contains a `Headers` dictionary.[](https://github.com/Krumelur/AzureNotificationsTest/blob/master/PushNotificationsBackend/API/PushNotificationsController.cs#L61)

### Communicating from the backend to Azure Notification Hub
The Notification Hub exposes a REST API but there’s also a Nuget [package](https://www.nuget.org/packages/Microsoft.Azure.NotificationHubs/) and that’s what I am using.

### Test sending and debug mode
The Azure Portal allows sending test notifications. This can be useful to see if the device registrations works as expected.
When using the backend, a debug mode can be [enabled](https://github.com/Krumelur/AzureNotificationsTest/blob/master/PushNotificationsBackend/API/PushNotificationsController.cs#L23). This tells the Notification  Hub to trace the sent messages. The `SendNotification()` method in the backend shows how to use this feature. It should not be used for productive environments because the number of allowed recipients is limited to 10.

### Issues
* With the installation model there is no way to return all registered devices from the Notification Hub. This API is only available with the [registration model](https://msdn.microsoft.com/en-us/library/azure/microsoft.azure.notificationhubs.notificationhubclient.getregistrationasync.aspx). The feedback I got from the team was that they are working on an API which will be using Azure Blob storage but there’s no estimation when this will be done.
* Housekeeping: whenever an app talks to the native servers and registers itself it generates a new record. There is no way to find out which devices have uninstalled the app. This means, over time our database will fill up with device registrations and we cannot tell if they are still in use. Notification Hub provides a feedback service and I tried to [implement](https://github.com/Krumelur/AzureNotificationsTest/blob/master/PushNotificationsBackend/API/PushNotificationsController.cs#L61) it but it does not work reliably and seems to be supported by Android only. Apple removed support for this. I don’t know about  Windows.

## Links I found useful:
* Using templates to send cross platform notifications: https://azure.microsoft.com/en-us/documentation/articles/notification-hubs-aspnet-cross-platform-notification/
* Push Notifications with Xamarin.iOS: https://azure.microsoft.com/en-us/documentation/articles/xamarin-notification-hubs-ios-push-notification-apns-get-started/
* Best practices for Notification Hubs (this introduces the „Installation model“): https://msdn.microsoft.com/en-us/magazine/dn948105.aspx
* „Installation model“ usage: https://github.com/Azure/azure-notificationhubs-java-backend#installation-api-usage
* Diagnosing Notification Hub: https://azure.microsoft.com/en-us/documentation/articles/notification-hubs-push-notification-fixer/
* Firebase Cloud Messaging: https://firebase.google.com/docs/cloud-messaging/
* Payloads for GCM messages: https://developers.google.com/cloud-messaging/concept-options
* The Notification Hub REST API: https://msdn.microsoft.com/en-us/library/azure/dn495827.aspx
* Well documented sample on sending notifications: https://github.com/saramgsilva/NotificationHubs
* How to access IIS-Express from another machine: https://github.com/icflorescu/iisexpress-proxy
