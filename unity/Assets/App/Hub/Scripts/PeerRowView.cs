using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MiniGames.App.Hub
{
    public sealed class PeerRowView : MonoBehaviour
    {
        [SerializeField] private TMP_Text _name;
        [SerializeField] private GameObject _readyDot;
        [SerializeField] private Button _button;

        public void Bind(string name, bool isReady, Action onTap)
        {
            _name.text = name;
            if (_readyDot != null) _readyDot.SetActive(isReady);
            _button.onClick.RemoveAllListeners();
            if (onTap != null) _button.onClick.AddListener(() => onTap());
            _button.interactable = onTap != null;
        }
    }
}
