using TMPro;
using UnityEngine;

public class PeopleInfo
{
    [SerializeField]
    public TextMeshProUGUI Text;

    public string Jid { get; set; }
    public string Name { get; set; }

    public PeopleInfo(string jid, string name)
    {
        this.Jid = jid;
        this.Name = name;
    }

    void Start()
    {
        Text.text = Name;
    }

}
