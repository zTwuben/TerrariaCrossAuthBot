using Discord;
using Discord.WebSocket;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace TerrariaCrossAuthBot
{
    internal class Program
    {

        private readonly DiscordSocketClient client;
        private string token;


        private Dictionary<string, ulong> pendingVerifications = new Dictionary<string, ulong>();


        public class Config
        {
            public string? Token { get; set; }
        }

        public Program()
        {


            //Loads Token
            LoadToken();

            var config = new DiscordSocketConfig()
            {
                GatewayIntents = GatewayIntents.Guilds |
                     GatewayIntents.GuildMessages |
                     GatewayIntents.MessageContent // Explicitly request message content
            };

            this.client = new DiscordSocketClient(config);
            this.client.Log += LogFuncAsync;
            this.client.MessageReceived += MessageHandler;
        }


        public async Task StartBotAsync()
        {

            // Log in and start the bot
            await this.client.LoginAsync(TokenType.Bot, token);
            await this.client.StartAsync();
            Console.WriteLine("Bot is running...");
            await Task.Delay(-1);
        }

        private void LoadToken()
        {
            // Read the JSON file and deserialize it to get the token
            string configPath = Path.Combine(Directory.GetCurrentDirectory(), "config.json");

            Console.WriteLine($"Looking for config.json at: {configPath}");

            if (File.Exists(configPath))
            {
                string json = File.ReadAllText(configPath);
                var config = JsonConvert.DeserializeObject<Config>(json);
                token = config?.Token;
            }
        }

        private Task LogFuncAsync(LogMessage message)
        {
            Console.WriteLine(message.ToString());
            return Task.CompletedTask;
        }


        private async Task MessageHandler(SocketMessage message)
        {
            if (message.Author.IsBot) return;


            string[] parts = message.Content.Split(' ');


            if (message.Content.StartsWith("!verify") && parts.Length == 2)
            {
                string terrariaUsername = parts[1];

                if (pendingVerifications.ContainsKey(terrariaUsername))
                {
                    ulong discordUserId = pendingVerifications[terrariaUsername];
                    if (discordUserId == message.Author.Id)
                    {
                        await message.Channel.SendMessageAsync($"✅ {message.Author.Mention}, your Terraria account `{terrariaUsername}` has been linked!");
                        pendingVerifications.Remove(terrariaUsername);

                        // TODO: Store this link in a database or file
                    }
                    else
                    {
                        await message.Channel.SendMessageAsync("❌ This username is not linked to your Discord account.");
                    }
                }
                else
                {
                    await message.Channel.SendMessageAsync("❌ No pending verification found for this Terraria username.");
                }
            }

        }


        public async Task RequestVerification(string terrariaUsername, ulong discordUserId)
        {
            pendingVerifications[terrariaUsername] = discordUserId;

            var user = client.GetUser(discordUserId);
            if (user != null)
            {
                await user.SendMessageAsync($"🔹 You have requested to link your Terraria account `{terrariaUsername}`.\n" +
                                            $"To complete the process, type `!verify {terrariaUsername}` in any Discord server channel.");
            }
        }


        static void Main(string[] args) =>
            new Program().StartBotAsync().GetAwaiter().GetResult();
    }
}
