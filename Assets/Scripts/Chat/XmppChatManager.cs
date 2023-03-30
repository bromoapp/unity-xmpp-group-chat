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
    #region << Bindable Class's Attributes >>
    /************************************************************************
     * Group Chat related bindable attributes
     ************************************************************************/
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
    private TMP_InputField GroupChatInput;

    /************************************************************************
     * Private Chat related bindable attributes
     ************************************************************************/
    // ----- Private chat related attributes
    [SerializeField]
    private Transform ChatBoardContent;

    [SerializeField]
    private GameObject ChatItemObj;

    [SerializeField]
    private Transform FriendsListContent;

    [SerializeField]
    private GameObject FriendItemObj;

    [SerializeField]
    private TextMeshProUGUI ChatWithText;

    [SerializeField]
    private TMP_InputField ChatInput;

    /************************************************************************
     * Common Chat related bindable attributes
     ************************************************************************/
    [SerializeField]
    private TMP_InputField UsernameInput;

    [SerializeField]
    private TMP_InputField PasswordInput;
    #endregion

    #region << Common Class's Attributes >>
    /************************************************************************
     * Group Chat related attributes
     ************************************************************************/
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

    // ----- Attributes in regards to Global group chat;
    private string globalChatJid = "";
    private bool joinGlobalChat = false;
    private static LinkedList<Message> globalGroupConversation;

    /************************************************************************
     * Private Chat related attributes
     ************************************************************************/
    // ----- Attributes in regards to personal friends  
    private bool renderPersonalFriendsList = false;

    private int lastTotalPersonalFriends = 0;
    private static List<Presence> friendPresenceUpdates;
    private static LinkedList<RosterItem> personalFriends;

    // ----- Attributes in regards to private chatting
    private bool renderOnScreenPrivateConversation = false;

    private string selectedFriendJid = "";
    private string onScreenFriendJid = "";
    private PrivateConversation onScreenPrivateConversation;

    private static List<string> friendsWhoHasNewMessage;
    private static List<Message> incomingPrivateMessages;
    private static Dictionary<string, LinkedList<Message>> privateConversations;

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
        /************************************************************************
         * Group Chat related initiations
         ************************************************************************/
        if (peoplePresenceUpdates == null || incomingGroupMessages == null ||
            groupsThatHasNewMessage == null || sentQueryIds == null ||
            receivedQueryResults == null || groupChatsList == null ||
            globalGroupConversation == null || receivedGroupPresences == null ||
            groupPeoplesList == null)
        {
            // ----- Initiate all static attributes
            groupPeoplesList = new Dictionary<string, PeopleInfo>();
            receivedGroupPresences = new LinkedList<Presence>();
            incomingGroupMessages = new LinkedList<Message>();
            globalGroupConversation = new LinkedList<Message>();
            groupsThatHasNewMessage = new List<string>();
            peoplePresenceUpdates = new List<Presence>();
            groupChatsList = new LinkedList<GroupInfo>();
            receivedQueryResults = new LinkedList<IQ>();
            sentQueryIds = new List<string>();
        }

        /************************************************************************
         * Private Chat related initiations
         ************************************************************************/
        if (friendPresenceUpdates == null || privateConversations == null ||
            incomingPrivateMessages == null || personalFriends == null ||
            friendsWhoHasNewMessage == null)
        {
            // ----- Initiate all static attributes
            privateConversations = new Dictionary<string, LinkedList<Message>>();
            personalFriends = new LinkedList<RosterItem>();
            incomingPrivateMessages = new List<Message>();
            friendsWhoHasNewMessage = new List<string>();
            friendPresenceUpdates = new List<Presence>();
        }
        if (conn == null)
        {
            // ----- Setting xmpp connection
            conn = new XmppClientConnection();
            conn.Status = PresenceType.available.ToString();
            conn.Show = ShowType.chat;
            conn.AutoPresence = true;
            conn.AutoRoster = true;
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
        /************************************************************************
         * Group Chat related on Update() functions
         ************************************************************************/
        #region << Group chat related update() function block >>
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

                                    // ---- Capture if there is global chat GroupInfo received
                                    if (groupItem.Jid.StartsWith(Config.GLOBAL_MUC_JID))
                                    {
                                        globalChatJid = groupItem.Jid;
                                        joinGlobalChat = true;
                                    }
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
        if (renderGroupsList && GroupListContent != null)
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

        // ----- Join the Global chat room
        if (joinGlobalChat)
        {
            selectedGroupJid = globalChatJid;
            onScreenGroupJid = selectedGroupJid;
            joinSelectedRoom(globalChatJid);

            // ----- Delete people list from on-screen People List
            groupPeoplesList.Clear();
            renderPeoplesList = true;
            joinGlobalChat = false;
        }

        // ----- Change 'Group' label and switch current on-screen group conversation
        if (selectedGroupJid != onScreenGroupJid)
        {
            // ----- If onScreenGroupJid not equals to Global chat Jid, leave the room
            if (onScreenGroupJid.Length > 0 && !onScreenGroupJid.StartsWith(Config.GLOBAL_MUC_JID))
            {
                leavePreviousRoom(onScreenGroupJid);
            }
            if (onScreenGroupJid.Length > 0)
            {
                if (selectedGroupJid.StartsWith(Config.GLOBAL_MUC_JID))
                {
                    // ----- If selectedFriendJid equals to Global chat Jid, join the room
                    // ----- clear onscreen group conversation and populate it with cached global chat
                    onScreenGroupConversation.Jid = selectedGroupJid;
                    onScreenGroupConversation.Messages.Clear();
                    foreach (Message msg in globalGroupConversation)
                    {
                        onScreenGroupConversation.Messages.AddLast(msg);
                    }
                    renderOnScreenGroupConversation = true;
                }
                else
                {
                    // ----- If selectedFriendJid not equals to Global chat Jid, join the room
                    joinSelectedRoom(selectedGroupJid);
                }
            }
            onScreenGroupJid = selectedGroupJid;

            // ----- Delete people list from on-screen People List
            groupPeoplesList.Clear();
            renderPeoplesList = true;
        }

        // ----- Processing incoming group Presences
        if (receivedGroupPresences != null && receivedGroupPresences.Count > 0 && GroupChatText != null)
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
        if (renderPeoplesList && PeopleListContent != null)
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
            bool renderMessage = true;
            for (int x = 0; x < messages.Length; x++)
            {
                Message msg = messages[x];
                // ----- Check if message origin is from Global group chat
                if (msg.From.Bare.StartsWith(Config.GLOBAL_MUC_JID))
                {
                    if (onScreenGroupJid.StartsWith(Config.GLOBAL_MUC_JID))
                    {
                        // ----- If the onscreen group chat happens to be the Global chat
                        // ----- store the message to cache and renders it to screen
                        onScreenGroupConversation.Messages.AddLast(msg);
                        addMessageToGlobalGroupConversation(msg);
                    }
                    else
                    {
                        // ----- If the onscreen group chat is not the Global chat
                        // ----- store the message to cache, and don't render it to screen
                        addMessageToGlobalGroupConversation(msg);
                        renderMessage = false;
                        incomingGroupMessages.Clear();
                    }
                    Debug.LogFormat(">>> TOTAL MSG IN CACHE: {0}", globalGroupConversation.Count);
                }
                else
                {
                    onScreenGroupConversation.Messages.AddLast(msg);
                }
            }
            if (renderMessage)
            {
                incomingGroupMessages.Clear();
                renderOnScreenGroupConversation = true;
            }
        }

        // ----- Renders group messages
        if (renderOnScreenGroupConversation && GroupChatBoardContent != null)
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
        #endregion
        /************************************************************************
         * Private Chat related on Update() functions
         ************************************************************************/
        #region << Private chat related update() functions block >>
        // ----- Rendering friends list
        if (renderPersonalFriendsList && FriendsListContent != null)
        {
            int totalPersonalFriends = personalFriends.Count;
            if (totalPersonalFriends > lastTotalPersonalFriends)
            {
                foreach (RosterItem roster in personalFriends)
                {
                    if (!roster.Jid.ToString().Contains(Config.MUC_MSG_KEYWORD))
                    {
                        UserProfile profile = Util.ParseProfileFromJid(roster.Jid.Bare);
                        if (profile != null)
                        {
                            GameObject friendItem = Instantiate(FriendItemObj, transform);
                            FriendItem friendItemObj = friendItem.GetComponent<FriendItem>();
                            friendItemObj.Name = profile.FirstName;
                            friendItemObj.Jid = roster.Jid.Bare;
                            friendItemObj.Status = "OFF";
                            lastTotalPersonalFriends += 1;
                            friendItem.transform.SetParent(FriendsListContent, false);
                        }
                    }
                }
            }
            renderPersonalFriendsList = false;
        }

        // ----- Rendering friend's presence status
        if (friendPresenceUpdates != null && friendPresenceUpdates.Count > 0 && FriendsListContent != null)
        {
            for (int a = 0; a < friendPresenceUpdates.Count; a++)
            {
                Presence pres = friendPresenceUpdates[a];
                string jid = pres.From.ToString().Split("/")[0];
                for (int i = 0; i < FriendsListContent.childCount; i++)
                {
                    GameObject friendItem = FriendsListContent.GetChild(i).gameObject;
                    if (friendItem != null)
                    {
                        FriendItem friendItemObj = friendItem.GetComponent<FriendItem>();
                        if (friendItemObj != null && friendItemObj.Jid == jid)
                        {
                            switch (pres.Type)
                            {
                                case PresenceType.available:
                                    friendItemObj.Status = "ON";
                                    break;
                                case PresenceType.unavailable:
                                    friendItemObj.Status = "OFF";
                                    break;
                                case PresenceType.error:
                                    friendItemObj.Status = "OFF";
                                    break;
                                case PresenceType.invisible:
                                    friendItemObj.Status = "OFF";
                                    break;
                            }
                        }
                    }
                }
            }
            friendPresenceUpdates.Clear();
        }

        // ----- Storing incoming private message
        if (incomingPrivateMessages != null && incomingPrivateMessages.Count > 0)
        {
            foreach (Message msg in incomingPrivateMessages)
            {
                string jid = msg.From.Bare;
                if (privateConversations.ContainsKey(jid))
                {
                    // ----- Updates cached conversations for received messages
                    LinkedList<Message> oldPrivateConversation = privateConversations.GetValueOrDefault(jid);
                    oldPrivateConversation.AddLast(msg);
                }
                else
                {
                    // ----- or create a new cache of conversation
                    LinkedList<Message> newPrivateConversation = new LinkedList<Message>();
                    newPrivateConversation.AddLast(msg);
                    privateConversations.Add(jid, newPrivateConversation);
                }
                updateOnScreenPrivateConversationOrUpdateSenderStatus(jid, Show.IF_NECESSARY);
            }
            incomingPrivateMessages.Clear();
        }

        // ----- Change friend's status if has message
        if (friendsWhoHasNewMessage != null && friendsWhoHasNewMessage.Count > 0 && FriendsListContent != null)
        {
            for (int x = 0; x < friendsWhoHasNewMessage.Count; x++)
            {
                string jid = friendsWhoHasNewMessage[x];
                for (int i = 0; i < FriendsListContent.childCount; i++)
                {
                    GameObject friendItem = FriendsListContent.GetChild(i).gameObject;
                    if (friendItem != null)
                    {
                        FriendItem friendItemObj = friendItem.GetComponent<FriendItem>();
                        if (friendItemObj != null && friendItemObj.Jid == jid)
                        {
                            friendItemObj.HasMessage = true;
                        }
                    }
                }
            }
            friendsWhoHasNewMessage.Clear();
        }

        // ----- Change 'Chat with' label and switch current on-screen conversation
        if (selectedFriendJid != onScreenFriendJid)
        {
            if (!privateConversations.ContainsKey(selectedFriendJid))
            {
                // ----- Create a new cache of conversation
                LinkedList<Message> newPrivateConversation = new LinkedList<Message>();
                privateConversations.Add(selectedFriendJid, newPrivateConversation);
            }
            onScreenFriendJid = selectedFriendJid;
            UserProfile profile = Util.ParseProfileFromJid(selectedFriendJid);
            setChatWithLabel(ChatWithText, profile);
            updateOnScreenPrivateConversationOrUpdateSenderStatus(onScreenFriendJid, Show.MUST_SHOW);
        }

        // ----- Rendering a private conversation to screen
        if (renderOnScreenPrivateConversation && ChatBoardContent != null)
        {
            if (onScreenPrivateConversation != null)
            {
                UserProfile profile = Util.ParseProfileFromJid(onScreenPrivateConversation.Jid);
                setChatWithLabel(ChatWithText, profile);
                // ----- Destroying all gameobjects in scroll view content
                if (ChatBoardContent.childCount > 0)
                {
                    while (ChatBoardContent.childCount > 0)
                    {
                        DestroyImmediate(ChatBoardContent.GetChild(0).gameObject);
                    }
                }
                // ----- Recreate gameobjects in scroll view content
                foreach (Message msg in onScreenPrivateConversation.Messages)
                {
                    GameObject chatItem = Instantiate(ChatItemObj, transform);
                    ConversationItem conversationItemObj = chatItem.GetComponent<ConversationItem>();
                    conversationItemObj.SenderJid = msg.From.Bare;
                    conversationItemObj.SenderName = Util.ParseProfileFromJid(msg.From.Bare).FirstName;
                    conversationItemObj.Message = msg.Body;
                    chatItem.transform.SetParent(ChatBoardContent, false);
                }
            }
            renderOnScreenPrivateConversation = false;
        }
        #endregion
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

    public void OnSendGroupChatBtnClicked()
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

    public void OnSendPrivateChatBtnClicked()
    {
        if (conn != null && conn.Authenticated)
        {
            if (onScreenPrivateConversation != null)
            {
                if (ChatInput.text.Length > 0)
                {
                    string message = ChatInput.text;
                    Jid jid = new Jid(onScreenPrivateConversation.Jid);
                    Message chatMsg = new Message(jid, conn.MyJID);
                    chatMsg.Body = message;
                    chatMsg.Type = MessageType.chat;
                    conn.Send(chatMsg);
                    addMessageToOnScreenPrivateConversation(jid.Bare, chatMsg);
                    ChatInput.text = "";
                }
            }
        }
    }

    public void SetSelectedJid(string jid)
    {
        if (jid != onScreenFriendJid)
        {
            selectedFriendJid = jid;
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
            if (msg.From.Bare.Contains(Config.MUC_MSG_KEYWORD))
            {
                incomingGroupMessages.AddLast(msg);
            }
            else
            {
                incomingPrivateMessages.Add(msg);
            }
        }
    }

    // ----- Handles incoming Presence message, currently is in use for both getting
    // ----- group chat room response after sending a join room request and receiving
    // ----- any presence status of anybody that joined in a room and also for updating
    // ----- friends on/offline status in friends list
    private void Conn_OnPresence(object sender, Presence pres)
    {
        if (pres.From.Bare.Contains(Config.MUC_MSG_KEYWORD))
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
        else
        {
            friendPresenceUpdates.Add(pres);
        }
    }

    // ----- Handles incoming Roster message, to notify this chat app that
    // ----- the server will start sending friends data
    private void Conn_OnRosterStart(object sender)
    {
        renderPersonalFriendsList = false;
    }

    // ----- Handles incoming Roster (friend's data) so this app can
    // ----- stores it to a collection object
    private void Conn_OnRosterItem(object sender, RosterItem item)
    {
        personalFriends.AddLast(item);
    }

    // ----- Handles incoming Roster message, to notify this chat app that
    // ----- the server has finished sending all his/her friends data
    private void Conn_OnRosterEnd(object sender)
    {
        renderPersonalFriendsList = true;
    }

    // ----- Handles incoming notification that the user successfully
    // ----- logged in into the server
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
     * Group Chat Helper functions
     ************************************************************************/
    #region << Group Chat related helper functions >>
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

    private void addMessageToGlobalGroupConversation(Message msg)
    {
        globalGroupConversation.AddLast(msg);
        if (globalGroupConversation.Count > Config.GLOBAL_MUC_CACHE_SIZE)
        {
            globalGroupConversation.RemoveFirst();
        }
    }
    #endregion
    /************************************************************************
     * Private Chat Helper functions
     ************************************************************************/
    #region << Private Chat related helper functions >>
    private void addMessageToOnScreenPrivateConversation(string jid, Message msg)
    {
        if (privateConversations.ContainsKey(jid))
        {
            LinkedList<Message> oldPrivateConversation = privateConversations.GetValueOrDefault(jid);
            oldPrivateConversation.AddLast(msg);
            updateOnScreenPrivateConversationOrUpdateSenderStatus(jid, Show.MUST_SHOW);
            renderOnScreenPrivateConversation = true;
        }
    }

    private void updateOnScreenPrivateConversationOrUpdateSenderStatus(string jid, Show show)
    {
        // ----- Checks if there is an on-screen conversation currently
        // ----- if there isn't any, create one and display it on-screen
        if (onScreenPrivateConversation != null)
        {
            if (onScreenPrivateConversation.Jid == jid)
            {
                // ----- Updates current on-screen conversation if the sender jid equals to
                // ----- the current on-screen conversation jid
                repopulateOnScreenPrivateConversation(jid);
            }
            else
            {
                switch (show)
                {
                    case Show.MUST_SHOW:
                        // ----- Switch on-screen conversation to the conversation with
                        // ----- sender jid
                        repopulateOnScreenPrivateConversation(jid);
                        break;
                    case Show.IF_NECESSARY:
                        // ----- Add 'MSG' tag in sender's roster in friends list
                        friendsWhoHasNewMessage.Add(jid);
                        break;
                }
            }
        }
        else
        {
            // ----- Creating a new on-screen conversation obj since there is no
            // ----- on-screen conversation currently
            onScreenPrivateConversation = new PrivateConversation(jid);
            // ----- Populate current private conversation from cached conversation
            repopulateOnScreenPrivateConversation(jid);
        }
    }

    private void repopulateOnScreenPrivateConversation(string jid)
    {
        selectedFriendJid = onScreenFriendJid = jid;
        onScreenPrivateConversation.Jid = jid;
        onScreenPrivateConversation.Messages.Clear();
        LinkedList<Message> oldPrivateConversation = privateConversations.GetValueOrDefault(jid);
        foreach (Message o in oldPrivateConversation.ToArray())
        {
            onScreenPrivateConversation.Messages.AddLast(o);
        }
        renderOnScreenPrivateConversation = true;
    }

    private void setChatWithLabel(TextMeshProUGUI component, UserProfile profile)
    {
        component.text = "Chat with: " + profile.FirstName + " " + profile.LastName;
    }
    #endregion
    #endregion
}
