using Mixer.Base;
using Mixer.Base.Clients;
using Mixer.Base.Model.Channel;
using Mixer.Base.Model.Interactive;
using Mixer.Base.Model.User;
using Mixer.Base.Util;
using ScpDriverInterface;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
namespace Mixer.ChatSample.Console
{


    public class MixerController : IDisposable
    {
        ScpBus bus;

        Timer sendThread;
        public MixerController()
        {
            bus = new ScpBus();
            bus.PlugIn(1);


            sendThread = new Timer(SendData, null, 100, 100);
        }

        private void SendData(object state)
        {
            bus.Report(1, controller.GetReport());
        }

        public X360Controller controller = new X360Controller();

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    sendThread.Dispose();
                    bus.Unplug(1);
                    bus.Dispose();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~MixerController()
        // {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion

    }

    public static class X360ControllerExtensions
    {
        public static void SetButton(this X360Controller controller, X360Buttons buttons, bool value)
        {
            var v = controller.Buttons & ~buttons;
            if (value)
                v |= buttons;
            controller.Buttons = v;
        }
    }
    public class Program
    {
        public static MixerController controller = new MixerController();
        private static InteractiveClient interactiveClient;
        private static List<InteractiveConnectedSceneModel> scenes = new List<InteractiveConnectedSceneModel>();
        private static List<InteractiveConnectedButtonControlModel> buttons = new List<InteractiveConnectedButtonControlModel>();
        private static List<InteractiveConnectedJoystickControlModel> joysticks = new List<InteractiveConnectedJoystickControlModel>();
        private static List<InteractiveConnectedLabelControlModel> labels = new List<InteractiveConnectedLabelControlModel>();
        private static List<InteractiveConnectedTextBoxControlModel> textBoxes = new List<InteractiveConnectedTextBoxControlModel>();

        public static void Main(string[] args)
        {
            List<OAuthClientScopeEnum> scopes = new List<OAuthClientScopeEnum>()
            {
                OAuthClientScopeEnum.channel__details__self,
                OAuthClientScopeEnum.channel__update__self,

                OAuthClientScopeEnum.interactive__manage__self,
                OAuthClientScopeEnum.interactive__robot__self,

                OAuthClientScopeEnum.user__details__self,
                OAuthClientScopeEnum.user__log__self,
                OAuthClientScopeEnum.user__notification__self,
                OAuthClientScopeEnum.user__update__self,
            };

            System.Console.WriteLine("Connecting to Mixer...");

            MixerConnection connection = MixerConnection.ConnectViaLocalhostOAuthBrowser(ConfigurationManager.AppSettings["ClientID"], scopes).Result;

            if (connection != null)
            {
                System.Console.WriteLine("Mixer connection successful!");

                UserModel user = connection.Users.GetCurrentUser().Result;
                ExpandedChannelModel channel = connection.Channels.GetChannel(user.username).Result;
                System.Console.WriteLine(string.Format("Logged in as: {0}", user.username));


                //List<InteractiveGameListingModel> games = new List<InteractiveGameListingModel>(connection.Interactive.GetOwnedInteractiveGames(channel).Result);

                //InteractiveGameListingModel game = games[4];


                var gameVersion = connection.Interactive.GetInteractiveGameVersion(185683).Result;
                var game = connection.Interactive.GetInteractiveGame(gameVersion.gameId).Result;


                if (gameVersion != null)
                {
                    System.Console.WriteLine();
                    System.Console.WriteLine(string.Format("Connecting to channel interactive using game {0}...", game.name));

                    interactiveClient = InteractiveClient.CreateFromChannel(connection, channel, game, gameVersion).Result;

                    if (interactiveClient.Connect().Result && interactiveClient.Ready().Result)
                    {
                        InteractiveConnectedSceneGroupCollectionModel scenes = interactiveClient.GetScenes().Result;
                        if (scenes != null)
                        {
                            Program.scenes.AddRange(scenes.scenes);

                            foreach (InteractiveConnectedSceneModel scene in Program.scenes)
                            {
                                foreach (InteractiveConnectedButtonControlModel button in scene.buttons)
                                {
                                    buttons.Add(button);
                                }

                                foreach (InteractiveConnectedJoystickControlModel joystick in scene.joysticks)
                                {
                                    joysticks.Add(joystick);
                                }

                                foreach (InteractiveConnectedLabelControlModel label in scene.labels)
                                {
                                    labels.Add(label);
                                }

                                foreach (InteractiveConnectedTextBoxControlModel textBox in scene.textBoxes)
                                {
                                    textBoxes.Add(textBox);
                                }

                                foreach (InteractiveControlModel control in scene.allControls)
                                {
                                    control.disabled = false;
                                }

                                interactiveClient.UpdateControls(scene, scene.allControls).Wait();
                            }

                            interactiveClient.OnDisconnectOccurred += InteractiveClient_OnDisconnectOccurred;
                            interactiveClient.OnParticipantJoin += InteractiveClient_OnParticipantJoin;
                            interactiveClient.OnParticipantLeave += InteractiveClient_OnParticipantLeave;
                            interactiveClient.OnGiveInput += InteractiveClient_OnGiveInput;
                            interactiveClient.OnEventOccurred += InteractiveClient_OnEventOccurred;
                            interactiveClient.OnPacketReceivedOccurred += InteractiveClient_OnPacketReceivedOccurred;
                            interactiveClient.OnMethodOccurred += InteractiveClient_OnMethodOccurred;
                            while (true) { }
                        }
                    }
                }
            }
        }


