using MixedReality.Toolkit.UX;
using UnityEngine;

namespace IMLD.MixedRealityAnalysis.Utils
{
    public static class ButtonHelper
    {
        public static void SetText(GameObject button, string text)
        {
            var child = button.transform.Find("IconAndText");
            if (child == null) return;

            var textComponent = child.GetComponent<TMPro.TextMeshPro>();
            if (textComponent != null)
            {
                textComponent.text = text;
            }
        }
    }
}