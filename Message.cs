using System.Text.RegularExpressions;

namespace Chat_WebSocket_Server
{
    public class Message
    {
        private string userId;
        private string userName;
        private string content;

        public Message(string userId, string userName, string message)
        {
            this.userId = userId;
            this.userName = userName;
            this.content = message;
        }

        public static bool isMessage(string potentialMessage)
        {
            return Regex.Matches(potentialMessage, ";").Count() == 2;
        }

        /// <summary>
        /// A message string must be of the form {id};{username};{content}
        /// </summary>
        /// <param name="potentialMessage"></param>
        /// <returns></returns>
        public static Message Parse(string potentialMessage)
        {
            if (!isMessage(potentialMessage))
            {
                return null;
            }

            string[] parts = potentialMessage.Split(";");

            return new Message(parts[0], parts[1], parts[2]);
        }

        public static string JSONify(Message message)
        {
            return $"{{ \"userId\": \"{message.userId}\", \"userName\": \"{message.userName}\", \"content\": \"{message.content}\" }}";
        }

        public string ToJson()
        {
            return JSONify(this);
        }

        public static implicit operator string(Message message)
        {
            return $"{message.userId};{message.userName};{message.content}";
        }

        public static implicit operator Message(string text) {
            return Parse(text);
        }
    }
}
