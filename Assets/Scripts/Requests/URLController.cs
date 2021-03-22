using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ARVRLab.VPSService
{
    public enum ServerType { Prod, Stage, Dev };

    /// <summary>
    /// Generation server url
    /// </summary>
    public static class URLController
    {
        // Blank for url:
        // 0 - subserver
        // 1 - building name
        // 2 - location id
        const string urlBlank = "http{0}://{1}api.{2}.vps.arvr.sberlabs.com/{3}";

        public static string CreateURL(string buildingName, string buildingGuid, ServerType serverType)
        {
            return string.Format(urlBlank, GetSecure(serverType), GetServerApi(serverType), buildingName, buildingGuid);
        }

        private static string GetServerApi(ServerType type)
        {
            switch (type)
            {
                case ServerType.Prod:
                    return "";
                case ServerType.Stage:
                    return "stage.";
                case ServerType.Dev:
                    return "dev.";
                default:
                    return "";
            }
        }

        private static string GetSecure(ServerType serverType)
        {
            return serverType == ServerType.Prod ? "s" : "";
        }
    }
}