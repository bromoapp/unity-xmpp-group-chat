using TMPro;
using UnityEngine;

public class FriendItem : MonoBehaviour
{
    [SerializeField]
    public TextMeshProUGUI Text;

    public string Jid { get; set; }
    public string Name { get; set; }
    public string Status { get; set; }
    public bool HasMessage { get; set; }

    private bool updateChatWithText = false;
    private string selectedJid;
    private XmppChatManager chatManager;

    void Start()
    {
        GameObject parent = transform.root.gameObject;
        if (parent != null)
        {
            // ------ Get SetSelectedJid method from ChatManager -------
            chatManager = parent.GetComponent<XmppChatManager>();
        }
        Text.text = Name + " [" + Status + "]";
    }

    void Update()
    {
        string status = Name + " [" + Status + "]";
        if (HasMessage)
        {
            status = status + " [MSG]";
        }
        Text.text = status;
        if (updateChatWithText)
        {
            chatManager.SetSelectedJid(selectedJid);
            updateChatWithText = false;
        }
    }

    public void OnChatBtnClicked()
    {
        HasMessage = false;
        selectedJid = Jid;
        updateChatWithText = true;
    }

}
