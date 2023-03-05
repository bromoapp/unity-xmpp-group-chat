using TMPro;
using UnityEngine;

public class GroupConversationItem : MonoBehaviour
{
    public string SenderJid { get; set; }
    public string SenderName { get; set; }
    public string Message { get; set; }

    [SerializeField]
    public TextMeshProUGUI SenderTxt;

    [SerializeField]
    public TextMeshProUGUI MessageTxt;

    public void Start()
    {
        SenderTxt.text = SenderName;
        MessageTxt.text = Message;
    }

    public override string ToString()
    {
        return SenderName + ": " + Message;
    }
}