        /*
         * 
        [{"type":"method","method":"onSceneCreate","params":{"scenes":[{"sceneID":"controls","etag":"","controls":[{"controlID":"controller_plugin_reminder","kind":"label","etag":"","disabled":false,"cooldown":0,"cost":0,"text":"locStrControllerReminder","meta":{},"position":[{"size":"large","x":32,"y":0,"width":47,"height":4}],"textSize":"12pt"},{"controlID":"crowdController","kind":"controller","etag":"","disabled":false,"cooldown":0,"cost":0,"meta":{},"text":"Xbox Controller","position":[{"width":30,"height":27,"size":"large","x":0,"y":0},{"size":"medium","width":30,"height":27,"x":0,"y":0},{"size":"small","width":30,"height":27,"x":0,"y":0}]},{"controlID":"leave","kind":"button","etag":"","disabled":false,"cooldown":0,"cost":0,"meta":{},"position":[{"size":"large","x":6,"y":0,"width":26,"height":4},{"size":"medium","x":0,"y":0,"width":26,"height":4},{"size":"small","x":0,"y":0,"width":26,"height":4}],"text":"locStrStopControlling"}],"groups":[{"groupID":"controlling","etag":"","sceneID":"controls","meta":{}}],"meta":{}}]},"id":0,"seq":4,"discard":true},{"type":"method","method":"onSceneDelete","params":{"sceneID":"default","reassignSceneID":"controls"},"id":0,"seq":4,"discard":true},{"type":"method","method":"onGroupCreate","params":{"groups":[{"groupID":"controlling","etag":"","sceneID":"controls","meta":{}}]},"id":0,"seq":4,"discard":true},{"type":"method","method":"onGroupDelete","params":{"groupID":"default","reassignGroupID":"controlling"},"id":0,"seq":4,"discard":true}]
        {"type":"method","method":"onParticipantUpdate","params":{"participants":[{"sessionID":"ff949acc-ea89-4ab0-8b29-617c84f6c2db","etag":"","userID":46740637,"username":"LizeLive","level":81,"lastInputAt":1566008741177,"connectedAt":1566008734386,"channelGroups":["User"],"disabled":false,"groupID":"controlling","anonymous":false,"meta":{}}]},"id":0,"seq":5,"discard":true}
        {"type":"method","method":"onControlUpdate","params":{"sceneID":"controls","controls":[{"controlID":"crowdController","kind":"controller","etag":"","disabled":false,"cooldown":0,"cost":0,"meta":{},"text":"Xbox Controller","position":[{"width":30,"height":27,"size":"large","x":0,"y":0},{"size":"medium","width":30,"height":27,"x":0,"y":0},{"size":"small","width":30,"height":27,"x":0,"y":0}]}]},"id":0,"seq":8,"discard":true}
        
             
             */





