using Alchemy.Inspector;
using UnityEngine;

namespace Game
{
    /// One of the seven rooms and the upgrades it offers. Rooms do not have levels; progression is
    /// tracked through completed upgrade ids. A room's STARTING capacities are never authored here —
    /// StartingBalanceDefinition.InitialRoomCapabilities is the sole authority for session-start
    /// values (see SessionStateFactory), so a room definition carries no capacity fields of its own.
    [CreateAssetMenu(menuName = "Dark Tower/Room", fileName = "Room")]
    public sealed class RoomDefinition : ScriptableObject
    {
        [SerializeField] private RoomId roomId;
        [SerializeField] private string displayName;
        [SerializeField] private RoomUpgradeDefinition[] upgrades;

        public RoomId RoomId => roomId;
        public string DisplayName => displayName;
        public RoomUpgradeDefinition[] Upgrades => upgrades;

        [Button]
        private void PrintInfo()
        {
            Debug.Log($"[Room:{roomId}] '{displayName}' upgrades={upgrades?.Length ?? 0}", this);
        }
    }
}
