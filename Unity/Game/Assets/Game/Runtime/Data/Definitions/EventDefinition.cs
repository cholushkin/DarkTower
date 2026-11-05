using Alchemy.Inspector;
using UnityEngine;

namespace Game
{
    /// Optional event/secret definition. Dungeon interrogation can reveal a secret that unlocks map
    /// locations or sets world flags. Effects here are data references resolved by future systems.
    [CreateAssetMenu(menuName = "Dark Tower/Event", fileName = "Event")]
    public sealed class EventDefinition : ScriptableObject, IIdentifiedDefinition
    {
        [SerializeField] private string eventId;
        [SerializeField] private string displayName;
        [SerializeField, TextArea] private string description;
        [SerializeField] private string setsWorldFlag;
        [SerializeField] private MissionDefinition[] unlocksMissions;

        public string Id => eventId;
        public string DisplayName => displayName;
        public string Description => description;
        public string SetsWorldFlag => setsWorldFlag;
        public MissionDefinition[] UnlocksMissions => unlocksMissions;

        [Button]
        private void PrintInfo()
        {
            Debug.Log($"[Event:{eventId}] '{displayName}' setsWorldFlag='{setsWorldFlag}' " +
                $"unlocksMissions={unlocksMissions?.Length ?? 0}", this);
        }
    }
}
