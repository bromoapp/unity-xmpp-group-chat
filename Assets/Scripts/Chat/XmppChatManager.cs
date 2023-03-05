using Assets.Scripts;
using Assets.Scripts.Chat;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using Xmpp;
using Xmpp.protocol.client;
using Xmpp.protocol.iq.roster;
using Xmpp.Xml.Dom;

public class XmppChatManager : MonoBehaviour
{
    #region << Class's Attributes >>
    /************************************************************************
     * Class's attributes
     ************************************************************************/

    // ----- Bindable attributes  
    [SerializeField]
    private Transform GroupChatBoardContent;

    [SerializeField]
    private GameObject GroupChatItemObj;

    [SerializeField]
    private Transform PeopleListContent;

    [SerializeField]
    private GameObject PeopleItemObj;

    [SerializeField]
    private Transform GroupListContent;

    [SerializeField]
    private GameObject GroupItemObj;

    [SerializeField]
    private TextMeshProUGUI GroupChatText;

    [SerializeField]
    private TMP_InputField UsernameInput;

    [SerializeField]
    private TMP_InputField PasswordInput;

    [SerializeField]
    private TMP_InputField GroupChatInput;

    // ----- Attributes in regards to XML messaging
    private static List<string> sentQueryIds;
    private static LinkedList<IQ> receivedQueryResults;
    private static LinkedList<Presence> receivedGroupPresences;

    // ----- Attributes in regards to group chatting
    private bool renderOnScreenGroupConversation = false;
    private bool renderPeoplesList = false;
    private bool renderGroupsList = false;

    private static Dictionary<string, PeopleInfo> groupPeoplesList;
    private static LinkedList<GroupInfo> groupChatsList;
    private static List<Presence> peoplePresenceUpdates;

    private string selectedGroupJid = "";
    private string onScreenGroupJid = "";
    private GroupConversation onScreenGroupConversation;

    private static List<string> groupsThatHasNewMessage;
    private static LinkedList<Message> incomingGroupMessages;

    // ----- Attributes related to the connection to the server 
    private static XmppClientConnection conn;

    private static int serv_port = Config.SERVER_PORT;
    private static string serv_url = Config.SERVER_URL;
    private static string username = "bromo_kunto_bromokun_gmail_com";
    private static string password = "555555";
    #endregion

    #region << MonoBehavior common functions >>
    /************************************************************************
     * MonoBehavior common functions
     ************************************************************************/
    // ----- Start is called before the first frame update
    void Start()
    {
        if (peoplePresenceUpdates == null || incomingGroupMessages == null ||
            groupsThatHasNewMessage == null || sentQueryIds == null ||
            receivedQueryResults == null || groupChatsList == null ||
            receivedGroupPresences == null || groupPeoplesList == null)
        {
            // ----- Initiate all static attributes
            groupPeoplesList = new Dictionary<string, PeopleInfo>();
            receivedGroupPresences = new LinkedList<Presence>();
            incomingGroupMessages = new LinkedList<Message>();
            groupsThatHasNewMessage = new List<string>();
            peoplePresenceUpdates = new List<Presence>();
            groupChatsList = new LinkedList<GroupInfo>();
            receivedQueryResults = new LinkedList<IQ>();
            sentQueryIds = new List<string>();
        }
        if (conn == null)
        {
            // ----- Setting xmpp connection
            conn = new XmppClientConnection();
            conn.Status = PresenceType.available.ToString();
            conn.Show = ShowType.chat;
            conn.AutoPresence = true;
            conn.AutoRoster = false;
            conn.AutoAgents = false;
            conn.EnableCapabilities = false;

            // ----- Setting xmpp related event handlers
            conn.OnLogin += Conn_OnLogin;
            conn.OnError += Conn_OnError;
            conn.OnRosterStart += Conn_OnRosterStart;
            conn.OnRosterItem += Conn_OnRosterItem;
            conn.OnRosterEnd += Conn_OnRosterEnd;
            conn.OnPresence += Conn_OnPresence;
            conn.OnMessage += Conn_OnMessage;
            conn.OnIq += Conn_OnIq;
        }
    }

