using System.Collections.Generic;
using Xmpp.protocol.client;

public class PrivateConversation
{
    public string Jid { get; set; }
    public LinkedList<Message> Messages { get; set; }

    public PrivateConversation(string Jid) 
    {
        this.Jid = Jid;
        this.Messages = new LinkedList<Message>();
    }
}
