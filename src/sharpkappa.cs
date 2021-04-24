using System;
using System.Threading.Tasks;
using System.IO;
using System.Net.Sockets;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace sharpkappa
{
    public class sharpkappaBot {
        const string ip = "irc.chat.twitch.tv";
        const int port = 6697;
        private TaskCompletionSource<int> connected = new TaskCompletionSource<int>();

        private string nick;
        private string oauth;
        private StreamReader streamReader;
        private StreamWriter streamWriter;

        public sharpkappaBot(string nick, string oauth) {
            this.nick = nick;
            this.oauth = oauth;
        }

        private bool ValidateServerCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors) {
            return sslPolicyErrors == SslPolicyErrors.None;
        }

        public async Task start() {
            var tcpClient = new TcpClient();
            await tcpClient.ConnectAsync(ip, port);
            SslStream sslStream = new SslStream(tcpClient.GetStream(), false, ValidateServerCertificate, null);
            await sslStream.AuthenticateAsClientAsync(ip);
            
            streamReader = new StreamReader(sslStream);
            streamWriter = new StreamWriter(sslStream) { NewLine = "\r\n", AutoFlush = true };

            await streamWriter.WriteLineAsync($"PASS {oauth}");
            await streamWriter.WriteLineAsync($"NICK {nick}");
            connected.SetResult(0);

            while(true) {
                string line = await streamReader.ReadLineAsync();
                //Console.WriteLine(line);

                string[] split = line.Split(" ");
                if(line.StartsWith("PING")) {
                    await streamWriter.WriteLineAsync($"PONG {split[1]}");
                }

                if(split.Length > 1 && split[1] == "PRIVMSG") {
                    //:messageSenderUsername!messageSenderUsername@messageSenderUsername.tmi.twitch.tv 
                    string username = split[0].Substring(1, split[0].IndexOf("!")-1);
                    string message = line.Substring(line.IndexOf(':', 1)+1);
                    Console.WriteLine($"{username}: {message}");
                }
            }
        }

        public async Task sendMessage(string channel, string message) {
            await connected.Task;
            await streamWriter.WriteLineAsync($"PRIVMSG #{channel} :{message}");
        }

        public async Task joinChannel(string channel) {
            await connected.Task;
            await streamWriter.WriteLineAsync($"JOIN #{channel}");
        }
    }
}