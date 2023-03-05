public class GroupInfo
{
    public string Jid { get; set; }
    public string Name { get; set; }
    public string Type { get; set; }

    public override string ToString()
    {
        return "JID: " + Jid + "; NAME: " + Name + "; TYPE: " + Type;
    }
}