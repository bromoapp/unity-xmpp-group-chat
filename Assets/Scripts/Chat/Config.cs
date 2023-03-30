namespace Assets.Scripts.Chat
{
    public class Config
    {
        public static string SERVER_URL = "dev-chat.planetpersib.com";
        public static int SERVER_PORT = 5222;

        public static string MUC_SERVICE_URL = "conference.dev-chat.planetpersib.com";

        public static string XMLNS_DISCO_ITEMS = "http://jabber.org/protocol/disco#items";
        public static string XMLNS_DISCO_INFO = "http://jabber.org/protocol/disco#info";
        public static string XMLNS_MUC_SUB = "urn:xmpp:mucsub:0";
        public static string XMLNS_MUC_USER = "http://jabber.org/protocol/muc#user";

        public static string MUC_MSG_KEYWORD = "conference";
        public static string GLOBAL_MUC_JID = "global@conference";
    }
}
