using UnityEngine;
using VContainer;

public class NotificationWidgetComposer : MonoBehaviour
{
    [SerializeField] private NotificationWidgetView _view;
    private NotificationWidgetViewModel _viewModel;

    [Inject]
    public void Construct(NotificationWidgetViewModel viewModel)
    {
        _viewModel = viewModel;
        
        if (_view != null)
        {
            _view.Bind(_viewModel);
        }
    }

    private void OnDestroy()
    {
        _view?.Dispose();
    }
}