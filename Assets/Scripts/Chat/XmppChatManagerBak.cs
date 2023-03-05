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

public class XmppChatManagerBak : MonoBehaviour
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
    private string currentSelectedGroupJid = "";
    private GroupConversation onScreenGroupConversation;

    private static List<string> groupsThatHasNewMessage;
    private static LinkedList<Message> incomingGroupMessages;
    private static Dictionary<string, LinkedList<Message>> groupConversations;

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
        if (peoplePresenceUpdates == null || groupConversations == null ||
            incomingGroupMessages == null || groupsThatHasNewMessage == null ||
            sentQueryIds == null || receivedQueryResults == null ||
            groupChatsList == null || receivedGroupPresences == null ||
            groupPeoplesList == null)
        {
            // ----- Initiate all static attributes
            groupConversations = new Dictionary<string, LinkedList<Message>>();
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
        // ----- Rendering available groups to screen
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
        if (selectedGroupJid != currentSelectedGroupJid)
        {
            currentSelectedGroupJid = selectedGroupJid;
            if (groupConversations.ContainsKey(currentSelectedGroupJid))
            {
                setCurrentGroupConversationIfNecessary(currentSelectedGroupJid, Show.MUST_SHOW);
            }
            else
            {
                joinSelectedRoom(currentSelectedGroupJid);
            }

            //Console.WriteLine("SELECTED GROUP: " + selectedGroupJid);

        }

        // ----- Processing incoming grouo chat messages
        if (incomingGroupMessages != null && incomingGroupMessages.Count > 0)
        {

            //Console.WriteLine("READING RECEIVED GROUP MESSAGES");

            Message[] messages = incomingGroupMessages.ToArray();
            for (int x = 0; x < messages.Length; x++)
            {
                Message msg = messages[x];
                if (groupConversations.ContainsKey(msg.From.Bare))
                {
                    LinkedList<Message> oldGroupConversation = groupConversations.GetValueOrDefault(msg.From.Bare);
                    oldGroupConversation.AddLast(msg);

                    //Console.WriteLine("TOTAL IN OLD GROUP CONV: " + oldGroupConversation.Count);

                }
                else
                {

                    //Console.WriteLine("CREATING NEW GROUP CONVERSATION IN CACHE 1");

                    LinkedList<Message> newGroupConversation = creatingNewGroupConversationInCache(msg.From.Bare);
                    newGroupConversation.AddLast(msg);
                }
                setCurrentGroupConversationIfNecessary(msg.From.Bare, Show.IF_NECESSARY);
            }
            incomingGroupMessages.Clear();
        }

        // ----- Processing incoming Presence messages
        if (receivedGroupPresences != null && receivedGroupPresences.Count > 0)
        {

            //Console.WriteLine("READING RECEIVED GROUP PRESENCES");

            Presence[] presences = receivedGroupPresences.ToArray();
            for (int x = 0; x < presences.Length; x++)
            {
                Presence presence = presences[x];
                if (presence.From.ToString() == currentSelectedGroupJid)
                {
                    string group = Util.ParseGroupFromJid(presence.From.Bare).Name;

                    //Console.WriteLine("SUCCEED IN JOINING THE SELECTED GROUP " + group);

                    setCurrentGroupConversationIfNecessary(currentSelectedGroupJid, Show.MUST_SHOW);
                }
            }
            receivedGroupPresences.Clear();
        }

        // ----- Processing incoming Presence messages
        if (peoplePresenceUpdates != null && peoplePresenceUpdates.Count > 0)
        {

            //Console.WriteLine("READING RECEIVED PEOPLES PRESENCES");

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
                    groupPeoplesList.Add(name, pi);
                }
            }
            peoplePresenceUpdates.Clear();
            renderPeoplesList = true;
        }

        // ----- Processing incoming IQ messages
        if (receivedQueryResults != null && receivedQueryResults.Count > 0)
        {

            //Console.WriteLine("PARSING RECEIVED QUERY RESULTS");

            foreach (IQ iqMsg in receivedQueryResults.ToArray<IQ>())
            {
                if (iqMsg.From == Config.MUC_SERVICE_URL)
                {
                    if (iqMsg.FirstChild.TagName == "query")
                    {
                        Element queryEl = iqMsg.FirstChild;
                        if (queryEl.Namespace == Config.XMLNS_DISCO_ITEMS)
                        {

                            //Console.WriteLine("POPULATING AVAILABLE GROUP CHATS");

                            foreach (Element item in queryEl.SelectElements("item"))
                            {
                                string gjid = item.GetAttribute("jid");
                                GroupInfo groupItem = Util.ParseGroupFromJid(item.GetAttribute("jid"));
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
            foreach (PeopleInfo pi in groupPeoplesList.Values)
            {
                GameObject peopleItem = Instantiate(PeopleItemObj, transform);
                PeopleItem peopleItemObj = peopleItem.GetComponent<PeopleItem>();
                peopleItemObj.Name = pi.Name;
                peopleItemObj.Jid = pi.Jid;
                peopleItem.transform.SetParent(PeopleListContent, false);
            }
            renderPeoplesList = false;
        }

        // ----- Renders group messages
        if (renderOnScreenGroupConversation)
        {
            if (onScreenGroupConversation != null)
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
    }

    public void SetSelectedGroupJid(string jid)
    {
        if (jid != currentSelectedGroupJid)
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

            //Console.WriteLine("NEW MESSAGE : " + msg.ToString());

            incomingGroupMessages.AddLast(msg);
        }
    }

    // ----- Handles incoming Presence message, currently is in use for both getting
    // ----- group chat room response after sending a join room request and receiving
    // ----- any presence status of anybody that joined in a room
    private void Conn_OnPresence(object sender, Presence pres)
    {

        //Console.WriteLine("NEW PRESENCE\n" + pres.ToString());

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

        //Console.WriteLine(ex.ToString());

    }
    #endregion

    #region << Helper functions >>
    /************************************************************************
     * Helper functions
     ************************************************************************/
    private void queryAllAccessibleRooms()
    {

        //Console.WriteLine("QUERY AVAILABLE GROUP CHAT");

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

        //Console.WriteLine("REQUESTING JOIN A ROOM");

        string qid = Util.GenerateRandomMsgId();
        Presence presence = new Presence();
        presence.Id = qid;
        presence.From = conn.MyJID.Bare;
        presence.To = jid + "/" + Util.ParseProfileFromJid(conn.MyJID.Bare).FirstName;
        presence.AddTag("x xmlns='http://jabber.org/protocol/muc'");

        conn.Send(presence);
    }

    private void addMessageToOnScreenGroupConversation(string jid, Message msg)
    {
        if (groupConversations.ContainsKey(jid))
        {
            LinkedList<Message> oldGroupConversation = groupConversations.GetValueOrDefault(jid);
            oldGroupConversation.AddLast(msg);
            setCurrentGroupConversationIfNecessary(jid, Show.MUST_SHOW);
            renderOnScreenGroupConversation = true;
        }
    }

    private void setCurrentGroupConversationIfNecessary(string jid, Show show)
    {
        // ----- Checks if there is an on-screen conversation
        if (onScreenGroupConversation != null)
        {

            //Console.WriteLine("GOT AN ONSCREEN GROUP CONVERSATION OBJ");

            // ----- Updates current on-screen conversation if the sender equals to
            // ----- the current on-screen conversation
            if (onScreenGroupConversation.Jid == jid)
            {

                //Console.WriteLine("UPDATING ONSCREEN GROUP CONVERSATION OBJ");

                populatingOnScreenGroupConversation(jid);
            }
            else
            {

                //Console.WriteLine("NOT UPDATING ONSCREEN GROUP CONVERSATION OBJ");

                // ----- Creating a new private conversation in cache if not yet exists
                if (!groupConversations.ContainsKey(jid))
                {

                    //Console.WriteLine("CREATING NEW GROUP CONVERSATION IN CACHE 2");

                    creatingNewGroupConversationInCache(jid);
                }
                switch (show)
                {
                    case Show.MUST_SHOW:

                        //Console.WriteLine("SWITCHING ONSCREEN GROUP CONVERSATION OBJ");

                        populatingOnScreenGroupConversation(jid);
                        break;
                    case Show.IF_NECESSARY:

                        //Console.WriteLine("CHANGE GROUP'S STATUS");

                        groupsThatHasNewMessage.Add(jid);
                        break;
                }
            }
        }
        else
        {
            onScreenGroupConversation = new GroupConversation(jid);
            populatingOnScreenGroupConversation(jid);
        }
    }

    private void populatingOnScreenGroupConversation(string jid)
    {
        onScreenGroupConversation.Jid = jid;
        onScreenGroupConversation.Messages.Clear();
        LinkedList<Message> oldGroupConversation = groupConversations.GetValueOrDefault(jid);
        if (oldGroupConversation != null)
        {
            foreach (Message m in oldGroupConversation)
            {
                onScreenGroupConversation.Messages.AddLast(m);
            }
        }
        renderOnScreenGroupConversation = true;
    }

    private LinkedList<Message> creatingNewGroupConversationInCache(string jid)
    {
        // ----- Creating a new private conversation in cache if not yet exists
        LinkedList<Message> newGroupConversation = new LinkedList<Message>();
        groupConversations.Add(jid, newGroupConversation);
        return newGroupConversation;
    }
    #endregion
}
