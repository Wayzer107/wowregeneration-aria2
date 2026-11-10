using System.IO;

namespace WoWRegeneration.Repositories
{
    public class WoW434 : WoWRepository
    {
        public override string GetBaseUrl()
        {
            return "http://dist.blizzard.com.edgesuite.net/wow-pod-retail/EU/15050.direct/";
        }

        public override string GetMFilName()
        {
            return "wow-15595-0C3502F50D17376754B9E9CB0109F4C5.mfil";
        }
    }
}