using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using LitMotion;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class NotificationItemView : MonoBehaviour
{
    [SerializeField] private TMP_Text _text;
    [SerializeField] private Image _imageStatusIcon;
    [SerializeField] private Image _imageTargetItem;
    
    [Tooltip("Required for LitMotion fading. Will auto-add if missing.")]
    [SerializeField] private CanvasGroup _canvasGroup;

    [Header("Icons")]
    [SerializeField] private Sprite _importantStatusIcon;
    [SerializeField] private Sprite _infoStatusIcon;

    private CancellationTokenSource _cts;

    public void Bind(NotificationItemViewModel viewModel)
    {
        if (_text != null)
        {
            _text.text = viewModel.Message;
            _text.color = viewModel.Color;
        }

        if (_imageStatusIcon != null)
        {
            _imageStatusIcon.sprite = viewModel.Level == MessageLevel.Important 
                ? _importantStatusIcon 
                : _infoStatusIcon;
        }

        // Only overwrite the Inspector's default sprite if a new one is provided
        if (_imageTargetItem != null && viewModel.TargetItem != null)
        {
            _imageTargetItem.sprite = viewModel.TargetItem;
        }

        PlayLifecycleAsync().Forget();
    }

    private async UniTaskVoid PlayLifecycleAsync()
    {
        _cts?.Cancel();
        _cts = new CancellationTokenSource();
        var ct = _cts.Token;

        if (_canvasGroup == null) 
            _canvasGroup = gameObject.AddComponent<CanvasGroup>();
            
        _canvasGroup.alpha = 0f;

        try
        {
            // 1. Fade In
            // await LMotion.Create(0f, 1f, 0.3f)
            //     .WithEase(Ease.OutQuad)
            //     .BindToCanvasGroupAlpha(_canvasGroup)
            //     .ToUniTask(ct);

            // 2. Wait
            await UniTask.Delay(TimeSpan.FromSeconds(4f), cancellationToken: ct);

            // 3. Fade Out
            // await LMotion.Create(1f, 0f, 0.3f)
            //     .WithEase(Ease.InQuad)
            //     .BindToCanvasGroupAlpha(_canvasGroup)
            //     .ToUniTask(ct);

            // 4. Visual death
            Destroy(gameObject);
        }
        catch (OperationCanceledException)
        {
            // Task canceled, do nothing
        }
    }

    private void OnDestroy()
    {
        _cts?.Cancel();
        _cts?.Dispose();
    }
}