    // ----- Update is called once per frame
    void Update()
    {
        // ----- Processing incoming IQ messages that returns the list of accessible
        // ----- chat rooms for this user
        if (receivedQueryResults != null && receivedQueryResults.Count > 0)
        {
            foreach (IQ iqMsg in receivedQueryResults.ToArray<IQ>())
            {
                if (iqMsg.From == Config.MUC_SERVICE_URL)
                {
                    if (iqMsg.FirstChild.TagName == "query")
                    {
                        Element queryEl = iqMsg.FirstChild;
                        // ----- Check if IQ message contains a list of accessible chat rooms
                        if (queryEl.Namespace == Config.XMLNS_DISCO_ITEMS)
                        {
                            // ----- Stores all accessible chat rooms to temporary collection object
                            foreach (Element item in queryEl.SelectElements("item"))
                            {
                                string gjid = item.GetAttribute("jid");
                                GroupInfo groupItem = Util.ParseGroupFromJid(gjid);
                                if (groupItem != null)
                                {
                                    groupChatsList.AddLast(groupItem);
                                }
                            }
                            renderGroupsList = true;
                        }
                    }
                }
                receivedQueryResults.Clear();
            }
        }

        // ----- Rendering accessible chat rooms to the screen
        if (renderGroupsList)
        {
            // ----- Destroying all gameobjects in scroll view content
            if (GroupListContent.childCount > 0)
            {
                while (GroupListContent.childCount > 0)
                {
                    DestroyImmediate(GroupListContent.GetChild(0).gameObject);
                }
            }
            // ----- Recreate gameobjects in scroll view content
            foreach (GroupInfo gi in groupChatsList)
            {
                GameObject groupItem = Instantiate(GroupItemObj, transform);
                GroupItem groupItemObj = groupItem.GetComponent<GroupItem>();
                groupItemObj.Name = gi.Name;
                groupItemObj.Jid = gi.Jid;
                groupItemObj.Type = gi.Type;
                groupItem.transform.SetParent(GroupListContent, false);
            }
            renderGroupsList = false;
        }

        // ----- Change 'Group' label and switch current on-screen group conversation
        if (selectedGroupJid != onScreenGroupJid)
        {
            if (onScreenGroupJid.Length > 0)
            {
                leavePreviousRoom(onScreenGroupJid);
            }
            joinSelectedRoom(selectedGroupJid);
            onScreenGroupJid = selectedGroupJid;

            // ----- Delete people list from on-screen People List
            groupPeoplesList.Clear();
            renderPeoplesList = true;
        }

        // ----- Processing incoming group Presences
        if (receivedGroupPresences != null && receivedGroupPresences.Count > 0)
        {
            Presence[] presences = receivedGroupPresences.ToArray();
            for (int x = 0; x < presences.Length; x++)
            {
                Presence presence = presences[x];
                // ----- If the presence JID equals to selected group JID, then this app alraedy connected with
                // ----- the selected group chat service
                if (presence.From.ToString() == onScreenGroupJid)
                {
                    GroupChatText.text = "Group: " + Util.ParseGroupFromJid(presence.From.Bare).Name;
                    onScreenGroupConversation = new GroupConversation(onScreenGroupJid);
                    renderOnScreenGroupConversation = true;
                }
            }
            receivedGroupPresences.Clear();
        }

        // ----- Processing incoming people Presence messages
        if (peoplePresenceUpdates != null && peoplePresenceUpdates.Count > 0)
        {
            Presence[] presences = peoplePresenceUpdates.ToArray();
            for (int x = 0; x < presences.Length; x++)
            {
                Presence presence = presences[x];
                string name = presence.From.ToString().Split("/")[1];
                if (groupPeoplesList.ContainsKey(name) && presence.Type == PresenceType.unavailable)
                {
                    // ----- Remove leaved occupant
                    groupPeoplesList.Remove(name);
                }
                else
                {
                    // ----- Add joined occupant
                    PeopleInfo pi = new PeopleInfo(null, name);
                    if (!groupPeoplesList.ContainsKey(name))
                    {
                        groupPeoplesList.Add(name, pi);
                    }
                }
            }
            peoplePresenceUpdates.Clear();
            renderPeoplesList = true;
        }

        // ----- Processing occupants presences
        if (renderPeoplesList)
        {
            // ----- Destroying all gameobjects in scroll view content
            if (PeopleListContent.childCount > 0)
            {
                while (PeopleListContent.childCount > 0)
                {
                    DestroyImmediate(PeopleListContent.GetChild(0).gameObject);
                }
            }
            // ----- Recreate gameobjects in scroll view content
            if (groupPeoplesList != null && groupPeoplesList.Count > 0)
            {
                foreach (PeopleInfo pi in groupPeoplesList.Values)
                {
                    GameObject peopleItem = Instantiate(PeopleItemObj, transform);
                    PeopleItem peopleItemObj = peopleItem.GetComponent<PeopleItem>();
                    peopleItemObj.Name = pi.Name;
                    peopleItemObj.Jid = pi.Jid;
                    peopleItem.transform.SetParent(PeopleListContent, false);
                }
            }
            renderPeoplesList = false;
        }

        // ----- Processing incoming group chat messages
        if (incomingGroupMessages != null && incomingGroupMessages.Count > 0)
        {
            Message[] messages = incomingGroupMessages.ToArray();
            for (int x = 0; x < messages.Length; x++)
            {
                Message msg = messages[x];
                onScreenGroupConversation.Messages.AddLast(msg);
            }
            incomingGroupMessages.Clear();
            renderOnScreenGroupConversation = true;
        }

        // ----- Renders group messages
        if (renderOnScreenGroupConversation)
        {
            GroupInfo gi = Util.ParseGroupFromJid(onScreenGroupConversation.Jid);
            GroupChatText.text = "Group: " + gi.Name;

            if (GroupChatBoardContent.childCount > 0)
            {
                while (GroupChatBoardContent.childCount > 0)
                {
                    DestroyImmediate(GroupChatBoardContent.GetChild(0).gameObject);
                }
            }
            // ----- Recreate gameobjects in scroll view content
            foreach (Message msg in onScreenGroupConversation.Messages)
            {
                GameObject chatItem = Instantiate(GroupChatItemObj, transform);
                GroupConversationItem conversationItemObj = chatItem.GetComponent<GroupConversationItem>();
                conversationItemObj.SenderName = msg.From.ToString().Split("/")[1];
                conversationItemObj.Message = msg.Body;
                chatItem.transform.SetParent(GroupChatBoardContent, false);
            }
            renderOnScreenGroupConversation = false;
        }
    }