        private static void InteractiveClient_OnMethodOccurred(object sender, Base.Model.Client.MethodPacket e)
        {
            System.Console.WriteLine($"{e.method} {e.seq} {e.id} {e.type} {e.parameters}");
        }

        private static void InteractiveClient_OnPacketReceivedOccurred(object sender, Base.Model.Client.WebSocketPacket e)
        {
            System.Console.WriteLine(e.type);
        }

        private static void InteractiveClient_OnEventOccurred(object sender, Base.Model.Client.EventPacket e)
        {
            System.Console.WriteLine(e.eventName);
        }

        public static Dictionary<string, InteractiveParticipantModel> Users = new Dictionary<string, InteractiveParticipantModel>();

        private static async void InteractiveClient_OnDisconnectOccurred(object sender, System.Net.WebSockets.WebSocketCloseStatus e)
        {
            System.Console.WriteLine("Disconnection Occurred, attempting reconnection...");

            do
            {
                await Task.Delay(2500);
            }
            while (!await interactiveClient.Connect() && !await interactiveClient.Ready());

            System.Console.WriteLine("Reconnection successful");
        }

        private static void InteractiveClient_OnParticipantJoin(object sender, InteractiveParticipantCollectionModel e)
        {
            if (e.participants != null)
            {
                foreach (InteractiveParticipantModel participant in e.participants)
                {
                    Users.Add(participant.sessionID, participant);

                    System.Console.WriteLine("Participant Joined: " + participant.username);
                }
            }
        }

        private static void InteractiveClient_OnParticipantLeave(object sender, InteractiveParticipantCollectionModel e)
        {
            if (e.participants != null)
            {
                foreach (InteractiveParticipantModel participant in e.participants)
                {
                    Users.Remove(participant.sessionID);
                    System.Console.WriteLine("Participant Left: " + participant.username);
                }
            }
        }

        private static void InteractiveClient_OnGiveInput(object sender, InteractiveGiveInputModel e)
        {

            var allow = Users.TryGetValue(e.participantID, out var participant);
            if (!allow)
                return;

            System.Console.WriteLine("Input Received: " + participant.username + " - " + e.input.eventType + " - " + e.input.controlID);



            var i = e.input;
            var c = controller.controller;


            if (i.eventType == "mousedown" && i.controlID == "join")
            {
                //interactiveClient.UpdateControls(scene, scene.allControls).Wait();

                //new InteractiveButtonControlModel
                //{
                //    controlID= "join",
                //    disabled = true,

                //}

                /*
 * {"type":"method","method":"onControlUpdate","params":{"sceneID":"default","controls":[{"controlID":"join","kind":"button","etag":"","disabled":true,"cooldown":0,"cost":0,"hidden":false,"position":[{"size":"large","width":20,"height":4,"x":58,"y":4},{"size":"medium","width":32,"height":4,"x":10,"y":7},{"size":"small","width":32,"height":4,"x":0,"y":13}],"text":"locStrJoin","meta":{}}]},"id":0,"seq":4,"discard":true}
 * 
 * 
 */

            }


            if (i.eventType == "mousedown" || i.eventType == "mouseup")
            {
                var pressed = i.eventType == "mousedown";

                if (Enum.TryParse<X360Buttons>(i.controlID, out var button))
                {
                    c.SetButton(button, pressed);
                }

            }
            else if (e.input.eventType == "move")
            {
                var x = (short)(short.MaxValue * e.input.x);
                var y = (short)(short.MaxValue * -e.input.y);

                if (i.controlID == "LeftStick")
                {
                    c.LeftStickX = x;
                    c.LeftStickY = y;
                }

                if (i.controlID == "RightStick")
                {
                    c.RightStickX = x;
                    c.RightStickY = y;
                }


                System.Console.WriteLine($"move {i.controlID} {x} {y}");
            }
            else
            {
                System.Console.WriteLine(e.input.eventType);
            }
        }
    }
}
