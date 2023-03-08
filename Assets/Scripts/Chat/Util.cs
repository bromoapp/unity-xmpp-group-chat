using System;
using System.Diagnostics;
using System.Linq;

namespace Assets.Scripts
{
    public class Util
    {
        public static UserProfile ParseProfileFromJid(string jid)
        {
            try
            {
                string[] data = jid.Split('_');
                UserProfile profile = new UserProfile();
                profile.FirstName = FirstCharToUpper(data[0]);
                profile.LastName = FirstCharToUpper(data[1]);
                profile.Email = data[2] + "@" + data[3] + "." + data[4];
                return profile;
            }
            catch (Exception e)
            {
                // Ignore false format
                return null;
            }
        }

        public static GroupInfo ParseGroupFromJid(string jid)
        {
            Debug.Write(">>> JID: " + jid);
            string[] raw = jid.Split('@');
            string gjid = raw[0];
            GroupInfo group = new GroupInfo();
            group.Jid = jid;
            if (gjid.Contains("clan_"))
            {
                group.Type = "CLAN";
                string rawName = gjid.Replace("clan_", "");
                if (rawName.Contains("_"))
                {
                    string[] names = rawName.Split("_");
                    for (int x = 0; x < names.Length; x++)
                    {
                        group.Name += Util.FirstCharToUpper(names[x]) + " ";
                    }
                }
                else
                {
                    group.Name = Util.FirstCharToUpper(rawName);
                }
            }
            else if (gjid.Contains("scene_"))
            {
                group.Type = "SCENE";
                string rawName = gjid.Replace("scene_", "");
                if (rawName.Contains("_"))
                {
                    string[] names = rawName.Split("_");
                    for (int x = 0; x < names.Length; x++)
                    {
                        group.Name += Util.FirstCharToUpper(names[x]) + " ";
                    }
                }
                else
                {
                    group.Name = Util.FirstCharToUpper(rawName);
                }
            }
            else
            {
                group.Type = null;
                group.Name = Util.FirstCharToUpper(gjid);
            }
            return group;
        }

        public static string FirstCharToUpper(string input)
        {
            if (String.IsNullOrEmpty(input))
                throw new ArgumentException("ARGH!");
            return input.First().ToString().ToUpper() + String.Join("", input.Skip(1));
        }

        public static string GenerateRandomMsgId()
        {
            return Guid.NewGuid().ToString();
        }
    }
}