    // ----- Functions to handle on application closed
    void OnApplicationQuit()
    {
        if (conn != null && conn.Authenticated)
        {
            conn.Close();
        }
    }
    #endregion

    #region << Events handlers functions >>
    /************************************************************************
     * User events handlers functions
     ************************************************************************/
    public void OnLoginBtnClicked()
    {
        if (conn != null && !conn.Authenticated)
        {
            conn.Port = serv_port;
            conn.Server = serv_url;
            conn.Open(username, password);
        }
    }

    public void OnSendBtnClicked()
    {
        if (conn != null && conn.Authenticated)
        {
            string nick = Util.ParseProfileFromJid(conn.MyJID.Bare).FirstName;
            string body = GroupChatInput.text;
            Message msg = new Message();
            msg.Body = body;
            msg.From = conn.MyJID.Bare;
            msg.To = onScreenGroupConversation.Jid;
            msg.Type = MessageType.groupchat;

            conn.Send(msg);
            GroupChatInput.text = "";
        }
    }

    public void SetSelectedGroupJid(string jid)
    {
        if (jid != onScreenGroupJid)
        {
            selectedGroupJid = jid;
        }
    }

    /************************************************************************
     * XMPP events handlers functions
     ************************************************************************/
    // ----- Handles incoming IQ message, currently is in use to receives query
    // ----- result for chat rooms that accessible by the user
    private void Conn_OnIq(object sender, IQ iq)
    {
        if (sentQueryIds.Contains(iq.Id))
        {
            receivedQueryResults.AddLast(iq);
        }
        sentQueryIds.Remove(iq.Id);
    }

    // ----- Handles incoming Message, currently is in use to receives chat room messages
    private void Conn_OnMessage(object sender, Message msg)
    {
        if (msg.Body != null && msg.Body.Length > 0)
        {
            incomingGroupMessages.AddLast(msg);
        }
    }

    // ----- Handles incoming Presence message, currently is in use for both getting
    // ----- group chat room response after sending a join room request and receiving
    // ----- any presence status of anybody that joined in a room
    private void Conn_OnPresence(object sender, Presence pres)
    {
        if (pres.From.ToString().Contains("/"))
        {
            peoplePresenceUpdates.Add(pres);
        }
        else
        {
            receivedGroupPresences.AddLast(pres);
        }
    }

    private void Conn_OnRosterStart(object sender)
    {
        /**
         * Please refer to PrivateChat project
         */
    }

    private void Conn_OnRosterItem(object sender, RosterItem item)
    {
        /**
         * Please refer to PrivateChat project
         */
    }

    private void Conn_OnRosterEnd(object sender)
    {
        /**
         * Please refer to PrivateChat project
         */
    }

    private void Conn_OnLogin(object sender)
    {
        queryAllAccessibleRooms();
    }

    private void Conn_OnError(object sender, Exception ex)
    {
        Console.WriteLine(ex.ToString());
    }
    #endregion

    #region << Helper functions >>
    /************************************************************************
     * Helper functions
     ************************************************************************/
    private void queryAllAccessibleRooms()
    {
        string qid = Util.GenerateRandomMsgId();
        IQ iq = new IQ();
        iq.Id = qid;
        iq.From = conn.MyJID.Bare;
        iq.To = Config.MUC_SERVICE_URL;
        iq.Type = IqType.get;
        iq.AddTag("query xmlns='http://jabber.org/protocol/disco#items'");

        conn.Send(iq);
        sentQueryIds.Add(qid);
    }

    private void joinSelectedRoom(string jid)
    {
        string qid = Util.GenerateRandomMsgId();
        Presence presence = new Presence();
        presence.Id = qid;
        presence.From = conn.MyJID.Bare;
        presence.To = jid + "/" + Util.ParseProfileFromJid(conn.MyJID.Bare).FirstName;
        presence.AddTag("x xmlns='http://jabber.org/protocol/muc'");

        conn.Send(presence);
    }

    private void leavePreviousRoom(string jid)
    {
        string qid = Util.GenerateRandomMsgId();
        Presence presence = new Presence();
        presence.Id = qid;
        presence.From = conn.MyJID.Bare;
        presence.To = jid;
        presence.Type = PresenceType.unavailable;
        presence.AddTag("x xmlns='http://jabber.org/protocol/muc'");

        conn.Send(presence);
    }

    private void addMessageToOnScreenGroupConversation(string jid, Message msg)
    {

    }

    #endregion
}
