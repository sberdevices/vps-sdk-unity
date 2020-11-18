using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ARVRLab.VPSService
{
    public enum ServerType { Prod, Stage, Dev };

    /// <summary>
    /// Класс для генерирования ссылки
    /// </summary>
    public static class URLController
    {
        // Заготовка для ссылки:
        // 0 - подсервер
        // 1 - имя проекта
        // 2 - location id
        const string urlBlank = "https://{0}api.{1}.vps.arvr.sberlabs.com/{2}";

        public static string CreateURL(VPSBuilding building, ServerType serverType)
        {
            return string.Format(urlBlank, GetServerApi(serverType), GetBuildingName(building), GetLocationId(building));
        }

        private static string GetLocationId(VPSBuilding building)
        {
            switch(building)
            {
                case VPSBuilding.Bootcamp:
                    return "eeb38592-4a3c-4d4b-b4c6-38fd68331521";
                case VPSBuilding.Polytech:
                    return "polytech";
                default:
                    return "";
            }
        }

        private static string GetBuildingName(VPSBuilding building)
        {
            switch (building)
            {
                case VPSBuilding.Bootcamp:
                    return "bootcamp";
                case VPSBuilding.Polytech:
                    return "polytech";
                default:
                    return "";
            }
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
    }
}