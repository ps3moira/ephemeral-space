using Content.Shared.Access.Components;
using Content.Shared.Containers.ItemSlots;
using Robust.Shared.Containers;

namespace Content.Shared._ES.Wallet
{
    public abstract class SharedWalletSystem : EntitySystem
    {
        [Dependency] protected readonly ItemSlotsSystem ItemSlotsSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<WalletComponent, ComponentInit>(OnComponentInit);
            SubscribeLocalEvent<WalletComponent, ComponentRemove>(OnComponentRemove);

            SubscribeLocalEvent<WalletComponent, GetAdditionalAccessEvent>(OnGetAdditionalAccess);
        }

        protected virtual void OnComponentInit(EntityUid uid, WalletComponent wallet, ComponentInit args)
        {
            if (wallet.IdCard != null)
                wallet.IdSlot.StartingItem = wallet.IdCard;

            ItemSlotsSystem.AddItemSlot(uid, WalletComponent.WalletIdSlotId, wallet.IdSlot);
        }

        protected void OnComponentRemove(EntityUid uid, WalletComponent wallet, ComponentRemove args)
        {
            ItemSlotsSystem.RemoveItemSlot(uid, wallet.IdSlot);
        }

        private void OnGetAdditionalAccess(EntityUid uid, WalletComponent wallet, ref GetAdditionalAccessEvent args)
        {
            var containedId = ItemSlotsSystem.GetItemOrNull(uid, WalletComponent.WalletIdSlotId);
            if (containedId is { } id)
                args.Entities.Add(id);
        }
    }
}
