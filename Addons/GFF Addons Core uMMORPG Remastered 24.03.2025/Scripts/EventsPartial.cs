using UnityEngine.Events;
using uMMORPG;


#if UNITY_EDITOR
using UnityEditor.Events;
#endif

namespace uMMORPG
{
    public class EventsPartial
    {
        private static bool CheckListener(UnityEvent unityEvent, UnityAction unityAction)
        {
            for (int index = 0; index < unityEvent.GetPersistentEventCount(); index++)
            {
                string curEventName = unityEvent.GetPersistentMethodName(index);
                if (curEventName == unityAction.Method.Name) return false;
            }

            return true;
        }

        private static bool CheckListener(UnityEventPlayer unityEvent, UnityAction<Player> unityAction)
        {
            for (int index = 0; index < unityEvent.GetPersistentEventCount(); index++)
            {
                string curEventName = unityEvent.GetPersistentMethodName(index);
                if (curEventName == unityAction.Method.Name) return false;
            }

            return true;
        }

        private static bool CheckListener(UnityEventCharacterCreateMsgPlayer unityEvent, UnityAction<CharacterCreateMsg, Player> unityAction)
        {
            for (int index = 0; index < unityEvent.GetPersistentEventCount(); index++)
            {
                string curEventName = unityEvent.GetPersistentMethodName(index);
                if (curEventName == unityAction.Method.Name) return false;
            }

            return true;
        }

        public static bool AddListenerOnceUnityEventPlayer(UnityEventPlayer unityEvent, UnityAction<Player> unityAction)
        {
            for (int index = 0; index < unityEvent.GetPersistentEventCount(); index++)
            {
                string curEventName = unityEvent.GetPersistentMethodName(index);
                if (curEventName == unityAction.Method.Name) return false;
            }

            return true;
        }

#if UNITY_EDITOR
        public static void AddListenerOnceOnConnected(UnityEvent unityEvent, UnityAction unityAction, Database database)
        {
            if (CheckListener(unityEvent, unityAction))
            {
                //var targetinfo = UnityEvent.GetValidMethodInfo(database, "onConnected", new Type[0]);
                UnityEventTools.AddPersistentListener(database.onConnected, unityAction);
            }
        }

        public static void AddListenerOnceCharacterLoad(UnityEventPlayer unityEvent, UnityAction<Player> unityAction, Database database)
        {
            if (CheckListener(unityEvent, unityAction))
            {
                //var targetinfo = UnityEvent.GetValidMethodInfo(database, "onCharacterLoad", new Type[0]);
                UnityEventTools.AddPersistentListener(database.onCharacterLoad, unityAction);
            }
        }

        public static void AddListenerOnceCharacterSave(UnityEventPlayer unityEvent, UnityAction<Player> unityAction, Database database)
        {
            if (CheckListener(unityEvent, unityAction))
            {
                //var targetinfo = UnityEvent.GetValidMethodInfo(database, "onCharacterSave", new Type[0]);
                UnityEventTools.AddPersistentListener(database.onCharacterSave, unityAction);
            }
        }

        public static void AddListenerOnceOnStartServer(UnityEvent unityEvent, UnityAction unityAction, NetworkManagerMMO manager)
        {
            if (CheckListener(unityEvent, unityAction))
            {
                UnityEventTools.AddPersistentListener(manager.onStartServer, unityAction);
            }
        }
        public static void AddListenerOnceOnServerCharacterCreate(UnityEventCharacterCreateMsgPlayer unityEvent, UnityAction<CharacterCreateMsg, Player> unityAction, NetworkManagerMMO manager)
        {
            if (CheckListener(unityEvent, unityAction))
            {
                UnityEventTools.AddPersistentListener(manager.onServerCharacterCreate, unityAction);
            }
        }
        public static void AddListenerOnceOnLevelUp(UnityEvent unityEvent, UnityAction unityAction, PlayerExperience experience)
        {
            if (CheckListener(unityEvent, unityAction))
            {
                UnityEventTools.AddPersistentListener(experience.onLevelUp, unityAction);
            }
        }
#endif
    }
}