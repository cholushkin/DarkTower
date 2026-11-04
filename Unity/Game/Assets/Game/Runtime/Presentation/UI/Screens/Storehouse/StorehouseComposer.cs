using UnityEngine;

namespace Game
{
    public sealed class StorehouseComposer : WindowComposer<StorehouseViewModel>
    {
        [SerializeField] private StorehouseView _view;

        protected override void Bind(StorehouseViewModel viewModel)
        {
            _view.Bind(viewModel);
        }

        protected override void OnDestroy()
        {
            _view?.Dispose();
            base.OnDestroy();
        }
    }
}
