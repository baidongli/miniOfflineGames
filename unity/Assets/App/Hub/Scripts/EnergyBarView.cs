using TMPro;
using UnityEngine;

namespace MiniGames.App.Hub
{
    public sealed class EnergyBarView : MonoBehaviour
    {
        [SerializeField] private TMP_Text _label;

        public void Render(int current, int max)
        {
            if (_label != null) _label.text = current.ToString();
        }
    }
}
