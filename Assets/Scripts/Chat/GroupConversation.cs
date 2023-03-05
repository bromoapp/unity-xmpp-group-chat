using System.Collections.Generic;
using Xmpp.protocol.client;

public class GroupConversation
{
    public string Jid { get; set; }
    public LinkedList<Message> Messages { get; set; }

    public GroupConversation(string Jid)
    {
        this.Jid = Jid;
        this.Messages = new LinkedList<Message>();
    }
}
