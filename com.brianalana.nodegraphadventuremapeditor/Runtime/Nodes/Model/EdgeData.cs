using System;
using System.Collections.Generic;
using UnityEngine;



namespace NGAME
{
    [System.Serializable]
    public class EdgeData
    {
        public string SourceNodeGuid = "";
        public string SourceSceneGuid = "";
        public string SourceSceneName = "";
        public string SourcePortName = "";


        public string DestinationNodeGuid = "";
        public string DestinationSceneGuid = "";
        public string DestinationSceneName = "";
        public string DestinationPortName = "";

        public bool IsEdgeValid { get => m_bIsValid; }
        [SerializeField, HideInInspector]
        protected bool m_bIsValid = true;
        public EdgeData()
        { }

        public static EdgeData Invert(EdgeData otherEdge)
        {
            EdgeData result = new();

            result.SourceNodeGuid = otherEdge.DestinationNodeGuid;
            result.SourceSceneGuid = otherEdge.DestinationSceneGuid;
            result.SourceSceneName = otherEdge.DestinationSceneName;
            result.SourcePortName = otherEdge.DestinationPortName;

            result.DestinationNodeGuid = otherEdge.SourceNodeGuid;
            result.DestinationSceneGuid = otherEdge.SourceSceneGuid;
            result.DestinationSceneName = otherEdge.SourceSceneName;
            result.DestinationPortName = otherEdge.SourcePortName;

            return result;
        }
        public EdgeData(string sourceNodeGuid, string sourceSceneGuid, string sourceSceneName, string sourcePortName,
            string destinationNodeGuid, string destinationSceneGuid, string destinationSceneName, string destinationPortName)
        {
            SourceNodeGuid = sourceNodeGuid;
            SourceSceneGuid = sourceSceneGuid;
            SourceSceneName = sourceSceneName;
            SourcePortName = sourcePortName;

            DestinationNodeGuid = destinationNodeGuid;
            DestinationSceneGuid = destinationSceneGuid;
            DestinationSceneName = destinationSceneName;
            DestinationPortName = destinationPortName;
        }


        // intended to be called by a node when its scene connections change
        public bool ReplaceSceneAtNodeGuid(string changedNodeGuid, SceneData sceneData)
        {
            if (sceneData == null)
            {
                m_bIsValid = false;
            }
            else if (changedNodeGuid == SourceNodeGuid)
            {
                List<string> exitNames = sceneData.Exits.ConvertAll(exit => exit.Name);

                if (!exitNames.Contains(SourcePortName))
                {
                    m_bIsValid = false;
                }

                SourceSceneGuid = sceneData.Guid;
                SourceSceneName = sceneData.Name;
                m_bIsValid = true;
            }
            else if(changedNodeGuid == DestinationNodeGuid)
            {
                List<string> entranceNames = sceneData.Entrances.ConvertAll(entrance => entrance.Name);

                if (!entranceNames.Contains(DestinationPortName))
                {
                    m_bIsValid = false;
                }

                DestinationSceneGuid = sceneData.Guid;
                DestinationSceneName = sceneData.Name;
                m_bIsValid = true;
            }

            return m_bIsValid;
        }
    }
}
