using Mixer.Base;
using Mixer.Base.Clients;
using Mixer.Base.Model.Channel;
using Mixer.Base.Model.Chat;
using Mixer.Base.Model.User;
using System;
using static System.Console;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using System.IO;
using ScpDriverInterface;

namespace Mixer.ChatSample.Console
{
    public class F4Fbot
    {
        private static ChatClient chatClient;
        

        public static void Controler()
        {
            using (var scpBus = new ScpBus())
            {
                scpBus.PlugIn(1);


            }

        }

        public static void Start()
        {
            //List<OAuthClientScopeEnum> scopes = new List<OAuthClientScopeEnum>()
            //{
            //    OAuthClientScopeEnum.chat__bypass_links,
            //    OAuthClientScopeEnum.chat__bypass_slowchat,
            //    OAuthClientScopeEnum.chat__change_ban,
            //    OAuthClientScopeEnum.chat__change_role,
            //    OAuthClientScopeEnum.chat__chat,
            //    OAuthClientScopeEnum.chat__connect,
            //    OAuthClientScopeEnum.chat__clear_messages,
            //    OAuthClientScopeEnum.chat__edit_options,
            //    OAuthClientScopeEnum.chat__giveaway_start,
            //    OAuthClientScopeEnum.chat__poll_start,
            //    OAuthClientScopeEnum.chat__poll_vote,
            //    OAuthClientScopeEnum.chat__purge,
            //    OAuthClientScopeEnum.chat__remove_message,
            //    OAuthClientScopeEnum.chat__timeout,
            //    OAuthClientScopeEnum.chat__view_deleted,
            //    OAuthClientScopeEnum.chat__whisper,

            //    OAuthClientScopeEnum.channel__details__self,
            //    OAuthClientScopeEnum.channel__update__self,

            //    OAuthClientScopeEnum.user__details__self,
            //    OAuthClientScopeEnum.user__log__self,
            //    OAuthClientScopeEnum.user__notification__self,
            //    OAuthClientScopeEnum.user__update__self,
            //    OAuthClientScopeEnum.channel__follow__self
            //};

            var scopes = Enum.GetValues(typeof(OAuthClientScopeEnum)) as OAuthClientScopeEnum[];

            System.Console.WriteLine("Connecting to Mixer...");


            MixerConnection connection = MixerConnection.ConnectViaLocalhostOAuthBrowser(ConfigurationManager.AppSettings["ClientID"], scopes).Result;

            if (connection != null)
            {
                System.Console.WriteLine("Mixer connection successful!");

                UserModel user = connection.Users.GetCurrentUser().Result;

                ExpandedChannelModel channel = connection.Channels.GetChannel(user.username).Result;

                System.Console.WriteLine(string.Format("Logged in as: {0}", user.username));

                WriteLine();

                System.Console.WriteLine("Connecting to channel chat...");



                chatClient = ChatClient.CreateFromChannel(connection, channel).Result;
                TrackFollows(connection, channel, chatClient);

                chatClient.SendMessage("i am still alive");

                chatClient.OnDisconnectOccurred += ChatClient_OnDisconnectOccurred;
                chatClient.OnMessageOccurred += ChatClient_OnMessageOccurred;
                chatClient.OnUserJoinOccurred += ChatClient_OnUserJoinOccurred;
                chatClient.OnUserLeaveOccurred += ChatClient_OnUserLeaveOccurred;

                if (chatClient.Connect().Result && chatClient.Authenticate().Result)
                {
                    System.Console.WriteLine("Chat connection successful!");

                    IEnumerable<ChatUserModel> users = connection.Chats.GetUsers(chatClient.Channel).Result;

                    System.Console.WriteLine(string.Format("There are {0} users currently in chat", users.Count()));
                    System.Console.WriteLine();

                    while (true) { }
                }
            }
        }

        private static async void TrackFollows(MixerConnection connection, ExpandedChannelModel channel, ChatClient chat)
        {
            var constalation = await ConstellationClient.Create(connection);

            WriteLine("made constellation");

            var connected = await constalation.Connect();

            if (!connected)
            {
                WriteLine("constalation failed");
                return;
            }


            WriteLine("connected to constellation");

            await constalation.SubscribeToEventsWithResponse(new ConstellationEventType[]
            {
                new ConstellationEventType(ConstellationEventTypeEnum.channel__id__followed, channel.id)
            });
            void Constalation_OnSubscribedEventOccurred(object sender, Base.Model.Constellation.ConstellationLiveEventModel e)
            {
                var following = e.payload.Value<bool>("following");
                var user = e.payload.Value<JObject>("user");
                var username = user.Value<string>("username");
                var mood = following ? "follow" : "unfollow";
                chat.Send($"/{mood} {username}");
                Log($"{username}\t{mood}");
            }
            constalation.OnSubscribedEventOccurred += Constalation_OnSubscribedEventOccurred;



        }

        private static void Log(string v)
        {
            var o = DateTime.Now + "\t" + v+"\n";
            Write(o);
            File.AppendAllText("log.txt", o);
        }

        private static async void ChatClient_OnDisconnectOccurred(object sender, System.Net.WebSockets.WebSocketCloseStatus e)
        {
            System.Console.WriteLine("Disconnection Occurred, attempting reconnection...");

            do
            {
                await Task.Delay(2500);
            }
            while (!await F4Fbot.chatClient.Connect() && !await F4Fbot.chatClient.Authenticate());

            System.Console.WriteLine("Reconnection successful");
        }

        private static void ChatClient_OnMessageOccurred(object sender, ChatMessageEventModel e)
        {
            string message = "";
            foreach (ChatMessageDataModel m in e.message.message)
            {
                message += m.text;
            }
            System.Console.WriteLine(string.Format("{0}: {1}", e.user_name, message));
        }

        private static void ChatClient_OnUserJoinOccurred(object sender, ChatUserEventModel e)
        {
            Log($"{e.username}\tjoined");
        }

        private static void ChatClient_OnUserLeaveOccurred(object sender, ChatUserEventModel e)
        {
            Log($"{e.username}\tleft");
        }
    }
}