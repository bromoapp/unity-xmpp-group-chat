using TMPro;
using UnityEngine;

public class GroupItem : MonoBehaviour
{
    [SerializeField]
    public TextMeshProUGUI Text;

    [SerializeField]
    public TextMeshProUGUI Button;

    public string Jid { get; set; }
    public string Name { get; set; }
    public string Type { get; set; }
    public bool HasMessage { get; set; }
    public bool HasJoined { get; set; }

    private bool updateGroupChatWithText = false;
    private string selectedGroupJid;
    private XmppChatManager chatManager;

    public GroupItem()
    {
        HasMessage = false;
        HasJoined = false;
    }

    void Start()
    {
        GameObject parent = transform.root.gameObject;
        if (parent != null)
        {
            // ------ Get SetSelectedGroupJid attribute from ChatManager -------
            chatManager = parent.GetComponent<XmppChatManager>();
        }

        Text.text = Name;
        if (Type != null)
        {
            Text.text += " [" + Type + "]";
        }
    }

    void Update()
    {
        string status = Name;
        if (Type != null)
        {
            status += " [" + Type + "]";
        }
        if (HasMessage)
        {
            status = status + " [Message]";
        }
        Text.text = status;

        if (HasJoined)
        {
            Button.text = "Left";
        }
        else
        {
            Button.text = "Join";
        }

        if (updateGroupChatWithText)
        {
            chatManager.SetSelectedGroupJid(selectedGroupJid);
            updateGroupChatWithText = false;
        }
    }

    public void OnJoinBtnClicked()
    {
        HasMessage = false;
        selectedGroupJid = this.Jid;
        updateGroupChatWithText = true;
    }

    public override string ToString()
    {
        return "JID: " + Jid + "; NAME: " + Name + "; TYPE: " + Type;
    }

}
