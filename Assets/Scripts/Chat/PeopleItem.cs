using TMPro;
using UnityEngine;

public class PeopleItem : MonoBehaviour
{
    [SerializeField]
    public TextMeshProUGUI Text;

    public string Jid { get; set; }
    public string Name { get; set; }

    void Start()
    {
        Text.text = Name;
    }

}
