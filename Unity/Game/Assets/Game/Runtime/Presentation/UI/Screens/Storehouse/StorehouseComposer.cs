using UnityEngine;
using UnityEngine.UI;

namespace Game
{
    public sealed class StorehouseComposer : WindowComposer<StorehouseViewModel>
    {
        [SerializeField] private StorehouseView _view;
        [SerializeField] private Button _closeBackButton;

        private StorehouseViewModel _viewModel;

        protected override void Bind(StorehouseViewModel viewModel)
        {
            _viewModel = viewModel;
            _view.Bind(viewModel);
            _closeBackButton.onClick.AddListener(viewModel.Close);
        }

        protected override void OnDestroy()
        {
            if (_viewModel != null) _closeBackButton?.onClick.RemoveListener(_viewModel.Close);
            _view?.Dispose();
            base.OnDestroy();
        }
    }
}
