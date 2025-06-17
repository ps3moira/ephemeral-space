using Content.Shared.Containers.ItemSlots;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared._ES.Wallet
{
    [RegisterComponent, NetworkedComponent]
    public sealed partial class WalletComponent : Component
    {
        public const string WalletIdSlotId = "wallet-id";

        [DataField("idSlot")]
        public ItemSlot IdSlot = new();

        [DataField("id", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
        public string? IdCard;

        [ViewVariables(VVAccess.ReadWrite)] public string? OwnerName;
        [ViewVariables(VVAccess.ReadWrite)] public EntityUid? WalletOwner;
    }
}
